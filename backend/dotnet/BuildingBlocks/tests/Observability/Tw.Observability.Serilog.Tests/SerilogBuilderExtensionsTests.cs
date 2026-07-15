using AwesomeAssertions;
using global::Serilog;
using global::Serilog.Core;
using global::Serilog.Events;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Observability.Serilog.Tests;

/// <summary>
/// 验证Serilog敏感属性脱敏配置入口的注册与参数边界
/// </summary>
public sealed class SerilogBuilderExtensionsTests
{
    /// <summary>
    /// 注册脱敏扩展后敏感标量属性由脱敏器替换
    /// </summary>
    [Fact]
    public void EnrichWithSensitiveDataRedaction_RegistersRedactingEnricher()
    {
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .EnrichWithSensitiveDataRedaction(DefaultDataMasker.CreateDefault())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("访问凭据 {AccessToken}", "token-abcdef");

        var logEvent = sink.Events.Should().ContainSingle().Subject;
        var sensitiveProperty = logEvent.Properties["AccessToken"]
            .Should().BeOfType<ScalarValue>().Subject;
        sensitiveProperty.Value.Should().Be("***");
    }

    /// <summary>
    /// 对同一配置重复注册时仅首个脱敏器生效且单个事件只脱敏一次
    /// </summary>
    [Fact]
    public void EnrichWithSensitiveDataRedaction_RepeatedRegistration_UsesFirstMaskerOnce()
    {
        var firstMasker = new RecordingDataMasker("first-mask");
        var secondMasker = new RecordingDataMasker("second-mask");
        var sink = new CapturingSink();
        var configuration = new LoggerConfiguration();

        configuration.EnrichWithSensitiveDataRedaction(firstMasker);
        configuration.EnrichWithSensitiveDataRedaction(secondMasker);

        using var logger = configuration.WriteTo.Sink(sink).CreateLogger();
        logger.Information("访问凭据 {AccessToken}", "token-abcdef");

        firstMasker.CallCount.Should().Be(1);
        secondMasker.CallCount.Should().Be(0);
        sink.Events.Should().ContainSingle().Subject.Properties["AccessToken"]
            .Should().BeEquivalentTo(new ScalarValue("first-mask"));
    }

    /// <summary>
    /// 并发注册同一个脱敏器时仅安装一个事件丰富器
    /// </summary>
    [Fact]
    public void EnrichWithSensitiveDataRedaction_ConcurrentRegistration_InstallsSingleEnricher()
    {
        var dataMasker = new RecordingDataMasker("concurrent-mask");
        var sink = new CapturingSink();
        var configuration = new LoggerConfiguration();

        Parallel.For(
            fromInclusive: 0,
            toExclusive: 32,
            _ => configuration.EnrichWithSensitiveDataRedaction(dataMasker));

        using var logger = configuration.WriteTo.Sink(sink).CreateLogger();
        logger.Information("访问凭据 {AccessToken}", "token-abcdef");

        dataMasker.CallCount.Should().Be(1);
        sink.Events.Should().ContainSingle().Subject.Properties["AccessToken"]
            .Should().BeEquivalentTo(new ScalarValue("concurrent-mask"));
    }

    /// <summary>
    /// 缺少Serilog配置对象时拒绝注册脱敏器
    /// </summary>
    [Fact]
    public void EnrichWithSensitiveDataRedaction_NullConfiguration_ThrowsArgumentNullException()
    {
        var act = () => SerilogBuilderExtensions.EnrichWithSensitiveDataRedaction(
            null!,
            DefaultDataMasker.CreateDefault());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// 缺少数据脱敏器时拒绝建立可能泄露敏感属性的日志管道
    /// </summary>
    [Fact]
    public void EnrichWithSensitiveDataRedaction_NullDataMasker_ThrowsArgumentNullException()
    {
        var act = () => new LoggerConfiguration().EnrichWithSensitiveDataRedaction(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataMasker");
    }

    /// <summary>
    /// 记录脱敏调用次数并返回固定替换值
    /// </summary>
    private sealed class RecordingDataMasker(string replacement) : IDataMasker
    {
        /// <summary>
        /// 已执行的脱敏调用次数
        /// </summary>
        private int _callCount;

        /// <summary>
        /// 获取已执行的脱敏调用次数
        /// </summary>
        public int CallCount => Volatile.Read(ref _callCount);

        /// <summary>
        /// 记录调用并返回固定替换值
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="kind">敏感数据类别</param>
        /// <returns>固定替换值</returns>
        public string Mask(string? value, SensitiveDataKind kind)
        {
            Interlocked.Increment(ref _callCount);
            return replacement;
        }
    }

    /// <summary>
    /// 保存测试期间写入的结构化日志事件
    /// </summary>
    private sealed class CapturingSink : ILogEventSink
    {
        /// <summary>
        /// 按写入顺序保存的日志事件
        /// </summary>
        private readonly List<LogEvent> _events = [];

        /// <summary>
        /// 获取测试断言可读取的日志事件快照
        /// </summary>
        public IReadOnlyList<LogEvent> Events => _events;

        /// <summary>
        /// 接收Serilog管道写出的单个日志事件
        /// </summary>
        /// <param name="logEvent">包含结构化属性的日志事件</param>
        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}

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
    /// 注册脱敏扩展后，写入管道的敏感标量属性由脱敏器替换
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
    /// 保存测试期间写入的结构化日志事件
    /// </summary>
    private sealed class CapturingSink : ILogEventSink
    {
        /// <summary>
        /// 按写入顺序保存的日志事件
        /// </summary>
        private readonly List<LogEvent> _events = [];

        /// <summary>
        /// 测试断言可读取的日志事件快照
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

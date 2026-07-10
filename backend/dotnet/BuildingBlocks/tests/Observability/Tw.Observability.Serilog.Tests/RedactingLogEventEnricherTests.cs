using AwesomeAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Observability.Serilog.Tests;

/// <summary>
/// 覆盖RedactingLog事件Enricher的核心行为和边界条件
/// </summary>
public sealed class RedactingLogEventEnricherTests
{
    /// <summary>
    /// 验证EnrichRedactsSensitiveScalarProperties
    /// </summary>
    [Fact]
    public void Enrich_RedactsSensitiveScalarProperties()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse("Password {Password}"),
            properties:
            [
                new LogEventProperty("Password", new ScalarValue("secret"))
            ]);

        new RedactingLogEventEnricher(DefaultDataMasker.CreateDefault()).Enrich(logEvent, new TestLogEventPropertyFactory());

        logEvent.Properties["Password"].ToString().Should().NotContain("secret");
    }

    /// <summary>
    /// 覆盖TestLog事件PropertyFactory的核心行为和边界条件
    /// </summary>
    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        /// <summary>
        /// 创建Property测试对象
        /// </summary>
        /// <param name="name">待匹配成员或资源的名称</param>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <param name="destructureObjects">用于提供destructureObjects</param>
        /// <returns>条件满足时返回 <see langword="true"/></returns>
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, value is LogEventPropertyValue propertyValue ? propertyValue : new ScalarValue(value));
        }
    }
}

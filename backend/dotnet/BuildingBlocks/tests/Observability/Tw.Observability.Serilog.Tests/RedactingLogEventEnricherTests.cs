using AwesomeAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Observability.Serilog.Tests;

/// <summary>验证 RedactingLogEventEnricherTests 相关行为</summary>
public sealed class RedactingLogEventEnricherTests
{
    /// <summary>验证 Enrich_RedactsSensitiveScalarProperties 场景</summary>
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

    /// <summary>验证 TestLogEventPropertyFactory 相关行为</summary>
    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        /// <summary>验证 CreateProperty 场景</summary>
        /// <param name="name">name 参数</param>
        /// <param name="value">value 参数</param>
        /// <param name="destructureObjects">destructureObjects 参数</param>
        /// <returns>CreateProperty 的执行结果</returns>
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, value is LogEventPropertyValue propertyValue ? propertyValue : new ScalarValue(value));
        }
    }
}

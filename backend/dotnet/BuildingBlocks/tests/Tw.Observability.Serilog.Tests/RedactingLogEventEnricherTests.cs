using AwesomeAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Observability.Serilog.Tests;

public sealed class RedactingLogEventEnricherTests
{
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

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, value is LogEventPropertyValue propertyValue ? propertyValue : new ScalarValue(value));
        }
    }
}

using AwesomeAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Observability.Serilog.Tests;

/// <summary>
/// 验证结构化日志属性脱敏的名称边界
/// </summary>
public sealed class RedactingLogEventEnricherTests
{
    /// <summary>
    /// 常见敏感属性名无论大小写或分隔形式都必须脱敏
    /// </summary>
    /// <param name="propertyName">待验证的属性名</param>
    [Theory]
    [InlineData("Password")]
    [InlineData("PasswordHash")]
    [InlineData("user_password")]
    [InlineData("ClientSecret")]
    [InlineData("ClientSecretValue")]
    [InlineData("client-secret")]
    [InlineData("AccessToken")]
    [InlineData("TokenPayload")]
    [InlineData("TokenPasswordPolicy")]
    [InlineData("access.token")]
    [InlineData("ConnectionString")]
    [InlineData("ConnectionStringValue")]
    [InlineData("CONNECTIONSTRING")]
    [InlineData("connection_string")]
    [InlineData("ApiKey")]
    [InlineData("ApiKeyValue")]
    [InlineData("apikey")]
    [InlineData("api_key")]
    [InlineData("API-KEY")]
    [InlineData("Authorization")]
    [InlineData("AuthorizationValue")]
    [InlineData("request_authorization")]
    [InlineData("AuthorizationHeader")]
    [InlineData("ClientCredential")]
    [InlineData("CredentialValue")]
    [InlineData("client-credential")]
    [InlineData("PrivateKey")]
    [InlineData("PrivateKeyPem")]
    [InlineData("PRIVATEKEY")]
    [InlineData("private_key")]
    [InlineData("SessionCookie")]
    [InlineData("CookieValue")]
    [InlineData("set-cookie")]
    [InlineData("CookieHeader")]
    public void Enrich_SensitivePropertyName_RedactsScalarValue(string propertyName)
    {
        var logEvent = CreateLogEvent(propertyName, "sensitive-value");

        new RedactingLogEventEnricher(DefaultDataMasker.CreateDefault()).Enrich(
            logEvent,
            new TestLogEventPropertyFactory());

        logEvent.Properties[propertyName].Should().BeEquivalentTo(new ScalarValue("***"));
    }

    /// <summary>
    /// 仅包含敏感词片段的正常业务属性不得被误伤
    /// </summary>
    /// <param name="propertyName">待验证的属性名</param>
    [Theory]
    [InlineData("PasswordPolicy")]
    [InlineData("SecretariatName")]
    [InlineData("TokenizationCount")]
    [InlineData("ConnectionStringBuilder")]
    [InlineData("ApiKeyboardLayout")]
    [InlineData("AuthorizationPolicy")]
    [InlineData("CredentialProvider")]
    [InlineData("PrivateKeyAlgorithm")]
    [InlineData("CookiePolicy")]
    public void Enrich_BenignPropertyName_PreservesScalarValue(string propertyName)
    {
        var logEvent = CreateLogEvent(propertyName, "ordinary-value");

        new RedactingLogEventEnricher(DefaultDataMasker.CreateDefault()).Enrich(
            logEvent,
            new TestLogEventPropertyFactory());

        logEvent.Properties[propertyName].Should().BeEquivalentTo(new ScalarValue("ordinary-value"));
    }

    /// <summary>
    /// 创建包含单个标量属性的日志事件
    /// </summary>
    /// <param name="propertyName">结构化属性名</param>
    /// <param name="propertyValue">结构化属性值</param>
    /// <returns>可供脱敏器处理的日志事件</returns>
    private static LogEvent CreateLogEvent(string propertyName, string propertyValue)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse($"Value {{{propertyName}}}"),
            properties:
            [
                new LogEventProperty(propertyName, new ScalarValue(propertyValue))
            ]);
    }

    /// <summary>
    /// 为测试创建结构化日志属性
    /// </summary>
    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        /// <summary>
        /// 创建结构化日志属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <param name="destructureObjects">是否解构对象</param>
        /// <returns>新建的日志属性</returns>
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(
                name,
                value is LogEventPropertyValue propertyValue ? propertyValue : new ScalarValue(value));
        }
    }
}

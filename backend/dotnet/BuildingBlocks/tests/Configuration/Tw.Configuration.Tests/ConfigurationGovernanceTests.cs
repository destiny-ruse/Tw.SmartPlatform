using AwesomeAssertions;
using Tw.Configuration;
using Xunit;

namespace Tw.Configuration.Tests;

/// <summary>验证 ConfigurationGovernanceTests 相关行为</summary>
public sealed class ConfigurationGovernanceTests
{
    /// <summary>验证 UserSecrets_AreAllowedOnlyInLocalOrDevelopment 场景</summary>
    [Fact]
    public void UserSecrets_AreAllowedOnlyInLocalOrDevelopment()
    {
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Development").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Local").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Production").Should().BeFalse();
    }
}

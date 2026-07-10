using AwesomeAssertions;
using Tw.Configuration;
using Xunit;

namespace Tw.Configuration.Tests;

/// <summary>
/// 覆盖ConfigurationGovernance的核心行为和边界条件
/// </summary>
public sealed class ConfigurationGovernanceTests
{
    /// <summary>
    /// 验证用户SecretsAreAllowedOnlyInLocalOrDevelopment
    /// </summary>
    [Fact]
    public void UserSecrets_AreAllowedOnlyInLocalOrDevelopment()
    {
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Development").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Local").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Production").Should().BeFalse();
    }
}

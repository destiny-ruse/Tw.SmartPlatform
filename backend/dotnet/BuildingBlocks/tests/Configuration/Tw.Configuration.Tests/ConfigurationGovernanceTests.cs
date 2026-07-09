using AwesomeAssertions;
using Tw.Configuration;
using Xunit;

namespace Tw.Configuration.Tests;

public sealed class ConfigurationGovernanceTests
{
    [Fact]
    public void UserSecrets_AreAllowedOnlyInLocalOrDevelopment()
    {
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Development").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Local").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Production").Should().BeFalse();
    }
}

namespace Tw.Configuration;

public interface IConfigurationGovernance
{
    bool IsSourceAllowed(string sourceName, string environmentName);
}

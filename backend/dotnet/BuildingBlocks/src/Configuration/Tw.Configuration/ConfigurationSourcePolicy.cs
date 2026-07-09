namespace Tw.Configuration;

public static class ConfigurationSourcePolicy
{
    public static bool IsUserSecretsAllowed(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Local", StringComparison.OrdinalIgnoreCase);
    }
}

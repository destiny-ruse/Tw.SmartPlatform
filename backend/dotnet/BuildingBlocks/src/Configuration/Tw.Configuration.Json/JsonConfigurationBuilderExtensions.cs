namespace Tw.Configuration.Json;

public static class JsonConfigurationBuilderExtensions
{
    public static JsonConfigurationManifest CreateManifest(params string[] files)
    {
        return new JsonConfigurationManifest(files);
    }
}

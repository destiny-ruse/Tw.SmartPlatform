namespace Tw.Configuration.Json;

/// <summary>
/// 封装JSONConfigurationManifest相关的数据和行为
/// </summary>
public sealed record JsonConfigurationManifest(IReadOnlyList<string> Files);

namespace Tw.Configuration.Json;

/// <summary>
/// 封装Configuration路径异常相关的数据和行为
/// </summary>
public sealed class ConfigurationPathException(string message) : Exception(message);

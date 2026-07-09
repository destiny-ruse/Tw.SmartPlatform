namespace Tw.Json.Abstractions;

/// <summary>
/// JSON 序列化器抽象
/// </summary>
public interface IJsonSerializer
{
    /// <summary>
    /// 将对象序列化为 JSON 字符串
    /// </summary>
    /// <param name="value">要序列化的值</param>
    /// <typeparam name="T">值类型</typeparam>
    /// <returns>JSON 字符串</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// 将 JSON 字符串反序列化为指定类型
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的值</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="json"/> 为 <see langword="null"/> 时抛出</exception>
    T? Deserialize<T>(string json);
}

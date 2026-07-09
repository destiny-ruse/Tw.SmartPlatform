namespace Tw.Json.Abstractions;

/// <summary>
/// JSON 序列化行为选项
/// </summary>
/// <param name="WriteLongAsString">是否将 64 位整数写为字符串，避免 JavaScript 数字精度丢失</param>
public sealed record JsonSerializerOptions(bool WriteLongAsString = true);

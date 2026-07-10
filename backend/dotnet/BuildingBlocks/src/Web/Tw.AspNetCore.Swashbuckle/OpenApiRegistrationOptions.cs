namespace Tw.AspNetCore.Swashbuckle;

/// <summary>
/// 配置OpenApiRegistration的运行行为
/// </summary>
public sealed record OpenApiRegistrationOptions(
    string DocumentName,
    string Title,
    string Version,
    IReadOnlyList<string> XmlCommentFiles);

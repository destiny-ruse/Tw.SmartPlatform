namespace Tw.AspNetCore.Swashbuckle;

/// <summary>表示 OpenApiRegistrationOptions 声明</summary>
public sealed record OpenApiRegistrationOptions(
    string DocumentName,
    string Title,
    string Version,
    IReadOnlyList<string> XmlCommentFiles);

namespace Tw.AspNetCore.Swashbuckle;

public sealed record OpenApiRegistrationOptions(
    string DocumentName,
    string Title,
    string Version,
    IReadOnlyList<string> XmlCommentFiles);

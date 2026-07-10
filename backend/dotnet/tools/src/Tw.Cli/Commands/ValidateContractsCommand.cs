namespace Tw.Cli.Commands;

/// <summary>
/// 提供 CLI 中校验Contracts命令的入口描述
/// </summary>
public static class ValidateContractsCommand
{
    /// <summary>
    /// CLI 命令在帮助信息中显示的说明文本
    /// </summary>
    public static string Description => "Validates HTTP, OpenAPI, gRPC, CAP event, and error-code contracts.";
}

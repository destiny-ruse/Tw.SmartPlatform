namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限定义
/// </summary>
/// <param name="Name">权限名称</param>
/// <param name="DisplayName">权限显示名称</param>
public sealed record PermissionDefinition(string Name, string DisplayName);

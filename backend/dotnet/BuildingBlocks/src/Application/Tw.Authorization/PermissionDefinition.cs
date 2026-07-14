namespace Tw.Authorization;

/// <summary>
/// 提供权限的稳定名称与面向使用者的显示元数据
/// </summary>
/// <param name="Name">用于授权判断与持久化的稳定权限名称</param>
/// <param name="DisplayName">面向管理界面或文档展示的权限名称</param>
public sealed record PermissionDefinition(string Name, string DisplayName);

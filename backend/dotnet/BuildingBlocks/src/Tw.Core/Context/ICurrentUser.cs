using System.Security.Claims;

namespace Tw.Core.Context;

/// <summary>
/// 暴露当前执行上下文中基于声明的身份信息
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 当前执行上下文是否具有已认证身份
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 当前身份包含稳定用户标识时的标识
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// 可用时的用户名声明值
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// 可用时的名字声明值
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 可用时的姓氏声明值
    /// </summary>
    string? SurName { get; }

    /// <summary>
    /// 可用时的电子邮箱声明值
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// 当前身份是否声明电子邮箱已验证
    /// </summary>
    bool EmailVerified { get; }

    /// <summary>
    /// 可用时的手机号码声明值
    /// </summary>
    string? PhoneNumber { get; }

    /// <summary>
    /// 当前身份是否声明手机号码已验证
    /// </summary>
    bool PhoneNumberVerified { get; }

    /// <summary>
    /// 角色声明值，作为身份上下文便利信息；这不是授权边界
    /// </summary>
    string[] Roles { get; }

    /// <summary>
    /// 查找第一个匹配给定声明类型的声明
    /// </summary>
    /// <param name="claimType">要搜索的声明类型</param>
    /// <returns>第一个匹配声明；没有匹配声明时返回 <see langword="null"/></returns>
    Claim? FindClaim(string claimType);

    /// <summary>
    /// 查找所有匹配给定声明类型的声明
    /// </summary>
    /// <param name="claimType">要搜索的声明类型</param>
    /// <returns>所有匹配声明；没有匹配声明时返回空数组</returns>
    Claim[] FindClaims(string claimType);

    /// <summary>
    /// 当前身份上下文中可用的所有声明
    /// </summary>
    /// <returns>当前身份上下文可见的所有声明</returns>
    Claim[] GetAllClaims();

    /// <summary>
    /// 仅作为便利方法检查当前身份上下文是否包含给定角色名称
    /// </summary>
    /// <param name="roleName">要在当前身份上下文中检查的角色名称</param>
    /// <returns>角色存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <remarks>此方法不是授权边界，不能替代策略或权限检查</remarks>
    bool IsInRole(string roleName);
}

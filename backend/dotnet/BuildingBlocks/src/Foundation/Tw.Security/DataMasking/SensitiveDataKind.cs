namespace Tw.Security.DataMasking;

/// <summary>
/// 敏感数据类别
/// </summary>
public enum SensitiveDataKind
{
    /// <summary>
    /// 未分类敏感数据
    /// </summary>
    Unknown,

    /// <summary>
    /// 手机号
    /// </summary>
    PhoneNumber,

    /// <summary>
    /// 身份证件号
    /// </summary>
    IdentityNumber,

    /// <summary>
    /// 电子邮箱
    /// </summary>
    Email,

    /// <summary>
    /// 密码
    /// </summary>
    Password,

    /// <summary>
    /// 访问令牌或会话令牌
    /// </summary>
    Token,

    /// <summary>
    /// 连接字符串
    /// </summary>
    ConnectionString,

    /// <summary>
    /// 证书私钥
    /// </summary>
    CertificatePrivateKey,

    /// <summary>
    /// 原始敏感载荷
    /// </summary>
    RawSensitivePayload,
}

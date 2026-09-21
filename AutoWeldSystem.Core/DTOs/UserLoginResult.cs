using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 登录失败原因。
/// 用枚举代替字符串判断，调用方会更容易理解和维护。
/// </summary>
public enum UserLoginFailureReason
{
    None = 0,
    InvalidCredentials = 1,
    UserDisabled = 2,
    RoleDisabled = 3
}

/// <summary>
/// 登录结果对象。
/// 让服务层返回结构化结果，而不是只返回 null。
/// </summary>
public sealed record UserLoginResult
{
    public bool IsSuccess { get; init; }

    public SysUser? User { get; init; }

    public UserLoginFailureReason FailureReason { get; init; }

    public static UserLoginResult Success(SysUser user)
    {
        return new UserLoginResult
        {
            IsSuccess = true,
            User = user,
            FailureReason = UserLoginFailureReason.None
        };
    }

    public static UserLoginResult Fail(UserLoginFailureReason failureReason)
    {
        return new UserLoginResult
        {
            IsSuccess = false,
            User = null,
            FailureReason = failureReason
        };
    }
}

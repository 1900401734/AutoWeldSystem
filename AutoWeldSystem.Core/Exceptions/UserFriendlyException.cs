namespace AutoWeldSystem.Core.Exceptions;

/// <summary>
/// 面向界面的业务异常。
/// 服务层只提供“消息键 + 参数”，具体显示什么语言由 UI 决定。
/// </summary>
public class UserFriendlyException : InvalidOperationException
{
    public UserFriendlyException(string messageKey, params object[] args)
        : base(messageKey)
    {
        MessageKey = messageKey;
        Args = args;
    }

    public string MessageKey { get; }

    public IReadOnlyList<object> Args { get; }
}

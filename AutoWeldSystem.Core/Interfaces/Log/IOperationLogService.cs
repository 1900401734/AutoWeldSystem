using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.Log;

public interface IOperationLogService
{
    void Write(string action, string detail, string level = "Info");

    IReadOnlyList<SysOperationLog> GetRecent(int take = 200);
}

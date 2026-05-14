using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

public interface IOperationLogService
{
    void Write(string action, string detail, string level = "Info");

    IReadOnlyList<SysOperationLog> GetRecent(int take = 200);
}

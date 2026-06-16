using AutoWeldSystem.Core.DTOs.Upload;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Builds task/work-order level upload overview data for the upload state page.
/// </summary>
public interface IUploadStatusSummaryService
{
    IReadOnlyList<UploadPendingSummaryRow> GetSummary(int maxCount = 200);
}

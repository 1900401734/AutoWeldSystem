using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.Mes.Request;

/// <summary>
/// Offline work-order information entered locally by the operator.
/// The selected local program is the source of product number, model, and recipe code.
/// </summary>
public sealed class OfflineExperimentStartReq
{
    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    public string WorkOrderId { get; set; } = string.Empty;

    public string Batch { get; set; } = string.Empty;

    public string Spec { get; set; } = string.Empty;

    public string ProcessNo { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public int PlannedQty { get; set; } = 1;

    public int ProgramLocalId { get; set; }

    public string ProgramId { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public string ProgramType { get; set; } = string.Empty;

    public string ProgramContent { get; set; } = "{}";

    public string ProductNum { get; set; } = string.Empty;

    public string ProductModel { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string DrawingNo { get; set; } = string.Empty;

    public string RecipeCode { get; set; } = string.Empty;
}

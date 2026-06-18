using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.UI.ViewModels;

public sealed class WeldParameterRow
{
    public int StationNo { get; init; }

    public string Station { get; init; } = string.Empty;

    public string ProductNo { get; set; } = string.Empty;

    public string ProductNum { get; init; } = string.Empty;

    public string ProductModel { get; init; } = string.Empty;

    public int TouchIndex { get; init; }

    public string TouchNo { get; init; } = string.Empty;

    public string TouchResult { get; set; } = "--";

    public string PointName { get; init; } = "焊点";

    public string PointNoHeader { get; init; } = "焊点序号";

    public string PointResultHeader { get; init; } = "焊点结果";

    public string PointCountHeader { get; init; } = "焊点数";

    public string ParameterName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public bool EnableActual { get; init; } = true;

    public bool EnableUpper { get; init; } = true;

    public bool EnableLower { get; init; } = true;

    public bool EnableResult { get; init; } = true;

    public string ActualHeader { get; init; } = string.Empty;

    public string UpperHeader { get; init; } = string.Empty;

    public string LowerHeader { get; init; } = string.Empty;

    public string ResultHeader { get; init; } = string.Empty;

    public string ActualAddress { get; init; } = string.Empty;

    public string UpperAddress { get; init; } = string.Empty;

    public string LowerAddress { get; init; } = string.Empty;

    public string ResultAddress { get; init; } = string.Empty;

    public string ActualDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

    public int ActualRule { get; init; }

    public string UpperDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

    public int UpperRule { get; init; }

    public string LowerDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

    public int LowerRule { get; init; }

    public string ResultDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

    public int ResultRule { get; init; }

    public string Value { get; set; } = "--";

    public string UpperValue { get; set; } = "--";

    public string LowerValue { get; set; } = "--";

    public string Result { get; set; } = "--";

    public string RecordTime { get; set; } = string.Empty;

    public int Sort { get; init; }

    public string ItemKey { get; init; } = string.Empty;

    public int TestItemId { get; init; }

    public int ProcessConfigId { get; init; }

    public string UniqueKey => $"{StationNo}|{ProductNum}|{ProductModel}|{TouchIndex}|{ItemKey}";
}

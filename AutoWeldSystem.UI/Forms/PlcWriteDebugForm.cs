using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.UI.Base;
using System.Globalization;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// PLC 地址写入调试窗口。
/// 只面向现场调试使用，业务流程中的正式写入仍应走各自服务。
/// </summary>
public partial class PlcWriteDebugForm : BaseWindow
{
    private const string LogAction = "PLC.DebugWrite";

    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IOperationLogService _operationLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;

    public PlcWriteDebugForm(
        IPlcCommunicationService plcCommunicationService,
        IOperationLogService operationLogService,
        IProgramExceptionLogService exceptionLogService)
    {
        InitializeComponent();

        _plcCommunicationService = plcCommunicationService;
        _operationLogService = operationLogService;
        _exceptionLogService = exceptionLogService;

        InitializeDataTypes();
        btnWrite.Click += Write_Click;
        btnClose.Click += (_, _) => CloseAsCancel();
        Shown += (_, _) =>
        {
            inputAddress.Focus();
            inputAddress.SelectAll();
            AcceptButton = btnWrite;
            CancelButton = btnClose;
        };
    }

    private void CloseAsCancel()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            CloseAsCancel();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// 写入类型固定为当前调试需求指定的四种类型。
    /// </summary>
    private void InitializeDataTypes()
    {
        selectDataType.Items.Clear();
        selectDataType.Items.Add(new PlcWriteTypeOption("short / Int16",PlcWriteDataType.Int16));
        selectDataType.Items.Add(new PlcWriteTypeOption("int / Int32",PlcWriteDataType.Int32));
        selectDataType.Items.Add(new PlcWriteTypeOption("float",PlcWriteDataType.Float));
        selectDataType.Items.Add(new PlcWriteTypeOption("string",PlcWriteDataType.String));
        selectDataType.SelectedIndex = 0;
    }

    private async void Write_Click(object? sender, EventArgs e)
    {
        if (!TryBuildRequest(out var request))
        {
            return;
        }

        btnWrite.Enabled = false;
        SetResult("正在写入 PLC...", SystemColors.GrayText);
        try
        {
            var result = await WriteAsync(request);
            if (result.IsSuccess)
            {
                var message = $"写入成功：{request.DataType} {request.Address} = {request.ValueText}";
                SetResult(message, Color.ForestGreen);
                WriteOperationLog(message);
                return;
            }

            var detail = $"写入失败：{request.DataType} {request.Address} = {request.ValueText}；原因：{result.Message}";
            SetResult(detail, Color.Firebrick);
            WriteOperationLog(detail, "Warn");
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "PLC.DebugWrite", $"Address={request.Address}, Type={request.DataType}");
            SetResult($"写入异常：{ex.Message}", Color.Firebrick);
        }
        finally
        {
            btnWrite.Enabled = true;
        }
    }

    /// <summary>
    /// 从界面输入构造写入请求，提前拦截地址为空和数值格式错误。
    /// </summary>
    private bool TryBuildRequest(out PlcWriteRequest request)
    {
        request = default;

        var address = inputAddress.Text.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            SetResult("请输入 PLC 地址。", Color.Firebrick);
            inputAddress.Focus();
            return false;
        }

        

        if(selectDataType.SelectedValue is not PlcWriteTypeOption option)
        {
            SetResult("请选择写入类型。", Color.Firebrick);
            selectDataType.Focus();
            return false;
        }

        var valueText = option.DataType == PlcWriteDataType.String
            ? inputValue.Text
            : inputValue.Text.Trim();
        if (string.IsNullOrWhiteSpace(valueText) && option.DataType != PlcWriteDataType.String)
        {
            SetResult("请输入写入值。", Color.Firebrick);
            inputValue.Focus();
            return false;
        }

        if (!IsValidValue(option.DataType, valueText, out var error))
        {
            SetResult(error, Color.Firebrick);
            inputValue.Focus();
            inputValue.SelectAll();
            return false;
        }

        request = new PlcWriteRequest(address, option.DataType, valueText);
        return true;
    }

    private static bool IsValidValue(PlcWriteDataType dataType, string valueText, out string error)
    {
        error = string.Empty;
        var isValid = dataType switch
        {
            PlcWriteDataType.Int16 => short.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            PlcWriteDataType.Int32 => int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            PlcWriteDataType.Float => TryParseFloat(valueText, out _),
            PlcWriteDataType.String => true,
            _ => false
        };

        if (isValid)
        {
            return true;
        }

        error = dataType switch
        {
            PlcWriteDataType.Int16 => "short 类型只能写入 -32768 到 32767 的整数。",
            PlcWriteDataType.Int32 => "int 类型只能写入整数。",
            PlcWriteDataType.Float => "float 类型只能写入数字，例如 12.34。",
            _ => "写入值格式不正确。"
        };
        return false;
    }

    private async Task<PlcServiceResult> WriteAsync(PlcWriteRequest request)
    {
        return request.DataType switch
        {
            PlcWriteDataType.Int16 => await _plcCommunicationService.WriteInt16Async(
                request.Address,
                short.Parse(request.ValueText, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            PlcWriteDataType.Int32 => await _plcCommunicationService.WriteInt32Async(
                request.Address,
                int.Parse(request.ValueText, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            PlcWriteDataType.Float => await _plcCommunicationService.WriteFloatAsync(
                request.Address,
                ParseFloat(request.ValueText)),
            PlcWriteDataType.String => await _plcCommunicationService.WriteStringAsync(
                request.Address,
                request.ValueText),
            _ => PlcServiceResult.Fail("不支持的 PLC 写入类型。")
        };
    }

    private static bool TryParseFloat(string valueText, out float value)
    {
        if (float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return float.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    private static float ParseFloat(string valueText)
    {
        TryParseFloat(valueText, out var value);
        return value;
    }

    private void SetResult(string message, Color color)
    {
        lblResult.ForeColor = color;
        lblResult.Text = message;
    }

    private void WriteOperationLog(string detail, string level = "Info")
    {
        try
        {
            _operationLogService.Write(LogAction, detail, level);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "PLC.DebugWrite.OperationLog", detail);
        }
    }

    private enum PlcWriteDataType
    {
        Int16,
        Int32,
        Float,
        String
    }

    private sealed record PlcWriteTypeOption(string Text, PlcWriteDataType DataType)
    {
        public override string ToString() => Text;
    }

    private readonly record struct PlcWriteRequest(
        string Address,
        PlcWriteDataType DataType,
        string ValueText);
}

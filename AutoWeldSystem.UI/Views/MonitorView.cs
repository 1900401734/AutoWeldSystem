using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

public partial class MonitorView : BaseView
{
    private const float MinTitleFontSize = 12F;
    private const float MaxTitleFontSize = 68F;
    private const int TitleTextPadding = 8;
    private const int HeaderLogoWidth = 168;
    private const int HeaderActionMinWidth = 156;
    private const int HeaderStatusCellMinWidth = 140;
    private const int HeaderStatusCellPadding = 36;
    private const int HeaderButtonPadding = 62;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitorService _mesConnectionMonitorService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private bool _syncingLanguageSelection;
    private string? _runtimeStatusKey = TextKeys.Monitor.RuntimeStatus.Idle;
    private object[] _runtimeStatusArgs = Array.Empty<object>();
    private string? _runtimeErrorKey;
    private object[] _runtimeErrorArgs = Array.Empty<object>();
    private string? _runtimeErrorText;
    private bool _adjustingTitleFont;
    private Font? _titleFont;
    private Font? _headerStatusFont;
    private Font? _headerButtonFont;
    private Font? _runtimeMessageFont;
    private Font? _runtimeGroupFont;

    public MonitorView(
        ILocalizationService localizer,
        IPlcCommunicationService plcCommunicationService,
        IMesConnectionMonitorService mesConnectionMonitorService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IWeldTaskService weldTaskService,
        IProgramExceptionLogService exceptionLogService)
    {
        InitializeComponent();

        _localizer = localizer;
        _plcCommunicationService = plcCommunicationService;
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _weldTaskService = weldTaskService;
        _exceptionLogService = exceptionLogService;

        LoadTitleLogo();
        ConfigureHeaderLayout();
        ConfigureRuntimeMessagePanels();
        ApplyLocalizedTexts();
        ConfigureTables();
        ConfigureProductionTableColumns();
        WireEvents();
        BindSessionInfo();
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(_plcProductionMonitorService.Current);
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 从输出目录加载标题 Logo；资源缺失时隐藏图片控件，避免界面出现空白占位图标。
    /// </summary>
    private void LoadTitleLogo()
    {
        if (!File.Exists(AppAssets.LogoPath))
        {
            picLogo.Visible = false;
            return;
        }

        picLogo.Visible = true;
        picLogo.ImageLocation = AppAssets.LogoPath;
    }

    /// <summary>
    /// Keeps the header readable when English labels are longer than Chinese labels.
    /// </summary>
    private void ConfigureHeaderLayout()
    {
        _headerStatusFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);
        _headerButtonFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);

        tlpLeftTop.AutoSize = false;
        tlpReportButton.AutoSize = false;
        tlpCommunicationStatus.MinimumSize = new Size(HeaderStatusCellMinWidth * 3, 0);
        tlpReportButton.MinimumSize = new Size(HeaderActionMinWidth, 0);

        ConfigureStatusTag(tagMes);
        ConfigureStatusTag(tagPLC);
        ConfigureStatusTag(tagDeviceStatus);
        ConfigureReportButton(btnExpStart);
        ConfigureReportButton(btnExpEnd);
        AdjustHeaderFixedColumns();
    }

    /// <summary>
    /// Status tags use compact bold text and a small margin so rounded corners do not cut into text.
    /// </summary>
    private void ConfigureStatusTag(AntdUI.Tag tag)
    {
        tag.Font = _headerStatusFont;
        tag.Margin = new Padding(4, 0, 4, 0);
        tag.AutoEllipsis = false;
        tag.TextMultiLine = true;
    }

    /// <summary>
    /// Report buttons keep a readable minimum width because icons consume part of the text area.
    /// </summary>
    private void ConfigureReportButton(AntdUI.Button button)
    {
        button.Font = _headerButtonFont;
        button.MinimumSize = new Size(HeaderActionMinWidth, 0);
        button.AutoEllipsis = false;
        button.TextCenterHasIcon = true;
        button.TextMultiLine = false;
    }

    /// <summary>
    /// The title column can shrink, while status cards and action buttons reserve measured widths.
    /// </summary>
    private void AdjustHeaderFixedColumns()
    {
        if (tlpLeftTop.ColumnStyles.Count < 4)
        {
            return;
        }

        var logoWidth = picLogo.Visible ? HeaderLogoWidth : 0;
        var statusWidth = CalculateHeaderStatusWidth();
        var actionWidth = CalculateHeaderActionWidth();

        tlpLeftTop.ColumnStyles[0].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[0].Width = logoWidth;
        tlpLeftTop.ColumnStyles[1].SizeType = SizeType.Percent;
        tlpLeftTop.ColumnStyles[1].Width = 100F;
        tlpLeftTop.ColumnStyles[2].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[2].Width = statusWidth;
        tlpLeftTop.ColumnStyles[3].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[3].Width = actionWidth;

        tlpCommunicationStatus.MinimumSize = new Size(statusWidth, 0);
        tlpReportButton.MinimumSize = new Size(actionWidth, 0);
    }

    /// <summary>
    /// Measures possible status words so every status card can show the longest translated value.
    /// </summary>
    private int CalculateHeaderStatusWidth()
    {
        var statusFont = _headerStatusFont ?? tagMes.Font;
        var statusTexts = new[]
        {
            "MES",
            "PLC",
            _localizer.GetString(TextKeys.Monitor.Label.DeviceStatus),
            _localizer.GetString(TextKeys.Mes.StateChecking),
            _localizer.GetString(TextKeys.Mes.StateConnected),
            _localizer.GetString(TextKeys.Mes.StateDisconnected),
            _localizer.GetString(TextKeys.Plc.StateStopped),
            _localizer.GetString(TextKeys.Plc.StateConnecting),
            _localizer.GetString(TextKeys.Plc.StateConnected),
            _localizer.GetString(TextKeys.Plc.StateReconnecting),
            _localizer.GetString(TextKeys.Plc.StateDisconnected),
            _localizer.GetString(TextKeys.Plc.StateFaulted),
            _localizer.GetString(TextKeys.DeviceStatus.Running),
            _localizer.GetString(TextKeys.DeviceStatus.Paused),
            _localizer.GetString(TextKeys.DeviceStatus.Stopped),
            _localizer.GetString(TextKeys.DeviceStatus.Alarm),
            _localizer.GetString(TextKeys.DeviceStatus.Unknown)
        };

        var maxTextWidth = statusTexts.Max(text => MeasureTextWidth(text, statusFont));
        var cellWidth = Math.Max(HeaderStatusCellMinWidth, maxTextWidth + HeaderStatusCellPadding);
        return cellWidth * 3;
    }

    /// <summary>
    /// Measures localized report button text and leaves extra room for the icon.
    /// </summary>
    private int CalculateHeaderActionWidth()
    {
        var buttonFont = _headerButtonFont ?? btnExpStart.Font;
        var startWidth = MeasureTextWidth(btnExpStart.Text ?? string.Empty, buttonFont);
        var finishWidth = MeasureTextWidth(btnExpEnd.Text ?? string.Empty, buttonFont);
        return Math.Max(HeaderActionMinWidth, Math.Max(startWidth, finishWidth) + HeaderButtonPadding);
    }

    /// <summary>
    /// Centralized text measurement avoids scattered magic width values in the header layout.
    /// </summary>
    private static int MeasureTextWidth(string text, Font font)
    {
        return TextRenderer.MeasureText(
            text,
            font,
            new Size(10000, 10000),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
    }

    /// <summary>
    /// 异常和运行状态是操作员最先看的信息，因此使用更大的加粗字体和状态色增强识别度。
    /// </summary>
    private void ConfigureRuntimeMessagePanels()
    {
        _runtimeMessageFont = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold);
        _runtimeGroupFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);

        groupBox1.Font = _runtimeGroupFont;
        groupBox2.Font = _runtimeGroupFont;
        inputErrorTips.Font = _runtimeMessageFont;
        inputRunningStatus.Font = _runtimeMessageFont;

        ApplyRuntimeErrorTone(hasError: false);
        ApplyRuntimeStatusTone();
    }

    /// <summary>
    /// 标题文字或容器尺寸变化后，重新计算一个尽量填满区域但不溢出的字号。
    /// </summary>
    private void TitleLayout_Changed(object? sender, EventArgs e)
    {
        AdjustHeaderFixedColumns();
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 使用二分查找寻找最大可用字号，比逐级递增更稳定，也能减少频繁重绘。
    /// </summary>
    private void AdjustTitleFontSize()
    {
        if (_adjustingTitleFont || string.IsNullOrWhiteSpace(lblTitle.Text))
        {
            return;
        }

        var availableSize = new Size(
            Math.Max(1, lblTitle.ClientSize.Width - TitleTextPadding),
            Math.Max(1, lblTitle.ClientSize.Height - TitleTextPadding));

        if (availableSize.Width <= 1 || availableSize.Height <= 1)
        {
            return;
        }

        _adjustingTitleFont = true;
        try
        {
            var bestSize = FindBestTitleFontSize(lblTitle.Text, lblTitle.Font, availableSize);
            if (Math.Abs(lblTitle.Font.Size - bestSize) < 0.25F)
            {
                return;
            }

            var oldFont = _titleFont;
            _titleFont = new Font(lblTitle.Font.FontFamily, bestSize, lblTitle.Font.Style, lblTitle.Font.Unit);
            lblTitle.Font = _titleFont;
            oldFont?.Dispose();
        }
        finally
        {
            _adjustingTitleFont = false;
        }
    }

    /// <summary>
    /// 测量单行标题在指定区域内能使用的最大字号。
    /// </summary>
    private static float FindBestTitleFontSize(string text, Font baseFont, Size availableSize)
    {
        var low = MinTitleFontSize;
        var high = MaxTitleFontSize;
        var best = MinTitleFontSize;
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        for (var i = 0; i < 12; i++)
        {
            var mid = (low + high) / 2F;
            using var testFont = new Font(baseFont.FontFamily, mid, baseFont.Style, baseFont.Unit);
            var measuredSize = TextRenderer.MeasureText(text, testFont, new Size(10000, 10000), flags);

            if (measuredSize.Width <= availableSize.Width && measuredSize.Height <= availableSize.Height)
            {
                best = mid;
                low = mid;
                continue;
            }

            high = mid;
        }

        return best;
    }

    /// <summary>
    /// 统一绑定事件，方便后续维护。
    /// </summary>
    private void WireEvents()
    {
        Load += MonitorView_Load;
        _timer.Tick += Timer_Tick;
        tlpLeftTop.SizeChanged += TitleLayout_Changed;
        lblTitle.SizeChanged += TitleLayout_Changed;
        lblTitle.TextChanged += TitleLayout_Changed;
        btnSwitchUser.Click += SwitchUser_Click;
        btnLogout.Click += Logout_Click;
        btnChangeWO.Click += ChangeWorkOrder_Click;
        btnExpStart.Click += StartReport_Click;
        btnExpEnd.Click += FinishReport_Click;
        select_Lang.SelectedIndexChanged += Language_SelectedIndexChanged;
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;
        _weldTaskService.StateChanged += WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged += MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged += PlcWorkIdMonitorService_WorkIdChanged;
    }

    /// <summary>
    /// 语言变化时，只补刷新运行时动态文本。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        BindSessionInfo();
        BindLanguageSelection();
        BindProductionRuntimeState();
        ConfigureProductionTableColumns();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(_plcProductionMonitorService.Current);
        AdjustTitleFontSize();
    }

    private void MonitorView_Load(object? sender, EventArgs e)
    {
        _timer.Start();
        ApplyLocalizedTexts();
        UpdateCurrentTime();
        BindSessionInfo();
        BindLanguageSelection();
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(_plcProductionMonitorService.Current);
        AdjustTitleFontSize();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
        _weldTaskService.StateChanged -= WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged -= PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged -= MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged -= PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged -= PlcWorkIdMonitorService_WorkIdChanged;
        _timer.Stop();
        _timer.Dispose();
        _titleFont?.Dispose();
        _headerStatusFont?.Dispose();
        _headerButtonFont?.Dispose();
        _runtimeMessageFont?.Dispose();
        _runtimeGroupFont?.Dispose();
        base.OnHandleDestroyed(e);
    }

    private void GlobalContext_SessionChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(BindSessionInfo);
            return;
        }

        BindSessionInfo();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateCurrentTime();
    }

    private void WeldTaskService_StateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(RefreshProductionRuntimeState);
            return;
        }

        RefreshProductionRuntimeState();
    }

    private void PlcCommunicationService_StatusChanged(object? sender, PlcConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyPlcStatus(e));
            return;
        }

        ApplyPlcStatus(e);
    }

    private void MesConnectionMonitorService_StatusChanged(object? sender, MesConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyMesStatus(e));
            return;
        }

        ApplyMesStatus(e);
    }

    private void PlcProductionMonitorService_StatusChanged(object? sender, PlcProductionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyProductionStatus(e));
            return;
        }

        ApplyProductionStatus(e);
    }

    private void PlcWorkIdMonitorService_WorkIdChanged(object? sender, PlcWorkIdSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyWorkIdSnapshot(e));
            return;
        }

        ApplyWorkIdSnapshot(e);
    }

    private void SwitchUser_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.SwitchUserConfirm, TextKeys.Monitor.Title.SwitchUserTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        FindForm()?.Close();
    }

    private void Logout_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.LogoutConfirm, TextKeys.Monitor.Title.LogoutTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        FindForm()?.Close();
    }

    private async void ChangeWorkOrder_Click(object? sender, EventArgs e)
    {
        await PrepareWorkOrderAndProgramAsync(forceManualInput: true);
    }

    private async void StartReport_Click(object? sender, EventArgs e)
    {
        if (ShouldPrepareWorkOrderBeforeStart()
            && !await PrepareWorkOrderAndProgramAsync(forceManualInput: false))
        {
            return;
        }

        var state = _weldTaskService.CurrentState;
        if (state.CurrentWorkOrder is null || state.SelectedProcess is null || state.SelectedProgram is null)
        {
            ShowWarning(TextKeys.Monitor.Message.StartPrerequisiteMissing);
            return;
        }

        if (!TryPromptPositiveInt(
                TextKeys.Monitor.Dialog.ActualQuantityTitle,
                TextKeys.Monitor.Dialog.ActualQuantityPrompt,
                state.SelectedProcess.StartAmount,
                out var actualQty))
        {
            return;
        }

        var employeeNumber = await PromptValidatedOperatorAsync();
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingStart);
            var task = await _weldTaskService.StartAsync(employeeNumber, actualQty);
            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.StartSuccess, task.ExpStartId ?? string.Empty);
        });
    }

    private async void FinishReport_Click(object? sender, EventArgs e)
    {
        var state = _weldTaskService.CurrentState;
        if (state.ActiveTask is null)
        {
            ShowWarning(TextKeys.Monitor.Message.FinishPrerequisiteMissing);
            return;
        }

        var employeeNumber = await PromptValidatedOperatorAsync();
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        var production = _plcProductionMonitorService.Current;
        var defaultActual = Math.Max(1, production.TotalProduction > 0 ? production.TotalProduction : state.ActiveTask.ActualQty);
        if (!TryPromptPositiveInt(TextKeys.Monitor.Dialog.ActualQuantityTitle, TextKeys.Monitor.Dialog.ActualQuantityPrompt, defaultActual, out var actualQty)
            || !TryPromptNonNegativeInt(TextKeys.Monitor.Dialog.QualifiedQuantityTitle, TextKeys.Monitor.Dialog.QualifiedQuantityPrompt, production.AcceptedQuantity, out var qualifiedQty)
            || !TryPromptNonNegativeInt(TextKeys.Monitor.Dialog.FailedQuantityTitle, TextKeys.Monitor.Dialog.FailedQuantityPrompt, production.RejectedQuantity, out var failedQty))
        {
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingFinish);
            await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty);
            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.FinishSuccess);
        });
    }

    private bool ShouldPrepareWorkOrderBeforeStart()
    {
        var state = _weldTaskService.CurrentState;
        if (state.CurrentWorkOrder is null || state.SelectedProcess is null || state.SelectedProgram is null)
        {
            return true;
        }

        var plcWorkId = _plcWorkIdMonitorService.Current.WorkId.Trim();
        return !string.IsNullOrWhiteSpace(plcWorkId)
            && !string.Equals(state.CurrentWorkOrder.SN, plcWorkId, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> PrepareWorkOrderAndProgramAsync(bool forceManualInput)
    {
        if (!TryResolveWorkId(forceManualInput, out var workId))
        {
            return false;
        }

        var isReady = false;
        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingWorkOrder);
            var workOrder = await _weldTaskService.GetWorkOrderInfoAsync(workId);
            if (workOrder is null)
            {
                ShowBusinessWarning(
                    "MES.GetWorkOrderInfo",
                    TextKeys.Monitor.Message.WorkOrderLoadFailed,
                    _weldTaskService.CurrentState.LastServerSyncMessage ?? string.Empty,
                    $"WorkId={workId}");
                return;
            }

            if (!TrySelectProcess(workOrder.ExpItems, out var process))
            {
                return;
            }

            _weldTaskService.SelectProcess(process);
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingPrograms);
            var programs = await _weldTaskService.LoadProgramsAsync();
            if (programs.Count == 0)
            {
                ShowBusinessWarning(
                    "MES.GetProgramList",
                    TextKeys.Monitor.Message.ProgramListEmpty,
                    "MES返回的程序列表为空。",
                    $"WorkId={workOrder.SN}; ProductNum={workOrder.ProdNum}");
                return;
            }

            if (!TrySelectProgram(programs, out var program))
            {
                return;
            }

            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.DownloadingProgram);
            var detail = await _weldTaskService.DownloadProgramAsync(program);
            if (detail is null)
            {
                ShowBusinessWarning(
                    "MES.DownloadProgram",
                    TextKeys.Monitor.Message.ProgramDownloadFailed,
                    "MES程序详情下载失败或返回空数据。",
                    FormatProgram(program));
                return;
            }

            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.WorkOrderReady);
            isReady = true;
        });

        return isReady;
    }

    private bool TryResolveWorkId(bool forceManualInput, out string workId)
    {
        var snapshot = _plcWorkIdMonitorService.Current;
        var plcWorkId = snapshot.WorkId.Trim();
        if (!forceManualInput && snapshot.IsSuccess && !string.IsNullOrWhiteSpace(plcWorkId))
        {
            workId = plcWorkId;
            return true;
        }

        if (!PromptInputForm.TryShow(
                this,
                _localizer.GetString(TextKeys.Monitor.Dialog.ScanWorkIdTitle),
                _localizer.GetString(TextKeys.Monitor.Dialog.ScanWorkIdPrompt),
                plcWorkId,
                _localizer.GetString(TextKeys.Common.ActionApply),
                _localizer.GetString(TextKeys.Common.ActionCancel),
                out var input))
        {
            workId = string.Empty;
            return false;
        }

        workId = input.Trim();
        if (!string.IsNullOrWhiteSpace(workId))
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.WorkIdRequired);
        return false;
    }

    private void Language_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingLanguageSelection)
        {
            return;
        }

        var targetLanguage = select_Lang.SelectedIndex == 0
            ? AppConstants.Languages.Chinese
            : AppConstants.Languages.English;

        _localizer.SetLanguage(targetLanguage);
    }

    /// <summary>
    /// 会话信息里的“未登录”文本要随语言一起切换。
    /// </summary>
    private void BindSessionInfo()
    {
        var user = GlobalContext.CurrentUser;
        lblCurrentUser.Text = user is null
            ? _localizer.GetString(TextKeys.Common.StatusNotLoggedIn)
            : $"{user.UserName} ({user.UserNumber})";
    }

    private void BindProductionRuntimeState()
    {
        var state = _weldTaskService.CurrentState;
        var workOrder = state.CurrentWorkOrder;
        var process = state.SelectedProcess;
        var program = state.SelectedProgram;
        var liveWorkId = _plcWorkIdMonitorService.Current.WorkId.Trim();

        inputSN.Text = !string.IsNullOrWhiteSpace(liveWorkId) ? liveWorkId : workOrder?.SN ?? string.Empty;
        inputProdNum.Text = workOrder?.ProdNum ?? string.Empty;
        inputBatch.Text = workOrder?.Batch ?? string.Empty;
        inputProductName.Text = workOrder?.ProductName ?? string.Empty;
        inputDrawingNo.Text = workOrder?.DrawingNo ?? string.Empty;
        inputProdModel.Text = workOrder?.ProdModel ?? string.Empty;
        inputSpec.Text = workOrder?.Spec ?? string.Empty;
        inputProcessNo.Text = process?.ProcessNo ?? string.Empty;
        inputItemName.Text = process?.ItemName ?? string.Empty;
        inputProgramName.Text = program?.ProgramName ?? string.Empty;
    }

    /// <summary>
    /// Refreshes work-order fields and metrics that depend on the selected MES process.
    /// </summary>
    private void RefreshProductionRuntimeState()
    {
        BindProductionRuntimeState();
        BindProductionMetrics(_plcProductionMonitorService.Current);
    }

    private void ApplyWorkIdSnapshot(PlcWorkIdSnapshot snapshot)
    {
        if (snapshot.IsSuccess)
        {
            BindProductionRuntimeState();
        }

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkIdReadFailed);
        }
    }

    /// <summary>
    /// 下拉框选项不是资源控件属性，所以这里手动刷新。
    /// </summary>
    private void BindLanguageSelection()
    {
        _syncingLanguageSelection = true;

        if (select_Lang.Items.Count > 0)
        {
            select_Lang.Items.Clear();
        }

        select_Lang.Items.AddRange(new object[]
        {
            _localizer.GetString(TextKeys.Common.LanguageChinese),
            _localizer.GetString(TextKeys.Common.LanguageEnglish)
        });

        select_Lang.SelectedIndex = GlobalContext.CurrentLanguage == AppConstants.Languages.English ? 1 : 0;

        _syncingLanguageSelection = false;
    }

    private void UpdateCurrentTime()
    {
        lblCurTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Static texts are refreshed here so designer hard-coded Chinese is not shown after language switching.
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Monitor.Title.AppTitle);
        btnExpStart.Text = _localizer.GetString(TextKeys.Monitor.Button.StartReport);
        btnExpEnd.Text = _localizer.GetString(TextKeys.Monitor.Button.FinishReport);
        btnChangeWO.Text = _localizer.GetString(TextKeys.Monitor.Button.ChangeWorkOrder);
        btnSwitchUser.Text = _localizer.GetString(TextKeys.Monitor.Button.SwitchUser);
        btnLogout.Text = _localizer.GetString(TextKeys.Monitor.Button.Logout);
        groupBox1.Text = _localizer.GetString(TextKeys.Monitor.Group.ExceptionTips);
        groupBox2.Text = _localizer.GetString(TextKeys.Monitor.Group.RunningStatus);
        table1.Text = _localizer.GetString(TextKeys.Monitor.Group.ProductionMetrics);

        lblCurUser.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentUser);
        lblCurLang.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentLang);
        lblWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Label.WorkOrderNo);
        lblProgramName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProgramName);
        lblProductNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductNo);
        lblProdModel.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductModel);
        lblBatchNo.Text = _localizer.GetString(TextKeys.Monitor.Label.BatchNo);
        lblSpec.Text = _localizer.GetString(TextKeys.Monitor.Label.Spec);
        lblPartName.Text = _localizer.GetString(TextKeys.Monitor.Label.PartName);
        lblDrawingNo.Text = _localizer.GetString(TextKeys.Monitor.Label.DrawingNo);
        lblProcessNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessNo);
        lblProcessName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessName);
        AdjustHeaderFixedColumns();
    }

    /// <summary>
    /// 将 PLC 状态快照转换成监控页右侧状态标签的文字和颜色。
    /// </summary>
    private void ApplyPlcStatus(PlcConnectionSnapshot snapshot)
    {
        tagPLC.Text = $"PLC\r\n{_localizer.GetString(GetPlcStateKey(snapshot.State))}";
        tagPLC.ForeColor = Color.White;
        tagPLC.BackColor = snapshot.State switch
        {
            PlcConnectionState.Connected => UiColors.Status.Success,
            PlcConnectionState.Connecting or PlcConnectionState.Reconnecting => UiColors.Status.Warning,
            PlcConnectionState.Stopped => UiColors.Status.Muted,
            _ => UiColors.Status.Danger
        };
    }

    private static string GetPlcStateKey(PlcConnectionState state)
    {
        return state switch
        {
            PlcConnectionState.Connecting => TextKeys.Plc.StateConnecting,
            PlcConnectionState.Connected => TextKeys.Plc.StateConnected,
            PlcConnectionState.Reconnecting => TextKeys.Plc.StateReconnecting,
            PlcConnectionState.Disconnected => TextKeys.Plc.StateDisconnected,
            PlcConnectionState.Faulted => TextKeys.Plc.StateFaulted,
            _ => TextKeys.Plc.StateStopped
        };
    }

    /// <summary>
    /// MES connectivity is judged by the MES monitor service; the view only maps it to color and text.
    /// </summary>
    private void ApplyMesStatus(MesConnectionSnapshot snapshot)
    {
        tagMes.Text = $"MES\r\n{_localizer.GetString(GetMesStateKey(snapshot))}";
        tagMes.ForeColor = Color.White;
        tagMes.BackColor = snapshot.UpdatedTime == default
            ? UiColors.Status.Warning
            : snapshot.IsConnected
                ? UiColors.Status.Success
                : UiColors.Status.Danger;
    }

    private static string GetMesStateKey(MesConnectionSnapshot snapshot)
    {
        if (snapshot.UpdatedTime == default)
        {
            return TextKeys.Mes.StateChecking;
        }

        return snapshot.IsConnected
            ? TextKeys.Mes.StateConnected
            : TextKeys.Mes.StateDisconnected;
    }

    /// <summary>
    /// Refreshes device state and production metrics from the latest PLC production snapshot.
    /// </summary>
    private void ApplyProductionStatus(PlcProductionSnapshot snapshot)
    {
        ApplyDeviceStatus(snapshot);
        BindProductionMetrics(snapshot);
    }

    private void ApplyDeviceStatus(PlcProductionSnapshot snapshot)
    {
        var stateKey = GetDeviceStatusKey(snapshot.DeviceStatusCode);
        var stateText = _localizer.GetString(stateKey);

        // The dynamic state is placed first so it stays visible even if the Tag only paints one line.
        tagDeviceStatus.Text = $"{stateText}\r\n{_localizer.GetString(TextKeys.Monitor.Label.DeviceStatus)}";
        tagDeviceStatus.ForeColor = Color.White;
        tagDeviceStatus.BackColor = GetDeviceStatusColor(snapshot.DeviceStatusCode, snapshot.IsSuccess);

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.ProductionCollectFailed);
        }
    }

    private void BindProductionMetrics(PlcProductionSnapshot snapshot)
    {
        var mesProductionQuantity = _weldTaskService.CurrentState.SelectedProcess?.StartAmount;
        var acceptedRate = CalculateRate(snapshot.AcceptedQuantity, snapshot.TotalProduction);
        var rejectedRate = CalculateRate(snapshot.RejectedQuantity, snapshot.TotalProduction);
        var achievementRate = mesProductionQuantity.GetValueOrDefault() > 0
            ? CalculateRate(snapshot.TotalProduction, mesProductionQuantity!.Value)
            : null;

        var rows = new List<ProductionMetricRow>
        {
            new(_localizer.GetString(TextKeys.Production.TotalProduction), snapshot.TotalProduction.ToString()),
            new(_localizer.GetString(TextKeys.Production.MesProductionQuantity), FormatNullable(mesProductionQuantity)),
            new(_localizer.GetString(TextKeys.Production.AcceptedQuantity), snapshot.AcceptedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.RejectedQuantity), snapshot.RejectedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedRate), FormatRate(acceptedRate)),
            new(_localizer.GetString(TextKeys.Production.RejectedRate), FormatRate(rejectedRate)),
            new(_localizer.GetString(TextKeys.Production.AchievementRate), FormatRate(achievementRate))
        };

        table1.DataSource = rows;
        table1.Refresh();
    }

    private void ConfigureProductionTableColumns()
    {
        table1.Columns.Clear();
        table1.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Name), _localizer.GetString(TextKeys.Production.MetricName))
        {
            Ellipsis = true
        });
        table1.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Value), _localizer.GetString(TextKeys.Production.MetricValue))
        {
            Ellipsis = true
        });
        TableStyleHelper.ApplyAntdColumnDefaults(table1);
    }

    /// <summary>
    /// Keeps monitor tables visually aligned with other management pages.
    /// </summary>
    private void ConfigureTables()
    {
        TableStyleHelper.ApplyAntdTable(table1, AntdUI.ColumnsMode.Fill);
        ApplyCompactProductionMetricTableStyle();
        TableStyleHelper.ApplyAntdTable(table2);
    }

    /// <summary>
    /// The metric table has only a few fixed rows, so a compact row height keeps the right panel readable.
    /// </summary>
    private void ApplyCompactProductionMetricTableStyle()
    {
        table1.RowHeight = 34;
        table1.RowHeightHeader = 36;
        table1.Gap = 4;
        table1.GapCell = 2;
        table1.Gaps = new Size(4, 4);
    }

    private static string GetDeviceStatusKey(short? statusCode)
    {
        return statusCode switch
        {
            1 => TextKeys.DeviceStatus.Running,
            2 => TextKeys.DeviceStatus.Paused,
            3 => TextKeys.DeviceStatus.Stopped,
            4 => TextKeys.DeviceStatus.Alarm,
            _ => TextKeys.DeviceStatus.Unknown
        };
    }

    private static Color GetDeviceStatusColor(short? statusCode, bool isSuccess)
    {
        if (!isSuccess)
        {
            return UiColors.Status.Danger;
        }

        return statusCode switch
        {
            1 => UiColors.Status.Success,
            2 => UiColors.Status.Warning,
            3 => UiColors.Status.Muted,
            4 => UiColors.Status.Danger,
            _ => UiColors.Status.Muted
        };
    }

    private string FormatNullable(int? value)
    {
        return value?.ToString() ?? _localizer.GetString(TextKeys.Production.NotAvailable);
    }

    private static double? CalculateRate(int numerator, int denominator)
    {
        return denominator > 0
            ? (double)numerator / denominator
            : null;
    }

    private string FormatRate(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("P2")
            : _localizer.GetString(TextKeys.Production.NotAvailable);
    }

    private bool TrySelectProcess(IReadOnlyList<ExpItemData> processes, out ExpItemData process)
    {
        if (processes.Count == 0)
        {
            ShowWarning(TextKeys.Monitor.Message.ProcessRequired);
            process = default!;
            return false;
        }

        var columns = new[]
        {
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.SequenceNo),
                process => process.SequenceNo,
                10F,
                DataGridViewContentAlignment.MiddleCenter),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProcessNo),
                process => process.ProcessNo,
                12F,
                DataGridViewContentAlignment.MiddleCenter),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProcessName),
                process => process.ItemName,
                38F),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProductionQuantity),
                process => process.StartAmount,
                14F,
                DataGridViewContentAlignment.MiddleRight)
        };

        return SelectionDialog.TrySelect(
            this,
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProcessTitle),
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProcessPrompt),
            processes,
            columns,
            _localizer.GetString(TextKeys.Common.ActionApply),
            _localizer.GetString(TextKeys.Common.ActionCancel),
            out process);
    }

    private bool TrySelectProgram(IReadOnlyList<MesProgramListItemData> programs, out MesProgramListItemData program)
    {
        return SelectionDialog.TrySelect(
            this,
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProgramTitle),
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProgramPrompt),
            programs,
            FormatProgram,
            _localizer.GetString(TextKeys.Common.ActionApply),
            _localizer.GetString(TextKeys.Common.ActionCancel),
            out program);
    }

    private static string FormatProgram(MesProgramListItemData program)
    {
        return $"{program.ProgramName} | {program.ProgramType} | {program.ProductNum} | {program.Id}";
    }

    private async Task<string> PromptValidatedOperatorAsync()
    {
        while (true)
        {
            using var form = new OperatorInputForm(_localizer);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return string.Empty;
            }

            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.ValidatingOperator);
            var response = await _weldTaskService.ValidateMesOperatorAsync(form.EmployeeNumber);
            if (response.IsSuccess)
            {
                return form.EmployeeNumber;
            }

            ShowBusinessWarning(
                "MES.ValidateOperator",
                TextKeys.Monitor.Message.OperatorValidationFailed,
                response.Msg,
                $"EmployeeNumber={form.EmployeeNumber}");
        }
    }

    private bool TryPromptPositiveInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        if (!TryPromptInt(titleKey, promptKey, defaultValue, out value))
        {
            return false;
        }

        if (value > 0)
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.QuantityInvalid);
        return false;
    }

    private bool TryPromptNonNegativeInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        if (!TryPromptInt(titleKey, promptKey, defaultValue, out value))
        {
            return false;
        }

        if (value >= 0)
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.QuantityInvalid);
        return false;
    }

    private bool TryPromptInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        if (!PromptInputForm.TryShow(
                this,
                _localizer.GetString(titleKey),
                _localizer.GetString(promptKey),
                Math.Max(0, defaultValue).ToString(),
                _localizer.GetString(TextKeys.Common.ActionApply),
                _localizer.GetString(TextKeys.Common.ActionCancel),
                out var text))
        {
            value = 0;
            return false;
        }

        if (int.TryParse(text, out value))
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.QuantityInvalid);
        return false;
    }

    private async Task RunUiOperationAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            await action();
        }
        catch (BusinessOperationException ex)
        {
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeErrorText(ex.Message);
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RunUiOperationAsync");
            var message = BuildLocalizedMessage(TextKeys.Monitor.RuntimeError.OperationFailed);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.OperationFailed);
            ShowError(message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void ShowInfo(string messageKey, params object[] args)
    {
        ClearRuntimeError();
        SetRuntimeStatus(messageKey, args);
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        SetRuntimeError(messageKey, args);
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// 可预见业务失败：界面显示短提示，详细原因写入日志管理。
    /// </summary>
    private void ShowBusinessWarning(string source, string messageKey, string detail, string? context = null)
    {
        var message = _localizer.GetString(messageKey);
        _exceptionLogService.WriteBusiness(source, message, detail, context);
        SetRuntimeError(messageKey);
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// 记录运行状态的资源键，语言切换时可以重新翻译当前提示。
    /// </summary>
    private void SetRuntimeStatus(string messageKey, params object[] args)
    {
        _runtimeStatusKey = messageKey;
        _runtimeStatusArgs = args;
        RefreshRuntimeStatus();
    }

    /// <summary>
    /// 记录异常提示的资源键，避免语言切换后仍显示旧语言。
    /// </summary>
    private void SetRuntimeError(string messageKey, params object[] args)
    {
        _runtimeErrorKey = messageKey;
        _runtimeErrorArgs = args;
        _runtimeErrorText = null;
        RefreshRuntimeError();
    }

    /// <summary>
    /// 用于显示已经整理过的业务短提示，例如“开工上报失败”。
    /// </summary>
    private void SetRuntimeErrorText(string message)
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = NormalizePanelMessage(message);
        RefreshRuntimeError();
    }

    /// <summary>
    /// 新业务动作开始时清空旧异常，避免用户把历史错误误认为当前错误。
    /// </summary>
    private void ClearRuntimeError()
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = null;
        inputErrorTips.Clear();
        ApplyRuntimeErrorTone(hasError: false);
    }

    private void RefreshRuntimePanels()
    {
        RefreshRuntimeStatus();
        RefreshRuntimeError();
    }

    private void RefreshRuntimeStatus()
    {
        inputRunningStatus.Text = _runtimeStatusKey is null
            ? string.Empty
            : BuildLocalizedMessage(_runtimeStatusKey, _runtimeStatusArgs);
        ApplyRuntimeStatusTone();
    }

    private void RefreshRuntimeError()
    {
        inputErrorTips.Text = _runtimeErrorKey is null
            ? _runtimeErrorText ?? string.Empty
            : BuildLocalizedMessage(_runtimeErrorKey, _runtimeErrorArgs);
        ApplyRuntimeErrorTone(!string.IsNullOrWhiteSpace(inputErrorTips.Text));
    }

    /// <summary>
    /// 运行状态使用颜色表达语义：绿色代表成功，蓝色代表处理中，灰色代表空闲。
    /// </summary>
    private void ApplyRuntimeStatusTone()
    {
        var color = GetRuntimeStatusColor(_runtimeStatusKey);
        groupBox2.ForeColor = color;
        inputRunningStatus.ForeColor = color;
    }

    /// <summary>
    /// 异常提示有内容时使用红色，无异常时弱化显示，避免用户误以为仍有故障。
    /// </summary>
    private void ApplyRuntimeErrorTone(bool hasError)
    {
        var color = hasError ? UiColors.Status.Danger : UiColors.Status.Muted;
        groupBox1.ForeColor = color;
        inputErrorTips.ForeColor = color;
    }

    private static Color GetRuntimeStatusColor(string? messageKey)
    {
        return messageKey switch
        {
            TextKeys.Monitor.Message.WorkOrderReady
                or TextKeys.Monitor.Message.StartSuccess
                or TextKeys.Monitor.Message.FinishSuccess => UiColors.Status.Success,
            TextKeys.Monitor.RuntimeStatus.Idle or null => UiColors.Status.Muted,
            _ => UiColors.Status.Primary
        };
    }

    private string BuildLocalizedMessage(string messageKey, params object[] args)
    {
        return NormalizePanelMessage(_localizer.GetString(messageKey, args));
    }

    private static string NormalizePanelMessage(string? message)
    {
        return string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
    }

    /// <summary>
    /// 确认框统一走本地化文本，减少重复代码。
    /// </summary>
    private bool ConfirmAction(string messageKey, string titleKey)
    {
        return MessageBox.Show(
                this,
                _localizer.GetString(messageKey),
                _localizer.GetString(titleKey),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)
            == DialogResult.Yes;
    }

    private sealed record ProductionMetricRow(string Name, string Value);
}

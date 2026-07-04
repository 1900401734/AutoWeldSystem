using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 上传状态页面。
/// 总览页只管理当前任务补传链路；程序文件同步保留为独立页签，避免和工单任务混在一起。
/// </summary>
public partial class StateManageView : BaseView
{
    private readonly IProgramManageService _programService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IUploadStatusSummaryService _summaryService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _bindingSource = new();
    private bool _initialized;

    public StateManageView(
        IProgramManageService programService,
        IUploadTaskService uploadTaskService,
        IUploadStatusSummaryService summaryService,
        ILocalizationService localizer)
    {
        _programService = programService;
        _uploadTaskService = uploadTaskService;
        _summaryService = summaryService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureGrid();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ApplyLocalizedTexts();
        ReloadActiveTasks();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureActiveGridColumns();
        dgvPending.Refresh();
    }

    private void ConfigureGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvPending);
        dgvPending.AutoGenerateColumns = false;
        dgvPending.DataSource = _bindingSource;
        ConfigureActiveGridColumns();
    }

    private void ConfigureActiveGridColumns()
    {
        dgvPending.Columns.Clear();

        if (IsSummaryTab())
        {
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.SequenceNo), "序号", 6));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.TaskIdentity), "任务ID", 22));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.WorkOrderId), "工单号", 14));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.StationNo), "工位", 6));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.StartReportStatus), "开工上报", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.ProcessParameterStatus), "过程参数", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.ReportFileStatus), "xlsx报表", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.FinishReportStatus), "完工上报", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadPendingSummaryRow.PendingCount), "待处理数", 8));
            dgvPending.Columns.Add(CreateDateTimeColumn(nameof(UploadPendingSummaryRow.UpdatedTime), "更新时间", 14));
            return;
        }

        if (IsProgramFileTab())
        {
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProgramName), "程序名称", 24));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProductNum), "产品工号", 12));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncStatus), "同步状态", 12));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncAction), "动作", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncMessage), "同步消息", 30));
            dgvPending.Columns.Add(CreateDateTimeColumn(nameof(ProgramSyncSummary.LastSyncTime), "最后同步时间", 16));
            return;
        }

        dgvPending.Columns.Add(CreateTextColumn(
            nameof(UploadTaskSummary.TaskIdentity),
            IsDeviceStatusTab() ? "状态标识" : "任务ID",
            16));
        if (IsProcessParameterTab())
        {
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.StationNo), "工位", 6));
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.ProductNo), "产品编号", 12));
        }

        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.Target), "目标平台", 10));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.Status), "上传状态", 12));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.RetryCount), "重试次数", 9));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.MaxRetryCount), "最大重试", 9));
        if (IsReportFileTab())
        {
            dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.FilePath), "文件路径", 24));
        }

        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.DisplayMessage), "处理消息", 30));
        dgvPending.Columns.Add(CreateDateTimeColumn(nameof(UploadTaskSummary.UpdatedTime), "更新时间", 14));
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string headerText, float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            FillWeight = fillWeight,
            HeaderText = headerText
        };
    }

    private static DataGridViewTextBoxColumn CreateDateTimeColumn(string propertyName, string headerText, float fillWeight)
    {
        var column = CreateTextColumn(propertyName, headerText, fillWeight);
        column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
        return column;
    }

    private void WireEvents()
    {
        btnRefresh.Click += (_, _) => ReloadActiveTasks();
        btnRetrySelected.Click += RetrySelected_ClickAsync;
        btnRetryAll.Click += RetryAll_ClickAsync;
        btnDeleteSelected.Click += DeleteSelected_Click;
        tabUploadCategories.SelectedIndexChanged += (_, _) => SwitchUploadCategory();
        dgvPending.CellFormatting += DgvPending_CellFormatting;
        dgvPending.SelectionChanged += (_, _) =>
        {
            ApplyRetrySelectedPermissionForActiveTab();
            ApplyDeletePermissionForActiveTab();
        };
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.StateManage.Title);
        lblDescription.Text = "查看开工、过程参数、xlsx报表和完工上报的本地待上传状态，支持断网恢复后人工补传。";
        btnRetrySelected.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetrySelected);
        btnRetryAll.Text = IsSummaryTab() ? "一键上传" : _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        btnDeleteSelected.Text = "删除选中";
        ApplyRetryAllPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
        btnRefresh.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        tabSummary.Text = "上传总览";
        tabStartReports.Text = "开工信息";
        tabFinishReports.Text = "完工信息";
        tabProcessParameters.Text = "过程参数";
        tabReportFiles.Text = "报告文件";
        tabWorkOrderStatuses.Text = "工单状态";
        tabDeviceStatuses.Text = "设备状态";
        tabProgramFiles.Text = "程序文件";
        SetSummary(GetPendingCount());
    }

    private void SwitchUploadCategory()
    {
        btnRetryAll.Text = IsSummaryTab() ? "一键上传" : _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        ApplyRetryAllPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
        ConfigureActiveGridColumns();
        ReloadActiveTasks();
    }

    private void ApplyRetryAllPermissionForActiveTab()
    {
        var permissionCode = IsSummaryTab()
            ? PermissionCodes.Buttons.State.UploadAll
            : PermissionCodes.Buttons.State.RetryAll;
        btnRetryAll.Tag = $"perm:{permissionCode}:enabled";
        btnRetryAll.Enabled = GlobalContext.HasPermission(permissionCode);
    }

    private void ApplyRetrySelectedPermissionForActiveTab()
    {
        if (IsSummaryTab())
        {
            btnRetrySelected.Enabled = false;
            return;
        }

        if (IsProgramFileTab())
        {
            btnRetrySelected.Enabled = dgvPending.CurrentRow?.DataBoundItem is ProgramSyncSummary
                && GlobalContext.HasPermission(PermissionCodes.Buttons.State.RetrySelected);
            return;
        }

        btnRetrySelected.Enabled = dgvPending.CurrentRow?.DataBoundItem is UploadTaskSummary { CanRetry: true }
            && GlobalContext.HasPermission(PermissionCodes.Buttons.State.RetrySelected);
    }

    private void ApplyDeletePermissionForActiveTab()
    {
        if (IsProgramFileTab())
        {
            btnDeleteSelected.Enabled = false;
            return;
        }

        var canDelete = dgvPending.CurrentRow?.DataBoundItem switch
        {
            UploadPendingSummaryRow => true,
            UploadTaskSummary { CanDelete: true } => true,
            _ => false
        };

        btnDeleteSelected.Enabled = canDelete
            && GlobalContext.HasPermission(PermissionCodes.Buttons.State.Delete);
    }

    private int GetPendingCount()
    {
        return _bindingSource.DataSource switch
        {
            ICollection<UploadPendingSummaryRow> summaries => summaries.Count(row => row.PendingCount > 0),
            ICollection<ProgramSyncSummary> programs => programs.Count,
            ICollection<UploadTaskSummary> tasks => tasks.Count,
            _ => 0
        };
    }

    private void SetSummary(int count)
    {
        lblSummary.Text = $"{GetActiveCategoryText()}待处理：{count} 条";
    }

    private void ReloadActiveTasks()
    {
        ApplyRetrySelectedPermissionForActiveTab();

        if (IsSummaryTab())
        {
            var rows = _summaryService.GetSummary().ToList();
            _bindingSource.DataSource = rows;
            SetSummary(rows.Count(row => row.PendingCount > 0));
            ApplyRetrySelectedPermissionForActiveTab();
            ApplyDeletePermissionForActiveTab();
            return;
        }

        if (IsProgramFileTab())
        {
            var programs = _programService.GetPendingSyncPrograms().ToList();
            _bindingSource.DataSource = programs;
            SetSummary(programs.Count);
            ApplyRetrySelectedPermissionForActiveTab();
            ApplyDeletePermissionForActiveTab();
            return;
        }

        var tasks = IsProcessParameterTab()
            ? _uploadTaskService.GetProcessParameterRows().ToList()
            : _uploadTaskService.GetTasks(GetActiveUploadTaskType()).ToList();
        _bindingSource.DataSource = tasks;
        SetSummary(tasks.Count);
        ApplyRetrySelectedPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
    }

    private async void RetrySelected_ClickAsync(object? sender, EventArgs e)
    {
        if (IsSummaryTab())
        {
            ShowWarning("上传总览请使用一键上传。");
            return;
        }

        if (IsProgramFileTab())
        {
            await RetrySelectedProgramAsync();
            return;
        }

        if (dgvPending.CurrentRow?.DataBoundItem is not UploadTaskSummary task)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        if (!task.CanRetry)
        {
            ShowWarning("该过程参数行来自产品历史，需等待达到批次数量后自动上传，不能手动重试。");
            return;
        }

        try
        {
            await _uploadTaskService.ExecuteAsync(task.Id);
            ReloadActiveTasks();
            ShowInfo("已执行选中的上传任务。");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    private async Task RetrySelectedProgramAsync()
    {
        if (dgvPending.CurrentRow?.DataBoundItem is not ProgramSyncSummary item)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        btnRetrySelected.Enabled = false;
        try
        {
            await _programService.SyncProgramAsync(item.Id);
            ReloadActiveTasks();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            ApplyRetrySelectedPermissionForActiveTab();
        }
    }

    private async void RetryAll_ClickAsync(object? sender, EventArgs e)
    {
        btnRetryAll.Enabled = false;
        try
        {
            if (IsSummaryTab())
            {
                var count = await ExecuteAllPendingUploadsAsync();
                ShowInfo($"已按开工、过程参数、xlsx报表、完工顺序执行 {count} 条待上传任务。");
            }
            else if (IsProgramFileTab())
            {
                await _programService.SyncAllPendingAsync();
            }
            else
            {
                var count = await _uploadTaskService.ExecuteAllPendingAsync(GetActiveUploadTaskType());
                ShowInfo($"已执行 {count} 条上传任务。");
            }

            ReloadActiveTasks();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            ApplyRetryAllPermissionForActiveTab();
        }
    }

    private void DeleteSelected_Click(object? sender, EventArgs e)
    {
        if (IsProgramFileTab())
        {
            ShowWarning("程序文件页签不支持在上传状态页删除，请到程序管理页处理。");
            return;
        }

        var selectedItem = dgvPending.CurrentRow?.DataBoundItem;
        if (selectedItem is UploadPendingSummaryRow summary)
        {
            _uploadTaskService.HideWeldTaskUploadState(summary.WeldTaskId);
            ReloadActiveTasks();
            ShowInfo("已从上传总览隐藏选中的任务。");
            return;
        }

        if (selectedItem is UploadTaskSummary task)
        {
            if (!task.CanDelete)
            {
                ShowWarning("该过程参数行来自产品历史，只用于查看待上传状态，不能删除。");
                return;
            }

            _uploadTaskService.DeleteTask(task.Id);
            ReloadActiveTasks();
            ShowInfo("已删除选中的上传任务。");
            return;
        }

        ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
    }

    private async Task<int> ExecuteAllPendingUploadsAsync()
    {
        var count = 0;
        var taskTypes = new[]
        {
            ProductionConstants.UploadTaskTypes.StartReport,
            ProductionConstants.UploadTaskTypes.ProcessParameter,
            ProductionConstants.UploadTaskTypes.ReportFile,
            ProductionConstants.UploadTaskTypes.FinishReport
        };

        foreach (var taskType in taskTypes)
        {
            count += await _uploadTaskService.ExecuteAllPendingAsync(taskType);
        }

        return count;
    }

    private void DgvPending_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null)
        {
            return;
        }

        var item = dgvPending.Rows[e.RowIndex].DataBoundItem;
        if (item is UploadPendingSummaryRow summary)
        {
            FormatSummaryCell(summary, e);
            return;
        }

        if (item is ProgramSyncSummary program)
        {
            FormatProgramCell(program, e);
            return;
        }

        if (item is UploadTaskSummary task)
        {
            FormatUploadTaskCell(task, e);
        }
    }

    private void FormatSummaryCell(UploadPendingSummaryRow item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        var rawText = Convert.ToString(e.Value);
        if (column.DataPropertyName.EndsWith("Status", StringComparison.Ordinal))
        {
            e.Value = GetUploadStatusText(rawText);
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = rawText switch
        {
            ProductionConstants.UploadStatuses.Failed => Color.Firebrick,
            ProductionConstants.UploadStatuses.Uploading => Color.RoyalBlue,
            ProductionConstants.UploadStatuses.Pending => Color.DarkOrange,
            ProductionConstants.UploadStatuses.Retrying => Color.DarkOrange,
            ProductionConstants.UploadStatuses.Uploaded => Color.SeaGreen,
            _ => item.PendingCount > 0 ? Color.DarkOrange : Color.FromArgb(36, 36, 36)
        };
    }

    private void FormatProgramCell(ProgramSyncSummary item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncStatus), StringComparison.Ordinal))
        {
            e.Value = GetProgramSyncStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }
        else if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncAction), StringComparison.Ordinal))
        {
            e.Value = GetProgramSyncActionText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = string.Equals(item.SyncStatus, AppConstants.ProgramSyncStatus.Failed, StringComparison.OrdinalIgnoreCase)
            ? Color.Firebrick
            : Color.DarkOrange;
    }

    private void FormatUploadTaskCell(UploadTaskSummary item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(UploadTaskSummary.Status), StringComparison.Ordinal))
        {
            e.Value = GetUploadStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = item.Status switch
        {
            ProductionConstants.UploadStatuses.Failed => Color.Firebrick,
            ProductionConstants.UploadStatuses.Uploaded => Color.SeaGreen,
            ProductionConstants.UploadStatuses.Retrying => Color.DarkOrange,
            _ => Color.FromArgb(36, 36, 36)
        };
    }

    private string GetActiveUploadTaskType()
    {
        if (tabUploadCategories.SelectedTab == tabReportFiles)
        {
            return ProductionConstants.UploadTaskTypes.ReportFile;
        }

        if (tabUploadCategories.SelectedTab == tabStartReports)
        {
            return ProductionConstants.UploadTaskTypes.StartReport;
        }

        if (tabUploadCategories.SelectedTab == tabFinishReports)
        {
            return ProductionConstants.UploadTaskTypes.FinishReport;
        }

        if (tabUploadCategories.SelectedTab == tabWorkOrderStatuses)
        {
            return ProductionConstants.UploadTaskTypes.WorkOrderStatus;
        }

        if (tabUploadCategories.SelectedTab == tabDeviceStatuses)
        {
            return ProductionConstants.UploadTaskTypes.DeviceStatus;
        }

        return ProductionConstants.UploadTaskTypes.ProcessParameter;
    }

    private string GetActiveCategoryText()
    {
        if (IsSummaryTab())
        {
            return "上传总览";
        }

        if (tabUploadCategories.SelectedTab == tabReportFiles)
        {
            return "报告文件";
        }

        if (tabUploadCategories.SelectedTab == tabStartReports)
        {
            return "开工信息";
        }

        if (tabUploadCategories.SelectedTab == tabFinishReports)
        {
            return "完工信息";
        }

        if (tabUploadCategories.SelectedTab == tabWorkOrderStatuses)
        {
            return "工单状态";
        }

        if (tabUploadCategories.SelectedTab == tabDeviceStatuses)
        {
            return "设备状态";
        }

        if (tabUploadCategories.SelectedTab == tabProgramFiles)
        {
            return "程序文件";
        }

        return "过程参数";
    }

    private bool IsProgramFileTab()
    {
        return tabUploadCategories.SelectedTab == tabProgramFiles;
    }

    private bool IsStartReportTab()
    {
        return tabUploadCategories.SelectedTab == tabStartReports;
    }

    private bool IsReportFileTab()
    {
        return tabUploadCategories.SelectedTab == tabReportFiles;
    }

    private bool IsProcessParameterTab()
    {
        return tabUploadCategories.SelectedTab == tabProcessParameters;
    }

    private bool IsDeviceStatusTab()
    {
        return tabUploadCategories.SelectedTab == tabDeviceStatuses;
    }

    private bool IsSummaryTab()
    {
        return tabUploadCategories.SelectedTab == tabSummary;
    }

    private string GetProgramSyncStatusText(string? status)
    {
        return status switch
        {
            AppConstants.ProgramSyncStatus.PendingCreate => _localizer.GetString(TextKeys.ProgramManage.StatusPendingCreate),
            AppConstants.ProgramSyncStatus.PendingUpdate => _localizer.GetString(TextKeys.ProgramManage.StatusPendingUpdate),
            AppConstants.ProgramSyncStatus.PendingDelete => _localizer.GetString(TextKeys.ProgramManage.StatusPendingDelete),
            AppConstants.ProgramSyncStatus.Synced => _localizer.GetString(TextKeys.ProgramManage.StatusSynced),
            AppConstants.ProgramSyncStatus.Failed => _localizer.GetString(TextKeys.ProgramManage.StatusFailed),
            AppConstants.ProgramSyncStatus.Deleted => _localizer.GetString(TextKeys.ProgramManage.StatusDeleted),
            _ => status ?? string.Empty
        };
    }

    private string GetProgramSyncActionText(string? action)
    {
        return action switch
        {
            AppConstants.ProgramSyncActions.Create => _localizer.GetString(TextKeys.ProgramManage.ActionCreate),
            AppConstants.ProgramSyncActions.Update => _localizer.GetString(TextKeys.ProgramManage.ActionUpdate),
            AppConstants.ProgramSyncActions.Delete => _localizer.GetString(TextKeys.ProgramManage.ActionDelete),
            _ => action ?? string.Empty
        };
    }

    private static string GetUploadStatusText(string? status)
    {
        return UploadStatusDisplayRules.GetDisplayText(status);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

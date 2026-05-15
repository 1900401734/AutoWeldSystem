using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 上传状态页面。
/// 该页面只做本地待上传任务的查看和人工重试入口，真正的上传执行逻辑由对应服务逐步接入。
/// </summary>
public partial class StateManageView : BaseView
{
    private readonly IProgramManageService _programService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _bindingSource = new();
    private bool _initialized;

    public StateManageView(
        IProgramManageService programService,
        IUploadTaskService uploadTaskService,
        ILocalizationService localizer)
    {
        _programService = programService;
        _uploadTaskService = uploadTaskService;
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
        ReloadActiveTasks();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureActiveGridColumns();
        dgvPending.Refresh();
    }

    /// <summary>
    /// 表格外观只配置一次，列会随当前页签重建。
    /// </summary>
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

        if (IsProgramFileTab())
        {
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProgramName), "程序名称", 24));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProductNum), "产品工号", 12));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncStatus), "同步状态", 12));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncAction), "动作", 10));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncMessage), "同步消息", 30));
            dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.LastSyncTime), "最后同步时间", 16));
            return;
        }

        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.BusinessId), "业务ID", 16));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.Target), "目标平台", 10));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.Status), "上传状态", 12));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.RetryCount), "重试次数", 9));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.MaxRetryCount), "最大重试", 9));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.FilePath), "文件路径", 24));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.Message), "处理消息", 30));
        dgvPending.Columns.Add(CreateTextColumn(nameof(UploadTaskSummary.UpdatedTime), "更新时间", 14));
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

    private void WireEvents()
    {
        btnRefresh.Click += (_, _) => ReloadActiveTasks();
        btnRetrySelected.Click += RetrySelected_ClickAsync;
        btnRetryAll.Click += RetryAll_ClickAsync;
        tabUploadCategories.SelectedIndexChanged += (_, _) => SwitchUploadCategory();
        dgvPending.CellFormatting += DgvPending_CellFormatting;
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.StateManage.Title);
        lblDescription.Text = "查看过程参数、报告文件和程序文件的本地上传状态，支持断网恢复后的人工重试。";
        btnRetrySelected.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetrySelected);
        btnRetryAll.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        btnRefresh.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        tabProcessParameters.Text = "过程参数";
        tabReportFiles.Text = "报告文件";
        tabProgramFiles.Text = "程序文件";
        SetSummary(GetPendingCount());
    }

    private void SwitchUploadCategory()
    {
        ConfigureActiveGridColumns();
        ReloadActiveTasks();
    }

    private int GetPendingCount()
    {
        return _bindingSource.DataSource switch
        {
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
        if (IsProgramFileTab())
        {
            var programs = _programService.GetPendingSyncPrograms().ToList();
            _bindingSource.DataSource = programs;
            SetSummary(programs.Count);
            return;
        }

        var tasks = _uploadTaskService.GetTasks(GetActiveUploadTaskType()).ToList();
        _bindingSource.DataSource = tasks;
        SetSummary(tasks.Count);
    }

    private async void RetrySelected_ClickAsync(object? sender, EventArgs e)
    {
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

        try
        {
            _uploadTaskService.RequestRetry(task.Id);
            ReloadActiveTasks();
            ShowInfo("已将选中的上传任务重新加入待上传队列。");
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
            btnRetrySelected.Enabled = true;
        }
    }

    private async void RetryAll_ClickAsync(object? sender, EventArgs e)
    {
        btnRetryAll.Enabled = false;
        try
        {
            if (IsProgramFileTab())
            {
                await _programService.SyncAllPendingAsync();
            }
            else
            {
                var count = _uploadTaskService.RequestRetryAll(GetActiveUploadTaskType());
                ShowInfo($"已将 {count} 条上传任务重新加入待上传队列。");
            }

            ReloadActiveTasks();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            btnRetryAll.Enabled = true;
        }
    }

    private void DgvPending_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null)
        {
            return;
        }

        var item = dgvPending.Rows[e.RowIndex].DataBoundItem;
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

        return ProductionConstants.UploadTaskTypes.ProcessParameter;
    }

    private string GetActiveCategoryText()
    {
        if (tabUploadCategories.SelectedTab == tabReportFiles)
        {
            return "报告文件";
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
        return status switch
        {
            ProductionConstants.UploadStatuses.Pending => "待上传",
            ProductionConstants.UploadStatuses.Uploading => "上传中",
            ProductionConstants.UploadStatuses.Uploaded => "已上传",
            ProductionConstants.UploadStatuses.Failed => "上传失败",
            ProductionConstants.UploadStatuses.Retrying => "重试中",
            ProductionConstants.UploadStatuses.Skipped => "已跳过",
            _ => status ?? string.Empty
        };
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

using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 上传状态页。
/// 当前先接入程序同步队列，后续生产数据、报告文件上传可沿用同样模式扩展。
/// </summary>
public partial class StateManageView : BaseView
{
    private readonly IProgramManageService _programService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _bindingSource = new();
    private bool _initialized;

    public StateManageView(IProgramManageService programService, ILocalizationService localizer)
    {
        _programService = programService;
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
        ReloadPendingPrograms();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyGridHeaders();
        dgvPending.Refresh();
    }

    private void ConfigureGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvPending);
        dgvPending.AutoGenerateColumns = false;
        dgvPending.Columns.Clear();
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProgramName), 26));
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.ProductNum), 12));
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncStatus), 12));
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncAction), 10));
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.SyncMessage), 34));
        dgvPending.Columns.Add(CreateTextColumn(nameof(ProgramSyncSummary.LastSyncTime), 16));
        dgvPending.DataSource = _bindingSource;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            FillWeight = fillWeight
        };
    }

    private void WireEvents()
    {
        btnRefresh.Click += (_, _) => ReloadPendingPrograms();
        btnRetrySelected.Click += RetrySelected_ClickAsync;
        btnRetryAll.Click += RetryAll_ClickAsync;
        dgvPending.CellFormatting += DgvPending_CellFormatting;
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.StateManage.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.StateManage.Description);
        btnRetrySelected.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetrySelected);
        btnRetryAll.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        btnRefresh.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        SetSummary(GetPendingCount());
    }

    private void ApplyGridHeaders()
    {
        SetColumnHeader(nameof(ProgramSyncSummary.ProgramName), TextKeys.Grid.ProgramName);
        SetColumnHeader(nameof(ProgramSyncSummary.ProductNum), TextKeys.Grid.ProgramProductNum);
        SetColumnHeader(nameof(ProgramSyncSummary.SyncStatus), TextKeys.Grid.ProgramSyncStatus);
        SetColumnHeader(nameof(ProgramSyncSummary.SyncAction), TextKeys.Grid.ProgramSyncAction);
        SetColumnHeader(nameof(ProgramSyncSummary.SyncMessage), TextKeys.Grid.ProgramSyncMessage);
        SetColumnHeader(nameof(ProgramSyncSummary.LastSyncTime), TextKeys.Grid.ProgramLastSyncTime);
    }

    private int GetPendingCount()
    {
        return _bindingSource.DataSource is ICollection<ProgramSyncSummary> items
            ? items.Count
            : 0;
    }

    private void SetSummary(int count)
    {
        lblSummary.Text = _localizer.GetString(TextKeys.StateManage.SummaryPendingPrograms, count);
    }

    private void SetColumnHeader(string propertyName, string headerKey)
    {
        foreach (DataGridViewColumn column in dgvPending.Columns)
        {
            if (string.Equals(column.DataPropertyName, propertyName, StringComparison.Ordinal))
            {
                column.HeaderText = _localizer.GetString(headerKey);
                return;
            }
        }
    }

    private void ReloadPendingPrograms()
    {
        var items = _programService.GetPendingSyncPrograms().ToList();
        _bindingSource.DataSource = items;
        SetSummary(items.Count);
    }

    private async void RetrySelected_ClickAsync(object? sender, EventArgs e)
    {
        if (dgvPending.CurrentRow?.DataBoundItem is not ProgramSyncSummary item)
        {
            ShowWarning(TextKeys.StateManage.MessageSelectPending);
            return;
        }

        btnRetrySelected.Enabled = false;
        try
        {
            await _programService.SyncProgramAsync(item.Id);
            ReloadPendingPrograms();
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
            await _programService.SyncAllPendingAsync();
            ReloadPendingPrograms();
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
        if (e.RowIndex < 0 || e.CellStyle is null || dgvPending.Rows[e.RowIndex].DataBoundItem is not ProgramSyncSummary item)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncStatus), StringComparison.Ordinal))
        {
            e.Value = GetSyncStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }
        else if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncAction), StringComparison.Ordinal))
        {
            e.Value = GetSyncActionText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }

        if (string.Equals(item.SyncStatus, AppConstants.ProgramSyncStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            e.CellStyle.ForeColor = Color.Firebrick;
        }
        else
        {
            e.CellStyle.ForeColor = Color.DarkOrange;
        }
    }

    private string GetSyncStatusText(string? status)
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

    private string GetSyncActionText(string? action)
    {
        return action switch
        {
            AppConstants.ProgramSyncActions.Create => _localizer.GetString(TextKeys.ProgramManage.ActionCreate),
            AppConstants.ProgramSyncActions.Update => _localizer.GetString(TextKeys.ProgramManage.ActionUpdate),
            AppConstants.ProgramSyncActions.Delete => _localizer.GetString(TextKeys.ProgramManage.ActionDelete),
            _ => action ?? string.Empty
        };
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        ShowWarningMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowWarningMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

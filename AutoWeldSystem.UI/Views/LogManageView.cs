using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 日志管理页面。
/// 当前先接入 MES 交互日志，其它日志分类只保留入口，后续可以用同样模式扩展。
/// </summary>
public partial class LogManageView : BaseView
{
    private const int MaxDisplayCount = 1000;
    private const string ColumnResultName = "colResult";
    private const string ColumnExceptionCategoryName = "colExceptionCategory";
    private const string ColumnExceptionSeverityName = "colExceptionSeverity";

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IMesInteractionLogService _mesLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _mesBindingSource = new();
    private readonly BindingSource _exceptionBindingSource = new();
    private readonly List<MesInteractionLogEntry> _mesLogs = new();
    private readonly List<ProgramExceptionLogEntry> _exceptionLogs = new();
    private bool _initialized;
    private string _keyword = string.Empty;
    private string _exceptionKeyword = string.Empty;

    private Label lblExceptionTitle = null!;
    private Label lblExceptionDescription = null!;
    private Label lblExceptionDate = null!;
    private DateTimePicker dtpExceptionDate = null!;
    private Label lblExceptionKeyword = null!;
    private TextBox txtExceptionKeyword = null!;
    private AntdUI.Button btnRefreshException = null!;
    private AntdUI.Button btnOpenExceptionFolder = null!;
    private AntdUI.Button btnOpenExceptionSource = null!;
    private AntdUI.Button btnCopyExceptionDetails = null!;
    private DataGridView dgvExceptionLogs = null!;
    private TabPage tabExceptionBasicInfo = null!;
    private TabPage tabExceptionStackTrace = null!;
    private TabPage tabExceptionContext = null!;
    private TextBox txtExceptionBasicInfo = null!;
    private TextBox txtExceptionStackTrace = null!;
    private TextBox txtExceptionContext = null!;

    public LogManageView(
        IMesInteractionLogService mesLogService,
        IProgramExceptionLogService exceptionLogService,
        ILocalizationService localizer)
    {
        _mesLogService = mesLogService;
        _exceptionLogService = exceptionLogService;
        _localizer = localizer;

        InitializeComponent();
        BuildExceptionLogTab();
        ConfigureMesGrid();
        ConfigureExceptionGrid();
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
        dtpMesDate.Value = DateTime.Today;
        dtpExceptionDate.Value = DateTime.Today;
        LoadMesLogs();
        LoadExceptionLogs();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyMesGridHeaders();
        ApplyExceptionGridHeaders();
        ApplyMesFilter();
        ApplyExceptionFilter();
    }

    private void BuildExceptionLogTab()
    {
        tabExceptionLogs.Controls.Clear();

        var exceptionRootLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        exceptionRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        exceptionRootLayout.RowStyles.Add(new RowStyle());
        exceptionRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var exceptionHeaderLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(20, 14, 20, 8),
            RowCount = 1
        };
        exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
        exceptionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var exceptionTitleLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            RowCount = 2
        };
        exceptionTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        exceptionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        exceptionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblExceptionTitle = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblExceptionDescription = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        exceptionTitleLayout.Controls.Add(lblExceptionTitle, 0, 0);
        exceptionTitleLayout.Controls.Add(lblExceptionDescription, 0, 1);

        var exceptionToolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0),
            WrapContents = false
        };

        lblExceptionDate = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 9, 8, 0)
        };
        dtpExceptionDate = new DateTimePicker
        {
            CustomFormat = "yyyy-MM-dd",
            Format = DateTimePickerFormat.Custom,
            Margin = new Padding(0, 2, 16, 0),
            Size = new Size(150, 30)
        };
        lblExceptionKeyword = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 9, 8, 0)
        };
        txtExceptionKeyword = new TextBox
        {
            Margin = new Padding(0, 2, 16, 0),
            Size = new Size(190, 30)
        };
        btnRefreshException = CreateToolbarButton("ReloadOutlined");
        btnRefreshException.Margin = new Padding(0, 0, 10, 0);
        btnOpenExceptionFolder = CreateToolbarButton("FolderOpenOutlined");

        exceptionToolbar.Controls.Add(lblExceptionDate);
        exceptionToolbar.Controls.Add(dtpExceptionDate);
        exceptionToolbar.Controls.Add(lblExceptionKeyword);
        exceptionToolbar.Controls.Add(txtExceptionKeyword);
        exceptionToolbar.Controls.Add(btnRefreshException);
        exceptionToolbar.Controls.Add(btnOpenExceptionFolder);

        exceptionHeaderLayout.Controls.Add(exceptionTitleLayout, 0, 0);
        exceptionHeaderLayout.Controls.Add(exceptionToolbar, 1, 0);

        var splitExceptionContent = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(20, 0, 20, 18),
            SplitterDistance = 760,
            SplitterWidth = 5
        };
        splitExceptionContent.Panel1.Padding = new Padding(0, 0, 12, 0);
        splitExceptionContent.Panel2.Padding = new Padding(12, 0, 0, 0);

        dgvExceptionLogs = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = SystemColors.Window,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            Dock = DockStyle.Fill,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            RowTemplate = { Height = 28 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        var exceptionDetailsLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2
        };
        exceptionDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        exceptionDetailsLayout.RowStyles.Add(new RowStyle());
        exceptionDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var exceptionDetailToolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            WrapContents = false
        };
        btnOpenExceptionSource = CreateToolbarButton("FileSearchOutlined");
        btnOpenExceptionSource.Margin = new Padding(0, 0, 10, 0);
        btnCopyExceptionDetails = CreateToolbarButton("CopyOutlined");
        exceptionDetailToolbar.Controls.Add(btnOpenExceptionSource);
        exceptionDetailToolbar.Controls.Add(btnCopyExceptionDetails);

        var tabExceptionDetails = new TabControl
        {
            Dock = DockStyle.Fill
        };
        tabExceptionBasicInfo = new TabPage();
        tabExceptionStackTrace = new TabPage();
        tabExceptionContext = new TabPage();
        txtExceptionBasicInfo = CreateReadonlyDetailTextBox();
        txtExceptionStackTrace = CreateReadonlyDetailTextBox();
        txtExceptionContext = CreateReadonlyDetailTextBox();

        tabExceptionBasicInfo.Controls.Add(txtExceptionBasicInfo);
        tabExceptionStackTrace.Controls.Add(txtExceptionStackTrace);
        tabExceptionContext.Controls.Add(txtExceptionContext);
        tabExceptionDetails.Controls.Add(tabExceptionBasicInfo);
        tabExceptionDetails.Controls.Add(tabExceptionStackTrace);
        tabExceptionDetails.Controls.Add(tabExceptionContext);

        exceptionDetailsLayout.Controls.Add(exceptionDetailToolbar, 0, 0);
        exceptionDetailsLayout.Controls.Add(tabExceptionDetails, 0, 1);

        splitExceptionContent.Panel1.Controls.Add(dgvExceptionLogs);
        splitExceptionContent.Panel2.Controls.Add(exceptionDetailsLayout);

        exceptionRootLayout.Controls.Add(exceptionHeaderLayout, 0, 0);
        exceptionRootLayout.Controls.Add(splitExceptionContent, 0, 1);
        tabExceptionLogs.Controls.Add(exceptionRootLayout);
    }

    private static AntdUI.Button CreateToolbarButton(string iconSvg)
    {
        return new AntdUI.Button
        {
            AutoSizeMode = AntdUI.TAutoSize.Width,
            BorderWidth = 1F,
            IconSvg = iconSvg,
            Size = new Size(118, 40)
        };
    }

    private static TextBox CreateReadonlyDetailTextBox()
    {
        return new TextBox
        {
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10F),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };
    }

    private void ConfigureMesGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvMesLogs);
        dgvMesLogs.AutoGenerateColumns = false;
        dgvMesLogs.Columns.Clear();

        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.SendTime),
            FillWeight = 18
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.Purpose),
            FillWeight = 18
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.Method),
            FillWeight = 9
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.HttpStatus),
            FillWeight = 9
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.MesStatus),
            FillWeight = 8
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = ColumnResultName,
            DataPropertyName = nameof(MesLogRow.Result),
            FillWeight = 10
        });
        dgvMesLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MesLogRow.Duration),
            FillWeight = 10
        });

        dgvMesLogs.DataSource = _mesBindingSource;
        ApplyMesGridHeaders();
    }

    private void ConfigureExceptionGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvExceptionLogs);
        dgvExceptionLogs.AutoGenerateColumns = false;
        dgvExceptionLogs.Columns.Clear();

        dgvExceptionLogs.Columns.Add(CreateTextColumn(nameof(ExceptionLogRow.OccurredTime), 15));
        dgvExceptionLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = ColumnExceptionCategoryName,
            DataPropertyName = nameof(ExceptionLogRow.Category),
            FillWeight = 10
        });
        dgvExceptionLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = ColumnExceptionSeverityName,
            DataPropertyName = nameof(ExceptionLogRow.Severity),
            FillWeight = 10
        });
        dgvExceptionLogs.Columns.Add(CreateTextColumn(nameof(ExceptionLogRow.ExceptionType), 16));
        dgvExceptionLogs.Columns.Add(CreateTextColumn(nameof(ExceptionLogRow.Message), 32));
        dgvExceptionLogs.Columns.Add(CreateTextColumn(nameof(ExceptionLogRow.Source), 16));
        dgvExceptionLogs.Columns.Add(CreateTextColumn(nameof(ExceptionLogRow.SourceLocation), 22));

        dgvExceptionLogs.DataSource = _exceptionBindingSource;
        ApplyExceptionGridHeaders();
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
        btnRefreshMes.Click += (_, _) => LoadMesLogs();
        btnOpenMesFolder.Click += (_, _) => OpenMesLogFolder();
        dtpMesDate.ValueChanged += (_, _) => LoadMesLogs();
        txtMesKeyword.TextChanged += (_, _) =>
        {
            _keyword = txtMesKeyword.Text.Trim();
            ApplyMesFilter();
        };
        dgvMesLogs.SelectionChanged += (_, _) => ShowSelectedMesLogDetails();
        dgvMesLogs.CellFormatting += DgvMesLogs_CellFormatting;
        _mesLogService.LogWritten += MesLogService_LogWritten;

        btnRefreshException.Click += (_, _) => LoadExceptionLogs();
        btnOpenExceptionFolder.Click += (_, _) => OpenExceptionLogFolder();
        btnOpenExceptionSource.Click += (_, _) => OpenSelectedExceptionSource();
        btnCopyExceptionDetails.Click += (_, _) => CopySelectedExceptionDetails();
        dtpExceptionDate.ValueChanged += (_, _) => LoadExceptionLogs();
        txtExceptionKeyword.TextChanged += (_, _) =>
        {
            _exceptionKeyword = txtExceptionKeyword.Text.Trim();
            ApplyExceptionFilter();
        };
        dgvExceptionLogs.SelectionChanged += (_, _) => ShowSelectedExceptionDetails();
        dgvExceptionLogs.CellFormatting += DgvExceptionLogs_CellFormatting;
        _exceptionLogService.LogWritten += ExceptionLogService_LogWritten;
        Disposed += (_, _) => _exceptionLogService.LogWritten -= ExceptionLogService_LogWritten;
    }

    private void ApplyLocalizedTexts()
    {
        tabMesLogs.Text = _localizer.GetString(TextKeys.Log.TitleMesInteraction);
        tabProductionLogs.Text = _localizer.GetString(TextKeys.Log.TabProductionFlow);
        tabExceptionLogs.Text = _localizer.GetString(TextKeys.Log.TabProgramException);
        lblMesTitle.Text = _localizer.GetString(TextKeys.Log.TitleMesInteraction);
        lblMesDescription.Text = _localizer.GetString(TextKeys.Log.DescriptionMesInteraction);
        lblExceptionTitle.Text = _localizer.GetString(TextKeys.Log.TabProgramException);
        lblExceptionDescription.Text = _localizer.GetString(TextKeys.Log.DescriptionProgramException);
        lblMesDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblExceptionDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblMesKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        lblExceptionKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        btnRefreshMes.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnRefreshException.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnOpenMesFolder.Text = _localizer.GetString(TextKeys.Log.ButtonOpenFolder);
        btnOpenExceptionFolder.Text = _localizer.GetString(TextKeys.Log.ButtonOpenFolder);
        btnOpenExceptionSource.Text = _localizer.GetString(TextKeys.Log.ButtonOpenSource);
        btnCopyExceptionDetails.Text = _localizer.GetString(TextKeys.Log.ButtonCopyDetails);
        tabBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailBasicInfo);
        tabRequestBody.Text = _localizer.GetString(TextKeys.Log.DetailRequest);
        tabResponseBody.Text = _localizer.GetString(TextKeys.Log.DetailResponse);
        tabExceptionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailBasicInfo);
        tabExceptionStackTrace.Text = _localizer.GetString(TextKeys.Log.DetailStackTrace);
        tabExceptionContext.Text = _localizer.GetString(TextKeys.Log.DetailContext);
        lblProductionReserved.Text = _localizer.GetString(TextKeys.Log.PlaceholderReserved);

        if (dgvMesLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowMesLogDetails(null);
        }

        if (dgvExceptionLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowExceptionDetails(null);
        }
    }

    private void ApplyMesGridHeaders()
    {
        if (dgvMesLogs.Columns.Count < 7)
        {
            return;
        }

        dgvMesLogs.Columns[0].HeaderText = _localizer.GetString(TextKeys.Log.ColumnSendTime);
        dgvMesLogs.Columns[1].HeaderText = _localizer.GetString(TextKeys.Log.ColumnPurpose);
        dgvMesLogs.Columns[2].HeaderText = _localizer.GetString(TextKeys.Log.ColumnMethod);
        dgvMesLogs.Columns[3].HeaderText = _localizer.GetString(TextKeys.Log.ColumnHttpStatus);
        dgvMesLogs.Columns[4].HeaderText = _localizer.GetString(TextKeys.Log.ColumnMesStatus);
        dgvMesLogs.Columns[5].HeaderText = _localizer.GetString(TextKeys.Log.ColumnSuccess);
        dgvMesLogs.Columns[6].HeaderText = _localizer.GetString(TextKeys.Log.ColumnDuration);
    }

    private void ApplyExceptionGridHeaders()
    {
        if (dgvExceptionLogs.Columns.Count < 7)
        {
            return;
        }

        dgvExceptionLogs.Columns[0].HeaderText = _localizer.GetString(TextKeys.Log.ColumnOccurredTime);
        dgvExceptionLogs.Columns[1].HeaderText = _localizer.GetString(TextKeys.Log.ColumnCategory);
        dgvExceptionLogs.Columns[2].HeaderText = _localizer.GetString(TextKeys.Log.ColumnSeverity);
        dgvExceptionLogs.Columns[3].HeaderText = _localizer.GetString(TextKeys.Log.ColumnExceptionType);
        dgvExceptionLogs.Columns[4].HeaderText = _localizer.GetString(TextKeys.Log.ColumnMessage);
        dgvExceptionLogs.Columns[5].HeaderText = _localizer.GetString(TextKeys.Log.ColumnSource);
        dgvExceptionLogs.Columns[6].HeaderText = _localizer.GetString(TextKeys.Log.ColumnSourceLine);
    }

    private void LoadMesLogs()
    {
        try
        {
            _mesLogs.Clear();
            _mesLogs.AddRange(_mesLogService
                .GetByDate(dtpMesDate.Value.Date, MaxDisplayCount)
                .Where(ShouldShowMesLog));
            ApplyMesFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadExceptionLogs()
    {
        try
        {
            _exceptionLogs.Clear();
            _exceptionLogs.AddRange(_exceptionLogService.GetByDate(dtpExceptionDate.Value.Date, MaxDisplayCount));
            ApplyExceptionFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ApplyMesFilter()
    {
        var rows = _mesLogs
            .Where(entry => IsMesLogMatched(entry, _keyword))
            .Select(CreateMesLogRow)
            .ToList();

        _mesBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowMesLogDetails(null);
            return;
        }

        if (dgvMesLogs.CurrentRow is null)
        {
            dgvMesLogs.Rows[0].Selected = true;
            dgvMesLogs.CurrentCell = dgvMesLogs.Rows[0].Cells[0];
        }

        ShowSelectedMesLogDetails();
    }

    private void ApplyExceptionFilter()
    {
        var rows = _exceptionLogs
            .Where(entry => IsExceptionLogMatched(entry, _exceptionKeyword))
            .Select(CreateExceptionLogRow)
            .ToList();

        _exceptionBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowExceptionDetails(null);
            return;
        }

        if (dgvExceptionLogs.CurrentRow is null)
        {
            dgvExceptionLogs.Rows[0].Selected = true;
            dgvExceptionLogs.CurrentCell = dgvExceptionLogs.Rows[0].Cells[0];
        }

        ShowSelectedExceptionDetails();
    }

    private MesLogRow CreateMesLogRow(MesInteractionLogEntry entry)
    {
        return new MesLogRow(
            entry,
            entry.IsSuccess
                ? _localizer.GetString(TextKeys.Log.ValueSuccess)
                : _localizer.GetString(TextKeys.Log.ValueFailed));
    }

    private ExceptionLogRow CreateExceptionLogRow(ProgramExceptionLogEntry entry)
    {
        return new ExceptionLogRow(entry, GetExceptionCategoryText(entry.Category));
    }

    private string GetExceptionCategoryText(string category)
    {
        return string.Equals(category, AppConstants.ExceptionLogCategories.Business, StringComparison.OrdinalIgnoreCase)
            ? _localizer.GetString(TextKeys.Log.ValueBusinessException)
            : _localizer.GetString(TextKeys.Log.ValueProgramException);
    }

    private static bool IsMesLogMatched(MesInteractionLogEntry entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return Contains(entry.Purpose, keyword)
            || Contains(entry.Method, keyword)
            || Contains(entry.Url, keyword)
            || Contains(entry.RequestBody, keyword)
            || Contains(entry.ResponseBody, keyword)
            || Contains(entry.MesStatus, keyword)
            || Contains(entry.MesMessage, keyword)
            || Contains(entry.ErrorMessage, keyword)
            || Contains(entry.TraceId, keyword);
    }

    private static bool IsExceptionLogMatched(ProgramExceptionLogEntry entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return Contains(entry.TraceId, keyword)
            || Contains(entry.Category, keyword)
            || Contains(entry.Severity, keyword)
            || Contains(entry.Source, keyword)
            || Contains(entry.ExceptionType, keyword)
            || Contains(entry.Message, keyword)
            || Contains(entry.SourceFilePath, keyword)
            || Contains(entry.SourceMemberName, keyword)
            || Contains(entry.TargetSite, keyword)
            || Contains(entry.Context, keyword)
            || Contains(entry.StackTrace, keyword)
            || Contains(entry.InnerException, keyword);
    }

    private static bool ShouldShowMesLog(MesInteractionLogEntry entry)
    {
        return !string.Equals(
            entry.Purpose,
            AppConstants.MesLogPurposes.GetServerTime,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void MesLogService_LogWritten(object? sender, MesInteractionLogEntry entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveMesLog(entry)));
            return;
        }

        AddLiveMesLog(entry);
    }

    private void ExceptionLogService_LogWritten(object? sender, ProgramExceptionLogEntry entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveExceptionLog(entry)));
            return;
        }

        AddLiveExceptionLog(entry);
    }

    private void AddLiveMesLog(MesInteractionLogEntry entry)
    {
        if (entry.SendTime.Date != dtpMesDate.Value.Date)
        {
            return;
        }

        if (!ShouldShowMesLog(entry))
        {
            return;
        }

        _mesLogs.Insert(0, entry);
        if (_mesLogs.Count > MaxDisplayCount)
        {
            _mesLogs.RemoveRange(MaxDisplayCount, _mesLogs.Count - MaxDisplayCount);
        }

        ApplyMesFilter();
    }

    private void AddLiveExceptionLog(ProgramExceptionLogEntry entry)
    {
        if (entry.OccurredTime.Date != dtpExceptionDate.Value.Date)
        {
            return;
        }

        _exceptionLogs.Insert(0, entry);
        if (_exceptionLogs.Count > MaxDisplayCount)
        {
            _exceptionLogs.RemoveRange(MaxDisplayCount, _exceptionLogs.Count - MaxDisplayCount);
        }

        ApplyExceptionFilter();
    }

    private void ShowSelectedMesLogDetails()
    {
        var row = dgvMesLogs.CurrentRow?.DataBoundItem as MesLogRow;
        ShowMesLogDetails(row?.Entry);
    }

    private void ShowSelectedExceptionDetails()
    {
        ShowExceptionDetails(GetSelectedExceptionEntry());
    }

    private void ShowMesLogDetails(MesInteractionLogEntry? entry)
    {
        if (entry is null)
        {
            txtBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoSelection);
            txtRequestBody.Clear();
            txtResponseBody.Clear();
            return;
        }

        txtBasicInfo.Text = BuildBasicInfo(entry);
        txtRequestBody.Text = PrettyPrintJson(entry.RequestBody);
        txtResponseBody.Text = PrettyPrintJson(entry.ResponseBody);
    }

    private void ShowExceptionDetails(ProgramExceptionLogEntry? entry)
    {
        var hasSourceFile = entry is not null
            && !string.IsNullOrWhiteSpace(entry.SourceFilePath)
            && File.Exists(entry.SourceFilePath);

        btnOpenExceptionSource.Enabled = hasSourceFile;
        btnCopyExceptionDetails.Enabled = entry is not null;

        if (entry is null)
        {
            txtExceptionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoExceptionSelection);
            txtExceptionStackTrace.Clear();
            txtExceptionContext.Clear();
            return;
        }

        txtExceptionBasicInfo.Text = BuildExceptionBasicInfo(entry);
        txtExceptionStackTrace.Text = entry.StackTrace;
        txtExceptionContext.Text = BuildExceptionContext(entry);
    }

    private ProgramExceptionLogEntry? GetSelectedExceptionEntry()
    {
        return (dgvExceptionLogs.CurrentRow?.DataBoundItem as ExceptionLogRow)?.Entry;
    }

    private static string BuildBasicInfo(MesInteractionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"TraceId: {entry.TraceId}");
        builder.AppendLine($"Purpose: {entry.Purpose}");
        builder.AppendLine($"Method: {entry.Method}");
        builder.AppendLine($"Url: {entry.Url}");
        builder.AppendLine($"SendTime: {entry.SendTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"ReceiveTime: {entry.ReceiveTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Duration: {entry.DurationMilliseconds} ms");
        builder.AppendLine($"HTTP: {entry.HttpStatusCode?.ToString() ?? "-"}");
        builder.AppendLine($"MES Status: {entry.MesStatus}");
        builder.AppendLine($"MES Message: {entry.MesMessage}");
        builder.AppendLine($"Success: {entry.IsSuccess}");

        if (!string.IsNullOrWhiteSpace(entry.ErrorMessage))
        {
            builder.AppendLine($"Error: {entry.ErrorMessage}");
        }

        return builder.ToString();
    }

    private static string BuildExceptionBasicInfo(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"TraceId: {entry.TraceId}");
        builder.AppendLine($"Category: {entry.Category}");
        builder.AppendLine($"Severity: {entry.Severity}");
        builder.AppendLine($"Source: {entry.Source}");
        builder.AppendLine($"ExceptionType: {entry.ExceptionType}");
        builder.AppendLine($"Message: {entry.Message}");
        builder.AppendLine($"OccurredTime: {entry.OccurredTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"SourceFile: {GetSourceLocation(entry)}");
        builder.AppendLine($"SourceMember: {entry.SourceMemberName}");
        builder.AppendLine($"TargetSite: {entry.TargetSite}");
        builder.AppendLine($"Thread: {entry.ThreadId} {entry.ThreadName}".TrimEnd());
        builder.AppendLine($"User: {entry.MachineName}\\{entry.UserName}");
        builder.AppendLine($"AppVersion: {entry.ApplicationVersion}");
        return builder.ToString();
    }

    private static string BuildExceptionContext(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(entry.Context))
        {
            builder.AppendLine("Context:");
            builder.AppendLine(entry.Context);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(entry.InnerException))
        {
            builder.AppendLine("InnerException:");
            builder.AppendLine(entry.InnerException);
        }

        return builder.ToString();
    }

    private static string BuildExceptionFullDetails(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine(BuildExceptionBasicInfo(entry));
        builder.AppendLine("StackTrace:");
        builder.AppendLine(entry.StackTrace);

        var context = BuildExceptionContext(entry);
        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.AppendLine();
            builder.AppendLine(context);
        }

        return builder.ToString();
    }

    private static string GetSourceLocation(ProgramExceptionLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SourceFilePath))
        {
            return string.IsNullOrWhiteSpace(entry.SourceMemberName)
                ? "-"
                : entry.SourceMemberName;
        }

        return entry.SourceLineNumber > 0
            ? $"{entry.SourceFilePath}:{entry.SourceLineNumber}"
            : entry.SourceFilePath;
    }

    private static string PrettyPrintJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
        }
        catch
        {
            return value;
        }
    }

    private void DgvMesLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || dgvMesLogs.Rows[e.RowIndex].DataBoundItem is not MesLogRow row)
        {
            return;
        }

        if (e.CellStyle is not null && dgvMesLogs.Columns[e.ColumnIndex].Name == ColumnResultName)
        {
            e.CellStyle.ForeColor = row.Entry.IsSuccess ? Color.ForestGreen : Color.Firebrick;
            e.CellStyle.Font = new Font(dgvMesLogs.Font, FontStyle.Bold);
        }
    }

    private void DgvExceptionLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || dgvExceptionLogs.Rows[e.RowIndex].DataBoundItem is not ExceptionLogRow row)
        {
            return;
        }

        if (e.CellStyle is null)
        {
            return;
        }

        if (dgvExceptionLogs.Columns[e.ColumnIndex].Name == ColumnExceptionCategoryName
            || dgvExceptionLogs.Columns[e.ColumnIndex].Name == ColumnExceptionSeverityName)
        {
            e.CellStyle.ForeColor = IsBusinessException(row.Entry)
                ? UiColors.Status.Business
                : UiColors.Status.Danger;
            e.CellStyle.Font = new Font(dgvExceptionLogs.Font, FontStyle.Bold);
        }
    }

    private static bool IsBusinessException(ProgramExceptionLogEntry entry)
    {
        return string.Equals(entry.Category, AppConstants.ExceptionLogCategories.Business, StringComparison.OrdinalIgnoreCase);
    }

    private void OpenMesLogFolder()
    {
        try
        {
            var folder = _mesLogService.GetLogDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenExceptionLogFolder()
    {
        try
        {
            var folder = _exceptionLogService.GetLogDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenSelectedExceptionSource()
    {
        var entry = GetSelectedExceptionEntry();
        if (entry is null || string.IsNullOrWhiteSpace(entry.SourceFilePath) || !File.Exists(entry.SourceFilePath))
        {
            ShowWarning(_localizer.GetString(TextKeys.Log.MessageSourceMissing));
            return;
        }

        try
        {
            Clipboard.SetText(GetSourceLocation(entry));
            Process.Start(new ProcessStartInfo
            {
                FileName = entry.SourceFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void CopySelectedExceptionDetails()
    {
        var entry = GetSelectedExceptionEntry();
        if (entry is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.Log.DetailNoExceptionSelection));
            return;
        }

        try
        {
            Clipboard.SetText(BuildExceptionFullDetails(entry));
            ShowInfo(_localizer.GetString(TextKeys.Log.MessageDetailsCopied));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
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

    private sealed class MesLogRow
    {
        public MesLogRow(MesInteractionLogEntry entry, string result)
        {
            Entry = entry;
            Result = result;
        }

        public MesInteractionLogEntry Entry { get; }

        public string SendTime => Entry.SendTime.ToString("HH:mm:ss.fff");

        public string Purpose => Entry.Purpose;

        public string Method => Entry.Method;

        public string HttpStatus => Entry.HttpStatusCode?.ToString() ?? "-";

        public string MesStatus => string.IsNullOrWhiteSpace(Entry.MesStatus) ? "-" : Entry.MesStatus;

        public string Result { get; }

        public string Duration => Entry.DurationMilliseconds.ToString();

    }

    private sealed class ExceptionLogRow
    {
        public ExceptionLogRow(ProgramExceptionLogEntry entry, string category)
        {
            Entry = entry;
            Category = category;
        }

        public ProgramExceptionLogEntry Entry { get; }

        public string OccurredTime => Entry.OccurredTime.ToString("HH:mm:ss.fff");

        public string Category { get; }

        public string Severity => Entry.Severity;

        public string ExceptionType => GetShortTypeName(Entry.ExceptionType);

        public string Message => Entry.Message;

        public string Source => Entry.Source;

        public string SourceLocation
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Entry.SourceFilePath))
                {
                    var fileName = Path.GetFileName(Entry.SourceFilePath);
                    return Entry.SourceLineNumber > 0
                        ? $"{fileName}:{Entry.SourceLineNumber}"
                        : fileName;
                }

                return string.IsNullOrWhiteSpace(Entry.SourceMemberName)
                    ? "-"
                    : Entry.SourceMemberName;
            }
        }

        private static string GetShortTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "-";
            }

            var lastDotIndex = typeName.LastIndexOf('.');
            return lastDotIndex >= 0 && lastDotIndex < typeName.Length - 1
                ? typeName[(lastDotIndex + 1)..]
                : typeName;
        }
    }
}

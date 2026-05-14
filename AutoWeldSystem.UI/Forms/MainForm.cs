using System.Drawing;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWeldSystem.UI.Forms;

public partial class MainForm : BaseWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISysUserService _userService;
    private readonly ILocalizationService _localizer;
    private readonly PermissionUiBinder _permissionUiBinder;
    private readonly Dictionary<string, UserControl> _viewCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PageDefinition> _allPages;
    private readonly List<PageDefinition> _visiblePages = new();

    public MainForm(
        IServiceProvider serviceProvider,
        ISysUserService userService,
        ILocalizationService localizer,
        PermissionUiBinder permissionUiBinder)
    {
        _serviceProvider = serviceProvider;
        _userService = userService;
        _localizer = localizer;
        _permissionUiBinder = permissionUiBinder;

        InitializeComponent();

        _allPages = BuildPages();
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;

        InitializeNavigation();
    }

    /// <summary>
    /// 语言变化时，导航标题和空页面提示也需要同步刷新。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        RebuildLocalizedPages();
    }

    private void segmented1_SelectIndexChanged(object sender, AntdUI.IntEventArgs e)
    {
        EnsureViewLoaded(e.Value);
        DisplayView(e.Value);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
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
            BeginInvoke(InitializeNavigation);
            return;
        }

        InitializeNavigation();
    }

    /// <summary>
    /// 页面标题是运行时代码生成的，所以语言切换后要重新构建。
    /// </summary>
    private void RebuildLocalizedPages()
    {
        _allPages.Clear();
        _allPages.AddRange(BuildPages());
        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        var previousPermissionCode = GetCurrentPagePermissionCode();

        segmented1.Items.Clear();
        _visiblePages.Clear();
        _visiblePages.AddRange(_allPages.Where(page => _userService.HasPermission(page.PermissionCode)));

        if (_visiblePages.Count == 0)
        {
            ShowEmptyPermissionPage();
            return;
        }

        foreach (var page in _visiblePages)
        {
            segmented1.Items.Add(new AntdUI.SegmentedItem { Text = page.Title });
        }

        var targetIndex = 0;
        if (!string.IsNullOrWhiteSpace(previousPermissionCode))
        {
            var previousIndex = _visiblePages.FindIndex(page =>
                string.Equals(page.PermissionCode, previousPermissionCode, StringComparison.OrdinalIgnoreCase));

            if (previousIndex >= 0)
            {
                targetIndex = previousIndex;
            }
        }

        segmented1.SelectIndex = targetIndex;
        EnsureViewLoaded(targetIndex);
        DisplayView(targetIndex);
    }

    private void EnsureViewLoaded(int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= _visiblePages.Count)
        {
            return;
        }

        var page = _visiblePages[viewIndex];
        if (_viewCache.TryGetValue(page.PermissionCode, out var cachedView))
        {
            _permissionUiBinder.Apply(cachedView);
            return;
        }

        var view = page.Factory();
        view.Dock = DockStyle.Fill;
        _permissionUiBinder.Apply(view);
        _viewCache[page.PermissionCode] = view;
    }

    private void DisplayView(int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= _visiblePages.Count)
        {
            return;
        }

        var page = _visiblePages[viewIndex];
        if (!_viewCache.TryGetValue(page.PermissionCode, out var view))
        {
            return;
        }

        pnlContent.Controls.Clear();
        pnlContent.Tag = page.PermissionCode;
        pnlContent.Controls.Add(view);
    }

    private string? GetCurrentPagePermissionCode()
    {
        return pnlContent.Tag as string;
    }

    private void ShowEmptyPermissionPage()
    {
        pnlContent.Controls.Clear();
        pnlContent.Tag = null;
        pnlContent.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = _localizer.GetString(TextKeys.Main.EmptyPermissionPage)
        });
    }

    /// <summary>
    /// 页面定义统一集中在这里，后续新增页面时改动点更少。
    /// </summary>
    private List<PageDefinition> BuildPages()
    {
        return new List<PageDefinition>
        {
            new(_localizer.GetString(TextKeys.Main.NavMonitor), PermissionCodes.Pages.Monitor, () => _serviceProvider.GetRequiredService<MonitorView>()),
            new(_localizer.GetString(TextKeys.Main.NavDataManage), PermissionCodes.Pages.DataManage, () => _serviceProvider.GetRequiredService<DataManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavUserManage), PermissionCodes.Pages.UserManage, () => _serviceProvider.GetRequiredService<UserManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavProgramManage), PermissionCodes.Pages.ProgramManage, () => _serviceProvider.GetRequiredService<ProgramManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavLogManage), PermissionCodes.Pages.LogManage, () => _serviceProvider.GetRequiredService<LogManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavStateManage), PermissionCodes.Pages.StateManage, () => _serviceProvider.GetRequiredService<StateManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavSystemSetting), PermissionCodes.Pages.SystemSetting, () => _serviceProvider.GetRequiredService<SystemSettingView>()),
            new(_localizer.GetString(TextKeys.Main.NavAddressManage), PermissionCodes.Pages.AddressManage, () => _serviceProvider.GetRequiredService<AddressManageView>())
        };
    }

    private sealed record PageDefinition(string Title, string PermissionCode, Func<UserControl> Factory);
}

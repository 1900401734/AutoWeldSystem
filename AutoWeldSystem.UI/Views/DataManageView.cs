using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 数据管理页。
/// </summary>
public partial class DataManageView : BaseView
{
    public DataManageView()
    {
        InitializeComponent();
        TableStyleHelper.ApplyAntdTable(table1);
    }
}

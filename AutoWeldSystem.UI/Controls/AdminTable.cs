
using AntdUI;
using AutoWeldSystem.Services.Controls;
using AutoWeldSystem.Services.Helper;
using AutoWeldSystem.UI.Components;
using AutoWeldSystem.UI.Extensions;
using System.ComponentModel;
using System.Reflection;

namespace AutoWeldSystem.UI.Controls;

public partial class AdminTable : UserControl
{
    #region 私有属性

    private object selectedItem;

    #endregion

    #region 公有属性

    /// <summary>
    /// Table实例
    /// </summary>
    [Description("Table实例")]
    public Table Table => this.table1;

    /// <summary>
    /// 数据类型
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Type ItemType { get; private set; }

    /// <summary>
    /// 分页设置
    /// </summary>
    [Description("分页设置")]
    [Category("外观")]
    public TableQueryOption TableQueryOption { get; set; } = new TableQueryOption();

    /// <summary>
    /// 查询数据方法
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<TableQueryOption, Task<object>>? QueryData { get; set; }

    /// <summary>
    /// 数据数组
    /// </summary>
    [Browsable(false)]
    [Description("数据数组")]
    [Category("数据")]
    [DefaultValue(null)]
    public object? DataSource { get => this.table1.DataSource; set => this.table1.DataSource = value; }

    /// <summary>
    /// 单击时发生
    /// </summary>
    [Description("单击时发生")]
    [Category("行为")]
    public event ClickEventHandler? CellClick;

    /// <summary>
    /// 单击时发生
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<int, int, Task> CellClickChanged;

    /// <summary>
    /// 新增按钮点击事件
    /// </summary>
    [Description("新增按钮点击事件")]
    [Category("行为")]
    public event EventHandler<CancelEventArgs> AddButtonClick;

    /// <summary>
    /// 保存数据事件
    /// </summary>
    [Description("保存数据事件")]
    [Category("功能")]
    public event EventHandler<AdminTableItemEventArgs> SaveItemData;

    /// <summary>
    /// 保存数据后事件
    /// </summary>
    [Description("保存数据后事件")]
    [Category("功能")]
    public event EventHandler<AdminTableItemEventArgs> SaveItemDataChanged;

    private ButtonCollection topButtons;
    /// <summary>
    /// 功能区域按钮
    /// </summary>
    [Description("功能区域按钮")]
    [Category("功能")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ButtonCollection TopButtons => topButtons ??= new ButtonCollection(this);

    /// <summary>
    /// 功能区域按钮
    /// </summary>
    /// <param name="owner"></param>
    public class ButtonCollection(AdminTable owner) : List<AntdUI.Button>
    {
        public new void Add(AntdUI.Button item)
        {
            owner.panel2.Controls.Add(item);
            base.Add(item);
        }
        public void AddRange(AntdUI.Button[] values)
        {
            owner.panel2.Controls.AddRange(values);
            base.AddRange(values);
        }
    }

    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    [Description("刷新按钮点击事件")]
    [Category("功能")]
    public event EventHandler<string>? QueryClick;

    #endregion

    #region 参数修改属性

    [Description("是否显示删除按钮")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowDeleteButton { get => btnDelete.Visible; set => btnDelete.Visible = value; }

    [Description("是否显示新增按钮")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowAddButton { get => btnAdd.Visible; set => btnAdd.Visible = value; }

    [Description("是否显示编辑按钮")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowEditButton { get => btnEdit.Visible; set => btnEdit.Visible = value; }

    [Description("是否显示搜索框")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowQuery { get => inputQuery.Visible; set => inputQuery.Visible = value; }

    [Description("是否显示导出按钮")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowEcportExcel { get => btnExport.Visible; set => btnExport.Visible = value; }

    /// <summary>
    /// 是否显示外框线
    /// </summary>
    [Description("是否显示外框线")]
    [Category("Wen")]
    [DefaultValue(true)]
    public bool IsShowBorderLine { get; set; } = true;

    [Description("是否按照ID倒序")]
    [Category("功能")]
    [DefaultValue(false)]
    public bool IsIdDesc { get; set; } = false;

    #endregion

    public AdminTable()
    {
        InitializeComponent();
    }

    #region 公用方法

    /// <summary>
    /// 设置数据类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void SetItemType<T>() => ItemType = typeof(T);
    /// <summary>
    /// 设置数据类型
    /// </summary>
    /// <param name="type"></param>
    public void SetItemType(Type type) => ItemType = type;

    /// <summary>
    /// 批量添加所有列 注意 属性上需要 标记 [Col]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void SetColumns<T>() where T : class
    {
        var builder = this.SetColumn<T>();
        this.ItemType ??= typeof(T);
        var properties = this.ItemType.GetProperties();

        foreach (var item in properties)
        {
            builder.Add(item);
        }
    }

    /// <summary>
    /// 设置列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <param name="action"></param>
    /// <param name="isFixed"></param>
    /// <returns></returns>
    public TableColumnBuilder<T> SetColumn<T>() where T : class
    {
        this.ItemType ??= typeof(T);
        var builder = new TableColumnBuilder<T>(this.table1);
        return builder;
    }

    /// <summary>
    /// 获取选中行数据
    /// </summary>
    /// <returns></returns>
    public object? GetSelectedItem() => selectedItem;

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="text"></param>
    public void Query(string text = "")
    {
        OnQuery(text);
    }

    #endregion

    #region 私有方法

    private void OnQuery(string text)
    {
        this.TableQueryOption.QueryText = text;

        if (QueryData != null)
        {
            Task.Run(async () =>
            {
                var data = QueryData.Invoke(this.TableQueryOption);
                if (data == null)
                    return;

                await Task.Run(async () => this.table1.DataSource = await data);

                this.pagination1.Total = this.TableQueryOption.TotalCount;
                this.pagination1.PageSize = this.TableQueryOption.PageSize;
                this.pagination1.Current = this.TableQueryOption.PageNumber;

                if (this.TableQueryOption.IsExpand)
                {
                    this.table1.ExpandAll();
                }
            });
        }
        else
        {
            DbQueryData(text);
        }
    }

    private void DbQueryData(string value)
    {
        try
        {
            var sql = fsql.Select<object>().AsType(ItemType);
            if (!string.IsNullOrEmpty(value))
            {
                var fi = new DynamicFilterInfo() { Filters = [], Logic = DynamicFilterLogic.Or };

                foreach (var prop in ItemType.GetProperties())
                {
                    if (prop.GetCustomAttribute<ColAttribute>() is ColAttribute { IsFilter: true })
                    {
                        var sub = new DynamicFilterInfo()
                        {
                            Field = prop.Name,
                            Value = value,
                            Operator = DynamicFilterOperator.Contains,
                        };
                        fi.Filters.Add(sub);
                    }
                }
                sql.WhereDynamicFilter(fi);
            }


            if ((typeof(IEntityBase).IsAssignableFrom(ItemType)
                || ItemType.GetProperty("ID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null)
                && IsIdDesc)
            {
                sql.OrderByPropertyName("ID", false);
            }

            var pageNumber = this.pagination1.Current;
            var pageSize = this.pagination1.PageSize;
            var count = sql.Count();
            var data = sql.Page(pageNumber, pageSize).ToList();
            this.pagination1.Total = (int)count;
            //var bls = new BindingList<object>(data);
            if (bindingSource == null)
            {
                bindingSource = [];
                bindingSource.ListChanged += BindingSource_ListChanged;
            }
            bindingSource.DataSource = data;
            //bindingSource.Dispose();
            //bindingSource ??= [];
            //bindingSource.DataSource = bls;
            //bindingSource.ListChanged += BindingSource_ListChanged;
            this.DataSource = bindingSource;
        }
        catch { }
    }

    private void BindingSource_ListChanged(object? sender, ListChangedEventArgs e)
    {
        this.table1.Refresh();
    }

    private BindingSource bindingSource;
    #endregion

    #region 事件方法


    private void Button1_Click(object sender, EventArgs e)
    {
        if (this.DataSource == null)
            return;

        var dia = new SaveFileDialog()
        {
            Filter = "(Excel文件)|*.xlsx"
        };
        if (dia.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        MiniExcelLibs.MiniExcel.SaveAs(dia.FileName, this.DataSource, overwriteFile: true);
    }

    private void Button2_Click(object sender, EventArgs e)
    {
        if (ItemType is null) return;

        if (AddButtonClick != null)
        {
            var cancel = new CancelEventArgs();
            AddButtonClick?.Invoke(this, cancel);
            if (cancel.Cancel)
                return;
        }

        var item = Activator.CreateInstance(ItemType);

        if (item == null)
            return;

        var fm = new TableEditForm(item, ItemChangedType.Add);
        fm.OkButtonClick += (s, e) =>
        {
            SaveData(e, ItemChangedType.Add);
        };
        fm.ShowDialog();
    }

    private void Button3_Click(object sender, EventArgs e)
    {
        var item = GetSelectedItem();
        if (ItemType is null || item == null) return;

        var fm = new TableEditForm(item, ItemChangedType.Update);
        fm.OkButtonClick += (s, e) =>
        {
            SaveData(e, ItemChangedType.Update);
        };
        fm.ShowDialog();
    }

    private void SaveData(object item, ItemChangedType changedType)
    {
        var e = new AdminTableItemEventArgs(item)
        {
            IsAdd = changedType == ItemChangedType.Add,
            IsUpdate = changedType == ItemChangedType.Update,
        };
        SaveItemData?.Invoke(this, e);

        if (!e.Cancel)
        {
            if (changedType == ItemChangedType.Add)
            {
                fsql.Insert<object>().AsType(item.GetType()).AppendData(item).ExecuteAffrows();
            }
            else
            {
                fsql.Update<object>().AsType(item.GetType()).SetSource(item).ExecuteAffrows();
            }
        }
        SaveItemDataChanged?.Invoke(this, e);
        OnQuery("");
    }

    private void DeleteData(object item)
    {
        var e = new AdminTableItemEventArgs(item)
        {
            IsDelete = true
        };
        SaveItemData?.Invoke(this, e);

        if (!e.Cancel)
        {
            fsql.Delete<object>().AsType(item.GetType()).Where(item).ExecuteAffrows();
        }
        SaveItemDataChanged?.Invoke(this, e);
        OnQuery("");
    }

    private void Button4_Click(object sender, EventArgs e)
    {
        var item = GetSelectedItem();
        if (ItemType is null || item == null) return;

        if (Msgbox.ShowAsk("确定删除") != DialogResult.OK)
            return;

        DeleteData(item);
    }

    #endregion

    #region 事件委托功能

    private void Table1_Paint(object? sender, PaintEventArgs e)
    {
        if (IsShowBorderLine)
        {
            e.Graphics.High().DrawBorderRound(this.table1, 2);
        }
    }

    private void Table1_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        selectedItem = e.Record;
        CellClick?.Invoke(sender, e);
        CellClickChanged?.Invoke(e.RowIndex, e.ColumnIndex);
    }

    private void Pagination1_ValueChanged(object sender, AntdUI.PagePageEventArgs e)
    {
        TableQueryOption.PageNumber = e.Current;
        TableQueryOption.PageSize = e.PageSize;
        TableQueryOption.TotalCount = e.Total;
        OnQuery(TableQueryOption.QueryText);
    }

    private void InputQuery1_QueryClick(object? sender, string e)
    {
        QueryClick?.Invoke(this, e);
        OnQuery(e);
    }

    #endregion

    public class AdminTableItemEventArgs(object? item) : EventArgs
    {
        public bool Cancel { get; set; }
        public object? Item { get; set; } = item;
        public bool IsAdd { get; set; }
        public bool IsUpdate { get; set; }
        public bool IsDelete { get; set; }
    }
}

namespace AutoWeldSystem.UI.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        AntdUI.SegmentedItem segmentedItem1 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem2 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem3 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem4 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem5 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem6 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem7 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem8 = new AntdUI.SegmentedItem();
        segmented1 = new AntdUI.Segmented();
        pnlContent = new AntdUI.Panel();
        pageHeader1 = new AntdUI.PageHeader();
        pageHeader2 = new AntdUI.PageHeader();
        tableLayoutPanel1 = new TableLayoutPanel();
        pnlContent.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
        SuspendLayout();
        // 
        // segmented1
        // 
        segmented1.BackColor = SystemColors.Control;
        resources.ApplyResources(segmented1, "segmented1");
        segmented1.Full = true;
        segmentedItem1.LocalizationText = "MonitorView";
        segmentedItem1.Text = "生产监控";
        segmentedItem2.Text = "数据管理";
        segmentedItem3.Text = "用户管理";
        segmentedItem4.Text = "程序管理";
        segmentedItem5.Text = "日志管理";
        segmentedItem6.Text = "上传状态";
        segmentedItem7.Text = "系统设置";
        segmentedItem8.Text = "地址维护";
        segmented1.Items.Add(segmentedItem1);
        segmented1.Items.Add(segmentedItem2);
        segmented1.Items.Add(segmentedItem3);
        segmented1.Items.Add(segmentedItem4);
        segmented1.Items.Add(segmentedItem5);
        segmented1.Items.Add(segmentedItem6);
        segmented1.Items.Add(segmentedItem7);
        segmented1.Items.Add(segmentedItem8);
        segmented1.Name = "segmented1";
        segmented1.SelectIndexChanged += segmented1_SelectIndexChanged;
        // 
        // pnlContent
        // 
        pnlContent.Back = SystemColors.Control;
        pnlContent.BackColor = SystemColors.ControlLightLight;
        pnlContent.Controls.Add(pageHeader1);
        resources.ApplyResources(pnlContent, "pnlContent");
        pnlContent.Name = "pnlContent";
        // 
        // pageHeader1
        // 
        resources.ApplyResources(pageHeader1, "pageHeader1");
        pageHeader1.Name = "pageHeader1";
        pageHeader1.ShowButton = true;
        pageHeader1.ShowIcon = true;
        // 
        // pageHeader2
        // 
        resources.ApplyResources(pageHeader2, "pageHeader2");
        pageHeader2.Name = "pageHeader2";
        pageHeader2.ShowButton = true;
        pageHeader2.ShowIcon = true;
        // 
        // tableLayoutPanel1
        // 
        resources.ApplyResources(tableLayoutPanel1, "tableLayoutPanel1");
        tableLayoutPanel1.Controls.Add(pageHeader2, 0, 0);
        tableLayoutPanel1.Controls.Add(pnlContent, 0, 1);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        // 
        // MainForm
        // 
        resources.ApplyResources(this, "$this");
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(tableLayoutPanel1);
        Controls.Add(segmented1);
        Name = "MainForm";
        WindowState = FormWindowState.Maximized;
        pnlContent.ResumeLayout(false);
        tableLayoutPanel1.ResumeLayout(false);
        ResumeLayout(false);
    }
    private AntdUI.Segmented segmented1;
    private AntdUI.Panel pnlContent;
    private AntdUI.PageHeader pageHeader1;
    private AntdUI.PageHeader pageHeader2;
    private TableLayoutPanel tableLayoutPanel1;
}

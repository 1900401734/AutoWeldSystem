# 生产监控工单设置入口设计

## 目标

将产品工号筛选程序和双工单开关集中到生产监控工单信息区，减少操作员在系统设置页和生产页之间来回切换。

## 范围

- 保留 `AppSettings.UseProductNumberFilter` 配置字段和在线/离线程序筛选业务规则，仅移动编辑入口。
- 使用短复选框文案，完整业务语义通过 `AntdUI.TooltipComponent` 悬停提示展示。
- 单工位禁用并清除 `EnableDualWorkOrder`，切回双工位后保持关闭，需用户重新启用。
- 从系统设置页移除 `chkUseProductNumberFilter`。

## 设计选择

- 产品工号筛选同时影响在线和离线开工；在线路径继续复用 `WeldTaskService.LoadProgramsAsync` 中的统一配置规则。
- ToolTip 组件由 MonitorView Designer 管理生命周期，运行时在本地化方法中更新文字。
- 单工位采用禁用而非隐藏，保留功能可见性并避免动态调整布局。

## 验证

控制台回归测试和解决方案构建分别执行；真实 PLC、MES 和人工 WinForms 悬停/点击验证需要现场确认。

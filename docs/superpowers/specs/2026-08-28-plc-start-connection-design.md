# PLC 连接前置条件设计

## 目标

PLC 未连接时禁止在线和离线开工，避免没有 PLC 通讯时创建无法执行的生产任务。

## 范围

- 在线开工和离线开工均要求本次操作涉及的全部 PLC 工位处于 `IsConnected == true`。
- 单工位检查当前工位；双工位共用工单检查两个工位；双工位独立工单检查当前目标工位。
- `Connecting`、`Reconnecting`、`Unverified` 和其他非已连接状态均不可开工。
- 已有在线或离线任务不因 PLC 断线被按钮规则阻止完工；完工数量读取仍优先使用 PLC，读取失败时由 `EnableFinishExpQtyPrompt` 决定是否人工补录，该开关默认关闭。

## 设计选择

- Core 的 `MonitorReportButtonRules` 负责按钮状态决策，MonitorView 在线/离线开工入口再做一次实时检查，防止按钮状态与点击时状态之间产生竞态。
- 复用 `monitor.message.plc_disconnected`，不新增重复提示资源。
- 不将 PLC 依赖下沉到 `WeldTaskService`，避免扩大服务接口和构造依赖；当前所有生产开工入口均由 MonitorView 统一编排。

## 验证

控制台回归测试覆盖 PLC 断线开工阻止和已有任务完工放行；解决方案使用备用输出目录构建。真实 PLC、MES、MySQL 和人工 WinForms 验证需现场确认。

# 方案明细五通道拆分设计

日期：2026-09-01

## 目标

方案明细的配置项由四个改为五个互相独立的通道，术语与实际去向一致：

- 实时预览（`treeSchemeDetails` 勾选）：决定哪些数据显示在 `MonitorView` 的 `dgvPreview1/2`。
- 本地保存：决定哪些数据保存在本地历史。
- 转发看板：决定哪些数据转发至中心服务器看板（本次新增通道）。
- 写入报表：决定哪些数据写入完工上报时上传的 xlsx 报表。
- 过程参数：决定哪些数据通过接口上传。

改动前"保存历史"同时驱动本地历史和中心看板转发，现场无法只转发不落本地、也无法只落本地不转发；"启用采集"既是实时预览开关，又被程序判定当作参与项选择器，且是其余输出的保存期前置校验。

## 设计

### 数据模型

`BizSchemeDetail` 新增 `ForwardActual`、`ForwardUpper`、`ForwardLower`、`ForwardResult` 四个布尔列，列说明为"实际值/上限/下限/结果转发中心看板"。

其余字段名与数据库列名一律不变。`Mes*` 与 `MesFieldName` 保留原名：CodeFirst 改列名等于删旧列建新列，会清空现场既有配置；且上传链路的字段名仍是 MES 协议原文。实体上补注释说明 `Mes*` 对应界面"过程参数"。

不写一次性回填代码。升级后 `Forward*` 全为 false，现场需在方案明细中重新勾选"转发看板"。

### 通道语义

| 编辑入口 | 字段 | 职责 |
|---|---|---|
| `treeSchemeDetails` 勾选 | `Enable*` | 实时预览显示 |
| 表格列 1 | `Save*` | 本地保存 |
| 表格列 2 | `Forward*` | 转发看板 |
| 表格列 3 | `Report*` | 写入报表 |
| 表格列 4 | `Mes*` | 过程参数 |

五通道互相独立，实时预览不再是其他通道的前置条件。表格中的"启用采集"列删除，`treeSchemeDetails` 成为实时预览的唯一编辑入口，消除同一字段两个编辑器互相改写的问题。

### SchemeDetailRoleRules

- 改名（仅 C# 标识符，不动数据库列名）：`IsCollectEnabled`/`SetCollectEnabled`/`HasAnyCollectEnabled` → `IsPreviewEnabled`/`SetPreviewEnabled`/`HasAnyPreviewEnabled`。
- 新增 `IsForwardEnabled`/`SetForwardEnabled` 与 `ShouldForwardCenterRole`。
- `ShouldPersistRole` 纳入 `IsForwardEnabled`：只勾转发的角色也必须写入 `RawDataJson`，中心转发从本地记录读值。
- `HasAnyConfiguredRole` 与 `ClearRole` 纳入 `Forward*`。
- 新增 `ShouldEvaluateProgramRole`（等价于 `ShouldPersistRole`），供程序判定各处调用，沿用文件已有的一行别名风格。
- 删除"实时预览开关不能作为历史、报表或 MES 数据源前置条件"这句与实际代码相反的注释，改为说明五通道独立。

### 程序判定参与项

整件检测程序判定模式下，"哪些测试项参与合格判定"改由输出通道决定：实际值配了表达式且勾选了本地保存、转发看板、写入报表或过程参数中任一项的测试项参与判定。

- `ProductCycleCollectionService` 的 `participatingItems` 改用 `ShouldEvaluateProgramRole`。
- `ProductCycleCollectionService.ReadTestItemValuesAsync` 删除 `|| (useProgramResult && EnableActual)` 兜底条件：新口径下参与判定必然蕴含至少一个输出通道，`ShouldReadProductRole` 已保证读值。
- `ProductRealtimePreviewService` 的 A/B 值定义改用同一口径。
- `ProductRealtimePreviewService` 的预览面结果判定按参与判定的 `ItemId` 集合过滤，复用行 DTO 已有的 `ItemId`，不新增字段。

采集落库与实时预览两条链路必须同源，否则会出现预览显示 OK、落库为 NG 的不一致。

### 实时预览读值

实时预览为"不显示但参与判定"的测试项读取实际值，保证预览判定与落库判定同源：

- 方案项过滤放宽为 `HasAnyPreviewEnabled || ShouldEvaluateProgramRole(Actual)`。这类项四个 `Enable*` 全为 false，`MonitorView.ResolveWeldPreviewItems` 现有过滤会自动将其排除在显示外。
- 实际值读取条件放宽为 `IsPreviewEnabled(Actual) || ShouldEvaluateProgramRole(Actual)`。

放宽只作用于实际值：上限、下限、结果的预览值只可能被显示，而显示必须 `IsPreviewEnabled`，为它们放宽只会增加 PLC 读请求而不改变任何可观察输出。

A/B 侧值收集去掉 `EnableActual` 过滤：值定义已按新口径过滤，未进入定义的测试项不会被查表。

代价：取消实时预览勾选不再能减少 PLC 读取量。实时预览的职责是决定显示范围，不承载读取量控制。

### 中心转发

- `BuildSavedFieldDefinitions` 的 `RawDataJson` 值过滤由 `Save*` 改为 `Forward*`，与动态列定义同源，避免列与值对不上。
- 删除私有包装 `ShouldForwardSavedRole`：`ShouldPersistRole && IsSaveEnabled` 在通道拆分后是冗余条件，各处直接调用 `IsForwardEnabled`，约束说明移到规则方法注释。
- 整件检测 A/B 模式对非实际值角色的限制保持现状，`Forward*` 不纳入该校验。中心看板动态列直接透传 `RawDataJson`、不做 A/B 聚合，上下限是常量、结果是逐面字符串，透传无歧义。

### 配置界面

- 删除"启用采集"列、`SchemeDetailRoleTableRow.Enabled` 及其 `GetEnabled`/`SetEnabled`，删除 `TextKeys.Address.ColumnDetailEnabled` 与两份 resx 条目。
- 新增"转发看板"列，位置在"本地保存"之后。
- 删除 `ValidateSchemeDetailRoleOutputs`（采集前置校验）及其调用。
- 保存校验改用 `HasAnyConfiguredRole`，与 `TestSchemeConfigService` 既有口径统一，文案改为"方案明细至少需要勾选实时预览、本地保存、转发看板、写入报表或过程参数中的一项。"。只做实时预览、不落任何库是解耦后合法的现场用法。
- `ValidateMesFieldName` 去掉 `collectEnabled` 形参：解耦后"勾过程参数但未勾实时预览"仍必须校验字段名，保留该形参会漏检。
- 重写方案明细提示文案，原文案"保存历史同时决定本地历史展示和中心服务器转发"在拆分后不成立。
- 删除 `UploadTaskService` 中未被调用的 `HasAnyEnabledRole`；`MainForm`、`MonitorView`、`ProductRealtimePreviewService` 三处同名私有方法改名 `HasAnyPreviewEnabled`，使调用点自明为预览门控。

### 本地化

中英两份 resx 同步：`保存历史`→`本地保存`、新增`转发看板`、`上传 MES`→`过程参数`、`MES字段名`→`过程参数字段名`。

## 验收

- 方案明细右侧表格显示"本地保存、转发看板、写入报表、过程参数"四个复选列，不再有"启用采集"列；实时预览只能在左侧树勾选。
- 只勾"转发看板"的角色进入中心看板动态列且值随之下发，不出现在本地历史。
- 只勾"本地保存"的角色进入本地历史，不进入中心看板。
- 未勾任何通道的方案明细保存时报错并给出五通道提示文案；勾选任一通道即可保存。
- 勾"过程参数"但未勾实时预览时，仍校验过程参数字段名不能为空。
- 整件检测程序判定模式下，只勾实时预览的测试项不参与合格判定；勾了任一输出通道的测试项参与判定，且实时预览与采集落库的判定结果一致。
- 控制台回归测试通过，解决方案构建通过，`git diff --check` 通过。
- 未执行真实 PLC、MES、中心服务器、MySQL 及人工 UI/DPI 验证。

## 测试

新增四个纯规则回归用例：转发看板独立于本地保存；程序判定参与项按输出通道筛选（含只勾预览不参与的反向断言）；中心列定义与值过滤同源；五通道任一勾选即通过保存校验。

改写三个既有用例：`CollectionDoesNotImplyOutput`、`SaveHistoryControlsProductHistoryVisibility`、`SchemeOutputRolesAreIndependentFromRealtimePreview`。

全部不依赖真实 PLC、MES、MySQL 或 UI 自动化，不新增源码文本匹配类断言。

## 边界

- 不改 `Mes*`、`Save*` 的属性名与数据库列名，不改 `MesFieldName` 相关字段名。
- 不写一次性数据回填代码，不留读取旧字段的兼容分支；现场手动重新勾选"转发看板"。
- 不把 `Forward*` 纳入整件检测 A/B 模式的非实际值角色限制。
- 地址预览列表仍按实时预览筛选显示地址，本次不改：解耦后只配输出通道的角色会被真实读取但不出现在该排查列表中，属已知遗留。
- README 不维护逐提交变更流水账，只在功能说明中写明两条现场影响：转发看板默认关闭需重新勾选，实时预览不再是其他通道前置且程序判定参与项改由输出通道决定。

## 版本

`Directory.Build.props` 由 2.18.0 升至 2.19.0（新增通道属功能新增），README 顶部当前版本、版本管理示例与发布标签示例同步。

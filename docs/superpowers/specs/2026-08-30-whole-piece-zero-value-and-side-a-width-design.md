# 整件检测零值误判与宽度按面取值设计

## 背景

整件检测设备（`WholePieceCheck`，四面工艺）在“检测结果来源 = 程序计算”模式下暴露两个现场问题。

**问题一：视觉检测失败回传的 `0` 被判成 OK。**

现场约定：某一面某一测试项检测失败时，视觉系统回传数值 `0`，检测成功才回传实际测量值。而 `WholePieceProgramResultRules.EvaluateFace` 的判定只有上限一侧——`actual > maximum` 才判 NG。`0` 必然小于任何有效上限，于是“没测到”被判成合格。高度、宽度是物理尺寸，真实值不可能为 `0`，出现 `0` 一定是检测失败；但对称度的真实值有可能就是 `0`，不能一概而论。

**问题二：`宽度` 被硬编码成四面取最大值。**

`WholePieceAbAggregationRules.IsProductMaximumItem` 把“高度”和“宽度”一起当作四面最大值项，A/B 两行写入同一个数值。而现场有一款产品 A 面与 B 面的宽度本就不同，取四面最大值等于用 B 面的宽度去和 A 面的上限比较，判定和上传数据都没有意义。协议侧只要求上传 A 面宽度，B 面宽度不需要进入过程参数和报表。

**衍生问题：单面结果与产品结果口径不同造成歧义。**

面结果按单面数据判定，产品结果按 A/B 合并值判定，两者依据不同。操作工在逐面视图看到某一面的结果，容易与合并视图的产品结果对不上号。

## 目标

- 程序计算模式下，A/B 合并后的 `高度`、`宽度` 仍为 `0` 或负值时判 NG，并在合并视图标红对应列。
- `宽度` 改为只取 A 面（面2、面4）最大值；B 行留空，不参与判定、MES 上传和报表。
- `对称度` 等配对项的默认聚合方式由平均值改为最大值。
- 新增“整件检测逐面结果显示”开关，允许在逐面模式下隐藏“面结果”列，且可在生产监控页实时切换。
- 版本升级到 `2.12.0`，同步 `README.md`。

## 非目标

- 不改变面级判定 `EvaluateFace` 的规则，单面回传 `0` 不判成面 NG。
- 不做 A/B 面归属配置：A=面2+面4、B=面1+面3 由机台机械结构固定，继续硬编码。
- 不为 `宽度` 引入 A/B 两套上限配置，因此不改动 MES 下发的程序内容 JSON 结构。
- 不为 `对称度` 增加零值检查，其真实值可能为 `0`。
- 不做历史数据的旧口径兼容，不在聚合规则中维护两套口径和切换点。
- 不改动 PLC 判定模式下的结果来源；PLC 给出的面结果和产品结果继续直接采用。

## 零值判定：只作用于合并值层

零值检查放在 `WholePieceProgramResultRules.EvaluateAggregated`，即 A/B 合并值这一层，`EvaluateFace` 保持原样。

这样划分的依据是数据的传导路径：面级结果会写入 `record.TestResult`，再决定 A/B 行的 `Result`（`ResolveProductResult([面2, 面4])`），而这个 `Result` 要上传 MES 并进入报表。若在面级判零值，一次视觉失败就会把该面判成不合格，并污染上传结果——但检测失败不等于产品不合格。产品结果本来就由合并值决定（采集路径 `ResolveProgramProductResult`、预览路径 `ResolveRealtimeProgramProductResult` 都以合并值优先），单面回传的 `0` 会被其他面的有效值在取最大值时自然覆盖；只有参与聚合的面全部失败，合并值才会仍是 `0`，这时才判 NG。

判定条件用 `<= 0` 而非 `== 0`：负值同样不是有效尺寸。

适用范围限定为 `IsProductLevelItem`（高度、宽度）。`对称度` 的真实值能否为 `0` 尚未与客户确认，纳入检查会把合格品判成 NG；误判 NG 在现场比漏判 OK 更难收场。待确认后若需扩大范围，是一行改动。

## 宽度按面取值

`IsProductMaximumItem` 的语义已经分叉，拆成三个谓词：

| 谓词 | 匹配项 | 含义 |
| --- | --- | --- |
| `IsFourSideMaximumItem` | 高度 | 四面最大值，A/B 两行同值，报表可按产品跨行合并单元格 |
| `IsSideAOnlyItem` | 宽度 | A 行取面2、面4 最大值；B 行留空，报表不可跨行合并 |
| `IsProductLevelItem` | 高度、宽度 | 监控合并视图只占一列，且适用零值检查 |

拆分是必需的而不是整理：`MergeByProduct` 依赖“A/B 两行同值”这一前提才能跨行合并单元格，宽度 B 行留空后这个前提不再成立，设备端报表和中心端报表列定义都必须只保留高度。

连带处理：

- `EvaluateAggregated` 判定 B 行时跳过 `宽度`。B 行该项是空字符串，不排除会被当成非法数字，导致整次判定失败。
- `WholePieceMergedDisplayRules.BuildValues` 中产品级单列改为显式查找 A 行，不再依赖 A/B 行的排列顺序。高度两行同值取谁都对，宽度只有 A 行有值。
- MES 上传链路无需改动：过程参数本来就输出 `SideNo=A/B` 两条数据，B 条的宽度字段值为空字符串，`TestItemUnitFormatRules.FormatValue` 对空值原样返回，不会拼上单位。
- 报表 B 行的宽度显示 `\`。空白容易被读成“采集失败”，斜杠表示“该行不适用”。这个替换只发生在 `ProductionReportFileService` 生成报表行时，聚合结果和 MES 数据仍是空字符串——斜杠是展示层装饰，不进数据流。

## 配对聚合默认值

`AppSettings.PairedAggregationMode` 的代码默认值由 `Average` 改为 `Maximum`。

理由与零值约定同源：取平均会把检测失败回传的 `0` 拉进结果。面2 对称度 0.5、面4 检测失败回传 0，平均值 0.25 比真实值更小，反而更容易判 OK——检测失败把不合格盖住了。取最大值时 `0` 在取 max 时被有效面自然覆盖，两面都失败则合并值为 `0`，由零值检查兜住。

因此不再需要“聚合时剔除 0 值”的特殊处理：最大值模式下它是冗余的，而且会与“对称度真值可能为 0”直接冲突——真值 `0` 会被当成检测失败剔除。

`PairedAggregationModes.Normalize` 的兜底保持 `Average` 不变，避免把现场显式配置的“配对平均值”意外改写。代价是：**已部署数据库中存量的 `Average` 不会被新默认值覆盖，需要在系统设置页手动改一次。**

## 逐面结果显示开关

新增 `AppSettings.EnableWholePieceFaceResultDisplay`（`bool?`，默认 `true` 显示），整条链路复用现有 `EnableWholePieceMergedDisplay` 的实现模式：

- 系统设置页新增复选框，仅在过程参数设备类型为整件检测时可见。
- 生产监控页新增复选框，实时切换并写回设置，`SyncFaceResultDisplayToggle` 反向同步，系统设置页改动经 `SettingsChanged` 事件联动。
- 该复选框只在**逐面模式**下出现：合并视图本身就没有面号和面结果列，此时显示一个不起作用的勾选框只会造成困惑。切换合并显示后需立即刷新它的可见性。
- 关闭时**只隐藏“面结果”列**，面号列和逐面实测值全部保留。
- 非整件检测设备的逐面预览始终显示该列，不受开关影响。
- 纯界面开关：`record.TestResult` 照常计算、入库，并进入历史数据和报表。

## 影响面与口径变化

XLSX 报表在导出时实时聚合，不是导出后固化。因此本次改动生效后重新导出**以前的产品**，数据会与此前已交付的报表不一致：

- 宽度从“四面最大值”变成“A 面最大值”，B 行变成 `\`，且不再跨行合并单元格。
- 对称度按新的默认聚合方式计算。
- 中心端报表为逐面明细口径，其产品级最大值覆盖只对 `MergeByProduct` 的列生效；宽度移出该集合后，中心端改为显示四面各自的原始宽度值。

这是有意为之：旧口径已被认定为错误，为它维护一套按时间或版本分流的兼容逻辑，是本次成本最高且长期负担最重的一块。

## 已知脆弱点

聚合策略靠**测试项中文名精确匹配**：取四面最大值的必须严格命名为“高度”，只取 A 面的必须严格命名为“宽度”。这是延用现有代码已有的约定，本次未引入新的脆弱性，但需要明确记录——现场改名会让这两条策略静默失效并退回普通 A/B 配对聚合，现象是“宽度又按四面算了”，从表象很难追到原因。`README.md` 的测试项配置章节已写明该约束。

## 测试与验收

回归用例 `Whole-piece zero merged value fails product level items` 覆盖：

- 高度合并值为 `0` 判 NG，且失败项包含“高度”。
- 宽度 A 行合并值为 `0` 判 NG，且失败项包含“宽度”。
- 对称度合并值为 `0` 仍判 OK。
- B 行宽度留空时跳过判定，不报“不是合法数字”，不影响产品结果。
- 面级 `EvaluateFace` 对 `0` 仍判 OK，单面视觉失败不会被判成面 NG。
- `PairedAggregationMode` 默认值为 `Maximum`，`EnableWholePieceFaceResultDisplay` 默认显示。

回归用例 `Whole-piece height uses four-side maximum and width uses side A` 覆盖：

- 高度 A/B 两行同为四面最大值。
- 宽度 A 行为面2、面4 的最大值，B 行为空字符串。
- 三个谓词的匹配范围。
- 设备端报表与中心端报表列定义只把高度标记为产品级合并列。

其他验收：

- 运行控制台回归 harness 与使用独立输出目录的解决方案构建。
- 手工确认：逐面模式下勾选与取消“面结果”复选框，面号与逐面数值保留；开启合并显示后该复选框隐藏；非整件检测设备始终显示面结果列。
- 现场确认：MES 过程参数 B 条数据的宽度字段为空字符串且被接口接受；XLSX 报表 B 行宽度显示 `\`。

## 范围

改动文件：

- `AutoWeldSystem.Core/Production/WholePieceAbAggregationRules.cs`
- `AutoWeldSystem.Core/Production/WholePieceProgramResultRules.cs`
- `AutoWeldSystem.Core/Production/WholePieceMergedDisplayRules.cs`
- `AutoWeldSystem.Core/Entities/AppSettings.cs`
- `AutoWeldSystem.Core/Constants/ProductionConstants.cs`、`TextKeys.cs`
- `AutoWeldSystem.Core/Localization/UiText.resx`、`UiText.en.resx`
- `AutoWeldSystem.Services/Production/ProductionReportFileService.cs`
- `AutoWeldSystem.Services/Center/CenterProductForwardingService.cs`
- `AutoWeldSystem.UI/Views/SystemSettingView.cs` 及其 Designer
- `AutoWeldSystem.UI/Views/MonitorView.cs` 及其 Designer
- `AutoWeldSystem.Tests/Program.cs`
- `Directory.Build.props`、`README.md`

不改动：MES 上传服务、程序内容 JSON 结构、PLC 通讯与地址配置、中心服务端报表写入器。

## 实施结果

- 构建：`dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=../artifacts/verify-bin/` 成功，0 警告 0 错误。使用独立输出目录，避免默认输出被运行中的程序锁定。
- 控制台回归 harness：432 项全部 PASS，无失败、无异常。
- `git diff --check` 无空白错误。
- Designer 筛查：未打开设计视图，两个 Designer 文件的改动均为手写新增控件。`tools/designer-diff.py` 因本地工具故障未能执行，改用 `git diff` 人工筛查，确认无设计器重序列化噪音，因此也不需要 `--clean`。

未执行的验证：PLC、MES、MySQL 实机验证与 WinForms 目视确认，均依赖现场设备，本地无法替代；新增设置列依赖 CodeFirst 自动加列，未在测试库验证。

## 合并后需要现场处理

1. **手动改一次设置**：已部署数据库中存量的 `PairedAggregationMode = Average` 不会被新默认值覆盖，需在系统设置页改为“配对最大值”，否则对称度口径不变。
2. **历史报表口径会变**：重新导出以前的产品时，宽度从“四面最大值”变成“A 面最大值”、B 行变 `\`、对称度按新聚合方式计算，与已交付报表不一致。中心端报表改为显示四面各自的原始宽度值。
3. **测试项名不能改**：取四面最大值的必须叫“高度”，只取 A 面的必须叫“宽度”，改名会让策略静默失效。

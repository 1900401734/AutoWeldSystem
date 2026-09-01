# 焊接位移量计算表达式设计

## 背景

点焊设备需要上传「焊接前后位移量」，但 PLC 只提供焊接前和焊接后两个位移量原始值，差值没有对应寄存器。该值可以完全由现有两个测试项计算得出，因此在测试项字典的偏移量表达式里直接兼容减法，不新增字段、不新增配置入口。

目标写法（测试项字典「实际值偏移」列）：

```text
(134-84):S-7_3
```

含义：读字节偏移 134 和 84 两个位移量，相减得到焊接前后位移量。类型 `S`、规则 `7`、小数位 `3` 对两个操作数同时生效。

现状障碍：`PlcOffsetExpression.Parse` 用 `int.TryParse` 解析地址段（`AutoWeldSystem.Core/Plc/PlcOffsetExpression.cs:32-40`），`(134-84)` 既不是数字也不是有效 PLC 绝对地址，直接抛 `FormatException`；`TestSchemeConfigService.cs:303` 在保存时校验，因此该写法目前无法通过界面保存。

## 目标

- 测试项字典的实际值偏移支持「两个偏移量相减」，语义与现有单地址表达式一致。
- 采集、实时预览、地址预览三条读取链路自动支持，调用方零改动。
- 减法只在最后格式化一次，不引入二次舍入。

## 非目标

- 不支持加法、乘除、多操作数、括号嵌套。真需要时从减法扩一个运算符分支即可。
- 不支持用测试项 ID 或名称作为操作数（如 `(10-5)`）。跨测试项引用会让删除或改名被引用项时静默失效，需要额外的引用完整性校验，且执行模型不同（先算测试项值再相减 vs 直接读两地址相减）。
- 不接入 OK/NG 判定。上限、下限、结果偏移保持留空，位移量只上传数值，判定由 MES 或人工负责。接入判定会牵扯整件检测的 A/B 聚合与零值检查，属另一量级改动。
- 不做绝对地址的减法（如 `(DB97.134-DB97.84)`）。当前需求是同一基地址下的两个偏移量；绝对地址各自独立，相减没有配对语义。

## 关键约束

**负数照实输出。** 焊后小于焊前在物理上异常，但它是真实测量结果。隐藏或改写会掩盖现场问题（例如两个偏移量配反），v2.12.0 的零值误判已证明静默改写异常值会造成更难排查的后果。

**任一操作数无效即采集失败。** 读取失败或 String 无法解析为数值时，按现有单地址失败处理：本件采集失败、不落库、不刷新报表、不创建 MES 数据，沿用现有 PLC 失败反馈要求重采。不输出空值，也不把无效值当 0——后者会算出看似合理的差值。

## 设计

### 表达式解析（`AutoWeldSystem.Core/Plc/PlcOffsetExpression.cs`）

`PlcOffsetExpression` 增加 `int? SubtrahendOffset`（减数字节偏移，`null` 表示非计算式）。

`Parse` 在解析地址段前先识别计算式形态：地址段被圆括号包裹且含 `-` 时，拆成被减数和减数两个整数偏移，其余部分（类型、规则、小数位）解析逻辑完全不变。

拒绝的情形，各配一条明确错误消息：

- 操作数不是整数：`(a-84)`
- 操作数数量不为 2：`(134)`、`(134-84-20)`
- 操作数使用绝对地址：`(DB97.134-DB97.84)`
- 括号不配对：`(134-84`

`ResolveAddress` 保持原样返回被减数地址。新增 `ResolveSubtrahendAddress(baseAddress, contextOffset)`，仅在 `SubtrahendOffset` 有值时返回减数地址。

`RuleHint` 追加计算式说明，供界面提示和保存校验的错误消息复用。

### 绑定结构（`AutoWeldSystem.Core/Plc/PlcExpressionBinding.cs`）

增加 `string? SubtrahendAddress`（默认 `null`）和只读属性 `IsCalculated => !string.IsNullOrWhiteSpace(SubtrahendAddress)`。

`ExpressionReadService.Resolve`（`AutoWeldSystem.Services/Plc/ExpressionReadService.cs:29`）在构造 `PlcExpressionBinding` 时填入减数地址。

### 读取与计算（`ExpressionReadService`）

`ReadBindingTextAsync` 是唯一改动点：检测到 `binding.IsCalculated` 时读两个地址、相减、格式化；否则走原路径。

**精度处理**：两个操作数各自读原始值并应用规则缩放（`rule` 的除 10/100/1000），**相减后只格式化一次**。不复用 `ReadResolvedAddressTextAsync`（它内部已完成格式化，会导致二次舍入），改为提取一个返回 `decimal` 的内部读取方法供两处共用。

需要复用的既有能力：
- `ApplyDisplayRule`（`:179`）的规则缩放逻辑
- `FormatNumericValue`（`:201`）的小数位格式化，含全局截断/四舍五入模式
- String 类型经 `PlcStringNumericFormatter` 解析为数值

**规则 4（结果值）与 Bool 类型禁止用于计算式**：结果值是 `2=NG/3=OK/4=焊前NG` 的枚举，相减无意义。在 `Parse` 阶段就拒绝，不留到读取时。

### 调用方（零改动）

全部读取路径已汇聚在 `Resolve` + `ReadBindingTextAsync` 两个入口，共 4 处调用点，均无需修改：

- `ProductCycleCollectionService.cs:458`（正式采集）
- `ProductRealtimePreviewService.cs:751`（实时预览测试项值）
- `ProductRealtimePreviewService.cs:698`（实时预览经 `ReadExpressionTextAsync` 的其他字段，内部转调同一入口）
- `AddressPreviewForm.cs:169`（地址预览）

### 保存校验

`TestSchemeConfigService.cs:303` 和 `AddressManageView.cs:2893` 都调用 `PlcOffsetExpression.Parse`，解析器支持后自动放行，无需改动。

### 地址预览显示（`AutoWeldSystem.UI/Forms/AddressPreviewForm.cs`）

计算式的地址列显示 `被减数地址 - 减数地址`（如 `DB97.134 - DB97.84`），值列显示最终计算结果。保持单行结构，不打乱与其他测试项的表格对齐。

## 数据兼容

不改实体、不改 CodeFirst、不改数据库。现有表达式全部按原逻辑解析，行为完全不变。计算式是纯新增语法，旧配置不受影响。

## 验证

回归测试加入 `AutoWeldSystem.Tests/Program.cs` 现有 `(Name, Run)` 列表：

1. **解析**：`(134-84):S-7_3` 正确拆出两个偏移、类型、规则、小数位；`ResolveSubtrahendAddress` 返回正确地址；非计算式的 `SubtrahendOffset` 为 `null`。
2. **拒绝非法形态**：非整数操作数、操作数数量不为 2、绝对地址操作数、括号不配对、规则 4、Bool 类型，各断言抛 `FormatException`。
3. **兼容性**：现有表达式（`14:F-0_2`、`DB97.26:F-0_2`、`0:S-8_3`）解析结果与改动前一致。
4. **精度**：相减后只格式化一次，与「各自格式化再相减」的结果在存在二次舍入的用例上不同——用一个能区分两种路径的数据验证。
5. **负数**：焊后小于焊前时照实输出负值。

现场验证（本地无法覆盖，须实机确认并在交付说明中单独列出）：

- 测试项字典保存 `(134-84):S-7_3` 能通过校验；
- 地址预览显示两个地址和计算结果，数值与手工计算一致；
- 实际采集后，实时预览、数据管理历史、XLSX 报表、MES 过程参数四处的位移量数值一致；
- 任一操作数地址读取失败时，本件采集失败并要求 PLC 重采，不落库半成品。

## 涉及文件

- `AutoWeldSystem.Core/Plc/PlcOffsetExpression.cs`（解析、地址解析、规则提示）
- `AutoWeldSystem.Core/Plc/PlcExpressionBinding.cs`（减数地址）
- `AutoWeldSystem.Services/Plc/ExpressionReadService.cs`（绑定构造、读取与相减）
- `AutoWeldSystem.UI/Forms/AddressPreviewForm.cs`（预览显示）
- `AutoWeldSystem.Tests/Program.cs`（回归用例）
- `Directory.Build.props`、`README.md`（版本与文档，新增功能升次版本）

# AutoWeldSystem

自动点焊系统上位机软件，用于对接 PLC、MES 和本地程序管理流程。当前版本：`v1.5.0`。

## 功能概览

- 生产监控：工单信息、程序信息、PLC/MES 连接状态、设备状态、生产指标实时显示。
- MES 交互：员工校验、工单获取、开工上报、完工上报、设备编号同步、程序上传/下载。
- 程序管理：本地程序版本管理、提交记录、MES 同步状态，以及按工位选择 PLC 配方名称并隐藏保存槽位配方号。
- 地址维护：维护固定业务信号对应的 PLC 实际地址，并按工位配置/预览 PLC 配方名称与数字槽位的映射。
- 日志管理：MES 交互日志、业务异常日志、程序异常日志，用于现场排查。
- 权限管理：本地用户、角色、页面权限和按钮权限控制。
- 国际化：支持简体中文和英文界面切换。

## 技术栈

- .NET 8 Windows Forms
- AntdUI
- Microsoft.Extensions.Hosting
- SqlSugar + MySQL
- HslCommunication

## 项目结构

```text
AutoWeldSystem
├─ AutoWeldSystem.Core       # 常量、DTO、接口、模型、权限定义和多语言资源
├─ AutoWeldSystem.Data       # SqlSugar 数据库上下文和数据库初始化
├─ AutoWeldSystem.Services   # MES、PLC、程序管理、日志、权限等业务服务
├─ AutoWeldSystem.UI         # WinForms 界面、窗体、控件和应用入口
├─ AutoWeldSystem.Libs       # 本地 DLL 依赖
├─ Directory.Build.props     # 全局程序集版本配置
└─ AutoWeldSystem.sln
```

## 环境要求

- Windows 10/11
- Visual Studio 2022
- .NET 8 SDK
- MySQL 5.7/8.0

项目引用了本地依赖 `AutoWeldSystem.Libs/HslCommunication.dll`，请保留该目录结构。

## 本地配置

真实配置文件 `AutoWeldSystem.UI/appsettings.json` 已加入 `.gitignore`，不会提交到仓库。首次拉取后可从示例文件复制一份：

```powershell
Copy-Item AutoWeldSystem.UI/appsettings.example.json AutoWeldSystem.UI/appsettings.json
```

然后按本机 MySQL 环境修改连接字符串。

当前数据库上下文会在启动时执行 CodeFirst 初始化，创建系统需要的表结构。默认管理员账号和权限数据由服务初始化逻辑维护。

首次初始化会创建三个内置账号：

| 账号 | 角色 | 初始密码 |
| --- | --- | --- |
| `admin` | Administrator | `123456` |
| `operator` | Operator | `123456` |
| `readonly` | Readonly | `123456` |

现场部署后请及时修改默认密码。

## 构建

```powershell
dotnet restore AutoWeldSystem.sln
dotnet build AutoWeldSystem.sln
```

## 发布

在仓库根目录执行以下命令，`-o` 指定产物目录，发布完成后进入对应目录取文件。

### 非自包含（默认，目标机需预装 .NET 8 运行时）

```powershell
dotnet publish AutoWeldSystem.UI\AutoWeldSystem.UI.csproj -c Release -r win-x64 --self-contained false -o artifacts\UI-win-x64
dotnet publish AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj -c Release -r win-x64 --self-contained false -o artifacts\CenterServer-win-x64
```

### 自包含（目标机无法安装运行时时使用）

```powershell
dotnet publish AutoWeldSystem.UI\AutoWeldSystem.UI.csproj -c Release -r win-x64 --self-contained true -o artifacts\UI-win-x64-selfcontained
dotnet publish AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj -c Release -r win-x64 --self-contained true -o artifacts\CenterServer-win-x64-selfcontained
```

### 产物位置

产物固定输出到仓库根目录下的 `artifacts\`：

| 内容 | 产物目录 | 启动文件 |
| --- | --- | --- |
| 上位机主程序（非自包含） | `artifacts\UI-win-x64\` | `AutoWeldSystem.UI.exe` |
| 上位机主程序（自包含） | `artifacts\UI-win-x64-selfcontained\` | `AutoWeldSystem.UI.exe` |
| 中心服务器（非自包含） | `artifacts\CenterServer-win-x64\` | `AutoWeldSystem.CenterServer.exe` |
| 中心服务器（自包含） | `artifacts\CenterServer-win-x64-selfcontained\` | `AutoWeldSystem.CenterServer.exe` |

现场部署时把整个产物目录复制到工控机，运行其中的 `.exe` 即可。

### 目标机运行时要求

非自包含产物体积小、运行时可独立打补丁，但**目标工控机必须预装 .NET 8 运行时**：

- 上位机主程序 UI 需要 **.NET Desktop Runtime 8**（WinForms 依赖）。
- 中心服务器 CenterServer 需要 **ASP.NET Core Runtime 8**。
- 同机部署两者时装 Desktop Runtime + ASP.NET Core Runtime 即可，均为 x64。
- 现场无法安装运行时或无外网时改用自包含发布，产物体积明显增大，但不依赖预装运行时。

发布产物目录 `artifacts\` 已在 `.gitignore` 中忽略，不进入版本库。UI 的 `appsettings.json` 会随发布输出，现场需按实际 PLC、MES、MySQL 参数单独维护，不要把现场配置提交回仓库。

## 运行

使用 Visual Studio 打开 `AutoWeldSystem.sln`，将 `AutoWeldSystem.UI` 设置为启动项目后运行。

启动前请确认：

- MySQL 服务可访问。
- PLC IP、端口、协议类型符合现场配置。
- MES 地址、设备编号、日志路径、数据路径可在系统设置界面维护。
- PLC 地址在地址维护界面中填写完整。

## 程序管理界面操作

- 左侧列表按产品工号去重，一行代表一个产品工号，显示工号、程序摘要和最近更新时间。
- 工号下有多个程序时，该行左侧出现展开箭头，展开后每个程序占一个子行，按流水号升序排列，子行显示流水号标签和“程序名称 + 版本 + 同步状态”摘要。点击子行切换右侧编辑内容。
- 工号下只有一个程序时不出现展开箭头，该程序的摘要直接显示在工号行上，点击工号行即可编辑，避免为单个程序多套一层。
- 同一工号下可以有多个程序，用“另存为新程序”按当前内容新建一条，流水号自动取该工号下的下一个可用值。
- 产品工号、零组件代码和工位配方名称为必填项，标签前带红色星号，留空时保存会立即提示。其余字段可以留空。
- 程序名称由工号、零组件代码、流水号和程序备注拼成。同工号下若流水号和程序备注都相同会产生重名，保存时会被拒绝，需调整流水号或程序备注。

## PLC 配方名称关联与生产可用性

- 普通业务界面只选择和显示 PLC 配方名称，不显示或手工编辑数字配方号；实际槽位号由“地址维护 -> 配方名称地址”映射，并随程序隐藏保存。
- 单工位程序必须配置工位 1 配方；双工位程序允许一侧选择“不适用”，但不能两侧同时不适用。双工位同工单生产时，两个工位都必须配置有效关联。
- 工位 1 与工位 2 的配方关联相互独立。生产开工、PLC 下发和回读校验始终按目标工位使用本机程序记录中的隐藏配方号，不复用另一工位值。
- MES 下载的程序不会携带本机 PLC 配方映射；下载后需在程序管理中选择对应工位的 PLC 配方名称并保存，完成前该程序不会出现在相应工位的可生产列表中。
- PLC 调整配方槽位顺序、名称或地址后，系统不会按名称自动迁移历史关联。请先在地址维护确认新映射，再到程序管理重新选择名称并保存。
- 数字配方号仅在地址维护映射和日志诊断中保留，供配置确认和现场排障使用。

## 离线开工产品工号选择

- 离线模式下“产品工号”是下拉选择框，选项来自本机程序库中当前工位可生产程序的工号，操作员选中后即可直接开工，无需手工输入。
- 在线模式下产品工号跟随 MES 工单只读展示，不可选择。
- 程序名称列表是否按产品工号收窄，由“系统设置 -> 按产品工号筛选程序”决定，离线与在线语义一致：启用时只显示该工号的程序；未启用时显示全部可生产程序，便于一款产品借用另一款工号的程序生产。
- 未启用筛选时选中工号仍会跳转到该工号的首个程序，之后可自由改选其他工号的程序。
- 同一工号存在多个程序时，列表按“程序名称”区分，可继续在程序名称下拉中选择具体程序。
- 双工位分别记忆各自选中的工号；切换工位或退出离线模式时该记忆自动清除。

## 系统设置设备管理锁定

- PLC 设备状态本身不控制“系统设置 -> 设备管理”的可编辑状态；即使 PLC 状态为 `1`，只要当前软件运行态没有活动生产任务，设备管理仍可编辑。
- 任一工位在线或离线开工后，当前运行态存在尚未完工的 `ActiveTask` 时，整个设备管理区域不可编辑；暂停中的活动任务仍保持锁定，完工后自动恢复。
- 数据库中的历史未完工记录不会单独锁定设备管理区域；软件重启后，只有当监控流程把任务恢复为当前运行态任务时才重新锁定。
## 日志管理显示

- 设备状态日志表格不显示“来源”和“工位”，生产流程日志表格不显示“步骤”，设备日志表格不显示“工位”；这些字段仍保留在右侧详情中并继续参与关键字搜索。
- 生产流程摘要统一使用集中维护的中文文本；旧 JSONL 中已存在的配方号调和、设备模式调和和工单状态英文摘要会在界面显示时转换为中文，不重写历史文件。

## 中心服务器同步与日志

- 设备端按系统设置中的心跳间隔先向中心服务器发送 `heartbeat`。中心服务器不可达时，只记录首次“心跳失败”；持续失败不重复刷日志，恢复连接时再记录一条“心跳成功”。
- 中心服务器连接成功后，设备启动首次同步会发送一次完整 `telemetry`；此后只有 PLC 连接、设备状态、报警、工单、产品信息或当日产量等看板字段发生变化时才发送，不再按固定时间重复推送未变化快照。
- 断线期间仅保留最新快照。恢复连接后，如果快照相对最后一次成功同步确有变化，会在心跳成功后补发一次；未变化时只恢复心跳，不重复发送设备状态。
- 设备端和中心服务器统一使用三类消息：`heartbeat=心跳`、`telemetry=设备状态`、`product-report=产品数据`。心跳只维护在线时间，设备状态负责更新完整工位快照。

## 生产报表与补传

- 工单完工后本地 XLSX 报表会先生成；只要 XLSX 生成成功，就会进入“待上传数据 -> 报告文件”任务队列并参与 MES 上传/补传，不再依赖产品明细行或 `ReportEnable` 输出项作为入队前置条件。
- 若历史工单已生成 `Biz_ProductionReportFile` 记录但缺少对应 `ReportFile` 上传任务，进入报表待上传页、全部重试或自动补传前会自动补齐任务；用户已经手动删除或已经上传成功的任务不会被恢复。
- 没有任何产品生产数据时，报表仍会按公共字段生成并上传；如果 MES 或文件路径失败，失败信息保留在待上传数据中供手动重试。

## 设备状态日志与补传

设备状态固定使用 `0=停机`、`1=开机`、`4=异常`、`5=异常恢复`、`6=程序执行开始`、`7=程序执行结束`。设备状态 JSONL 是唯一事实来源，文件位于配置日志根目录下的 `DeviceStatus/*.jsonl`；日志管理、当前状态查询和待上传数据中的设备状态都从这里读取。

- 状态变化会先写入 JSONL，落盘成功后才会刷新界面、上传 MES 或建立补传任务。
- PLC 报警判定可在“系统设置 -> PLC 配置”选择“仅报警地址”或“设备状态异常且报警地址触发”；旧配置默认使用后者，保存后下一轮采集立即生效。报警读取关闭时冻结当前报警周期，重新启用后再按真实读取结果处理。
- 每个报警地址独立形成“异常触发 -> 异常恢复”链路；多个地址可同轮触发或恢复。部分恢复后会按 `5...5 -> 4` 的顺序重申一个仍有效报警，保证 MES 最终状态仍为异常。共享报警设备级只记录一次（`StationNo=0`），但两个工位监控页都会显示。
- MES 异常 Remark 使用 `工位1：报警内容；`（共享报警为 `报警内容；`）；恢复 Remark 使用 `异常恢复-工位1：报警内容；`（共享报警为 `异常恢复：报警内容；`），均不包含报警地址。设备状态日志表格不显示报警地址和内容，但仍可按这两项搜索并在详情中查看；设备日志不再新增报警或恢复记录，旧设备日志文件保持不变。
- 程序启动时会先把当前 `1=开机` 写入 JSONL，再在后台扫描全部日期文件，按发生时间从旧到新补传 `Pending/Failed` 状态；旧记录上传失败时会暂停本批后续状态，避免新的开机或运行状态越过旧 `0=停机`。该过程不阻塞登录界面。
- MES 断线重连后会复用同一设备状态补传流程，并继续按发生时间顺序处理全部日期 JSONL。
- 关闭窗口时界面立即退出；后台进程会在一个 MES 超时时间内（最少 3 秒，默认 10 秒）同步等待本次 `0=停机` 上报。成功记录为 `Uploaded`，失败记录为 `Failed`，超时前已落盘但未完成的记录保留为 `Pending/Failed`，下次启动继续补传。
- 删除某个日期文件或整个 `DeviceStatus` 目录后，刷新或重新进入日志管理/待上传页面即可移除对应记录；其中未成功上传的状态不再参与单条或批量补传。
- 已成功上传到 MES 的结果不会因为删除本地 JSONL 而撤销，已有上传任务历史也不会回退为待上传。
- JSONL 首次落盘失败时不会上传该状态，也不会生成补传任务。请到“日志管理 -> 程序异常日志”检查磁盘空间、日志目录和写入权限。
- 升级不会删除旧的 `Biz_DeviceStatusLog` 物理表，但程序不再创建、读取或写入该表。

## 版本管理

软件版本统一配置在 `Directory.Build.props`：

```xml
<Version>1.5.0</Version>
<AssemblyVersion>1.5.0.0</AssemblyVersion>
<FileVersion>1.5.0.0</FileVersion>
<InformationalVersion>1.5.0</InformationalVersion>
```

建议使用语义化版本：

- 主版本：架构或兼容性发生重大变化。
- 次版本：新增功能。
- 修订号：缺陷修复或小范围优化。

发布新版本时：

```powershell
git tag -a v1.5.0 -m "Release v1.5.0"
git push origin main
git push origin v1.5.0
```

## Git 使用

远程仓库：

```text
https://github.com/1900401734/AutoWeldSystem.git
```

常用命令：

```powershell
git status
git add .
git commit -m "说明本次修改内容"
git push
```

拉取远程更新：

```powershell
git pull --rebase
```

## 注意事项

- 不要提交本机真实 `appsettings.json`、数据库密码、现场 MES 地址、现场 PLC 地址等敏感配置。
- `bin/`、`obj/`、`.vs/` 为本地构建产物，不需要提交。
- 数据库结构由 CodeFirst 初始化，修改模型后应先在测试库验证。
- 现场联调前应先在系统设置界面确认 MES、PLC、日志路径和设备编号。

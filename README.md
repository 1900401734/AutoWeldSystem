# AutoWeldSystem

自动点焊系统上位机软件，用于对接 PLC、MES 和本地程序管理流程。当前版本：`v1.0.9`。

## 功能概览

- 生产监控：工单信息、程序信息、PLC/MES 连接状态、设备状态、生产指标实时显示。
- MES 交互：员工校验、工单获取、开工上报、完工上报、设备编号同步、程序上传/下载。
- 程序管理：本地程序版本管理、提交记录、MES 同步状态、失败后手动重试。
- 地址维护：维护固定业务信号对应的 PLC 实际地址，支持启用状态、数据类型和长度配置。
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

发布 Release：

```powershell
dotnet publish AutoWeldSystem.UI/AutoWeldSystem.UI.csproj -c Release -r win-x64 --self-contained false
```

## 运行

使用 Visual Studio 打开 `AutoWeldSystem.sln`，将 `AutoWeldSystem.UI` 设置为启动项目后运行。

启动前请确认：

- MySQL 服务可访问。
- PLC IP、端口、协议类型符合现场配置。
- MES 地址、设备编号、日志路径、数据路径可在系统设置界面维护。
- PLC 地址在地址维护界面中填写完整。

## 设备状态日志与补传

设备状态固定使用 `0=停机`、`1=开机`、`4=异常`、`5=异常恢复`、`6=程序执行开始`、`7=程序执行结束`。设备状态 JSONL 是唯一事实来源，文件位于配置日志根目录下的 `DeviceStatus/*.jsonl`；日志管理、当前状态查询和待上传数据中的设备状态都从这里读取。

- 状态变化会先写入 JSONL，落盘成功后才会刷新界面、上传 MES 或建立补传任务。
- 删除某个日期文件或整个 `DeviceStatus` 目录后，刷新或重新进入日志管理/待上传页面即可移除对应记录；其中未成功上传的状态不再参与单条或批量补传。
- 已成功上传到 MES 的结果不会因为删除本地 JSONL 而撤销，已有上传任务历史也不会回退为待上传。
- JSONL 首次落盘失败时不会上传该状态，也不会生成补传任务。请到“日志管理 -> 程序异常日志”检查磁盘空间、日志目录和写入权限。
- 升级不会删除旧的 `Biz_DeviceStatusLog` 物理表，但程序不再创建、读取或写入该表。

## 版本管理

软件版本统一配置在 `Directory.Build.props`：

```xml
<Version>1.0.9</Version>
<AssemblyVersion>1.0.9.0</AssemblyVersion>
<FileVersion>1.0.9.0</FileVersion>
<InformationalVersion>1.0.9</InformationalVersion>
```

建议使用语义化版本：

- 主版本：架构或兼容性发生重大变化。
- 次版本：新增功能。
- 修订号：缺陷修复或小范围优化。

发布新版本时：

```powershell
git tag -a v1.0.9 -m "Release v1.0.9"
git push origin main
git push origin v1.0.9
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

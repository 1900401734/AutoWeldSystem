<#
=====================================================================================
 AutoWeldSystem 数据库备份计划任务注册脚本  register-backup-task.ps1
=====================================================================================

【这个脚本做什么】
  在 Windows“任务计划程序”里创建一个每日定时任务，到点自动运行同目录下的 backup-mysql.ps1。
  任务以 SYSTEM 账号运行，不依赖任何用户登录，工控机开机即可生效。
  任务命令行里只有脚本路径、配置文件路径、备份目录和保留天数，没有数据库密码；
  密码由 backup-mysql.ps1 在运行时从 appsettings.json 读取。

【必须以管理员身份运行】
  创建 SYSTEM 账号的计划任务需要管理员权限。
  打开方式：开始菜单搜索 PowerShell，右键“以管理员身份运行”。普通窗口运行会直接报错退出。

【参数说明】
  -TaskName         任务名称，默认 AutoWeldSystemBackup，一般不用改。
  -AppSettingsPath  必填。appsettings.json 的完整路径，可以给多个，用逗号分隔。
  -DailyAt          每天执行时间，HH:mm 格式，默认 02:00（凌晨两点，避开生产时段）。
  -BackupDir        备份存放目录，默认 D:\AutoWeldBackup，不存在会自动创建。
  -RetentionDays    保留天数，默认 30；填 0 表示永不删除旧备份。
  -MysqldumpPath    可选，mysqldump.exe 的完整路径，一般不用填。
  -Unregister       卸载任务。带这个参数时其他参数都不需要。

【使用步骤】
  第 1 步  先用 backup-mysql.ps1 手动备份一次并确认成功（见该脚本头部说明），再来注册任务。
  第 2 步  管理员 PowerShell 里执行（把路径换成现场实际路径）：
            powershell -ExecutionPolicy Bypass -File D:\AutoWeld\tools\register-backup-task.ps1 -AppSettingsPath D:\AutoWeld\UI\appsettings.json -BackupDir D:\AutoWeldBackup
          同机同时有上位机和中心服务器时，两个路径都写上：
            powershell -ExecutionPolicy Bypass -File D:\AutoWeld\tools\register-backup-task.ps1 -AppSettingsPath "D:\AutoWeld\UI\appsettings.json","D:\AutoWeld\Center\appsettings.json"
          想改时间或保留天数，加 -DailyAt 03:30 -RetentionDays 60 即可；重复执行会覆盖同名旧任务。
  第 3 步  立即触发一次并查看结果，确认任务能正常跑：
            Start-ScheduledTask -TaskName AutoWeldSystemBackup
            Get-ScheduledTaskInfo -TaskName AutoWeldSystemBackup
          LastTaskResult 为 0 表示成功；非 0 时打开备份目录下的 backup.log 看 [ERROR] 行。
          也可以打开“任务计划程序”窗口，在任务列表里找到 AutoWeldSystemBackup 查看。
  第 4 步  以后不需要再手动操作。每月看一次备份目录是否有最近日期的 zip 即可。

【卸载任务】
  管理员 PowerShell 里执行：
            powershell -ExecutionPolicy Bypass -File D:\AutoWeld\tools\register-backup-task.ps1 -Unregister

【常见问题】
  提示需要管理员权限            → 没有用“以管理员身份运行”打开 PowerShell。
  提示“未找到备份脚本”          → backup-mysql.ps1 没有和本脚本放在同一个文件夹。
  提示“配置文件不存在”          → -AppSettingsPath 路径写错，检查文件是否真的在那里。
  任务存在但 LastTaskResult 非 0 → 查看备份目录下 backup.log 的最后几行。
  移动了脚本或程序目录           → 任务里记录的是绝对路径，移动后必须重新执行本脚本注册一次。
=====================================================================================
#>

# 没有管理员权限时 PowerShell 直接拒绝运行本脚本。
#Requires -RunAsAdministrator

# ---------- 参数定义 ----------
# 两个参数集：Register（注册，默认）和 Unregister（卸载），互斥。
[CmdletBinding(DefaultParameterSetName = 'Register')]
param(
    # 任务名称，注册和卸载共用。
    [string]$TaskName = 'AutoWeldSystemBackup',

    # 一个或多个 appsettings.json 路径；注册时必填。
    [Parameter(ParameterSetName = 'Register', Mandatory = $true)]
    [string[]]$AppSettingsPath,

    # 每天执行时间，HH:mm。
    [Parameter(ParameterSetName = 'Register')]
    [string]$DailyAt = '02:00',

    # 备份目录。
    [Parameter(ParameterSetName = 'Register')]
    [string]$BackupDir = 'D:\AutoWeldBackup',

    # 保留天数，范围 0～3650。
    [Parameter(ParameterSetName = 'Register')]
    [ValidateRange(0, 3650)]
    [int]$RetentionDays = 30,

    # 可选：手动指定 mysqldump.exe，原样透传给备份脚本。
    [Parameter(ParameterSetName = 'Register')]
    [string]$MysqldumpPath,

    # 卸载开关；带上它就走 Unregister 参数集。
    [Parameter(ParameterSetName = 'Unregister', Mandatory = $true)]
    [switch]$Unregister
)

# 任何命令出错都当作异常抛出，避免半成品任务被注册进去。
$ErrorActionPreference = 'Stop'

# ---------- 卸载分支 ----------
if ($Unregister) {
    # 查一下任务是否存在；不存在就直接结束，不报错。
    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if (-not $existing) {
        Write-Host "计划任务 $TaskName 不存在，无需卸载。"
        exit 0
    }
    # 删除任务，-Confirm:$false 免去交互确认。
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "已卸载计划任务 $TaskName。"
    exit 0
}

# ---------- 定位备份脚本 ----------
# $PSScriptRoot 是本脚本所在文件夹，备份脚本必须和本脚本放在一起。
$backupScript = Join-Path $PSScriptRoot 'backup-mysql.ps1'
if (-not (Test-Path -LiteralPath $backupScript)) {
    throw "未找到备份脚本：$backupScript"
}
# 转成绝对路径。计划任务由 SYSTEM 在自己的工作目录运行，相对路径会找不到文件。
$backupScript = (Resolve-Path -LiteralPath $backupScript).Path

# ---------- 校验并转换配置文件路径 ----------
$resolvedSettings = @()
foreach ($path in $AppSettingsPath) {
    # 注册前就检查文件存在，避免到凌晨才发现路径写错。
    if (-not (Test-Path -LiteralPath $path)) { throw "配置文件不存在：$path" }
    $resolvedSettings += (Resolve-Path -LiteralPath $path).Path
}

# ---------- 准备备份目录 ----------
# 不存在就创建，然后转成绝对路径写进任务。
if (-not (Test-Path -LiteralPath $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}
$BackupDir = (Resolve-Path -LiteralPath $BackupDir).Path

# ---------- 校验执行时间格式 ----------
# 严格按 HH:mm 解析，例如 02:00、14:30；格式不对就报错，避免注册出错误的触发时间。
$parsedTime = [DateTime]::MinValue
if (-not [DateTime]::TryParseExact($DailyAt, 'HH:mm', $null, [System.Globalization.DateTimeStyles]::None, [ref]$parsedTime)) {
    throw "-DailyAt 必须是 HH:mm 格式，例如 02:00，当前值：$DailyAt"
}

# ---------- 拼接任务要执行的命令行 ----------
# 多个配置文件路径各自加引号后用逗号连接，PowerShell 会把它解析成数组。
$settingsArg = ($resolvedSettings | ForEach-Object { '"{0}"' -f $_ }) -join ','
# powershell.exe 的参数：不加载用户配置、绕过执行策略、运行备份脚本并传入各项参数。
$argument = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -AppSettingsPath {1} -BackupDir "{2}" -RetentionDays {3}' -f $backupScript, $settingsArg, $BackupDir, $RetentionDays
# 用户指定了 mysqldump 路径时追加透传。
if ($MysqldumpPath) {
    $argument += ' -MysqldumpPath "{0}"' -f (Resolve-Path -LiteralPath $MysqldumpPath).Path
}

# ---------- 组装计划任务的四个部分 ----------
# 1. 动作：运行 powershell.exe，工作目录设为脚本所在文件夹。
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argument -WorkingDirectory (Split-Path $backupScript)
# 2. 触发器：每天在指定时间触发一次。
$trigger = New-ScheduledTaskTrigger -Daily -At $parsedTime
# 3. 运行身份：SYSTEM 账号、最高权限；不依赖用户登录。
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
# 4. 运行设置：
#    -StartWhenAvailable        错过执行时间（如夜里关机）则下次开机后尽快补跑
#    -AllowStartIfOnBatteries   使用电池时也允许启动（笔记本调试时有用）
#    -DontStopIfGoingOnBatteries 切换到电池供电时不中断
#    -MultipleInstances IgnoreNew 上一次还没跑完时忽略新的触发，避免同时跑两份
#    -ExecutionTimeLimit 2 小时  超过两小时强制结束，防止卡死
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)
# 把四部分组合成任务对象，并写上说明文字（在任务计划程序窗口里可见）。
$task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'AutoWeldSystem MySQL 每日备份（tools\backup-mysql.ps1）'

# ---------- 注册任务 ----------
# -Force：同名任务已存在时直接覆盖，重复执行本脚本即可修改时间或参数。
Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

# ---------- 输出结果和后续验证命令 ----------
Write-Host "已注册计划任务 $TaskName：每日 $DailyAt 以 SYSTEM 运行。"
Write-Host "备份目录：$BackupDir，保留 $RetentionDays 天。"
Write-Host "配置文件：$($resolvedSettings -join '; ')"
Write-Host ''
Write-Host '验证命令：'
Write-Host "  Start-ScheduledTask -TaskName $TaskName            # 立即手动触发一次"
Write-Host "  Get-ScheduledTaskInfo -TaskName $TaskName          # LastTaskResult 为 0 表示成功"
Write-Host "  Get-Content `"$BackupDir\backup.log`" -Tail 20      # 查看备份日志"

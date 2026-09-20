<#
=====================================================================================
 AutoWeldSystem MySQL 数据库备份脚本  backup-mysql.ps1
=====================================================================================

【这个脚本做什么】
  1. 读取上位机或中心服务器的 appsettings.json，取出数据库连接信息（地址、端口、库名、账号、密码）。
  2. 调用 MySQL 自带的 mysqldump.exe 把整个库导出成一个 .sql 文件。
  3. 把 .sql 压缩成 .zip 后删除 .sql，节省磁盘。
  4. 删除超过保留天数的旧 .zip（只删本脚本生成的、同一个库的备份，不碰其他文件）。
  5. 全过程写入备份目录下的 backup.log，方便事后检查。
  任一库失败时脚本以退出码 1 结束，全部成功时退出码 0；计划任务的“上次运行结果”就能看出是否正常。

【适用环境】
  Windows 7/10/11 自带的 Windows PowerShell 5.1 即可，不需要安装 PowerShell 7。
  目标机器需要能找到 mysqldump.exe：它随 MySQL Server 一起安装在 bin 目录，
  例如 C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe。脚本会自动探测，找不到时用 -MysqldumpPath 指定。

【不要用右键“使用 PowerShell 运行”】
  右键运行不带参数，脚本会逐条向你索要 AppSettingsPath，并且跑完窗口立即关闭看不到结果。
  正确做法是打开 PowerShell 窗口，按下面的命令带参数执行。

【参数说明】
  -AppSettingsPath   必填（与 -ConnectionString 二选一）。appsettings.json 的完整路径，可以给多个，用逗号分隔。
                     上位机的文件在上位机程序目录下，中心服务器的文件在中心服务器程序目录下，脚本会自动识别两种格式。
  -ConnectionString  与 -AppSettingsPath 二选一。直接给连接串，例如
                     "Server=127.0.0.1;Port=3306;Database=autoweldsystem_db;Uid=root;Pwd=密码;"
  -BackupDir         备份存放目录，默认 D:\AutoWeldBackup，不存在会自动创建。
  -RetentionDays     保留天数，默认 30；填 0 表示永不删除旧备份。
  -MysqldumpPath     mysqldump.exe 的完整路径，一般不用填。
  -LogFile           日志文件路径，默认 <BackupDir>\backup.log。

【现场部署步骤】
  第 1 步  把 tools 文件夹里的 backup-mysql.ps1 和 register-backup-task.ps1 复制到工控机，
          例如 D:\AutoWeld\tools\。两个文件必须放在同一个文件夹。
  第 2 步  打开普通 PowerShell 窗口，手动跑一次，确认能成功（把路径换成现场实际路径）：
            powershell -ExecutionPolicy Bypass -File D:\AutoWeld\tools\backup-mysql.ps1 -AppSettingsPath D:\AutoWeld\UI\appsettings.json -BackupDir D:\AutoWeldBackup
          看到“全部备份成功”，并且 D:\AutoWeldBackup 里出现 zip 文件，说明环境没问题。
  第 3 步  用管理员身份打开 PowerShell（右键“以管理员身份运行”），注册每日自动任务：
            powershell -ExecutionPolicy Bypass -File D:\AutoWeld\tools\register-backup-task.ps1 -AppSettingsPath D:\AutoWeld\UI\appsettings.json -BackupDir D:\AutoWeldBackup
          默认每天 02:00 执行；夜里关机错过了，下次开机会自动补跑。
  第 4 步  仍在管理员窗口里立即触发一次并查看结果，确认计划任务本身能跑：
            Start-ScheduledTask -TaskName AutoWeldSystemBackup
            Get-ScheduledTaskInfo -TaskName AutoWeldSystemBackup
          LastTaskResult 显示 0 就是成功。
  中心服务器在另一台机器时，在那台机器重复以上步骤，-AppSettingsPath 改成中心服务器目录下的 appsettings.json。
  同一台机器同时装了上位机和中心服务器时，两个路径都写上：
            -AppSettingsPath "D:\AutoWeld\UI\appsettings.json","D:\AutoWeld\Center\appsettings.json"

【日常检查】
  每月看一次备份目录：有最近日期的 zip、backup.log 末尾没有 [ERROR]，就是正常。
  备份目录只是本机一份副本，硬盘坏了会一起丢，建议定期把整个目录复制到 U 盘或别的机器。

【出问题时怎么恢复】
  第 1 步  关闭上位机（中心库出问题就关中心服务器），恢复期间程序不能写库。
  第 2 步  在备份目录里选出问题“之前”最近的 zip，解压得到 .sql 文件。
          如果当前库还能连上，先手动跑一次本脚本把现状也备份一份，避免想找回后来的数据时没有余地。
  第 3 步  打开 MySQL 命令行（输入 MySQL 密码后出现 mysql> 提示符）：
            & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -uroot -p --default-character-set=utf8mb4
  第 4 步  在 mysql> 里依次执行（库名以 appsettings.json 里 Database= 后面的值为准）：
            DROP DATABASE IF EXISTS autoweldsystem_db;
            CREATE DATABASE autoweldsystem_db CHARACTER SET utf8mb4;
            USE autoweldsystem_db;
            source D:\AutoWeldBackup\autoweldsystem_db_20260920_020000.sql;
          等回到 mysql> 提示符就导完了。
  第 5 步  启动程序，核对工单、程序列表和系统设置是否为备份时的状态。
  注意：恢复会丢失备份时间点之后产生的数据；回退程序版本时，程序版本和备份要配套。

【常见问题】
  提示“未找到 mysqldump.exe”      → 安装 MySQL 客户端，或加 -MysqldumpPath "C:\...\bin\mysqldump.exe"。
  提示“Access denied”            → appsettings.json 里的数据库账号或密码不对。
  提示“Can't connect”            → MySQL 服务没启动，或地址端口不对。
  中文显示乱码                    → 本文件必须以“UTF-8 带 BOM”编码保存，用记事本另存为时选该编码。
=====================================================================================
#>

# ---------- 参数定义 ----------
# DefaultParameterSetName：不指定时默认按 AppSettings 方式解析参数。
[CmdletBinding(DefaultParameterSetName = 'AppSettings')]
param(
    # 一个或多个 appsettings.json 路径；属于 AppSettings 参数集且必填。
    [Parameter(ParameterSetName = 'AppSettings', Mandatory = $true)]
    [string[]]$AppSettingsPath,

    # 直接给连接串；属于 ConnectionString 参数集且必填。两个参数集互斥，只能选一种。
    [Parameter(ParameterSetName = 'ConnectionString', Mandatory = $true)]
    [string]$ConnectionString,

    # 备份文件存放目录。
    [string]$BackupDir = 'D:\AutoWeldBackup',

    # 保留天数，范围 0～3650；0 表示不清理。
    [ValidateRange(0, 3650)]
    [int]$RetentionDays = 30,

    # 可选：手动指定 mysqldump.exe 路径。
    [string]$MysqldumpPath,

    # 可选：日志文件路径，留空则放在备份目录下。
    [string]$LogFile
)

# ---------- 全局行为设置 ----------
# 任何命令出错都当作异常抛出，避免错误被忽略后继续往下跑。
$ErrorActionPreference = 'Stop'
# 关闭 Compress-Archive 等命令的进度条，计划任务无窗口时进度条会拖慢速度。
$ProgressPreference = 'SilentlyContinue'

# ---------- 准备备份目录 ----------
# 目录不存在就创建。
if (-not (Test-Path -LiteralPath $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}
# 转成绝对路径。Windows PowerShell 5.1 的 Compress-Archive 遇到相对路径会报“空值调用方法”的错误，
# 同时也保证计划任务在任何工作目录下运行都能找到正确位置。
$BackupDir = (Resolve-Path -LiteralPath $BackupDir).Path

# 没指定日志文件时，默认放在备份目录下。
if (-not $LogFile) {
    $LogFile = Join-Path $BackupDir 'backup.log'
}

# ---------- 函数：写日志 ----------
# 同时输出到屏幕和日志文件，每行格式：时间 [级别] 内容。
function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    # 拼出带时间戳的一行文本。
    $line = '{0} [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Message
    # 打印到屏幕，手动运行时能直接看到。
    Write-Host $line
    try {
        # 以 UTF-8 追加写入日志文件，避免中文乱码。
        [System.IO.File]::AppendAllText($LogFile, $line + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    }
    catch {
        # 日志写不进去不应影响备份本身，只给个警告。
        Write-Warning "写日志失败：$($_.Exception.Message)"
    }
}

# ---------- 函数：查找 mysqldump.exe ----------
function Find-Mysqldump {
    param([string]$Explicit)
    # 用户手动指定了路径：存在就用，不存在直接报错。
    if ($Explicit) {
        if (Test-Path -LiteralPath $Explicit) { return (Resolve-Path -LiteralPath $Explicit).Path }
        throw "指定的 mysqldump 不存在：$Explicit"
    }
    # 先看系统 PATH 环境变量里有没有。
    $cmd = Get-Command mysqldump.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    # 再按常见安装目录逐个探测，同一目录下多个版本时取版本号最高的。
    $patterns = @(
        'C:\Program Files\MySQL\MySQL Server *\bin\mysqldump.exe',
        'C:\Program Files\MariaDB *\bin\mysqldump.exe',
        'C:\Program Files (x86)\MySQL\MySQL Server *\bin\mysqldump.exe'
    )
    foreach ($pattern in $patterns) {
        $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    # 都没找到，抛出明确提示。
    throw '未找到 mysqldump.exe，请安装 MySQL 客户端或用 -MysqldumpPath 指定路径。'
}

# ---------- 函数：解析连接串 ----------
# 把 "Server=...;Port=...;Database=...;Uid=...;Pwd=...;" 拆成一个哈希表。
function ConvertFrom-MySqlConnectionString {
    param([string]$Value, [string]$Source)
    # 默认值：地址本机、端口 3306；账号和库名必须由连接串提供。
    $result = @{ Host = '127.0.0.1'; Port = '3306'; User = $null; Password = ''; Database = $null }
    # 按分号拆成一段段 key=value。
    foreach ($segment in $Value -split ';') {
        # 跳过空段（连接串末尾通常有个多余的分号）。
        if ([string]::IsNullOrWhiteSpace($segment)) { continue }
        # 只按第一个等号拆分，密码里含等号也不会被截断。
        $pair = $segment -split '=', 2
        if ($pair.Count -ne 2) { continue }
        # 键统一转小写，便于不区分大小写比较。
        $key = $pair[0].Trim().ToLowerInvariant()
        $val = $pair[1].Trim()
        # 兼容 MySQL 连接串常见的几种键名写法。
        switch ($key) {
            { $_ -in 'server', 'host', 'data source' } { $result.Host = $val }
            'port' { $result.Port = $val }
            { $_ -in 'uid', 'user id', 'user', 'username' } { $result.User = $val }
            { $_ -in 'pwd', 'password' } { $result.Password = $val }
            { $_ -in 'database', 'initial catalog' } { $result.Database = $val }
        }
    }
    # 缺少库名或账号无法备份，直接报错并指出来源文件。
    if (-not $result.Database) { throw "连接串缺少 Database：$Source" }
    if (-not $result.User) { throw "连接串缺少 Uid：$Source" }
    # 记录来源，日志里能看出这条连接来自哪个文件。
    $result.Source = $Source
    return $result
}

# ---------- 函数：收集所有要备份的库 ----------
function Get-ConnectionTargets {
    $targets = @()
    # 用户直接给了连接串：只有这一个目标。
    if ($PSCmdlet.ParameterSetName -eq 'ConnectionString') {
        $targets += ConvertFrom-MySqlConnectionString -Value $ConnectionString -Source '-ConnectionString'
        return $targets
    }
    # 否则逐个读取 appsettings.json。
    foreach ($path in $AppSettingsPath) {
        if (-not (Test-Path -LiteralPath $path)) { throw "配置文件不存在：$path" }
        # 以 UTF-8 整体读入并解析成 JSON 对象。
        $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        $raw = $null
        # 上位机的格式：{"Database":{"ConnectionString":"..."}}
        if ($json.Database -and $json.Database.ConnectionString) {
            $raw = $json.Database.ConnectionString
        }
        # 中心服务器的格式：{"ConnectionStrings":{"Default":"..."}}
        elseif ($json.ConnectionStrings -and $json.ConnectionStrings.Default) {
            $raw = $json.ConnectionStrings.Default
        }
        # 两种都不是，说明文件给错了。
        if (-not $raw) { throw "配置文件中未找到 Database:ConnectionString 或 ConnectionStrings:Default：$path" }
        $targets += ConvertFrom-MySqlConnectionString -Value $raw -Source $path
    }
    return $targets
}

# ---------- 函数：清理过期备份 ----------
function Remove-ExpiredBackups {
    param([string]$DatabaseName)
    # 保留天数为 0 表示永不清理。
    if ($RetentionDays -le 0) { return }
    # 早于这个时间点的文件视为过期。
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    # 只匹配“库名_8位日期_6位时间.zip”这种本脚本生成的命名，其他文件一律不动。
    $pattern = '^{0}_\d{{8}}_\d{{6}}\.zip$' -f [Regex]::Escape($DatabaseName)
    $expired = Get-ChildItem -LiteralPath $BackupDir -File |
        Where-Object { $_.Name -match $pattern -and $_.LastWriteTime -lt $cutoff }
    # 逐个删除并记日志；某个删不掉只记警告，不影响其余文件。
    foreach ($file in $expired) {
        try {
            Remove-Item -LiteralPath $file.FullName -Force
            Write-Log "清理过期备份：$($file.Name)"
        }
        catch {
            Write-Log "清理 $($file.Name) 失败：$($_.Exception.Message)" 'WARN'
        }
    }
}

# ---------- 函数：备份一个库 ----------
# 成功返回 $true，失败返回 $false（错误已写日志）。
function Backup-Database {
    param($Target, [string]$Dumper)
    $db = $Target.Database
    # 用当前时间生成文件名，例如 autoweldsystem_db_20260920_020000。
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $sqlPath = Join-Path $BackupDir ('{0}_{1}.sql' -f $db, $stamp)
    # 压缩包与 sql 同名，只换扩展名。
    $zipPath = [System.IO.Path]::ChangeExtension($sqlPath, '.zip')
    # 临时文件放在系统 TEMP 目录，用随机名避免冲突：一个存密码配置，一个接 mysqldump 的错误输出。
    $cnfPath = Join-Path $env:TEMP ('awbk_{0}.cnf' -f [Guid]::NewGuid().ToString('N'))
    $errPath = Join-Path $env:TEMP ('awbk_{0}.err' -f [Guid]::NewGuid().ToString('N'))

    Write-Log "开始备份 $db（来源：$($Target.Source)，主机：$($Target.Host):$($Target.Port)）"
    try {
        # 把地址、端口、账号、密码写进临时配置文件，通过 --defaults-extra-file 交给 mysqldump。
        # 这样密码不会出现在命令行里（任务管理器和计划任务都看不到）。
        $cnf = "[client]`nhost=$($Target.Host)`nport=$($Target.Port)`nuser=$($Target.User)`npassword=""$($Target.Password)""`n"
        # 以无 BOM 的 UTF-8 写入，mysqldump 不认 BOM。
        [System.IO.File]::WriteAllText($cnfPath, $cnf, (New-Object System.Text.UTF8Encoding($false)))
        # 收紧文件权限：去掉继承，只允许当前运行身份读取。用完整身份名而不是用户名，兼容计划任务的 SYSTEM 账号。
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        & icacls.exe $cnfPath /inheritance:r /grant:r "${identity}:R" | Out-Null

        # mysqldump 参数列表。--defaults-extra-file 必须是第一个参数。
        $dumpArgs = @(
            "--defaults-extra-file=`"$cnfPath`"",   # 从临时文件读取连接信息和密码
            '--single-transaction',                  # InnoDB 一致性快照，备份期间不锁表，生产中也能跑
            '--routines',                            # 包含存储过程和函数
            '--events',                              # 包含定时事件
            '--triggers',                            # 包含触发器
            '--hex-blob',                            # 二进制字段用十六进制导出，避免被编码破坏
            '--default-character-set=utf8mb4',       # 按 utf8mb4 导出，中文不乱码
            "--result-file=`"$sqlPath`"",            # 由 mysqldump 直接写文件，不经 PowerShell 重定向改写编码
            $db                                      # 要备份的库名
        )
        # 启动 mysqldump 并等待结束；错误输出重定向到临时文件便于记录。
        $proc = Start-Process -FilePath $Dumper -ArgumentList $dumpArgs -NoNewWindow -Wait -PassThru -RedirectStandardError $errPath
        # 读取错误输出。用 ReadAllText 而不是 Get-Content -Raw：后者在 5.1 下读空文件返回 null，会导致后面 Trim 报错。
        $stderr = ''
        if (Test-Path -LiteralPath $errPath) {
            $stderr = [System.IO.File]::ReadAllText($errPath).Trim()
        }

        # 退出码非 0 就是失败，把 mysqldump 的错误信息一起带出。
        if ($proc.ExitCode -ne 0) {
            throw "mysqldump 退出码 $($proc.ExitCode)。$stderr"
        }
        # 退出码为 0 但文件不存在或为空，同样视为失败。
        if (-not (Test-Path -LiteralPath $sqlPath) -or (Get-Item -LiteralPath $sqlPath).Length -eq 0) {
            throw "mysqldump 未生成有效文件。$stderr"
        }
        # 有警告文本但成功了，记为 WARN 级别。
        if ($stderr) { Write-Log "mysqldump 提示：$stderr" 'WARN' }

        # 压缩成 zip，再删掉原始 sql。
        Compress-Archive -LiteralPath $sqlPath -DestinationPath $zipPath -CompressionLevel Optimal -Force
        Remove-Item -LiteralPath $sqlPath -Force
        # 记录压缩包大小（MB，保留两位小数）。
        $sizeMb = [Math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 2)
        Write-Log "备份完成：$zipPath（$sizeMb MB）"

        # 本库备份成功后再清理它的旧备份，避免备份失败时把旧的也删光。
        Remove-ExpiredBackups -DatabaseName $db
        return $true
    }
    catch {
        # 写明错误原因和出错的脚本行号，便于排查。
        Write-Log "备份 $db 失败：$($_.Exception.Message)（脚本第 $($_.InvocationInfo.ScriptLineNumber) 行）" 'ERROR'
        # 失败时清掉可能残留的不完整 sql，避免被误当成有效备份。
        if (Test-Path -LiteralPath $sqlPath) { Remove-Item -LiteralPath $sqlPath -Force -ErrorAction SilentlyContinue }
        return $false
    }
    finally {
        # 无论成败都删除临时文件，尤其是含密码的 cnf。
        foreach ($tmp in @($cnfPath, $errPath)) {
            if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue }
        }
    }
}

# ---------- 主流程 ----------
# 统计失败的库数量。
$failed = 0
try {
    # 找到 mysqldump 并记录用的是哪一个。
    $dumper = Find-Mysqldump -Explicit $MysqldumpPath
    Write-Log "使用 mysqldump：$dumper"
    # 收集所有目标库，逐个备份；一个失败不影响其余。
    $targets = Get-ConnectionTargets
    foreach ($target in $targets) {
        if (-not (Backup-Database -Target $target -Dumper $dumper)) { $failed++ }
    }
}
catch {
    # 找不到 mysqldump、配置文件不存在等前置错误，直接中止。
    Write-Log "备份中止：$($_.Exception.Message)" 'ERROR'
    exit 1
}

# 有失败就以 1 退出，计划任务会显示非 0 结果。
if ($failed -gt 0) {
    Write-Log "本次有 $failed 个库备份失败" 'ERROR'
    exit 1
}
Write-Log '全部备份成功'
exit 0

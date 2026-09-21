<#
=====================================================================================
 AutoWeldSystem 上位机一键更新脚本  update-ui.ps1
=====================================================================================

【这个脚本做什么】
  把 U 盘（本脚本所在目录）里的新版 AutoWeldSystem.UI.exe 和 Assets 文件夹复制到工控机的程序目录，覆盖旧版本。
  数据库配置在 C:\ProgramData\AutoWeldSystem\appsettings.json，不在程序目录里，更新不会碰它。
  1. 通过程序的单实例互斥体判断程序是否在运行；在运行就提示先退出并中止，不会强行结束进程。
  2. 把旧版 exe、旧版多文件布局（*.dll、*.pdb、*.deps.json、*.runtimeconfig.json、runtimes、语言资源目录）
     和旧 Assets 整体移到程序目录下的 previous-version 文件夹，只保留最近一次的旧版本。
  3. 复制新 exe 和 Assets；备份或复制任一步失败时自动把 previous-version 里的内容移回去。
  4. 打印更新前后的版本号和数据库配置文件状态。
  程序目录里的 Logs、Data、appsettings.json 以及其他不属于程序布局的文件一律不动。

【怎么用】
  现场工程师双击同目录下的 update-ui.cmd 即可，不用打开 PowerShell。
  程序目录默认 D:\AutoWeld\UI；现场不一样时用记事本打开 update-ui.cmd，改 TARGET= 那一行。
  也可以在 PowerShell 窗口里带参数运行：
            powershell -ExecutionPolicy Bypass -File update-ui.ps1 -TargetDir D:\AutoWeld\UI

【参数说明】
  -TargetDir   工控机上的程序目录（AutoWeldSystem.UI.exe 所在目录），默认 D:\AutoWeld\UI。
               目录不存在或里面没有 exe 时按首次安装处理，会自动创建目录。
  -SourceDir   新版本所在目录，默认本脚本所在目录，一般不用填。

【回退】
  退出程序，删除程序目录里的 AutoWeldSystem.UI.exe 和 Assets，
  把 previous-version 文件夹里的全部内容复制回程序目录即可。

【常见问题】
  提示“程序正在运行”          → 先在程序里退出（或任务管理器结束 AutoWeldSystem.UI.exe），再重新运行。
  提示“找不到新版本文件”       → U 盘上本脚本旁边必须有 AutoWeldSystem.UI.exe 和 Assets 文件夹。
  提示“拒绝访问”              → 程序目录不可写，右键 update-ui.cmd 选“以管理员身份运行”。
  中文显示乱码                → 本文件必须以“UTF-8 带 BOM”编码保存，用记事本另存为时选该编码。
=====================================================================================
#>

[CmdletBinding()]
param(
    # 工控机上的程序目录。
    [string]$TargetDir = 'D:\AutoWeld\UI',

    # 新版本所在目录，留空时取脚本自身所在目录。
    [string]$SourceDir
)

# 任何命令出错都当作异常抛出，避免半更新状态被忽略。
$ErrorActionPreference = 'Stop'

if (-not $SourceDir) { $SourceDir = $PSScriptRoot }

$exeName = 'AutoWeldSystem.UI.exe'
$assetsName = 'Assets'
$previousName = 'previous-version'
# 与程序 Program.cs 中的单实例互斥体名称一致。
$mutexName = 'Global\AutoWeldSystem'
$configPath = Join-Path $env:ProgramData 'AutoWeldSystem\appsettings.json'

# ---------- 函数：读取 exe 文件版本 ----------
function Get-FileVersionText {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '（无）' }
    $version = (Get-Item -LiteralPath $Path).VersionInfo.FileVersion
    if ($version) { return $version }
    return '（未知）'
}

# ---------- 函数：判断程序是否在运行 ----------
function Test-ProgramRunning {
    $mutex = $null
    try {
        # 程序启动时会创建这个全局互斥体，能打开就说明程序还在运行。
        return [System.Threading.Mutex]::TryOpenExisting($mutexName, [ref]$mutex)
    }
    catch [System.UnauthorizedAccessException] {
        # 互斥体存在但当前账号无权打开（程序以别的账号运行），同样视为在运行。
        return $true
    }
    finally {
        if ($mutex) { $mutex.Dispose() }
    }
}

# ---------- 函数：识别旧版多文件布局中的语言资源目录 ----------
# 例如 en、zh-Hans、pt-BR，且目录里只有 *.resources.dll。
function Test-CultureResourceDirectory {
    param([System.IO.DirectoryInfo]$Directory)
    if ($Directory.Name -cnotmatch '^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$') { return $false }
    $files = @(Get-ChildItem -LiteralPath $Directory.FullName -File -Recurse)
    if ($files.Count -eq 0) { return $false }
    foreach ($file in $files) {
        if ($file.Name -notlike '*.resources.dll') { return $false }
    }
    return $true
}

# ---------- 函数：收集旧版本文件 ----------
# 只挑属于程序布局的内容：exe、程序集、符号、运行时描述文件、runtimes、语言资源目录、Assets。
function Get-PreviousVersionItems {
    param([string]$Directory)
    $items = @(Get-ChildItem -LiteralPath $Directory -File | Where-Object {
        $_.Name -ieq $exeName -or
        $_.Extension -in '.dll', '.pdb' -or
        $_.Name -like '*.deps.json' -or
        $_.Name -like '*.runtimeconfig.json'
    })
    $items += @(Get-ChildItem -LiteralPath $Directory -Directory | Where-Object {
        $_.Name -ieq 'runtimes' -or
        $_.Name -ieq $assetsName -or
        (Test-CultureResourceDirectory $_)
    })
    return $items
}

# ---------- 函数：更新失败时把旧版本移回程序目录 ----------
# 无论是备份阶段还是复制阶段失败都可调用：已备份的项先删掉程序目录里的同名新文件再移回，未备份的项原样未动。
function Restore-PreviousVersion {
    param([string]$PreviousDir, [string]$Directory)
    foreach ($item in Get-ChildItem -LiteralPath $PreviousDir) {
        $destination = Join-Path $Directory $item.Name
        if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
        Move-Item -LiteralPath $item.FullName -Destination $Directory -Force
    }
    Remove-Item -LiteralPath $PreviousDir -Recurse -Force
}

# ---------- 检查新版本文件 ----------
$sourceExe = Join-Path $SourceDir $exeName
$sourceAssets = Join-Path $SourceDir $assetsName
if (-not (Test-Path -LiteralPath $sourceExe)) { throw "找不到新版本文件：$sourceExe" }
if (-not (Test-Path -LiteralPath $sourceAssets)) { throw "找不到新版本资源目录：$sourceAssets" }

$targetExe = Join-Path $TargetDir $exeName
$oldVersion = Get-FileVersionText $targetExe
$newVersion = Get-FileVersionText $sourceExe

Write-Host "新版本来源：$SourceDir"
Write-Host "程序目录：  $TargetDir"
Write-Host "当前版本：  $oldVersion"
Write-Host "新版本：    $newVersion"
Write-Host ''

if (Test-ProgramRunning) {
    throw '程序正在运行，请先退出 AutoWeldSystem 再更新。'
}

if (-not (Test-Path -LiteralPath $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    Write-Host '程序目录不存在，按首次安装处理，已创建目录。'
}

# 源目录和程序目录相同时没有可更新的内容，且会把新版本自己移进 previous-version。
$resolvedSource = (Resolve-Path -LiteralPath $SourceDir).Path.TrimEnd('\')
$resolvedTarget = (Resolve-Path -LiteralPath $TargetDir).Path.TrimEnd('\')
if ($resolvedSource -ieq $resolvedTarget) {
    throw '新版本目录与程序目录相同，请把新版本放在 U 盘或其他目录后再运行。'
}

# ---------- 备份旧版本并复制新版本 ----------
$previousDir = Join-Path $TargetDir $previousName
$hadPrevious = Test-Path -LiteralPath $targetExe
try {
    if ($hadPrevious) {
        # 只保留最近一次的旧版本。
        if (Test-Path -LiteralPath $previousDir) { Remove-Item -LiteralPath $previousDir -Recurse -Force }
        New-Item -ItemType Directory -Path $previousDir -Force | Out-Null
        foreach ($item in Get-PreviousVersionItems $TargetDir) {
            Move-Item -LiteralPath $item.FullName -Destination $previousDir -Force
        }
        Write-Host "旧版本已移到：$previousDir"
    }

    Copy-Item -LiteralPath $sourceExe -Destination $targetExe -Force
    $targetAssets = Join-Path $TargetDir $assetsName
    New-Item -ItemType Directory -Path $targetAssets -Force | Out-Null
    Copy-Item -Path (Join-Path $sourceAssets '*') -Destination $targetAssets -Recurse -Force
}
catch {
    if ($hadPrevious -and (Test-Path -LiteralPath $previousDir)) {
        Restore-PreviousVersion -PreviousDir $previousDir -Directory $TargetDir
        Write-Host '更新失败，已恢复旧版本。'
    }
    throw
}

# ---------- 汇报结果 ----------
Write-Host ''
Write-Host "更新完成：$oldVersion -> $(Get-FileVersionText $targetExe)"
if (Test-Path -LiteralPath $configPath) {
    Write-Host "数据库配置：$configPath（已存在，保持不变）"
}
elseif (Test-Path -LiteralPath (Join-Path $TargetDir 'appsettings.json')) {
    Write-Host "数据库配置：$configPath 尚不存在，程序首次启动会自动从程序目录的 appsettings.json 复制一份。"
}
else {
    Write-Host "数据库配置：$configPath 尚不存在，程序首次启动会生成模板并提示填写后退出。"
}
Write-Host '现在可以通过桌面快捷方式启动程序，或重启电脑由开机自启拉起。'

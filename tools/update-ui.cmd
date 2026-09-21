@echo off
rem AutoWeldSystem UI updater. Double-click to run.
rem If the program is installed somewhere else, edit the TARGET line below.
set "TARGET=D:\AutoWeld\UI"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0update-ui.ps1" -TargetDir "%TARGET%"
echo.
if errorlevel 1 (
    echo Update FAILED. See the messages above.
) else (
    echo Update finished.
)
pause

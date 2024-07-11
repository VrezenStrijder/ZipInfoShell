@echo off

NET SESSION >nul 2>&1
if %errorLevel% neq 0 (
    echo 此脚本需要管理员权限运行。
    pause
    exit /b 1
)

cd /d "%~dp0"
RegAsm.exe /unregister ZipInfoShell.dll

taskkill /F /IM explorer.exe
start explorer.exe

pause&&exit
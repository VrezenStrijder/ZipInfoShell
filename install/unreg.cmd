@echo off

NET SESSION >nul 2>&1
if %errorLevel% neq 0 (
    echo �˽ű���Ҫ����ԱȨ�����С�
    pause
    exit /b 1
)

cd /d "%~dp0"
RegAsm.exe /unregister ZipInfoShell.dll

taskkill /F /IM explorer.exe
start explorer.exe

pause&&exit
@echo off

NET SESSION >nul 2>&1
if %errorLevel% neq 0 (
    echo �˽ű���Ҫ����ԱȨ�����С�
    pause
    exit /b 1
)

cd /d "%~dp0"
copy 7z.dll %systemroot%\system32
regasm.exe ZipInfoShell.dll /Codebase

taskkill /F /IM explorer.exe
start explorer.exe

pause&&exit
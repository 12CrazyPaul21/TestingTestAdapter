@echo off
setlocal

call scripts\vs-env.bat
if %errorlevel% neq 0 exit /b 1

dotnet tool restore > nul

powershell -ExecutionPolicy Bypass .\scripts\bump.ps1 %*

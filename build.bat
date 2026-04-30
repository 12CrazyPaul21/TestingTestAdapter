@echo off
setlocal

call scripts\vs-env.bat
if %errorlevel% neq 0 exit /b 1

msbuild src\TestingTestAdapter.sln /t:Restore /p:Configuration=Release /p:Platform="Any CPU"
msbuild src\TestingTestAdapter.sln /p:Configuration=Release /p:Platform="Any CPU" /m

if %errorlevel% neq 0 (
    echo.
    echo Build FAILED!
    exit /b 1
)

for /f "usebackq tokens=*" %%i in (`msbuild src\TestingTestAdapter\TestingTestAdapter.csproj /t:GetVsixVersion /nologo -getProperty:VsixVersion 2^>nul`) do (
    set "VSIX_VERSION=%%i"
)

if not exist "artifacts" mkdir artifacts
copy /y "src\TestingTestAdapter\bin\Release\Phantom.Testing.TestAdapter.vsix" "artifacts\TestingTestAdapter-%VSIX_VERSION%.vsix" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Failed to copy vsix file
    exit /b 1
)
echo Copied to artifacts\Phantom.Testing.TestAdapter.%VSIX_VERSION%.vsix

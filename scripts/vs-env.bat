@echo off

if defined VSINSTALLDIR goto :vs_found

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -property installationPath`) do (
        call "%%i\Common7\Tools\VsDevCmd.bat" >nul
    )
)

if not defined VSINSTALLDIR (
    echo ERROR: must run in Developer Command Prompt for Visual Studio
    echo Press any key to exit...
    exit /b 1
)

:vs_found

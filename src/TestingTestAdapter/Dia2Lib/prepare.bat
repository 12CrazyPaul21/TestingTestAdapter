@echo off

if not defined VSINSTALLDIR (
    echo ERROR: must run in Developer Command Prompt for Visual Studio
    exit /b 1
)

if not exist "%VSINSTALLDIR%\DIA SDK" (
    echo The DIA SDK could not be found.
    exit /b 1
)

set DIA_SDK=%VSINSTALLDIR%\DIA SDK

midl /I "%DIA_SDK%\include" "%DIA_SDK%\idl\dia2.idl" /tlb dia2.tlb /header nul /iid nul /proxy nul /dlldata nul
tlbimp dia2.tlb /out:dia2.dll /namespace:Microsoft.Dia
REM tlbimp dia2.tlb /out:dia2.dll /namespace:Microsoft.Dia /machine:x64

if not exist x86 mkdir x86
if not exist x64 mkdir x64

copy /y "%DIA_SDK%\bin\msdia140.dll" x86
copy /y "%DIA_SDK%\bin\amd64\msdia140.dll" x64

echo 1 > dia2.prepared

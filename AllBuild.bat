@echo off
setlocal

set "CONFIG=%~1"

cd /d "%~dp0"

if exist "Compile.log" del /q "Compile.log"

rem ========================================
rem Visual Studio environment
rem 2026がなければ、2022でビルドする
rem ========================================
set "VS2022=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat"
set "VS2026=C:\Program Files\Microsoft Visual Studio\2026\Professional\Common7\Tools\VsDevCmd.bat"

if exist "%VS2026%" (
    echo Visual Studio 2026 Professional is used.
    call "%VS2026%"
) else if exist "%VS2022%" (
    echo Visual Studio 2026 was not found.
    echo Visual Studio 2022 Professional is used.
    call "%VS2022%"
) else (
    echo ERROR: Visual Studio 2022 or 2026 Professional was not found.
    exit /b 1
)

if errorlevel 1 exit /b 1

rem ========================================
rem ControllerEngineeringCommon
rem ========================================
MSBuild ".\YCEService\ControllerEngineeringCommon.sln" ^
    /restore ^
    /t:Rebuild ^
    /p:Configuration=%CONFIG% ^
    /p:Platform="Any CPU" ^
    /flp:LogFile="Compile.log";Verbosity=normal;Append

if errorlevel 1 exit /b 1

rem ========================================
rem ControllerEngineeringLegacyBridgeService
rem ========================================
MSBuild ".\YCEService\ControllerLegacyBridgeService.sln" ^
    /restore ^
    /t:Rebuild ^
    /p:Configuration=%CONFIG% ^
    /p:Platform=x86 ^
    /flp:LogFile="Compile.log";Verbosity=normal;Append

if errorlevel 1 exit /b 1

rem ========================================
rem ControllerEngineeringService
rem ========================================
MSBuild ".\YCEService\ControllerEngineeringService.sln" ^
    /restore ^
    /t:Rebuild ^
    /p:Configuration=%CONFIG% ^
    /p:Platform="Any CPU" ^
    /flp:LogFile="Compile.log";Verbosity=normal;Append

if errorlevel 1 exit /b 1

rem ========================================
rem ControllerEngineeringStudio
rem ========================================
MSBuild ".\YCEStudio\ControllerEngineeringStudio.sln" ^
    /restore ^
    /t:Rebuild ^
    /p:Configuration=%CONFIG% ^
    /p:Platform="Any CPU" ^
    /flp:LogFile="Compile.log";Verbosity=normal;Append

if errorlevel 1 exit /b 1

echo Build succeeded.
exit /b 0
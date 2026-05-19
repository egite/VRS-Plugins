@echo off
rem Shared MSBuild helper for VRS plugins.
rem Usage:  _build-plugin.bat ^<PluginName^> [Config]
rem        Config defaults to Release.
rem Called by the per-plugin build-^<Name^>.bat wrappers.
rem
rem Plugin source uses C# 6+ features (string interpolation), so we MUST find a
rem Roslyn-era MSBuild (VS 2015+ / MSBuild 14.0+). Framework MSBuild 4.0 will fail.

setlocal EnableExtensions

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "NAME=%~1"
set "CFG=%~2"
if "%NAME%"=="" (
    echo Usage: _build-plugin.bat ^<PluginName^> [Config]
    exit /b 1
)
if "%CFG%"=="" set "CFG=Release"

set "PROJ=%ROOT%\Plugin.%NAME%\Plugin.%NAME%.csproj"
if not exist "%PROJ%" (
    echo ERROR: project not found: %PROJ%
    exit /b 1
)

set "MSBUILD="

rem 1. Try vswhere (most reliable on any VS 2017+ box).
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%I in (`call "%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"`) do (
        if not defined MSBUILD set "MSBUILD=%%I"
    )
)

rem 2. Common VS 2022 / 2019 / 2017 paths if vswhere unavailable.
if not defined MSBUILD call :probe "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
if not defined MSBUILD call :probe "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"

if not defined MSBUILD (
    echo ERROR: No suitable MSBuild found.
    echo This project uses C# 6+ syntax which requires MSBuild 14.0+ ^(VS 2015 or later^).
    echo Install Visual Studio Community or the Build Tools for Visual Studio.
    exit /b 1
)

echo MSBuild: %MSBUILD%
echo Building Plugin.%NAME% [%CFG%]...
"%MSBUILD%" "%PROJ%" /t:Rebuild /p:Configuration=%CFG% /p:Platform=AnyCPU /v:minimal /nologo
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" (
    echo.
    echo BUILD FAILED ^(exit %RC%^) for Plugin.%NAME%
    exit /b %RC%
)

echo.
echo Built: %ROOT%\Plugin.%NAME%\bin\%CFG%\VirtualRadar.Plugin.%NAME%.dll
exit /b 0


:probe
if exist "%~1" set "MSBUILD=%~1"
exit /b 0

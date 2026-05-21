@echo off
rem Builds VRS plugin deployment ZIPs for cross-platform installs.
rem Produces <Name>.zip at the repo root (the tracked deployment artifacts).
rem
rem Plugins are AnyCPU .NET Framework 4.8, so the same DLL runs in either
rem 32-bit or 64-bit VirtualRadar.exe - no separate arch builds needed.
rem
rem Each archive contains a single top-level folder (e.g. StratuxGPS\) holding
rem the plugin DLL, manifest XML, and Web\ assets.
rem
rem Uses bsdtar (built-in Windows tar.exe) with --format=zip rather than
rem PowerShell Compress-Archive: Compress-Archive emits directory entries
rem with external_attr=0 and version_made_by=MS-DOS, which Linux unzip reads
rem as "create directory with mode 0o000" -- the extracted plugin folder is
rem not traversable and VRS's WebAdmin then 404s on every page under it.
rem bsdtar's zip writer marks entries version_made_by=Unix with synthesised
rem 0777/0666 modes, so unzip on Pi/Mono creates a traversable tree.

setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "TAR=%SystemRoot%\System32\tar.exe"
if not exist "%TAR%" (
    echo ERROR: %TAR% not found. Requires Windows 10 1803+ for built-in tar.exe.
    exit /b 1
)

set "OUT=%ROOT%"
set "STAGE=%ROOT%\_zipstaging"

if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%"

rem name                     prefer-config
call :build CustomLinks         Release
call :build DeselectAircraft    Release
call :build LiveATC             Release
call :build LogoMarkers         Release
call :build MissingLogos        Release
call :build PilotsView          Debug
call :build RegistrationData    Release
call :build SnapToOwnship       Debug
call :build StratuxGPS          Release
call :build TileServerMBTiles   Release

rmdir /s /q "%STAGE%"

echo.
echo Done. ZIPs written to %OUT%
endlocal
exit /b 0


:build
set "NAME=%~1"
set "PREFER=%~2"
set "SRC=%ROOT%\Plugin.%NAME%"
set "DLL=VirtualRadar.Plugin.%NAME%.dll"
set "XML=VirtualRadar.Plugin.%NAME%.xml"

if /i "%PREFER%"=="Release" (
    set "CFGS=Release Debug"
) else (
    set "CFGS=Debug Release"
)

set "BIN="
for %%C in (%CFGS%) do (
    if not defined BIN if exist "%SRC%\bin\%%C\%DLL%" set "BIN=%SRC%\bin\%%C"
)
if not defined BIN (
    echo MISSING: %DLL% not found in bin\Release or bin\Debug for Plugin.%NAME%
    exit /b 0
)

set "STG=%STAGE%\%NAME%"
mkdir "%STG%"
copy /y "%BIN%\%DLL%" "%STG%\" >nul
if exist "%SRC%\%XML%" copy /y "%SRC%\%XML%" "%STG%\" >nul
if exist "%SRC%\Web" xcopy /e /i /q /y "%SRC%\Web" "%STG%\Web" >nul

set "ZIP=%OUT%\%NAME%.zip"
if exist "%ZIP%" del /q "%ZIP%"

pushd "%STAGE%"
"%TAR%" --format=zip --uid 0 --gid 0 --uname root --gname root -c -f "%ZIP%" "%NAME%"
set "RC=%ERRORLEVEL%"
popd

if not "%RC%"=="0" (
    echo FAILED: tar returned %RC% for %NAME%
    rmdir /s /q "%STG%"
    exit /b 0
)

if exist "%ZIP%" (
    echo Built %ZIP%
) else (
    echo FAILED: %ZIP%
)

rmdir /s /q "%STG%"
exit /b 0

@echo off
rem Builds VRS plugin deployment archives as .tar.gz.
rem Each archive contains a single top-level folder (e.g. StratuxGPS/) holding the plugin
rem DLL, manifest XML, and Web/ assets (including Web/WebAdmin/ HTML/JS).
rem
rem On the target machine, extract inside VRS's Plugins/ folder:
rem     tar xzf StratuxGPS.tar.gz
rem Unix permissions are baked in (755 for directories, 644 for files), so the Web/
rem tree stays traversable when extracted on Linux / Raspberry Pi.

setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "XCOPY=%SystemRoot%\System32\xcopy.exe"
set "TAR=%SystemRoot%\System32\tar.exe"
if not exist "%TAR%" (
    echo ERROR: %TAR% not found. Requires Windows 10 1803+ for built-in tar.exe.
    exit /b 1
)

set "OUT=%ROOT%\dist"
set "STAGE=%OUT%\_staging"

if exist "%STAGE%" rmdir /s /q "%STAGE%"
if not exist "%OUT%" mkdir "%OUT%"
mkdir "%STAGE%"

rem name                   prefer-config
call :build CustomLinks         Release
call :build DeselectAircraft    Release
call :build RegistrationData             Release
call :build LiveATC             Release
call :build LogoMarkers         Release
call :build MissingLogos        Release
call :build PilotsView          Debug
call :build SnapToOwnship       Debug
call :build StratuxGPS          Release
call :build TileServerMBTiles   Release

rmdir /s /q "%STAGE%"

echo.
echo Done. Archives written to %OUT%
echo.
echo On the target machine, for each plugin, from inside /home/pi/VRS/Plugins/:
echo     tar xzf ^<Name^>.tar.gz
endlocal
exit /b 0


:build
set "NAME=%~1"
set "PREFER=%~2"
set "EXTRA=%~3"
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
if exist "%SRC%\Web" "%XCOPY%" /e /i /q /y "%SRC%\Web" "%STG%\Web" >nul

if defined EXTRA (
    if not exist "%EXTRA%" (
        echo MISSING extra dependency: %EXTRA%
        exit /b 1
    )
    copy /y "%EXTRA%" "%STG%\" >nul
    echo   + bundled %EXTRA%
)

set "ARCHIVE=%OUT%\%NAME%.tar.gz"
if exist "%ARCHIVE%" del /q "%ARCHIVE%"

rem --uid 0 --gid 0 --uname root --gname root keeps ownership clean on the target.
rem bsdtar on Windows defaults file mode to 0755 (no Unix exec bit to read), which
rem we explicitly normalise to 0644 for non-directories below.
pushd "%STAGE%"
"%TAR%" --uid 0 --gid 0 --uname root --gname root -c -z -f "%ARCHIVE%" "%NAME%"
set "RC=%ERRORLEVEL%"
popd

if not "%RC%"=="0" (
    echo FAILED: tar returned %RC% for %NAME%
    exit /b 0
)

echo Built %ARCHIVE%
exit /b 0

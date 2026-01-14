@echo off
REM Script to download and install Luau for Windows

setlocal enabledelayedexpansion

set LUAU_VERSION=0.703
set LUAU_DIR=.luau
set LUAU_ANALYZE=%LUAU_DIR%\luau-analyze.exe

REM Check if already installed
if exist "%LUAU_ANALYZE%" (
    echo Luau is already installed at %LUAU_ANALYZE%
    exit /b 0
)

echo Downloading Luau v%LUAU_VERSION%...

REM Create directory
if not exist "%LUAU_DIR%" mkdir "%LUAU_DIR%"

REM Download for Windows x64
set LUAU_URL=https://github.com/luau-lang/luau/releases/download/%LUAU_VERSION%/luau-windows.zip
powershell -Command "Invoke-WebRequest -Uri '!LUAU_URL!' -OutFile '%LUAU_DIR%\luau-windows.zip'"

REM Extract
powershell -Command "Expand-Archive -Path '%LUAU_DIR%\luau-windows.zip' -DestinationPath '%LUAU_DIR%' -Force"

REM Cleanup
del "%LUAU_DIR%\luau-windows.zip"

REM Verify installation
if exist "%LUAU_ANALYZE%" (
    echo Luau installed successfully!
    echo   luau: %LUAU_DIR%\luau.exe
    echo   luau-analyze: %LUAU_ANALYZE%

    REM Verify
    "%LUAU_ANALYZE%" --version
) else (
    echo ERROR: Failed to install Luau
    exit /b 1
)

@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  TheWatch - Stop Workflows
echo ============================================
echo.

:: Check gh CLI is available
where gh >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: GitHub CLI ^(gh^) is not installed or not in PATH.
    echo Install from: https://cli.github.com
    exit /b 1
)

:: Map workflow keys to filenames
set "file_build=build.yml"
set "file_test=test.yml"
set "file_code-quality=code-quality.yml"
set "file_security-scan=security-scan.yml"
set "file_docker-build=docker-build.yml"
set "file_release=release.yml"

set disabled=0
set skipped=0

:: Parse workflow-settings.json and disable each enabled workflow
for %%W in (build test code-quality security-scan docker-build release) do (
    set "key=%%W"
    set "filename=!file_%%W!"

    :: Use PowerShell to read the JSON value
    for /f %%V in ('powershell -NoProfile -Command "(Get-Content 'scripts\workflow-settings.json' | ConvertFrom-Json).workflows.'%%W'"') do (
        if /i "%%V"=="True" (
            echo [DISABLING] %%W
            gh workflow disable !filename! 2>nul
            if !errorlevel! neq 0 (
                echo   WARNING: Could not disable !filename!
            ) else (
                echo   Disabled !filename!
                set /a disabled+=1
            )
        ) else (
            echo [SKIPPED]   %%W ^(already off in settings^)
            set /a skipped+=1
        )
    )
)

echo.
echo ============================================
echo  Done: %disabled% disabled, %skipped% skipped
echo ============================================

endlocal

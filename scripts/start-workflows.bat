@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  TheWatch - Start Workflows
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

set enabled=0
set skipped=0

:: Parse workflow-settings.json and process each workflow
for %%W in (build test code-quality security-scan docker-build release) do (
    set "key=%%W"
    set "filename=!file_%%W!"

    :: Use PowerShell to read the JSON value
    for /f %%V in ('powershell -NoProfile -Command "(Get-Content 'scripts\workflow-settings.json' | ConvertFrom-Json).workflows.'%%W'"') do (
        if /i "%%V"=="True" (
            echo [ENABLING] %%W
            gh workflow enable !filename! 2>nul
            if !errorlevel! neq 0 (
                echo   WARNING: Could not enable !filename!
            ) else (
                echo   Enabled !filename!
                echo [RUNNING]  %%W
                gh workflow run !filename! 2>nul
                if !errorlevel! neq 0 (
                    echo   WARNING: Could not trigger !filename!
                ) else (
                    echo   Triggered !filename!
                )
                set /a enabled+=1
            )
        ) else (
            echo [SKIPPED]  %%W
            set /a skipped+=1
        )
    )
)

echo.
echo ============================================
echo  Done: %enabled% enabled, %skipped% skipped
echo ============================================

endlocal

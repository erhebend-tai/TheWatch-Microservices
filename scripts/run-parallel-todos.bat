@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1

:: ============================================================================
:: TheWatch — Parallel TODO Completion via Claude Code
:: Launches 8 Claude instances across 2 waves using git worktrees
:: ============================================================================

set "REPO_ROOT=%~dp0"
set "REPO_ROOT=%REPO_ROOT:~0,-1%"
set "WORKTREE_BASE=%REPO_ROOT%\..\worktrees"
set "PROMPT_DIR=%REPO_ROOT%\scripts\parallel-build\prompts"
set "LOG_DIR=%REPO_ROOT%\scripts\parallel-build\logs"
set "TIMESTAMP=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

echo ============================================================================
echo   TheWatch — Parallel TODO Builder
echo   %DATE% %TIME%
echo ============================================================================
echo.

:: ============================================================================
:: PHASE 0: SETUP — Validate state, create directories
:: ============================================================================

echo [Phase 0] Setting up...

:: Check git is available
git --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: git not found in PATH
    exit /b 1
)

:: Check claude is available
claude --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: claude CLI not found in PATH
    exit /b 1
)

:: Create directories
if not exist "%WORKTREE_BASE%" mkdir "%WORKTREE_BASE%"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

:: Verify we're in a git repo
cd /d "%REPO_ROOT%"
git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo ERROR: %REPO_ROOT% is not a git repository
    exit /b 1
)

:: Get current branch
for /f "tokens=*" %%i in ('git rev-parse --abbrev-ref HEAD') do set "CURRENT_BRANCH=%%i"
echo   Current branch: %CURRENT_BRANCH%

:: Check for uncommitted changes
git diff --quiet 2>nul
if errorlevel 1 (
    echo WARNING: You have uncommitted changes. Commit or stash before running.
    echo Press any key to continue anyway, or Ctrl+C to abort...
    pause >nul
)

echo   Worktrees: %WORKTREE_BASE%
echo   Logs: %LOG_DIR%
echo   Prompts: %PROMPT_DIR%
echo.

:: ============================================================================
:: PHASE 1: WAVE 1 — Create 6 worktrees and launch Claude instances
:: ============================================================================

echo ============================================================================
echo [Phase 1] WAVE 1 — Launching 6 parallel Claude instances
echo ============================================================================
echo.

:: Define stream names
set "STREAMS=stream1-schema stream2-auth stream3-mobile stream4-docker-k8s stream5-cloud-azure stream6-cloud-gcp-cf"

:: Create worktrees for Wave 1
for %%S in (%STREAMS%) do (
    set "WT_PATH=%WORKTREE_BASE%\%%S"
    set "BRANCH=todo/%%S"

    echo   Creating worktree: %%S
    if exist "!WT_PATH!" (
        echo     Removing existing worktree...
        git worktree remove "!WT_PATH!" --force 2>nul
        git branch -D "!BRANCH!" 2>nul
    )
    git worktree add "!WT_PATH!" -b "!BRANCH!" %CURRENT_BRANCH%
    if errorlevel 1 (
        echo ERROR: Failed to create worktree for %%S
        exit /b 1
    )
)

echo.
echo   All 6 worktrees created. Launching Claude instances...
echo.

:: Launch Claude for each Wave 1 stream
:: Each runs in its own worktree with a file-based prompt

:: Stream 1: SCHEMA
set "S1_LOG=%LOG_DIR%\stream1-schema_%TIMESTAMP%.log"
echo   [Stream 1] SCHEMA — Items 15-21 (SQL schema adaptation)
start "Stream1-SCHEMA" /B cmd /c "cd /d "%WORKTREE_BASE%\stream1-schema" && claude -p "$(cat "%PROMPT_DIR%\stream1-schema.md")" --dangerously-skip-permissions --model sonnet > "%S1_LOG%" 2>&1 && echo STREAM1_DONE > "%LOG_DIR%\stream1.done""

:: Stream 2: AUTH
set "S2_LOG=%LOG_DIR%\stream2-auth_%TIMESTAMP%.log"
echo   [Stream 2] AUTH — Items 61-80 (full auth stack)
start "Stream2-AUTH" /B cmd /c "cd /d "%WORKTREE_BASE%\stream2-auth" && claude -p "$(cat "%PROMPT_DIR%\stream2-auth.md")" --dangerously-skip-permissions --model sonnet > "%S2_LOG%" 2>&1 && echo STREAM2_DONE > "%LOG_DIR%\stream2.done""

:: Stream 3: MOBILE
set "S3_LOG=%LOG_DIR%\stream3-mobile_%TIMESTAMP%.log"
echo   [Stream 3] MOBILE — Items 57-59, 81-105 (MAUI production)
start "Stream3-MOBILE" /B cmd /c "cd /d "%WORKTREE_BASE%\stream3-mobile" && claude -p "$(cat "%PROMPT_DIR%\stream3-mobile.md")" --dangerously-skip-permissions --model sonnet > "%S3_LOG%" 2>&1 && echo STREAM3_DONE > "%LOG_DIR%\stream3.done""

:: Stream 4: DOCKER-K8S
set "S4_LOG=%LOG_DIR%\stream4-docker-k8s_%TIMESTAMP%.log"
echo   [Stream 4] DOCKER-K8S — Items 106-120 (containerization + CI/CD)
start "Stream4-DOCKER" /B cmd /c "cd /d "%WORKTREE_BASE%\stream4-docker-k8s" && claude -p "$(cat "%PROMPT_DIR%\stream4-docker-k8s.md")" --dangerously-skip-permissions --model sonnet > "%S4_LOG%" 2>&1 && echo STREAM4_DONE > "%LOG_DIR%\stream4.done""

:: Stream 5: CLOUD-AZURE
set "S5_LOG=%LOG_DIR%\stream5-cloud-azure_%TIMESTAMP%.log"
echo   [Stream 5] CLOUD-AZURE — Items 121-131 (Azure Bicep)
start "Stream5-AZURE" /B cmd /c "cd /d "%WORKTREE_BASE%\stream5-cloud-azure" && claude -p "$(cat "%PROMPT_DIR%\stream5-cloud-azure.md")" --dangerously-skip-permissions --model sonnet > "%S5_LOG%" 2>&1 && echo STREAM5_DONE > "%LOG_DIR%\stream5.done""

:: Stream 6: CLOUD-GCP-CF
set "S6_LOG=%LOG_DIR%\stream6-cloud-gcp-cf_%TIMESTAMP%.log"
echo   [Stream 6] CLOUD-GCP-CF — Items 132-140 (Google Cloud + Cloudflare)
start "Stream6-GCP" /B cmd /c "cd /d "%WORKTREE_BASE%\stream6-cloud-gcp-cf" && claude -p "$(cat "%PROMPT_DIR%\stream6-cloud-gcp-cf.md")" --dangerously-skip-permissions --model sonnet > "%S6_LOG%" 2>&1 && echo STREAM6_DONE > "%LOG_DIR%\stream6.done""

echo.
echo   All 6 streams launched. Waiting for completion...
echo   Monitor progress: tail -f %LOG_DIR%\stream*.log
echo.

:: ============================================================================
:: PHASE 1b: WAIT for all Wave 1 streams to complete
:: ============================================================================

:WAIT_WAVE1
set "DONE_COUNT=0"
for /L %%i in (1,1,6) do (
    if exist "%LOG_DIR%\stream%%i.done" set /a DONE_COUNT+=1
)

if %DONE_COUNT% lss 6 (
    echo   [%TIME%] %DONE_COUNT%/6 streams complete...
    timeout /t 30 /nobreak >nul
    goto WAIT_WAVE1
)

echo.
echo   All 6 Wave 1 streams complete!
echo.

:: ============================================================================
:: PHASE 2: MERGE WAVE 1 — Sequential merge of all 6 branches
:: ============================================================================

echo ============================================================================
echo [Phase 2] Merging Wave 1 branches
echo ============================================================================
echo.

cd /d "%REPO_ROOT%"

:: Merge order: schema -> cloud-azure -> cloud-gcp-cf -> docker-k8s -> auth -> mobile
set "MERGE_ORDER=stream1-schema stream5-cloud-azure stream6-cloud-gcp-cf stream4-docker-k8s stream2-auth stream3-mobile"

for %%S in (%MERGE_ORDER%) do (
    echo   Merging todo/%%S...
    git merge --no-ff "todo/%%S" -m "merge: %%S into %CURRENT_BRANCH%"
    if errorlevel 1 (
        echo ERROR: Merge conflict on todo/%%S!
        echo   Resolve conflicts manually, then re-run from Phase 3.
        echo   Conflict branch: todo/%%S
        exit /b 1
    )
    echo     OK
)

echo.
echo   All Wave 1 branches merged successfully.
echo.

:: Build verification
echo   Running build verification...
dotnet build "%REPO_ROOT%\TheWatch.P1.CoreGateway\TheWatch.P1.CoreGateway.csproj" --nologo -v q >nul 2>&1
if errorlevel 1 (
    echo WARNING: Build verification failed. Wave 2 integration stream will fix.
) else (
    echo     Build OK (P1 sample verified)
)

echo.

:: ============================================================================
:: PHASE 3: WAVE 2 — Create 2 worktrees and launch Claude instances
:: ============================================================================

echo ============================================================================
echo [Phase 3] WAVE 2 — Launching 2 parallel Claude instances
echo ============================================================================
echo.

:: Create worktrees for Wave 2
for %%S in (stream7-advanced stream8-integration) do (
    set "WT_PATH=%WORKTREE_BASE%\%%S"
    set "BRANCH=todo/%%S"

    echo   Creating worktree: %%S
    if exist "!WT_PATH!" (
        git worktree remove "!WT_PATH!" --force 2>nul
        git branch -D "!BRANCH!" 2>nul
    )
    git worktree add "!WT_PATH!" -b "!BRANCH!" %CURRENT_BRANCH%
)

echo.

:: Stream 7: ADVANCED
set "S7_LOG=%LOG_DIR%\stream7-advanced_%TIMESTAMP%.log"
echo   [Stream 7] ADVANCED — Items 141-150 (ML, compliance, graph, observability)
start "Stream7-ADVANCED" /B cmd /c "cd /d "%WORKTREE_BASE%\stream7-advanced" && claude -p "$(cat "%PROMPT_DIR%\stream7-advanced.md")" --dangerously-skip-permissions --model sonnet > "%S7_LOG%" 2>&1 && echo STREAM7_DONE > "%LOG_DIR%\stream7.done""

:: Stream 8: INTEGRATION
set "S8_LOG=%LOG_DIR%\stream8-integration_%TIMESTAMP%.log"
echo   [Stream 8] INTEGRATION — Dashboard map + auth wiring + verification
start "Stream8-INTEGRATE" /B cmd /c "cd /d "%WORKTREE_BASE%\stream8-integration" && claude -p "$(cat "%PROMPT_DIR%\stream8-integration.md")" --dangerously-skip-permissions --model sonnet > "%S8_LOG%" 2>&1 && echo STREAM8_DONE > "%LOG_DIR%\stream8.done""

echo.
echo   Wave 2 streams launched. Waiting for completion...
echo.

:: ============================================================================
:: PHASE 3b: WAIT for Wave 2
:: ============================================================================

:WAIT_WAVE2
set "DONE_COUNT=0"
if exist "%LOG_DIR%\stream7.done" set /a DONE_COUNT+=1
if exist "%LOG_DIR%\stream8.done" set /a DONE_COUNT+=1

if %DONE_COUNT% lss 2 (
    echo   [%TIME%] %DONE_COUNT%/2 streams complete...
    timeout /t 30 /nobreak >nul
    goto WAIT_WAVE2
)

echo.
echo   All Wave 2 streams complete!
echo.

:: ============================================================================
:: PHASE 4: FINAL MERGE
:: ============================================================================

echo ============================================================================
echo [Phase 4] Final merge
echo ============================================================================
echo.

cd /d "%REPO_ROOT%"

for %%S in (stream7-advanced stream8-integration) do (
    echo   Merging todo/%%S...
    git merge --no-ff "todo/%%S" -m "merge: %%S into %CURRENT_BRANCH%"
    if errorlevel 1 (
        echo ERROR: Merge conflict on todo/%%S!
        echo   Resolve conflicts manually.
        exit /b 1
    )
    echo     OK
)

echo.

:: ============================================================================
:: PHASE 5: CLEANUP
:: ============================================================================

echo ============================================================================
echo [Phase 5] Cleanup
echo ============================================================================
echo.

:: Remove worktrees
for %%S in (%STREAMS% stream7-advanced stream8-integration) do (
    echo   Removing worktree: %%S
    git worktree remove "%WORKTREE_BASE%\%%S" --force 2>nul
    git branch -D "todo/%%S" 2>nul
)

:: Clean up .done files
del /q "%LOG_DIR%\*.done" 2>nul

:: Remove empty worktree base
rmdir "%WORKTREE_BASE%" 2>nul

echo.
echo ============================================================================
echo   COMPLETE!
echo ============================================================================
echo.
echo   Logs:    %LOG_DIR%
echo   Branch:  %CURRENT_BRANCH%
echo.
echo   Next steps:
echo     1. Review: git log --oneline -20
echo     2. Build:  dotnet build TheWatch.sln
echo     3. Test:   dotnet test
echo.

endlocal

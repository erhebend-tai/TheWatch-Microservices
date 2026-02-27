@echo off
REM ============================================================
REM  TheWatch — Resume Development Session
REM  Starts Claude Code with full permissions and context.
REM  
REM  Configure the following environment variables before use:
REM    THEWATCH_PROJECT_DIR  — Path to the project working directory
REM    THEWATCH_CLAUDE_DIR   — Path to Claude project memory directory
REM    THEWATCH_SOURCE_DIR   — Path to source output directory
REM ============================================================

if not defined THEWATCH_PROJECT_DIR (
    echo ERROR: THEWATCH_PROJECT_DIR is not set.
    echo Set it to your project working directory.
    exit /b 1
)

cd /d "%THEWATCH_PROJECT_DIR%"

claude --dangerously-skip-permissions "Read these files to get up to speed on The Watch project, then tell me what session number we are on, what was done last, and what is next. Files to read: (1) %THEWATCH_CLAUDE_DIR%\memory\session-log.md (2) %THEWATCH_CLAUDE_DIR%\memory\next-steps.md (3) %THEWATCH_CLAUDE_DIR%\memory\decisions.md (4) %THEWATCH_SOURCE_DIR%\ROADMAP.md (5) %THEWATCH_SOURCE_DIR%\TODO.md — After reading all five files, give me a brief status report: session number, what stages are complete (1-7 were sessions 1-7), what the next stage is (Stage 5: Database Layer per the ROADMAP), and list the first 5 TODO items ready to start. Then ask me what I want to work on today."

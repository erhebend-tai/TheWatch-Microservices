@echo off
REM ============================================================
REM  TheWatch — Resume Development Session
REM  Starts Claude Code with full permissions and context.
REM  Run from any terminal.
REM ============================================================

cd /d "C:\Users\erheb\OneDrive\SCRAPE\_Consolidated"

claude --dangerously-skip-permissions "Read these files to get up to speed on The Watch project, then tell me what session number we are on, what was done last, and what is next. Files to read: (1) C:\Users\erheb\.claude\projects\C--Users-erheb-OneDrive-SCRAPE--Consolidated\memory\session-log.md (2) C:\Users\erheb\.claude\projects\C--Users-erheb-OneDrive-SCRAPE--Consolidated\memory\next-steps.md (3) C:\Users\erheb\.claude\projects\C--Users-erheb-OneDrive-SCRAPE--Consolidated\memory\decisions.md (4) E:\json_output\Microservices\ROADMAP.md (5) E:\json_output\Microservices\TODO.md — After reading all five files, give me a brief status report: session number, what stages are complete (1-7 were sessions 1-7), what the next stage is (Stage 5: Database Layer per the ROADMAP), and list the first 5 TODO items ready to start. Then ask me what I want to work on today."

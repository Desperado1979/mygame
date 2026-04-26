@echo off
setlocal

set EXE_PATH=%~1
if "%EXE_PATH%"=="" (
  echo Usage: run_dedicated_server.bat "D:\Path\To\EpochOfDawn.exe" [port]
  exit /b 1
)

set PORT=%~2
if "%PORT%"=="" set PORT=7777

echo Starting dedicated server on port %PORT%
"%EXE_PATH%" -batchmode -nographics -server -port %PORT%

endlocal

@echo off
setlocal

set EXE_PATH=%~1
set PORT=%~2

if "%EXE_PATH%"=="" (
  echo Usage: run_host_local.bat "D:\Path\To\EpochOfDawn.exe" [port]
  exit /b 1
)

if "%PORT%"=="" set PORT=7777

echo Starting local host on 127.0.0.1:%PORT%
"%EXE_PATH%" -host -address 127.0.0.1 -port %PORT%

endlocal

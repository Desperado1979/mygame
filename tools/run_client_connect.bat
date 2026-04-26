@echo off
setlocal

set EXE_PATH=%~1
set PUBLIC_IP=%~2
set PORT=%~3

if "%EXE_PATH%"=="" (
  echo Usage: run_client_connect.bat "D:\Path\To\EpochOfDawn.exe" public_ip [port]
  exit /b 1
)

if "%PUBLIC_IP%"=="" (
  echo Missing public_ip.
  echo Example: run_client_connect.bat "D:\Build\EpochOfDawn.exe" 1.2.3.4 7777
  exit /b 1
)

if "%PORT%"=="" set PORT=7777

echo Starting client to %PUBLIC_IP%:%PORT%
"%EXE_PATH%" -client -address %PUBLIC_IP% -port %PORT%

endlocal

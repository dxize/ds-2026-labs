@echo off
setlocal EnableExtensions

REM перейти в корень репозитория
cd /d "%~dp0.."

echo Stopping nginx-lb...
docker rm -f nginx-lb >nul 2>&1

echo Stopping Valuator on port 5001...
for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":5001 .*LISTENING"') do (
  echo   taskkill PID %%P
  taskkill /PID %%P /T /F >nul 2>&1
)

echo Stopping Valuator on port 5002...
for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":5002 .*LISTENING"') do (
  echo   taskkill PID %%P
  taskkill /PID %%P /T /F >nul 2>&1
)

echo Done.
pause
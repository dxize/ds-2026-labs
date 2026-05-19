@echo off
setlocal

echo Stopping dotnet processes...
taskkill /F /IM dotnet.exe >nul 2>nul

echo Stopping pa7 containers...
docker rm -f pa7-redis pa7-rabbitmq nginx-lb >nul 2>nul

echo.
echo Stopped.
pause
exit /b 0
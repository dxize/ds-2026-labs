@echo off
setlocal EnableExtensions

cd /d "%~dp0.."
set "ROOT=%cd%"

set "VALUATOR_DIR=%ROOT%\Valuator"
set "RANK_DIR=%ROOT%\RankCalculator"
set "EVENTS_DIR=%ROOT%\EventsLogger"

set "VALUATOR=%VALUATOR_DIR%\Valuator.csproj"
set "RANK=%RANK_DIR%\RankCalculator.csproj"
set "EVENTS=%EVENTS_DIR%\EventsLogger.csproj"

set "CONF=%ROOT%\nginx\conf\nginx.conf"
set "LOGS=%ROOT%\nginx\logs"

set "PIDDIR=%ROOT%\scripts\.pids"
set "RUNNERDIR=%ROOT%\scripts\.runners"

set "DB_MAIN=127.0.0.1:6000"
set "DB_RU=127.0.0.1:6001"
set "DB_EU=127.0.0.1:6002"
set "DB_ASIA=127.0.0.1:6003"

if not exist "%VALUATOR%" (
    echo ERROR: not found "%VALUATOR%"
    pause
    exit /b 1
)

if not exist "%RANK%" (
    echo ERROR: not found "%RANK%"
    pause
    exit /b 1
)

if not exist "%EVENTS%" (
    echo ERROR: not found "%EVENTS%"
    pause
    exit /b 1
)

if not exist "%CONF%" (
    echo ERROR: nginx.conf not found: "%CONF%"
    pause
    exit /b 1
)

if not exist "%LOGS%" mkdir "%LOGS%"
if not exist "%PIDDIR%" mkdir "%PIDDIR%"
if not exist "%RUNNERDIR%" mkdir "%RUNNERDIR%"

del /q "%PIDDIR%\*.pid" >nul 2>&1
del /q "%RUNNERDIR%\*.cmd" >nul 2>&1

if /I "%~1"=="rebuild" goto :build
if not exist "%VALUATOR_DIR%\bin\Debug\net8.0\Valuator.dll" goto :build
if not exist "%RANK_DIR%\bin\Debug\net8.0\RankCalculator.dll" goto :build
if not exist "%EVENTS_DIR%\bin\Debug\net8.0\EventsLogger.dll" goto :build
goto :docker

:build
echo Building Valuator...
dotnet build "%VALUATOR%"
if errorlevel 1 (
    echo Valuator build failed
    pause
    exit /b 1
)

echo Building RankCalculator...
dotnet build "%RANK%"
if errorlevel 1 (
    echo RankCalculator build failed
    pause
    exit /b 1
)

echo Building EventsLogger...
dotnet build "%EVENTS%"
if errorlevel 1 (
    echo EventsLogger build failed
    pause
    exit /b 1
)

:docker
echo Recreating Redis DB_MAIN...
docker rm -f pa6-redis-main >nul 2>&1
docker run -d --name pa6-redis-main -p 6000:6379 redis:7-alpine >nul
if errorlevel 1 (
    echo Failed to start Redis DB_MAIN
    pause
    exit /b 1
)

echo Recreating Redis DB_RU...
docker rm -f pa6-redis-ru >nul 2>&1
docker run -d --name pa6-redis-ru -p 6001:6379 redis:7-alpine >nul
if errorlevel 1 (
    echo Failed to start Redis DB_RU
    pause
    exit /b 1
)

echo Recreating Redis DB_EU...
docker rm -f pa6-redis-eu >nul 2>&1
docker run -d --name pa6-redis-eu -p 6002:6379 redis:7-alpine >nul
if errorlevel 1 (
    echo Failed to start Redis DB_EU
    pause
    exit /b 1
)

echo Recreating Redis DB_ASIA...
docker rm -f pa6-redis-asia >nul 2>&1
docker run -d --name pa6-redis-asia -p 6003:6379 redis:7-alpine >nul
if errorlevel 1 (
    echo Failed to start Redis DB_ASIA
    pause
    exit /b 1
)

echo Recreating RabbitMQ...
docker rm -f pa6-rabbitmq >nul 2>&1
docker run -d --name pa6-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management >nul
if errorlevel 1 (
    echo Failed to start RabbitMQ
    pause
    exit /b 1
)

echo Recreating nginx-lb...
docker rm -f nginx-lb >nul 2>&1
docker run -d --name nginx-lb -p 8080:8080 ^
  -v "%CONF%:/etc/nginx/nginx.conf:ro" ^
  -v "%LOGS%:/logs" ^
  nginx:alpine >nul
if errorlevel 1 (
    echo Failed to start nginx-lb
    pause
    exit /b 1
)

echo Waiting for RabbitMQ...
timeout /t 20 /nobreak >nul

echo Creating runner files...

> "%RUNNERDIR%\valuator-5001.cmd" (
    echo @echo off
    echo title Valuator-5001
    echo set "DB_MAIN=%DB_MAIN%"
    echo set "DB_RU=%DB_RU%"
    echo set "DB_EU=%DB_EU%"
    echo set "DB_ASIA=%DB_ASIA%"
    echo cd /d "%VALUATOR_DIR%"
    echo dotnet run --no-build --urls http://0.0.0.0:5001
)

> "%RUNNERDIR%\valuator-5002.cmd" (
    echo @echo off
    echo title Valuator-5002
    echo set "DB_MAIN=%DB_MAIN%"
    echo set "DB_RU=%DB_RU%"
    echo set "DB_EU=%DB_EU%"
    echo set "DB_ASIA=%DB_ASIA%"
    echo cd /d "%VALUATOR_DIR%"
    echo dotnet run --no-build --urls http://0.0.0.0:5002
)

> "%RUNNERDIR%\rank-1.cmd" (
    echo @echo off
    echo title RankCalculator-1
    echo set "DB_MAIN=%DB_MAIN%"
    echo set "DB_RU=%DB_RU%"
    echo set "DB_EU=%DB_EU%"
    echo set "DB_ASIA=%DB_ASIA%"
    echo cd /d "%RANK_DIR%"
    echo dotnet run --no-build
)

> "%RUNNERDIR%\rank-2.cmd" (
    echo @echo off
    echo title RankCalculator-2
    echo set "DB_MAIN=%DB_MAIN%"
    echo set "DB_RU=%DB_RU%"
    echo set "DB_EU=%DB_EU%"
    echo set "DB_ASIA=%DB_ASIA%"
    echo cd /d "%RANK_DIR%"
    echo dotnet run --no-build
)

> "%RUNNERDIR%\events-logger-1.cmd" (
    echo @echo off
    echo title EventsLogger-1
    echo cd /d "%EVENTS_DIR%"
    echo dotnet run --no-build
)

> "%RUNNERDIR%\events-logger-2.cmd" (
    echo @echo off
    echo title EventsLogger-2
    echo cd /d "%EVENTS_DIR%"
    echo dotnet run --no-build
)

echo Starting Valuator-5001...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\valuator-5001.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\valuator-5001.pid" echo %PID%

echo Starting Valuator-5002...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\valuator-5002.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\valuator-5002.pid" echo %PID%

echo Starting RankCalculator-1...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\rank-1.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\rank-1.pid" echo %PID%

echo Starting RankCalculator-2...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\rank-2.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\rank-2.pid" echo %PID%

echo Starting EventsLogger-1...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\events-logger-1.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\events-logger-1.pid" echo %PID%

echo Starting EventsLogger-2...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$p = Start-Process cmd.exe -ArgumentList '/k', '""%RUNNERDIR%\events-logger-2.cmd""' -PassThru; $p.Id"') do set "PID=%%P"
> "%PIDDIR%\events-logger-2.pid" echo %PID%

echo.
echo Done.
echo Open: http://localhost:8080
echo RabbitMQ UI: http://localhost:15672
echo Redis:
echo   DB_MAIN=%DB_MAIN%
echo   DB_RU=%DB_RU%
echo   DB_EU=%DB_EU%
echo   DB_ASIA=%DB_ASIA%
pause
exit /b 0
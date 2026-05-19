@echo off
setlocal

set "ROOT=%~dp0.."
for %%I in ("%ROOT%") do set "ROOT=%%~fI"

set "VALUATOR_DIR=%ROOT%\Valuator"
set "RANK_DIR=%ROOT%\RankCalculator"
set "EVENTS_DIR=%ROOT%\EventsLogger"

set "REDIS_PASSWORD=local_redis_password"
set "RABBITMQ_USER=local_rabbit_user"
set "RABBITMQ_PASSWORD=local_rabbit_password"

echo Root: %ROOT%
echo.

echo Stopping old pa7 containers...
docker rm -f pa7-redis pa7-rabbitmq nginx-lb >nul 2>nul

echo.
echo Starting Redis with password...
docker run -d ^
  --name pa7-redis ^
  -p 6379:6379 ^
  redis:7 ^
  redis-server --requirepass "%REDIS_PASSWORD%"

echo.
echo Starting RabbitMQ with user/password...
docker run -d ^
  --name pa7-rabbitmq ^
  -p 5672:5672 ^
  -p 15672:15672 ^
  -e RABBITMQ_DEFAULT_USER="%RABBITMQ_USER%" ^
  -e RABBITMQ_DEFAULT_PASS="%RABBITMQ_PASSWORD%" ^
  rabbitmq:3-management

echo.
echo Waiting for Redis and RabbitMQ...
timeout /t 7 /nobreak >nul

echo.
echo Starting nginx...
docker run -d ^
  --name nginx-lb ^
  -p 8080:80 ^
  -v "%ROOT%\nginx\conf\nginx.conf:/etc/nginx/nginx.conf:ro" ^
  nginx:latest

echo.
echo Starting Valuator-5001...
start "Valuator-5001" cmd /k "cd /d ""%VALUATOR_DIR%"" && dotnet run --no-build --urls http://0.0.0.0:5001"

echo Starting Valuator-5002...
start "Valuator-5002" cmd /k "cd /d ""%VALUATOR_DIR%"" && dotnet run --no-build --urls http://0.0.0.0:5002"

echo Starting RankCalculator-1...
start "RankCalculator-1" cmd /k "cd /d ""%RANK_DIR%"" && dotnet run --no-build"

echo Starting RankCalculator-2...
start "RankCalculator-2" cmd /k "cd /d ""%RANK_DIR%"" && dotnet run --no-build"

echo Starting EventsLogger-1...
start "EventsLogger-1" cmd /k "cd /d ""%EVENTS_DIR%"" && dotnet run --no-build"

echo Starting EventsLogger-2...
start "EventsLogger-2" cmd /k "cd /d ""%EVENTS_DIR%"" && dotnet run --no-build"

echo.
echo Done.
echo.
echo Site:
echo http://localhost:8080
echo.
echo Register:
echo http://localhost:8080/Register
echo.
echo Login:
echo http://localhost:8080/Login
echo.
echo RabbitMQ UI:
echo http://localhost:15672
echo login: %RABBITMQ_USER%
echo password: %RABBITMQ_PASSWORD%
echo.
pause
exit /b 0
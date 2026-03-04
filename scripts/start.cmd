@echo off
setlocal EnableExtensions

REM перейти в корень репозитория
cd /d "%~dp0.."
set "ROOT=%cd%"

set "CONF=%ROOT%\nginx\conf\nginx.conf"
set "LOGS=%ROOT%\nginx\logs"

if not exist "%CONF%" (
  echo ERROR: nginx.conf not found: "%CONF%"
  pause
  exit /b 1
)

if not exist "%LOGS%" mkdir "%LOGS%"

REM Проверка Docker
docker version >nul 2>&1
if errorlevel 1 (
  echo ERROR: Docker not running or not installed.
  pause
  exit /b 1
)

REM Проверка конфига nginx (ВАЖНО: с примонтированными logs)
docker run --rm -v "%CONF%:/etc/nginx/nginx.conf:ro" -v "%LOGS%:/logs" nginx:alpine nginx -t
if errorlevel 1 (
  echo ERROR: nginx config test failed.
  pause
  exit /b 1
)

REM Пересоздаём nginx контейнер
docker rm -f nginx-lb >nul 2>&1
docker run -d --name nginx-lb -p 8080:8080 ^
  -v "%CONF%:/etc/nginx/nginx.conf:ro" ^
  -v "%LOGS%:/logs" ^
  nginx:alpine >nul

echo Nginx started: http://localhost:8080/
echo Nginx access log: %LOGS%\access.log
echo.

REM Открываем ОТДЕЛЬНЫЕ ОКНА для инстансов (их можно закрывать вручную)
start "Valuator-5001" cmd /k "cd /d ""%ROOT%"" && dotnet run --project ""Valuator\Valuator.csproj"" --urls ""http://0.0.0.0:5001"""
start "Valuator-5002" cmd /k "cd /d ""%ROOT%"" && dotnet run --project ""Valuator\Valuator.csproj"" --urls ""http://0.0.0.0:5002"""

echo Started Valuator instances in separate windows.
pause
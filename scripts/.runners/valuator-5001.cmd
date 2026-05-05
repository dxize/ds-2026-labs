@echo off
title Valuator-5001
set "DB_MAIN=127.0.0.1:6000"
set "DB_RU=127.0.0.1:6001"
set "DB_EU=127.0.0.1:6002"
set "DB_ASIA=127.0.0.1:6003"
cd /d "C:\Users\gimps\study\DISTRIBUTED-PROGRAMMING\DISTRIBUTED-PROGRAMMING\Valuator"
dotnet run --no-build --urls http://0.0.0.0:5001

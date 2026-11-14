@echo off

echo ==========================================
echo        NOAZUL MIGRATOR
echo ==========================================
echo.

echo Applying EF Core migrations...
echo.

cd /d %~dp0

dotnet ef database update ^
  --project src\NoAzul.Infra.Data ^
  --startup-project src\NoAzul.Infra.Data

echo.
echo Done.
pause

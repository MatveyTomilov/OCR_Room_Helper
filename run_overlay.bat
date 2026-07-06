@echo off
setlocal
cd /d %~dp0

echo Starting AbyssOverlay...

dotnet run --project AbyssOverlay\AbyssOverlay.csproj

if errorlevel 1 (
  echo.
  echo ERROR: The app failed to start. See messages above.
)

echo.
pause
endlocal

@echo off
setlocal
if exist "%~dp0XiaoheiCompanion.exe" (
  start "" "%~dp0XiaoheiCompanion.exe"
  exit /b 0
)
if exist "%~dp0dist\XiaoheiCompanion.exe" (
  start "" "%~dp0dist\XiaoheiCompanion.exe"
  exit /b 0
)
echo Build first with Build.ps1, or download and extract the Portable ZIP.
pause
exit /b 1

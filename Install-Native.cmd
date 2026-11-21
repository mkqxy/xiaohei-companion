@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Install-Native.ps1"
set "pet_install_exit=%errorlevel%"
echo.
if not "%pet_install_exit%"=="0" echo Installation failed. Read the error above.
pause
exit /b %pet_install_exit%

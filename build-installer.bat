@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  Genera el instalador todo-en-uno de Parqueo Mahischa.
rem  Uso:   build-installer.bat
rem  Hace:  publica (self-contained) y compila el .iss con Inno Setup.
rem  La version se toma automaticamente del .csproj.
rem  Si installer\redist\SQLEXPR_x64_ENU.exe existe, incluye SQL Express.
rem ============================================================

set "ROOT=%~dp0"
cd /d "%ROOT%"

rem --- Leer la version del .csproj ---
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([regex]::Match((Get-Content 'SistemaParkingMaisha.csproj' -Raw),'<Version>(.*?)</Version>')).Groups[1].Value"`) do set "VERSION=%%v"
if "%VERSION%"=="" (
  echo No se pudo leer la version del .csproj
  exit /b 1
)
echo Version detectada: %VERSION%

rem --- Ubicar ISCC.exe (compilador de Inno Setup) ---
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo No se encontro ISCC.exe. Instale Inno Setup 6.
  exit /b 1
)

echo.
echo === 1/2  Publicando (self-contained) ===
if exist publish rmdir /s /q publish
dotnet publish SistemaParkingMaisha.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish
if errorlevel 1 goto :error

echo.
echo === 2/2  Compilando instalador ===
"%ISCC%" /DAppVersion=%VERSION% "installer\ParqueoMahischa.iss"
if errorlevel 1 goto :error

echo.
echo ============================================================
echo  LISTO. Instalador en:
echo    installer\Output\ParqueoMahischa-Setup-%VERSION%.exe
echo ============================================================
exit /b 0

:error
echo.
echo *** ERROR: el proceso se detuvo. Revise el mensaje anterior. ***
exit /b 1

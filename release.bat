@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

rem ============================================================
rem  Publica una nueva version de Parqueo Mahischa en GitHub.
rem  Uso:   release.bat 1.0.1
rem         release.bat 1.0.1 "Novedades de esta version"
rem  Hace:  sube version -> publish -> zip -> commit/push -> release
rem ============================================================

if "%~1"=="" (
  echo Uso: release.bat ^<version^>   por ejemplo: release.bat 1.0.1
  exit /b 1
)

set "VERSION=%~1"
set "TAG=v%VERSION%"
set "NOTES=%~2"
if "%NOTES%"=="" set "NOTES=Actualizacion %TAG%"
set "ROOT=%~dp0"
cd /d "%ROOT%"

rem --- Ubicar gh (en PATH o en Program Files) ---
where gh >nul 2>&1 && (set "GH=gh") || (set "GH=C:\Program Files\GitHub CLI\gh.exe")

echo.
echo === 1/5  Actualizando version a %VERSION% en el .csproj ===
powershell -NoProfile -Command "$p='SistemaParkingMaisha.csproj'; $c=Get-Content $p -Raw; $c=$c -replace '<Version>.*?</Version>','<Version>%VERSION%</Version>'; $c=$c -replace '<AssemblyVersion>.*?</AssemblyVersion>','<AssemblyVersion>%VERSION%.0</AssemblyVersion>'; $c=$c -replace '<FileVersion>.*?</FileVersion>','<FileVersion>%VERSION%.0</FileVersion>'; Set-Content $p $c -Encoding UTF8"
if errorlevel 1 goto :error

echo.
echo === 2/5  Publicando (self-contained, sin .NET en el cliente) ===
if exist publish rmdir /s /q publish
dotnet publish SistemaParkingMaisha.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish
if errorlevel 1 goto :error

echo.
echo === 3/5  Comprimiendo paquete ===
set "ZIP=%ROOT%SistemaParkingMahischa-%VERSION%.zip"
if exist "%ZIP%" del /q "%ZIP%"
powershell -NoProfile -Command "Compress-Archive -Path 'publish\*' -DestinationPath '%ZIP%' -CompressionLevel Optimal"
if errorlevel 1 goto :error

echo.
echo === 4/5  Guardando cambio de version en git ===
git add SistemaParkingMaisha.csproj
git commit -m "Version %VERSION%"
git push

echo.
echo === 5/5  Creando release %TAG% en GitHub ===
"%GH%" release create %TAG% "%ZIP%" --title "Parqueo Mahischa %TAG%" --notes "%NOTES%"
if errorlevel 1 goto :error

echo.
echo ============================================================
echo  LISTO. Release %TAG% publicado.
echo  Los clientes veran el boton "Actualizar a %TAG%" al abrir.
echo ============================================================
exit /b 0

:error
echo.
echo *** ERROR: el proceso se detuvo. Revise el mensaje anterior. ***
exit /b 1

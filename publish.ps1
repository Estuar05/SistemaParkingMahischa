# Publica Parqueo Mahischa como aplicación self-contained (no requiere instalar .NET en el cliente).
# Genera la carpeta .\publish que usan tanto el instalador como el paquete de actualización de GitHub.
#
# Uso:   powershell -ExecutionPolicy Bypass -File .\publish.ps1
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$output = Join-Path $projectDir "publish"

Write-Host "Limpiando carpeta de publicación..." -ForegroundColor Cyan
if (Test-Path $output) { Remove-Item $output -Recurse -Force }

Write-Host "Publicando ($Configuration / $Runtime, self-contained)..." -ForegroundColor Cyan
dotnet publish (Join-Path $projectDir "SistemaParkingMaisha.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $output

if ($LASTEXITCODE -ne 0) { throw "La publicación falló." }

Write-Host ""
Write-Host "Listo. Archivos en: $output" -ForegroundColor Green
Write-Host "  - Para el INSTALADOR: compile installer\ParqueoMahischa.iss con Inno Setup." -ForegroundColor Green
Write-Host "  - Para una ACTUALIZACION: comprima el CONTENIDO de 'publish' en un .zip y adjúntelo" -ForegroundColor Green
Write-Host "    a un nuevo release de GitHub con un tag mayor (ej. v1.0.1)." -ForegroundColor Green

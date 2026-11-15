param(
    [string]$Version = "1.0.0",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$publishDir = Join-Path $root "publish"
$installerDir = Join-Path $root "installer"

Write-Host "=== Building FitCore v$Version ===" -ForegroundColor Cyan

Get-Process -Name "FitCore*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 1500

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "`nPublishing Desktop app..." -ForegroundColor Yellow
dotnet publish "$root\src\FitCore.Desktop\FitCore.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -p:Version=$Version -o "$publishDir\app"

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

New-Item -ItemType Directory -Path "$publishDir\app\defaults" -Force | Out-Null
Copy-Item "$root\defaults\fitcore.defaults.json" "$publishDir\app\defaults\" -Force

$exePath = "$publishDir\app\FitCore.Desktop.exe"
if (-not (Test-Path $exePath)) { throw "Published exe missing at $exePath" }

$size = [math]::Round((Get-ChildItem $publishDir\app -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
Write-Host "`nPublished: $size MB at $publishDir\app" -ForegroundColor Green

if ($SkipInstaller) { Write-Host "Skipping installer." -ForegroundColor Yellow; exit 0 }

$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    Write-Host "`nInno Setup not installed at $iscc" -ForegroundColor Red
    Write-Host "Download: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nBuilding installer..." -ForegroundColor Yellow
& $iscc "/DAppVersion=$Version" "$installerDir\FitCore.iss"

$setupPath = "$installerDir\Output\FitCore-Setup-$Version.exe"
if (Test-Path $setupPath) {
    $setupSize = [math]::Round((Get-Item $setupPath).Length / 1MB, 1)
    Write-Host "`n=== DONE ===" -ForegroundColor Green
    Write-Host "Installer: $setupPath ($setupSize MB)" -ForegroundColor Cyan
} else {
    throw "Installer file missing: $setupPath"
}

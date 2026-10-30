param(
    [string]$Profile = $env:ULTRAKILL_PROFILE,
    [switch]$NoInstall
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageName = "JukeboxLimbusCompany"
$version = "1.0.0"

Write-Host "== build =="
foreach ($proj in @(
    "JukeboxCore\JukeboxCore.csproj",
    "Jukebox\Jukebox.csproj",
    "Jukebox.ThornExtensions\Jukebox.ThornExtensions.csproj"
)) {
    dotnet build (Join-Path $root $proj) -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "build failed: $proj" }
}

Write-Host "== package =="
$pkg = Join-Path $root "_pkg\$packageName"
if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
New-Item -ItemType Directory (Join-Path $pkg "Assets") -Force | Out-Null
Copy-Item (Join-Path $root "_stage\plugins\Jukebox\*.dll") $pkg

$thirdParty = Join-Path $root "third_party"
if (Test-Path $thirdParty) {
    $binaries = Get-ChildItem $thirdParty -File -ErrorAction SilentlyContinue
    if ($binaries) { $binaries | Copy-Item -Destination $pkg -Force }
    $assets = Join-Path $thirdParty "Assets"
    if (Test-Path $assets) { Copy-Item (Join-Path $assets "*") (Join-Path $pkg "Assets") -Recurse -Force }
}

Copy-Item (Join-Path $root "packaging\manifest.json"), (Join-Path $root "packaging\README.md"), (Join-Path $root "packaging\icon.png") $pkg -Force
$fontsDir = Join-Path $root "packaging\fonts"
if (Test-Path $fontsDir) { Copy-Item $fontsDir (Join-Path $pkg "Fonts") -Recurse -Force }

$zip = Join-Path $root "JukeboxLimbusCompany-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $pkg "*") -DestinationPath $zip
Write-Host "package: $pkg"
Write-Host "zip: $zip"

if ($NoInstall) { return }

if ([string]::IsNullOrWhiteSpace($Profile)) {
    throw "Pass -Profile <r2modman profile folder> or set the ULTRAKILL_PROFILE environment variable."
}

Write-Host "== install (r2modman cache + profile) =="
$localRoot = Split-Path (Split-Path $Profile -Parent) -Parent
$cacheDir = Join-Path $localRoot "cache\$packageName\$version"
if (Test-Path (Join-Path $localRoot "cache\$packageName")) {
    Remove-Item (Join-Path $localRoot "cache\$packageName") -Recurse -Force
}
New-Item -ItemType Directory $cacheDir -Force | Out-Null
Copy-Item (Join-Path $pkg "*") $cacheDir -Recurse

$plugins = Join-Path $Profile "BepInEx\plugins\$packageName"
New-Item -ItemType Directory $plugins -Force | Out-Null
Copy-Item (Join-Path $pkg "*") $plugins -Recurse -Force
Write-Host "installed: $plugins"
Write-Host "Keep Flazhik-CybergrindMusicExplorer disabled in r2modman."

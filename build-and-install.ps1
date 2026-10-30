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

Write-Host "== package (Thunderstore layout) =="
# manifest.json / icon.png / README.md stay at the zip root.
# Runtime files live under BepInEx/plugins/ so the installer preserves the folder.
$pkg = Join-Path $root "_pkg\$packageName"
$runtime = Join-Path $pkg "BepInEx\plugins\$packageName"
if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
New-Item -ItemType Directory (Join-Path $runtime "Assets") -Force | Out-Null
Copy-Item (Join-Path $root "_stage\plugins\Jukebox\*.dll") $runtime

$thirdParty = Join-Path $root "third_party"
if (Test-Path $thirdParty) {
    # Runtime libraries only; the CGME Downloader (ffmpeg/ffprobe/yt-dlp) is intentionally unsupported.
    Get-ChildItem $thirdParty -File -Filter *.dll -ErrorAction SilentlyContinue | Copy-Item -Destination $runtime -Force
    $assets = Join-Path $thirdParty "Assets"
    if (Test-Path $assets) { Copy-Item (Join-Path $assets "*") (Join-Path $runtime "Assets") -Recurse -Force }
}

Copy-Item (Join-Path $root "packaging\manifest.json"), (Join-Path $root "packaging\README.md"), (Join-Path $root "packaging\icon.png") $pkg -Force
$fontsDir = Join-Path $root "packaging\fonts"
if (Test-Path $fontsDir) { Copy-Item $fontsDir (Join-Path $runtime "Fonts") -Recurse -Force }

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
Get-ChildItem $plugins -Force -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Get-ChildItem $runtime -Force | ForEach-Object {
    $dest = Join-Path $plugins $_.Name
    if ($_.PSIsContainer) {
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue }
        Copy-Item $_.FullName $dest -Recurse -Force
    } else {
        Copy-Item $_.FullName $dest -Force
    }
}
Write-Host "installed: $plugins"
Write-Host "Keep Flazhik-CybergrindMusicExplorer disabled in r2modman."

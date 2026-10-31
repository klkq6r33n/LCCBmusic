param(
    [string]$Profile = "C:\Users\A\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\zhibo",
    [switch]$NoInstall
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageName = "Claude-JukeboxLimbusCompany"
$version = "2.0.0"

$refDir = Join-Path $root "third_party\cgme_ref"
foreach ($dll in @("Jukebox.dll", "JukeboxCore.dll", "Jukebox.ThornExtensions.dll", "SubtitlesParser.dll", "RTLTMPro.dll", "TagLibSharp.dll")) {
    if (-not (Test-Path (Join-Path $refDir $dll))) {
        throw "Missing CGME reference DLL: $refDir\$dll`nCopy the original CybergrindMusicExplorer 2.3.2 DLLs there first (see README)."
    }
}

Write-Host "== build extension DLL =="
$project = Join-Path $root "JukeboxLimbusCompany\JukeboxLimbusCompany.csproj"
dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }

Write-Host "== package (Thunderstore layout) =="
# Only the extension assembly is shipped. CGME, its libs/assets and the fonts
# are supplied by the CybergrindMusicExplorer / Fffffff dependencies.
$pkg = Join-Path $root "_pkg\$packageName"
$runtime = Join-Path $pkg "BepInEx\plugins\$packageName"
if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
New-Item -ItemType Directory $runtime -Force | Out-Null
Copy-Item (Join-Path $root "JukeboxLimbusCompany\.bin\netstandard2.1\JukeboxLimbusCompany.dll") $runtime

Copy-Item (Join-Path $root "packaging\manifest.json"), (Join-Path $root "packaging\README.md"), (Join-Path $root "packaging\icon.png") $pkg -Force

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
Write-Host "CybergrindMusicExplorer must be ENABLED. The old standalone fork DLLs must be removed."

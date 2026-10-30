# third_party

These runtime-only files are **not** stored in the repository. Copy them here
before running `build-and-install.ps1`, or add them to your local build folder.

## Assets/

Catalog and asset bundles from the original **Jukebox 2.3.2** release:

- `catalog.json`
- `*.bundle` files

Download the original Jukebox release from
<https://github.com/Flazhik/Jukebox/releases> and copy its `Assets/` folder here.
They are needed at runtime (Addressables loads the UI prefabs from them) and are
licensed under Apache-2.0 together with the original project.

## Runtime libraries

The build packages these DLLs from this folder:

- `RTLTMPro.dll`
- `SubtitlesParser.dll`
- `TagLibSharp.dll`

Copy them from the original Jukebox 2.3.2 release.

## Downloader binaries (NOT supported)

`ffmpeg.exe`, `ffprobe.exe`, `yt-dlp.exe` and `yt-dlp.sha256` are **intentionally
not used or shipped**. The CGME Downloader is unsupported in this fork, so URL /
YouTube downloads are unavailable. Do not add them for a release build.

## Fonts

Release packages additionally ship `Fonts/NotoSansJP-Variable.ttf` and
`Fonts/NotoSansSC-Variable.ttf` (SIL OFL 1.1, see `packaging/fonts/`), together
with their license texts. They are optional; the mod falls back to installed
Windows CJK fonts.

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

## ffmpeg / ffprobe / yt-dlp

Required by CGMEDownloader. Download the Windows x64 builds:

- FFmpeg: <https://www.gyan.dev/ffmpeg/builds/> (or any official build)
- yt-dlp: <https://github.com/yt-dlp/yt-dlp/releases>

Copy `ffmpeg.exe`, `ffprobe.exe`, `yt-dlp.exe` and `yt-dlp.sha256` into this
folder. The downloader verifies the yt-dlp hash, so use the matching release and
keep `yt-dlp.sha256` alongside it.

## Fonts

Release packages additionally ship `Fonts/NotoSansJP-Variable.ttf` and
`Fonts/NotoSansSC-Variable.ttf` (SIL OFL 1.1, see `packaging/fonts/`), together
with their license texts. They are optional; the mod falls back to installed
Windows CJK fonts.

# third_party

Reference-only files. **Nothing in this folder is committed or shipped.**

## cgme_ref/

To build the extension, install **CybergrindMusicExplorer 2.3.2** (and its
dependencies) and copy these files from the CGME plugin folder into
`third_party/cgme_ref/`:

- `Jukebox.dll`
- `JukeboxCore.dll`
- `Jukebox.ThornExtensions.dll`
- `SubtitlesParser.dll`
- `RTLTMPro.dll`
- `TagLibSharp.dll`

They are used only as compile-time references. The build output and release
package contain **only** `JukeboxLimbusCompany.dll`.

## Not redistributed

This project does not redistribute or download:

- CGME's `Assets/` Addressables catalog and bundles
- `RTLTMPro.dll`, `SubtitlesParser.dll`, `TagLibSharp.dll`
- `ffmpeg`, `ffprobe`, `yt-dlp`, `yt-dlp.sha256`
- any font files

At runtime those come from the CybergrindMusicExplorer / Fffffff packages and
the user's own installation.

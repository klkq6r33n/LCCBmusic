# Jukebox LimbusCompany

**Author / 作者：6r3²n**

**LimbusCompany-style lyric performance extension for CybergrindMusicExplorer (Jukebox).**

**CybergrindMusicExplorer（Jukebox）的 LimbusCompany 风格歌词演出扩展。**

> **AI disclosure / AI 说明**
> This project was made with AI assistance. The author (**6r3²n**) has limited programming knowledge and used AI to build it.
> 本项目在 AI 辅助下完成。作者（**6r3²n**）计算机知识有限，使用 AI 制作了此 mod。
>
> **Known issue:** in some cases lyrics may still overlap; the author lacks the computer knowledge to fix it.
> **已知问题：** 部分情况下歌词仍可能重叠；作者计算机知识不足，无法修复该问题。

---

## What is this / 这是什么

A standalone fork of **Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.1** that replaces Jukebox's Cyber Grind subtitle panel with a LimbusCompany-style lyric layer: colors, outline, glow, font size/spacing, shake, fade, rise, random angle, pseudo-perspective and position ranges.

基于 **Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.1** 的独立分支，把 Jukebox 的 Cyber Grind 字幕面板替换为 LimbusCompany 演出风格歌词层。

This repository is **source only**. Release packages (DLLs, `Assets/`, ffmpeg/yt-dlp binaries, fonts) are built from it; see [Build](#build--构建).

## Features / 功能

- Bilingual lyrics: `Track.srt` + optional `Track.zh.srt` / `.cn` / `.ja` / `.trans` sidecars, shown simultaneously at separate positions.
- Independent lyric lines: a tight next cue does not replace the previous line in place; the old line keeps fading at its own position while the new one is placed elsewhere (avoids close-time overlap).
- Full-screen random position by default, plus fixed anchors (top-left … bottom-right) and editable X/Y ranges.
- Independent font sizes for main/translation; ULTRAKILL font for Latin lines, dynamic CJK font for Japanese/Chinese.
- Typewriter, fade, shake, rise, perspective, random rotation, custom colors/stroke/glow.
- Audio visualizer: time-driven hue gradient, bar density, size, position (bottom/top/left/right), X mirror, black outline, own canvas below the game UI.
- Level BGM replacement (standalone **F5** panel, independent of the Cyber Grind menu): pick any `.mp3` / `.ogg` / `.wav` under `Cybergrind/Music` to replace the current level music; restoring resumes the level BGM from the exact paused position. The visualizer follows both the replacement and the level music.
- The F5 panel also toggles the lyrics layer (**L**) and the now-playing frame (**N**), rebinds every panel key (**K**), and adjusts the cover/title position (**P**).
- Now-playing frame: ULK-style bordered cover frame in the top-right (cover from ID3 or a same-name / `cover` / `folder` image), hollow when the track has no cover, with the title and artist underneath; position and size are editable.
- In-menu LANGUAGE (English default / 中文) and SETTINGS panel with numeric inputs.
- NFKC text normalization (fixes compatibility ideographs such as U+F9EC) and diagnostic logging.

## Requirements / 依赖

- ULTRAKILL (Unity Mono) + **BepInEx 5.4.23** (x64).
- .NET SDK 8 or newer (`global.json` pins the SDK feature band).
- The game's managed assemblies and BepInEx core assemblies for compilation.
- **Disable `Flazhik-CybergrindMusicExplorer`** when using this mod: the fork reuses its assembly names and plugin GUID (`dev.flazhik.jukebox`) because Addressables bundles bind scripts by assembly name and CGMEDownloader depends on that GUID.

## Build / 构建

1. Install the .NET SDK.
2. Copy `Directory.Build.props.example` to `Directory.Build.props` and set your game path (or set the `ULTRAKILL_DIR` environment variable):

   ```xml
   <GameDir>D:\SteamLibrary\steamapps\common\ULTRAKILL</GameDir>
   ```

3. Build the three projects:

   ```powershell
   dotnet build JukeboxCore/JukeboxCore.csproj -c Release
   dotnet build Jukebox/Jukebox.csproj -c Release
   dotnet build Jukebox.ThornExtensions/Jukebox.ThornExtensions.csproj -c Release
   ```

4. `build-and-install.ps1` packages the DLLs and installs them into an r2modman profile:

   ```powershell
   .\build-and-install.ps1 -Profile "C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\<profile>"
   # or set ULTRAKILL_PROFILE and run without -Profile
   ```

Runtime-only files are **not** committed here:

- `third_party/Assets/` — catalog + asset bundles taken from the original Jukebox 2.3.2 release.
- `third_party/RTLTMPro.dll`, `SubtitlesParser.dll`, `TagLibSharp.dll` — runtime libraries from the original Jukebox release.
- Windows fonts (Yu Gothic / Microsoft YaHei / Noto Sans JP / SC) are system-provided; release packages bundle Noto Sans JP/SC (SIL OFL 1.1).

**The CGME Downloader is not supported.** `ffmpeg.exe`, `ffprobe.exe`,
`yt-dlp.exe` and `yt-dlp.sha256` are intentionally not bundled and the
Downloader dependency is not declared. See `third_party/README.md`.

## Lyrics / 歌词

Put a `.srt` next to the audio file, same base name:

```
Cybergrind/Music/Artist - Track/Artist - Track.mp3
Cybergrind/Music/Artist - Track/Artist - Track.srt
```

Optional translation sidecar: `Track.zh.srt`, `Track.cn.srt`, `Track.chs.srt`, `Track.cht.srt`, `Track.ja.srt`, `Track.jp.srt`, `Track.trans.srt`, `Track.translation.srt`, `Track.中文.srt`, `Track.翻译.srt`, `Track_zh.srt`, `Track_cn.srt`, `Track_ja.srt`, `Track_trans.srt`.

## Settings / 设置

Open the Jukebox menu (F4) in Cyber Grind: **LANGUAGE** switches the mod UI (English default, 中文 second); **SETTINGS** opens LAYOUT / TEXT / MOTION / TRANS / VISUAL pages. All values are numeric/text inputs and are saved to `BepInEx/config/LimbusCompany/lyric_config.json` immediately.

Press **F5** in any level for the standalone panel: `[` / `]` (or arrows) change track, **Enter** replaces the level BGM, **Backspace** restores it, **L** toggles lyrics, **N** toggles the now-playing frame, **K** rebinds keys (1–7), **P** adjusts the cover/title position (arrows move, Shift = fine, T switches cover/title, `-` / `+` resize, P/Esc done).

## Known limitations / 已知限制

- **The CGME Downloader is not supported** — no `yt-dlp`, `ffmpeg` or `ffprobe` is bundled, so URL/YouTube downloads are unavailable. Use local audio files.
- **Lyrics may still overlap in some cases; the author (6r3²n) lacks the computer knowledge to fix it.**
- 部分情况下歌词仍可能重叠；作者（**6r3²n**）计算机知识不足，无法修复该问题。
- Reuses Jukebox's plugin GUID: cannot be enabled together with the original CybergrindMusicExplorer.
- Font family changes require a restart; glow depends on TMP shader variants shipped with the game.

## Credits / 特别感谢

- **Flazhik and the Jukebox / CybergrindMusicExplorer team** — original mod and source (Apache-2.0), which this fork is based on.
- **greycsont** — ULTRAKILL font reference and font mods (Fffffff / FNT_font_no_tsubasa).
- **end_4 / Thorn** — HUD framework used by `Jukebox.ThornExtensions`.
- **Google Fonts / Noto team** — Noto Sans JP / Noto Sans SC (SIL OFL 1.1) in release packages.
- **SubtitlesParser, RTLTMPro, TagLib#** authors — runtime libraries.

## License / 许可证

Licensed under the **Apache License 2.0** — see [LICENSE](LICENSE). This is the same license as the upstream Jukebox project; the original copyright notices are retained. See [NOTICE](NOTICE) for attribution and a list of changes.

本项目使用 **Apache License 2.0**，与上游 Jukebox 一致；保留原始版权声明。归属与修改说明见 [NOTICE](NOTICE)。

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

An **extension DLL** for **Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.2**. CGME stays installed and enabled; this assembly only adds a LimbusCompany-style lyric layer (colors, outline, glow, font size/spacing, shake, fade, rise, random angle, pseudo-perspective and position ranges) and blanks the original subtitle panel at runtime. It does not replace or redistribute CGME's assemblies.

**Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.2 的扩展 DLL。** CGME 保持安装并启用；本程序集只追加 LimbusCompany 演出风格歌词层，并在运行时清空原版字幕面板，不替换、也不重新分发 CGME 的程序集。

This repository is **source only**. Release packages contain just `JukeboxLimbusCompany.dll`; see [Build](#build--构建).

## Features / 功能

- Bilingual lyrics: `Track.srt` + optional `Track.zh.srt` / `.cn` / `.ja` / `.trans` sidecars, shown simultaneously at separate positions.
- Independent lyric lines: a tight next cue does not replace the previous line in place; the old line keeps fading at its own position while the new one is placed elsewhere (avoids close-time overlap).
- Full-screen random position by default, plus fixed anchors (top-left … bottom-right) and editable X/Y ranges.
- Independent font sizes for main/translation; ULTRAKILL font for Latin lines, dynamic CJK font for Japanese/Chinese.
- Typewriter, fade, shake, rise, perspective, random rotation, custom colors/stroke/glow.
- Audio visualizer: time-driven hue gradient, bar density, size, position (bottom/top/left/right), X mirror, black outline, own canvas below the game UI.
- Level BGM replacement (standalone **F5** panel, independent of the Cyber Grind menu): pick any `.mp3` / `.ogg` / `.wav` under `Cybergrind/Music` to replace the current level music; restoring resumes the level BGM from the exact paused position. While replacing, **every** source under the game's `MusicManager` — including multi-segment clean/battle/boss sources and USTManager-driven ones — is paused and muted, so nothing plays over the replacement. The replacement clip is prefixed `LCCB_` so USTManager never replaces it a second time. The visualizer follows both the replacement and the level music.
- **F5 mouse menu** (levels only; the Cyber Grind keeps **F4** and ignores F5): MUSIC / SETTINGS / DISPLAY pages with the level BGM browser, every lyric setting, 中文 / ENGLISH switching, lyrics and now-playing toggles. MUSIC includes a draggable **progress bar**, elapsed/total time and **PAUSE / RESUME**. The window is split into a **left control column** and a **right track preview** (cover on top, title and artist below). Opening it pauses the game and forcibly shows/unlocks the cursor.
- Only **F5** (open/close) and **Esc** (close) remain as keyboard shortcuts; other shortcuts were removed.
- Now-playing **row**: cover / title / artist drawn on one line (cover first, artist last), small by default and anchored at the **bottom-left** (2%, 90%) — the text size is the reference and the cover is only ~1.3× the text. Like the F5 menu, the row is kept inside a 12 px safe area and uses the current window size (Screen size / canvas scale) for sizing and positioning, so window resizes and resolution changes re-adapt immediately. Title and artist share the same font size, and the cover keeps a ULK-style border (hollow when the track has no cover). Cover from a same-name image, common cover names (`cover` / `folder` / `front` / `album` / `artwork` …), any image in the song folder, or the embedded ID3 picture. If performer/composer tags are missing, the artist is recovered from an `Artist - Title` file or folder name. F5 → DISPLAY has a size slider (10–120) and reset button; while F5 is open you can also drag the cover square and scroll over it to resize.
- In-menu LANGUAGE (English default / 中文) and SETTINGS panel with numeric inputs.
- **User presets**: the F4 SETTINGS panel has a **PRESET** tab and the F5 menu has a **PRESETS** tab. Type a name and save the current lyric/visualizer/row style, apply any saved preset with a click, or delete it. Presets live under `"user_presets"` in `lyric_config.json`; language, BGM track/key bindings and the master lyrics switch are not included.
- NFKC text normalization (fixes compatibility ideographs such as U+F9EC) and diagnostic logging.

## Requirements / 依赖

- ULTRAKILL (Unity Mono) + **BepInEx 5.4.23** (x64).
- **CybergrindMusicExplorer 2.3.2+** — a required Thunderstore dependency and the host of this extension. Keep it **enabled**; do not install the old 1.x standalone fork.
- **greycsont-Fffffff** (ULTRAKILL font fallback mod) — declared as a required Thunderstore dependency; install it from Thunderstore or r2modman.
- **ZedDev-USTManager** — optional, supported. F5 replacement pauses/mutes multi-segment and UST-driven soundtrack sources, and USTManager cannot replace the F5 track.
- .NET SDK 8 or newer (`global.json` pins the SDK feature band).
- The game's managed assemblies, BepInEx core assemblies, and the original CGME 2.3.2 DLLs for compilation.

## Build / 构建

1. Install the .NET SDK.
2. Copy `Directory.Build.props.example` to `Directory.Build.props` and set your game path (or set the `ULTRAKILL_DIR` environment variable):

   ```xml
   <GameDir>D:\SteamLibrary\steamapps\common\ULTRAKILL</GameDir>
   ```

3. Install **CybergrindMusicExplorer 2.3.2** (and BepInEx) first, then copy its `Jukebox.dll`, `JukeboxCore.dll`, `Jukebox.ThornExtensions.dll`, `SubtitlesParser.dll`, `RTLTMPro.dll` and `TagLibSharp.dll` into `third_party/cgme_ref/`. These are **reference-only** and are neither committed nor shipped.

4. Build the extension:

   ```powershell
   dotnet build JukeboxLimbusCompany/JukeboxLimbusCompany.csproj -c Release
   ```

5. `build-and-install.ps1` packages `JukeboxLimbusCompany.dll` and installs it into an r2modman profile:

   ```powershell
   .\build-and-install.ps1 -Profile "C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\<profile>"
   # or set ULTRAKILL_PROFILE and run without -Profile
   ```

Nothing from CGME is redistributed: no `Assets/` bundles, no `RTLTMPro.dll` / `SubtitlesParser.dll` / `TagLibSharp.dll`, no fonts, and no downloader binaries (`yt-dlp` / `ffmpeg` / `ffprobe`). At runtime CGME supplies all of those.

## Lyrics / 歌词

**Songs:** this mod bundles **no audio**. All tracks are read from ULTRAKILL's built-in Cyber Grind music folder, `ULTRAKILL/Cybergrind/Music` (the F5 MUSIC page lists that folder recursively).

**歌曲：** 本 mod **不附带任何音频**，所有曲目都读取自 ULTRAKILL 自带的电子血宫音乐文件夹 `ULTRAKILL/Cybergrind/Music`（F5 的 MUSIC 页递归列出该文件夹）。

Put a `.srt` next to the audio file, same base name:

```
Cybergrind/Music/Artist - Track/Artist - Track.mp3
Cybergrind/Music/Artist - Track/Artist - Track.srt
```

Optional translation sidecar: `Track.zh.srt`, `Track.cn.srt`, `Track.chs.srt`, `Track.cht.srt`, `Track.ja.srt`, `Track.jp.srt`, `Track.trans.srt`, `Track.translation.srt`, `Track.中文.srt`, `Track.翻译.srt`, `Track_zh.srt`, `Track_cn.srt`, `Track_ja.srt`, `Track_trans.srt`.

Folder-form songs are supported: the main subtitle can be `<audio>.srt`, `<folder name>.srt`, or any non-translation `.srt` inside the folder. Level BGM replaced via **F5 → MUSIC** reads the same sidecars and drives the lyric layer from its own playback position.

## Settings / 设置

Open the Jukebox menu (F4) in Cyber Grind: **LANGUAGE** switches the mod UI (English default, 中文 second); **SETTINGS** opens LAYOUT / TEXT / MOTION / TRANS / VISUAL pages. All values are numeric/text inputs and are saved to `BepInEx/config/LimbusCompany/lyric_config.json` immediately.

Press **F5** in any level for the mouse menu (F4 is the Cyber Grind menu only; F5 is ignored there). MUSIC selects and replaces/restores the level BGM, with a draggable progress bar, elapsed/total time and PAUSE/RESUME; The category bar shows **PERSONAL** (songs outside category folders) plus every registered category, starting with `ULTRAKILL-OST`. Click **+** to create your own category folder under `Cybergrind/Music`; names are saved under `"song_categories"` in `lyric_config.json`. **DEL** on a custom category deletes the folder and all its songs after confirmation (PERSONAL and ULTRAKILL-OST are protected). SETTINGS mirrors the LAYOUT / TEXT / MOTION / TRANS / VISUAL pages; DISPLAY switches 中文 / ENGLISH and toggles lyrics and the now-playing row. While F5 is open, drag the small cover square to move the in-game row and scroll over it to resize. The left side holds the controls, the right side previews the selected track's cover above its title and artist. Only **F5** and **Esc** remain as keyboard shortcuts.

## Known limitations / 已知限制

- **No downloader is bundled** — `yt-dlp`, `ffmpeg` and `ffprobe` are not included. URL/YouTube downloading is provided by CGME itself when its Downloader is installed.
- **Lyrics may still overlap in some cases; the author (6r3²n) lacks the computer knowledge to fix it.**
- 部分情况下歌词仍可能重叠；作者（**6r3²n**）计算机知识不足，无法修复该问题。
- **The cover / title / artist row position and its window adaptation may still be inaccurate in some window sizes or aspect ratios; the author (6r3²n) lacks the computer knowledge to fully fix it.**
- **曲绘 / 曲名 / 作者行的位置及其窗口自适应，在部分窗口尺寸或宽高比下仍可能不准确；作者（6r3²n）计算机知识不足，无法完全修复。**
- Requires CybergrindMusicExplorer 2.3.2+ enabled. The old 1.x standalone fork (which reused CGME's GUID) must be removed first.
- Font family changes require a restart; glow depends on TMP shader variants shipped with the game.

## Credits / 特别感谢

- **Flazhik and the Jukebox / CybergrindMusicExplorer team** — the original mod (Apache-2.0) this extension depends on.
- **greycsont** — ULTRAKILL font reference and font mods (Fffffff / FNT_font_no_tsubasa).
- **end_4 / Thorn** — HUD framework used by `Jukebox.ThornExtensions`.
- **Google Fonts / Noto team** — Noto Sans JP / Noto Sans SC (SIL OFL 1.1); users may install them system-wide, but no font files are bundled.
- **SubtitlesParser, RTLTMPro, TagLib#** authors — runtime libraries.

## License / 许可证

Licensed under the **Apache License 2.0** — see [LICENSE](LICENSE). This is the same license as the upstream Jukebox project; the original copyright notices are retained. See [NOTICE](NOTICE) for attribution and a list of changes.

本项目使用 **Apache License 2.0**，与上游 Jukebox 一致；保留原始版权声明。归属与修改说明见 [NOTICE](NOTICE)。

GitHub / 开源地址：https://github.com/klkq6r33n/LCCBmusic

[EN]
NOTICE: Disable Flazhik-CybergrindMusicExplorer (this fork reuses its plugin GUID).
Downloader: this package does NOT support CGME's Downloader — yt-dlp / ffmpeg / ffprobe are not bundled. Use local audio files.
Install: install this zip with r2modman / Thunderstore Mod Manager, or extract it into the ULTRAKILL folder (runtime files live under BepInEx/plugins/Claude-JukeboxLimbusCompany/).
Lyrics: put Track.srt next to Track.mp3. Optional translation: also add Track.zh.srt / Track.cn.srt / Track.ja.srt / Track.trans.srt. Both lines are shown at different, non-overlapping positions.
Songs: this mod bundles no audio. Every song is read from ULTRAKILL's built-in Cyber Grind music folder (ULTRAKILL/Cybergrind/Music); put your audio there.
UI: default language is English. In the Jukebox menu (F4) use LANGUAGE to switch to Chinese and SETTINGS for precise numeric inputs (position, color, font size, motion, translation).
Safety: lyrics are clamped on-screen and never overlap each other.
Special thanks to greycsont for the ULTRAKILL font, and to Flazhik and the Jukebox / CybergrindMusicExplorer team for the original mod and source code this fork is based on.

[中文]
注意：必须禁用 Flazhik-CybergrindMusicExplorer（本 fork 沿用其插件 GUID）。
下载器：本包**不支持 CGME 的下载器**——未附带 yt-dlp / ffmpeg / ffprobe，请使用本地音频文件。
安装：用 r2modman / Thunderstore Mod Manager 安装本 zip，或直接解压到 ULTRAKILL 游戏根目录（运行文件位于 BepInEx/plugins/Claude-JukeboxLimbusCompany/）。
歌词：把 Track.srt 放在 Track.mp3 同目录。可选翻译：再放 Track.zh.srt / Track.cn.srt / Track.ja.srt / Track.trans.srt，两行歌词会在不同且不重叠的位置同时显示。
歌曲：本 mod **不附带任何音频**，所有歌曲均读取自 ULTRAKILL 自带的电子血宫音乐文件夹（ULTRAKILL/Cybergrind/Music），把音频放进该文件夹即可。
界面：默认英文。在 Jukebox 菜单（F4）用 LANGUAGE 切换中文，用 SETTINGS 精确填写位置、颜色、字号、动态与翻译。
防重叠/防出屏：歌词自动限制在屏幕内，且不会互相交叠。
特别感谢 greycsont 提供的 ULTRAKILL 字体，以及 Flazhik 与 Jukebox / CybergrindMusicExplorer 团队提供的原型 mod 与源码。

---

# Jukebox LimbusCompany

**Author / 作者：6r3²n**

**LimbusCompany lyric performance extension for CybergrindMusicExplorer.**

**CybergrindMusicExplorer 的 LimbusCompany 歌词演出扩展 mod。**

Based on the open-source **Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.1** source.
It replaces Jukebox's built-in Cyber Grind subtitle rendering with a LimbusCompany-style lyric layer: text color, outline, glow, font size/spacing, shake, fade-in, rise, random angle, pseudo-perspective and position range.

> This is a **standalone mod**, not an add-on inside the original mod folder. It reuses Jukebox's assembly names and plugin GUID (Addressables bundles bind scripts by assembly name, and Thorn extensions depend on that GUID), so **you must disable `Flazhik-CybergrindMusicExplorer` in r2modman**. The two cannot be enabled at the same time.
>
> **The CGME Downloader is not supported.** This package does not ship `yt-dlp`, `ffmpeg` or `ffprobe`, and the Downloader dependency was removed from the manifest; URL/YouTube downloads will not work. Use local audio files with SRT sidecars.

基于 **Flazhik/Jukebox (CybergrindMusicExplorer) 2.3.1 源码** 的独立 fork。
在 Cyber Grind 里把 Jukebox 自带的字幕显示替换成 LimbusCompany 演出风格的歌词层：文字颜色、描边、辉光、字号/字距、抖动、淡入、上升、随机角度、近似透视、位置范围。

> 本包是**独立 mod**，不是原 mod 文件夹里的附属扩展。因为它复用 Jukebox 的程序集名与插件 GUID（资源包按程序集名绑定脚本，Thorn 扩展也依赖该 GUID），**必须先在 r2modman 中禁用 `Flazhik-CybergrindMusicExplorer`**，两者不能同时启用。
>
> **不支持 CGME 的下载器。** 本包不附带 `yt-dlp`、`ffmpeg`、`ffprobe`，manifest 中也已移除 Downloader 依赖；通过 URL / YouTube 下载歌曲的功能不可用。请使用本地音频文件与 SRT 字幕。

---

## Contents / 内容

`manifest.json`, `icon.png` and `README.md` stay at the zip root; all runtime files below are packaged under `BepInEx/plugins/Claude-JukeboxLimbusCompany/`.

`manifest.json`、`icon.png`、`README.md` 位于 zip 根目录；下列运行文件均在 `BepInEx/plugins/Claude-JukeboxLimbusCompany/` 下。

```
Jukebox.dll                  fork main assembly (plugin GUID: dev.flazhik.jukebox, display: Jukebox LimbusCompany)
JukeboxCore.dll              built from the same source
Jukebox.ThornExtensions.dll  Thorn HUD modules (same GUID as upstream)
RTLTMPro.dll / SubtitlesParser.dll / TagLibSharp.dll   runtime dependencies
Assets/                      catalog.json + asset bundles (taken from the 2.3.2 original)

No downloader binaries are included: the CGME Downloader is unsupported, so
yt-dlp / ffmpeg / ffprobe / yt-dlp.sha256 are intentionally absent.

Jukebox.dll                  fork 主体（插件 GUID: dev.flazhik.jukebox，显示名 Jukebox LimbusCompany）
JukeboxCore.dll              同源码编译
Jukebox.ThornExtensions.dll  Thorn HUD 模块（沿用原 GUID）
RTLTMPro.dll / SubtitlesParser.dll / TagLibSharp.dll   运行时依赖
Assets/                      catalog.json 与资源 bundle（取自 2.3.2 原 mod）

不包含下载器程序：本 mod 不支持 CGME 下载器，因此刻意不附带
yt-dlp / ffmpeg / ffprobe / yt-dlp.sha256。
```

## Configuration / 配置

On first launch, `BepInEx/config/LimbusCompany/lyric_config.json` is created:
1. If `E:\084music\lyric_config.json` (an external lyrics tool) exists on this machine, it is copied as the initial config;
2. Otherwise the built-in defaults are written.

Changes are hot-reloaded within one second (font family needs a restart).

首次启动生成 `BepInEx/config/LimbusCompany/lyric_config.json`：
1. 若本机存在 `E:\084music\lyric_config.json`（外部歌词工具），直接复制为初始值；
2. 否则写入内置默认值。

保存后 1 秒内热重载（字体族除外，需重启）。

### Supported `settings` fields / 支持的 settings 字段

`text_color`, `stroke_color`, `stroke_width`, `glow_color`, `glow_enabled`, `glow_size`, `glow_alpha`, `font_family`, `font_size`, `spacing`, `shake_intensity`, `shake_speed`, `fade_speed`, `rise_speed`, `angle_min`, `angle_max`, `delay`, `perspective_enabled`, `persp_x_strength`, `persp_y_strength`, `pos_x_min`, `pos_x_max`, `pos_y_min`, `pos_y_max`, `zoom`, `language`.

### Extension fields / 扩展字段

| Field / 字段 | Default / 默认 | Meaning / 说明 |
|---|---|---|
| `enabled` | true | Master switch of the lyric layer / 歌词层总开关 |
| `replace_original` | true | Keep the original SubtitlesPanel blank / 让原 SubtitlesPanel 保持空白 |
| `random_position` | true | true = random inside the pos range (default whole screen); false = centre of the pos range. true = 每行在 pos 范围内随机（默认整屏）；false = 固定在范围中心 |
| `position_anchor` | `random` | `random` or a fixed anchor (`top-left`, `top`, `top-right`, `left`, `center`, `right`, `bottom-left`, `bottom`, `bottom-right`). `random` 或固定锚点（左下 / 右下等） |
| `random_angle` | true | Random rotation per line / 每行随机旋转 |

### Language / 语言

- Default UI language is **English**. Switch it in the Cyber Grind with the **LANGUAGE** button injected into the F4 menu, or in any level with **F5 → DISPLAY → 中文 / ENGLISH** (keyboard **G** also toggles it).
- The choice is saved as `"language": "en"` / `"zh"` in `lyric_config.json` and re-applied on launch.
- Chinese mode localizes **this mod's own UI only** (the LANGUAGE / SETTINGS buttons, the settings panel and the F5 menu). The original CybergrindMusicExplorer menu stays English.
- 默认界面语言为**英文**。血宫内用 F4 菜单里的 **LANGUAGE** 按钮切换；任意关卡内用 **F5 → DISPLAY → 中文 / ENGLISH**（键盘 **G** 也可切换）。
- 选择会写回 `lyric_config.json` 的 `"language"`（`en`/`zh`），下次启动自动生效。
- 中文模式**只翻译本 mod 自己的界面**（LANGUAGE / SETTINGS 按钮、设置面板与 F5 菜单），原版 CGME 菜单保持英文。

### In-game feature settings / 游戏内功能设置

While the Jukebox menu is open, the **SETTINGS / 设置** button (next to LANGUAGE) opens a 3-page panel where every value is a **numeric / text input field** (no coarse presets):

- **LAYOUT / 位置**: X min/max %, Y min/max %, zoom %, angle min/max, delay (ms), random position / random angle
- **TEXT / 文字**: text color, font size, spacing, stroke color/width, glow color/size/alpha, glow on/off, font family
- **MOTION / 动态**: shake intensity/speed, fade speed, rise speed, **typewriter on/off + chars per second**, perspective + X/Y strength, lyrics on/off, replace original subtitles
- **VISUAL / 频谱**: audio visualizer on/off, wave size, max extent (% of screen), position (BOTTOM / TOP / LEFT / RIGHT), bar density (8–256), gradient range (0–1), gradient speed, gradient mode (FIXED 2 / RANGE x~y / MULTI RGB), colour A RGB, colour B RGB, multi RGB (space separated), X mirror — plus an in-panel usage note
- **F5 mouse menu** (levels only; the Cyber Grind uses **F4** and F5 is ignored there): a clickable menu with MUSIC / SETTINGS / DISPLAY pages — browse `Cybergrind/Music` (the game's built-in Cyber Grind music folder; no audio is bundled), replace/restore the level BGM, switch the UI language, edit every lyric setting, toggle lyrics/now-playing, and drag/resize the cover row. The window is split: **left = controls, right = track preview** (cover on top, title and artist below). Opening it pauses the game and forcibly unlocks/shows the cursor, like the CGME menu.
- **Now-playing frame** (top-right by default): ULK-style bordered frame around the current track's cover art, with the title and artist underneath; when the track has no cover the frame stays hollow. Cover art is read from the audio file's ID3 tag or a same-name / `cover` / `folder` image; its position and size are adjustable.
- RESET PAGE (resets only the currently open page) / CLOSE

Every change is written to `lyric_config.json` immediately; the lyric style refreshes within a second. (`replace_original` takes full effect after re-entering Cyber Grind; font family needs a restart.)

打开 Jukebox 菜单后，右上角 **LANGUAGE** 旁边的 **SETTINGS / 设置** 按钮会打开一个设置面板，每一项都是**数值/文本输入框**（不再用固定档位）：

- **LAYOUT / 位置**：X 最小/最大 %、Y 最小/最大 %、缩放 %、角度最小/最大、延迟（毫秒）、随机位置/随机旋转
- **TEXT / 文字**：文字颜色、字号、字距、描边颜色/宽度、辉光颜色/大小/透明度、辉光开关、字体
- **MOTION / 动态**：抖动强度/速度、淡入速度、上飘速度、**逐字显示开关 + 每秒字数**、透视与 X/Y 强度、歌词层开关、替换原版字幕
- **VISUAL / 频谱**：音频可视化开关、波动大小、最大幅度（屏幕百分比）、显示位置（底部/顶部/左侧/右侧）、频谱密度（8–256）、渐变范围（0–1）、渐变强度（速度）、渐变模式（固定双色 / 区间渐变 / 多色渐变）、颜色 A RGB、颜色 B RGB、多色 RGB（空格分隔）、X 轴镜像，面板内附使用说明
- **F5 鼠标菜单**（仅关卡可用；血宫内只用 **F4**，F5 会被忽略）：可点击的 MUSIC / SETTINGS / DISPLAY 三页——浏览 `Cybergrind/Music`（ULTRAKILL 自带的电子血宫音乐文件夹，mod 不附带音频）、取代/归还关卡 BGM、切换界面语言、修改全部歌词设置、开关歌词/曲绘行、用鼠标拖动缩放曲绘行。窗口分为**左侧功能栏**与**右侧曲目预览**（曲绘在上，曲名与作者在下）。打开时暂停游戏并强行解锁/显示鼠标，操作方式与 CGME 菜单一致。
- **曲绘框**（默认右上角）：ULTRAKILL 风格的描边框住当前歌曲的曲绘，下方显示歌名与歌手；没有曲绘时框体镂空。曲绘从音频文件的 ID3 标签或同名 / `cover` / `folder` 图片读取，位置和大小都可调整。
- 重置本页（只重置当前打开的页面）/ 关闭

每次改动都会立刻写回 `lyric_config.json`，歌词样式 1 秒内刷新。（`replace_original` 需要重新进入 Cyber Grind 才完全生效；字体需要重启。）

### Audio visualizer / 音频可视化

- Reacts to the currently playing Jukebox track or level BGM (`AudioSource.GetSpectrumData`), drawn as bars behind the lyrics.
- `visualizer_enabled` (default off), `visualizer_amplitude` (wave size multiplier, 0.1–5), `visualizer_size` (max extent as % of the screen dimension, default 50, **no upper limit**), `visualizer_position` (`bottom` default / `top` / `left` / `right`), `visualizer_bars` (density, 8–256, default 96), `visualizer_gradient_range` (0 = solid start colour, 1 = full gradient spread), `visualizer_gradient_speed` (gradient animation speed; the "gradient strength"), `visualizer_gradient_mode`, `visualizer_color_a`, `visualizer_color_b`, `visualizer_colors`, `visualizer_mirror_x` (X-axis mirror).
- **Gradient speed / strength** (`visualizer_gradient_speed`, 0–2, default `0.08`): how fast the colours move; `0` freezes the gradient.
- **Gradient modes** (`visualizer_gradient_mode`) — every mode animates per bar, so different frequency blocks change colour independently:
  - `fixed2` — **two colours**: colour A → colour B flows in one direction along the row and loops.
  - `range` (default) — **x~y**: the same two colours sweep back and forth over time.
  - `multi` — **multiple RGB stops**: the whole row flows through every colour in `visualizer_colors`.
- **RGB input**: each colour is written as `R,G,B` (0–255), separated by `,` or `，`, e.g. `255,211,77`. `visualizer_colors` holds several such triplets separated by **spaces**, e.g. `255,0,0 0,255,0 0,0,255`. Defaults: A `216,165,35`, B `0,229,255`, multi `255,0,0 255,165,0 255,255,0 0,255,0 0,140,255 160,0,255`.
- Colours never react to audio intensity (bar height still follows the music). Every bar also has a **black outline**.
- The visualizer is drawn on its **own canvas below the original game UI**, so it never covers the vanilla HUD.
- Bars are flush with the screen edge (no gap at TOP/BOTTOM/LEFT/RIGHT).
- The maximum extent is entirely user-defined (no 50% cap); set it as large as you want.

- 跟随当前 Jukebox 曲目或关卡 BGM（`AudioSource.GetSpectrumData`），以可配置密度的柱状条绘制在歌词层下方。
- `visualizer_enabled`（默认关）、`visualizer_amplitude`（波动大小倍数，0.1–5）、`visualizer_size`（最大幅度占屏幕对应方向的百分比，默认 50，**无上限**）、`visualizer_position`（默认 `bottom`，可选 `top`/`left`/`right`）、`visualizer_bars`（频谱密度，8–256，默认 96）、`visualizer_gradient_range`（0=起点单色，1=整排铺满渐变）、`visualizer_gradient_speed`（渐变速度 = 渐变强度）、`visualizer_gradient_mode`、`visualizer_color_a`、`visualizer_color_b`、`visualizer_colors`、`visualizer_mirror_x`（X 轴镜像）。
- **渐变强度 / 速度**（`visualizer_gradient_speed`，0–2，默认 `0.08`）：数值越大颜色流动越快；`0` 表示渐变静止。
- **三种渐变模式**（`visualizer_gradient_mode`）—— 三种模式都会按柱子的不同相位随时间变色：
  - `fixed2` —— **双色流动**：颜色 A → 颜色 B 沿整排单向流动并循环；
  - `range`（默认）—— **x~y 区间渐变**：同样两种颜色，混合比例随时间来回扫动；
  - `multi` —— **多色渐变**：整排按 `visualizer_colors` 里的多个 RGB 色标依次流动。
- **RGB 输入格式**：单个颜色写作 `R,G,B`（0–255），用 `,` 或 `，` 分隔，如 `255,211,77`；`visualizer_colors` 里多个颜色用**空格**分隔，如 `255,0,0 0,255,0 0,0,255`。默认：A `216,165,35`、B `0,229,255`、多色 `255,0,0 255,165,0 255,255,0 0,255,0 0,140,255 160,0,255`。
- 颜色**不随音频强度变化**（柱高仍然跟随音乐）。每根柱状条都带**黑色描边**。
- 波形图放在**独立画布、排序低于游戏原版 UI**，不会盖住原版界面。
- 柱状条紧贴屏幕边缘（TOP/BOTTOM/LEFT/RIGHT 均无空隙）。
- 最大幅度完全由用户设置（不再限制 50%），可以设得任意大。

### Level BGM replacement / 关卡 BGM 取代

- Press **F5** in any level to open the mouse menu. It is separate from the Cyber Grind menu: **F4 works only in the Cyber Grind, F5 only in levels.** Opening the menu pauses the game and unlocks the cursor.
- **MUSIC**: lists every `.mp3` / `.ogg` / `.wav` under `Cybergrind/Music` recursively (page buttons at the bottom); click a track to select it, then **REPLACE LEVEL BGM** or **RESTORE BGM**.
- **SETTINGS**: the same LAYOUT / TEXT / MOTION / TRANS / VISUAL pages as the in-Cyber-Grind menu, fully mouse-driven (inputs, toggles, per-page reset).
- **DISPLAY**: switch **中文 / ENGLISH**, toggle lyrics and the now-playing frame, move/resize the cover and title, and click any key binding to rebind it (press the new key, Esc cancels).
- Keyboard shortcuts still work: `[` / `]` or **← / →** change track, **Enter** replaces, **Backspace** restores, **L** lyrics, **N** cover, **G** language, **K** rebind (1–8), **P** position, **Esc** closes, **F5** toggles.
- **Replace** pauses the game's level music (clean / battle / boss themes) and starts the selected track on a loop. The paused level music keeps its exact position.
- **Restore** stops the replacement and unpauses the level music, which continues from the point where it was paused. Scene changes also end the replacement automatically.
- The audio visualizer reads the replacement track while it is active, and reads the game's level music after restoration — so the spectrum works for both.
- Only local files are used; the CGME Downloader is still unsupported.

- 在任意关卡中按 **F5** 打开鼠标菜单。它与血宫菜单分开：**F4 只在血宫可用，F5 只在关卡可用。** 打开时暂停游戏并解锁鼠标。
- **MUSIC**：递归列出 `Cybergrind/Music` 下所有 `.mp3` / `.ogg` / `.wav`（底部翻页）；点击曲目选中，再点 **取代关卡 BGM** 或 **归还原 BGM**。
- **SETTINGS**：与血宫内菜单相同的 位置 / 文字 / 动态 / 翻译 / 频谱 五页，全部可用鼠标操作（输入框、开关、单页重置）。
- **DISPLAY**：切换 **中文 / English**、开关歌词与曲绘框、移动/缩放曲绘与歌名，并点击任意按键绑定后按下新键改绑（Esc 取消）。
- 键盘快捷键仍可用：`[` / `]` 或 **← / →** 切换曲目，**Enter** 取代，**Backspace** 归还，**L** 歌词，**N** 曲绘，**G** 语言，**K** 改键（1–8），**P** 调位置，**Esc** 关闭，**F5** 开关菜单。
- **取代**：暂停游戏当前的关卡音乐（clean / battle / boss 三个主题），循环播放选定曲目；被暂停的关卡音乐保留准确进度。
- **归还**：停止替代曲目并恢复关卡音乐，从暂停的位置继续；切换关卡时也会自动结束取代。
- 频谱在取代期间读取替代曲目，归还后读取关卡原音乐，两种情况下都有效。
- 只使用本地文件；CGME 下载器仍不受支持。

### Now-playing row / 曲绘行

- While a Jukebox song or a replaced level track is playing, one horizontal row is drawn: **cover / title / artist** (曲绘 / 曲名 / 作者). The row is **small by default**: the text size is the reference and the cover is only slightly larger than the text (about 1.3×), never the other way around. The title and artist use the **same font size**; the artist is the last item in the row.
- The cover sits in a ULK-style bordered frame. No cover → the frame stays hollow (only the border is drawn).
- Cover source, in order: an image with the audio file's base name (`.png` / `.jpg` / `.jpeg`), `cover.*` / `folder.*` in the same folder, then the embedded ID3 picture.
- **Mouse adjustment** (F5 → DISPLAY → **ADJUST ROW WITH MOUSE**): the menu hides, then **drag the whole row** to move it and **scroll the mouse wheel** to change its size; press **Esc** or click **DONE** to finish (the menu closes so you can see the result). The DISPLAY page also has arrow / `-` `+` / reset buttons, and keyboard **P** still works (arrows move, Shift = fine, `-` / `+` resize, P/Esc finish). Changes save immediately.
- The **F5 right-hand preview** uses a vertical layout instead: the selected track's cover is shown **above** its title and author. The in-game row stays horizontal (`cover / title / artist`).
- Config keys: `now_playing_enabled`, `cover_pos_x/y` (row anchor: right edge X and vertical center Y, in % of the screen), `cover_size` (**text/row size**, 10–120, default 32; the cover is about 1.3× this); `title_pos_x/y` are kept for old configs but are no longer used.

- 当 Jukebox 曲目或取代的关卡曲目正在播放时，会绘制**一整行**：**曲绘 / 曲名 / 作者**。整行默认**很小**：以文字大小为基准，曲绘只比文字略大（约 1.3 倍），而不是把文字放大到曲绘那么大。曲名与作者使用**相同字号**，作者排在整行最后。
- 曲绘外面是 ULK 风格的描边框。没有曲绘时框体镂空（只画边框）。
- 曲绘来源依次为：与音频同名的图片（`.png` / `.jpg` / `.jpeg`）、同目录的 `cover.*` / `folder.*`、音频内嵌的 ID3 图片。
- **鼠标调整**（F5 → DISPLAY → **用鼠标调整曲绘行**）：菜单会隐藏，此时**拖动整行**移动、**滚轮**改变大小，按 **Esc** 或点击 **完成** 结束（结束后菜单关闭并恢复游戏，方便立刻查看效果）。DISPLAY 页也保留方向按钮、`-` `+` 与重置按钮；键盘 **P** 仍可用（方向键移动，Shift 微调，`-` / `+` 缩放，P/Esc 结束）。改动立即保存。
- **F5 右侧预览**使用竖排布局：选中曲目的曲绘在**上**，曲名与作者在**下**；游戏内的曲绘行仍然是横排（`曲绘 / 曲名 / 作者`）。
- 配置项：`now_playing_enabled`、`cover_pos_x/y`（整行锚点：右边缘 X、垂直中心 Y，屏幕百分比）、`cover_size`（**文字/整行大小**，10–120，默认 32，曲绘约为它的 1.3 倍）；`title_pos_x/y` 仅为兼容旧配置保留，已不再使用。

### Position safety / 位置防重叠与防出屏

- Random-position mode re-rolls each new line until it does not overlap the previous line's box, so consecutive lines never stack on the same spot.
- Lines that appear within the `avoid_window` time window (default 6 s) are forced into different lanes, even when random position is off; the required spacing is `avoid_gap` pixels (default 30) and the lane step is `avoid_step` % of screen height (default 10). All are editable in SETTINGS → LAYOUT.
- Additionally, **overlap with any line from the last 2.5 s is a hard constraint** (minimum 12 px). With very large `avoid_gap` values the preferred spacing is relaxed, but two nearby lines can never share the same box.
- **Each cue is an independent line**: when a new line starts, the previous line stays at its own position and finishes its **own fade-out** (it is not replaced in place), while the new line is placed somewhere else and avoids every still-visible line. So cues that are 0.1 s apart never collapse onto the same spot, and neither line cuts the other's fade-in/fade-out. This applies to every song.
- **Position is user-controlled**: `random_position` (default **true**) makes every line spawn at a random point; the available area is the `pos_x_min/max` × `pos_y_min/max` percentage rectangle, which defaults to the **whole screen (0–100)**. Turn `random_position` off to keep all lines at the centre of that rectangle.
- **Position anchor** (`position_anchor`, default `random`): choosing `top-left` / `top` / `top-right` / `left` / `center` / `right` / `bottom-left` / `bottom` / `bottom-right` pins every lyric to that spot (e.g. bottom-left / bottom-right); `random` keeps the full-screen random behaviour. Cycle it in SETTINGS → LAYOUT. When a previous line is still fading at the anchor, the new line steps away from the anchored edge instead of being drawn on top of it.
- Every line is still clamped inside the screen using its measured size and rotation, so it can never render off-screen.
- When a translation SIDECAR exists, the main lyric is restricted to the upper half of the screen and the translation always sits in the lower half, so the two can never overlap, regardless of `anti_overlap_mode`.
- Translation placement is always **STRICT** now: the main lyric uses the **upper half of the safe area** and the translation the **lower half** (safe area = screen minus the visualizer band). The two halves never intersect, so overlap is physically impossible regardless of gaps, randomness, rise or typewriter. The `anti_overlap_mode` setting (MIRROR / BELOW / AUTO / CUSTOM) is kept for compatibility but no longer changes translation placement.
- Main lyric and translation line avoid each other both at spawn and while moving; each new line is shifted up/down/left/right until the boxes no longer intersect.
- The lyric box is clamped inside the canvas using its measured text size and rotation, so it never renders off-screen at any resolution.
- 随机位置模式下，每一句都会重新取点，直到不与上一句的文本框重叠。
- 在 `avoid_window` 时间窗（默认 6 秒）内出现的歌词会强制换到不同车道（即使关闭随机位置），间距由 `avoid_gap`（默认 30 像素）控制，车道步长为 `avoid_step`（屏高百分比，默认 10）；均在 SETTINGS → LAYOUT 可调。
- 另外，**与最近 2.5 秒内任何歌词的重叠是硬约束**（至少 12px）。`avoid_gap` 设得再大也只是放宽偏好间距，不会让相近歌词共用同一个位置。
- **每一句都是独立歌词**：新句出现时，上一句仍留在**自己的位置**继续完成它自己的渐隐（不会被就地替换），新句则另选位置并避开所有仍然可见的旧句。因此 0.1 秒间隔的连句不会挤到同一处，两段的淡入/淡出互不影响；所有歌曲都按这个方式处理。
- **位置由用户决定**：`random_position`（默认 **true**）时每句在 `pos_x_min/max` × `pos_y_min/max` 百分比范围内随机出现，默认范围为**整屏（0–100）**；关闭后所有歌词固定在该范围的中心点。
- **固定位置**（`position_anchor`，默认 `random`）：可选 `top-left` / `top` / `top-right` / `left` / `center` / `right` / `bottom-left` / `bottom` / `bottom-right`，把歌词固定钉在对应位置（例如左下 / 右下）；`random` 则保持全屏随机。可在 SETTINGS → LAYOUT 里循环切换。若上一句还在锚点处渐隐，新句会沿着远离该边缘的方向逐级让位，不会直接压上去。
- 每句都会按实际文字尺寸和旋转角度钳制在屏幕内，不会跑出屏幕。
- 存在翻译 sidecar 时，主歌词限制在屏幕上半、翻译固定在屏幕下半，两者无论 `anti_overlap_mode` 怎么设置都不可能重叠。
- 翻译歌词现在**固定使用分区 STRICT**：主歌词占**安全区上半**，翻译占**下半**（安全区 = 屏幕去掉波形占用带），两个区域互不相交，物理上不可能重叠（不受间距、随机、上飘、逐字显示影响）。`anti_overlap_mode` 里的 MIRROR / BELOW / AUTO / CUSTOM 仅为兼容旧配置保留，不再改变翻译位置。
- 主歌词与翻译歌词在生成和移动过程中都会互相避让，上下左右错开直到包围盒不相交。
- 歌词框会按实际文字尺寸和旋转角度限制在画布内，任意分辨率下都不会跑到屏幕外。

### Fields from the external tool that are ignored / 不生效的外部工具字段

`loop`, `trans_only`, `mode`, `margin_time`, `max_interval`, `max_duration`, `player`, `source`, `inst_patterns`, `netease_adapter_enabled`, `presets`, `players`.
These belong to an external player's lyric fetching / auto timing. In game, the SRT timeline is used directly.

这些属于外部播放器抓词/自动时间轴的部分；游戏内直接使用 SRT 时间轴。

## Lyrics source / 歌词来源

**Songs:** this mod bundles **no audio at all**. It only reads the tracks already placed in ULTRAKILL's built-in Cyber Grind music folder — `ULTRAKILL/Cybergrind/Music` (shown as `Cybergrind/Music` below). The F5 MUSIC page lists exactly that folder recursively.

**歌曲：** 本 mod **不附带任何音频**，只读取 ULTRAKILL 自带电子血宫音乐文件夹 `ULTRAKILL/Cybergrind/Music`（下文简写为 `Cybergrind/Music`）里已有的曲目；F5 的 MUSIC 页就是递归列出这个文件夹。

Same as the original mod: a **`.srt` file with the exact same filename** next to the audio file.

与原 mod 相同：音频旁**同名 `.srt`**（同目录）。

```
Cybergrind/Music/Mili - Rendezvous/Mili - Rendezvous.mp3
Cybergrind/Music/Mili - Rendezvous/Mili - Rendezvous.srt
```

Folder-form songs are fully supported. Inside the song folder, the main subtitle can be any of:

- `<audio file name>.srt` (preferred);
- `<folder name>.srt`;
- any other `.srt` in the folder (files that look like translation sidecars, e.g. `.zh.srt` / `.trans.srt`, are skipped as the main subtitle).

Translation sidecars are also read from the same folder. A level BGM replaced through **F5 → MUSIC** follows the same rules and displays its lyrics from the replacement track's own playback position.

文件夹形式的歌曲已完整支持。歌曲文件夹里的主字幕可以是：

- `<音频文件名>.srt`（优先）；
- `<文件夹名>.srt`；
- 该文件夹内任意其他 `.srt`（类似翻译字幕的例如 `.zh.srt` / `.trans.srt` 不会被当作主字幕）。

翻译字幕同样从该文件夹读取。通过 **F5 → MUSIC** 取代的关卡 BGM 也按相同规则读取字幕，并按替代曲目自己的播放进度显示歌词。

### Bilingual lyrics / 双语歌词

Add a second, translated SRT next to the track. Any of these suffixes is picked up automatically:

在音频旁再放一份翻译字幕，以下后缀会被自动识别：

```
Track.zh.srt   Track.cn.srt   Track.chs.srt   Track.cht.srt
Track.ja.srt   Track.jp.srt   Track.trans.srt Track.translation.srt
Track.中文.srt  Track.翻译.srt
Track_zh.srt   Track_cn.srt   Track_ja.srt    Track_trans.srt
```

- The original line and the translation line are **shown at the same time**, at different positions.
- The translation line is placed separately and is shifted until it does not overlap the main lyric box; both are clamped on-screen.
- Translation style: `translation_enabled`, `translation_font_size` (absolute size for the translated text; `0` = auto), `translation_scale` (used only when the absolute size is 0; default 0.75), `translation_color` (default `#d8d8d8`), editable in the **TRANS / 翻译** page of the in-game settings panel.
- The main lyric and the translation line have **independent font sizes**: set `font_size` for the original language and `translation_font_size` for the translated language.

- 原歌词与翻译歌词会**同时显示**，位置不同。
- 翻译行单独取点，会自动上下移动直到不与主歌词框重叠；两者都被限制在屏幕内。
- 翻译样式：`translation_enabled`、`translation_font_size`（翻译文字的绝对字号，`0` 表示自动）、`translation_scale`（仅当绝对字号为 0 时生效，默认 0.75）、`translation_color`（默认 `#d8d8d8`），可在游戏内设置的 **TRANS / 翻译** 页修改。
- 主歌词与翻译歌词的**字号相互独立**：`font_size` 设置原文语言字号，`translation_font_size` 设置翻译语言字号。

## Special thanks / 特别感谢

- **greycsont** — ULTRAKILL font reference and font mods (Fffffff / FNT_font_no_tsubasa).
- **Flazhik and the Jukebox / CybergrindMusicExplorer team** — the original mod and source code; this project is a fork of their work.
- **end_4 / Thorn** — HUD framework used by `Jukebox.ThornExtensions`.
- **Google Fonts / Noto team** — Noto Sans JP / Noto Sans SC, included under the SIL Open Font License 1.1.
- **SubtitlesParser, RTLTMPro, TagLib#** authors — runtime libraries.

- **greycsont** —— ULTRAKILL 字体参考与字体 mod（Fffffff / FNT_font_no_tsubasa）。
- **Flazhik 与 Jukebox / CybergrindMusicExplorer 团队** —— 原型 mod 与源码，本项目是其分支。
- **end_4 / Thorn** —— `Jukebox.ThornExtensions` 使用的 HUD 框架。
- **Google Fonts / Noto 团队** —— Noto Sans JP / Noto Sans SC，以 SIL Open Font License 1.1 随包分发。
- **SubtitlesParser、RTLTMPro、TagLib#** 的作者 —— 运行时库。

## Bundled fonts / 随包字体

`Fonts\` contains optional SIL OFL fonts for complete Japanese/Chinese glyph coverage:

- `NotoSansJP-Variable.ttf` (SIL OFL 1.1)
- `NotoSansSC-Variable.ttf` (SIL OFL 1.1)
- `README.txt` with install steps and licence texts.

Right-click → **Install**, then restart the game: the mod automatically prefers **Noto Sans JP / Noto Sans SC** when they are installed. The ULTRAKILL Latin font itself is not redistributed; install greycsont's font mods for the game-exact look.

`Fonts\` 内附可选的 SIL OFL 字体，用于补全日文/中文字形：

- `NotoSansJP-Variable.ttf`（SIL OFL 1.1）
- `NotoSansSC-Variable.ttf`（SIL OFL 1.1）
- `README.txt`（安装说明与许可证）

右键 → **安装**，重启游戏后 mod 会自动优先使用 **Noto Sans JP / Noto Sans SC**。ULTRAKILL 官方英文字体不随包分发，需要官方字形请安装 greycsont 的字体 mod。

## Known limitations / 已知限制

- The original `SubtitlesPanel` is blanked. Thorn's Subtitle HUD module keeps an empty frame (auto-hidden when `HideBackgroundWhenEmpty`).
- Fonts are chosen per line: English / Latin lines use the ULTRAKILL official font (the Jukebox subtitle panel font, i.e. the game UI font; if unavailable, loaded TMP fonts named VCR/ULTRAKILL/OSD are tried). CJK lines use the panel font if it has CJK glyphs, otherwise a system font turned into a dynamic TMP asset (Yu Gothic → Microsoft YaHei → DengXian → …).
- Outline/glow depend on the TMP shader variants shipped with the game; glow may be missing if the variant was stripped.
- Changing `font_family` requires a restart.
- Lyric text is NFKC-normalized at runtime: CJK compatibility ideographs (e.g. U+F9EC `溺` → `溺`), variation selectors and stray control characters are removed, so copy-pasted lyrics cannot flicker as missing glyphs.
- Diagnostics are written to `BepInEx/LogOutput.log` under `[Jukebox LimbusCompany]` (normalized text, line placement, and a warning if an overlap is still detected).
- **The CGME Downloader is not supported.** `yt-dlp`, `ffmpeg`, `ffprobe` and `yt-dlp.sha256` are not bundled, and the Downloader dependency is not declared. URL / YouTube downloads are unavailable; local audio files are required.
- **Only the lyric layer is added.** CGME's non-lyric localization extras and helper scripts are not included; the Jukebox core (player, playlists, menu) is still required and remains as-is.
- **Known issue:** in some cases lyrics may still overlap; the author (**6r3²n**) lacks computer knowledge and made this mod with AI, but the AI used could not fix this issue.

- 原版 `SubtitlesPanel` 被置空；Thorn 的 Subtitle HUD 模块会保留一个空框（`HideBackgroundWhenEmpty` 时自动隐藏）。
- 特别感谢 **greycsont** 提供的 ULTRAKILL 字体。 / Special thanks to **greycsont** for the ULTRAKILL font.
- 字体按行自动选择：英文/拉丁行使用 ULTRAKILL 官方字体（Jukebox 字幕面板字体，即游戏 UI 字库；若取不到则在已加载的 TMP 字体中找 VCR/ULTRAKILL/OSD 命名的字体）；中日文行优先用含 CJK 字形的面板字体，否则从系统字体动态生成（Yu Gothic → Microsoft YaHei → 等线 → …）。
- 描边/辉光依赖游戏内 TMP 着色器变体；若被裁剪可能看不到辉光。
- 换 `font_family` 需重启游戏。
- 歌词在运行时做 NFKC 归一化：CJK 兼容汉字（如 U+F9EC `溺` → `溺`）、变体选择符和多余控制字符会被替换/移除，避免从音乐平台复制的歌词因缺字反复闪烁。
- 诊断信息写入 `BepInEx/LogOutput.log` 的 `[Jukebox LimbusCompany]` 下（字符归一化、每句落点，以及仍检测到重叠时的警告）。
- **不支持 CGME 下载器。** 不附带 `yt-dlp`、`ffmpeg`、`ffprobe`、`yt-dlp.sha256`，manifest 也不再声明 Downloader 依赖；URL / YouTube 下载功能不可用，请使用本地音频文件。
- **本包只新增歌词层。** 不包含 CGME 的非歌词本地化内容与辅助脚本；Jukebox 核心（播放器、播放列表、菜单）仍是必需部分，保持原样。
- **已知问题：** 部分情况下歌词仍可能重叠；作者（**6r3²n**）计算机知识不足，使用 AI 制作此 mod，但所使用的 AI 无法修复该问题。

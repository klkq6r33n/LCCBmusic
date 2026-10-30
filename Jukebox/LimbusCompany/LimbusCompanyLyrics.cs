using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox.LimbusCompany;

/// <summary>
/// LimbusCompany 演出风格歌词层的全局入口与配置。
/// </summary>
public static class LimbusCompanyLyrics
{
    public static LimbusCompanyLyricsSettings Settings { get; private set; }

    /// <summary>本 mod 独立日志源（不会被 JukeboxDownloader 的初始化覆盖）。</summary>
    public static ManualLogSource Log { get; private set; }

    /// <summary>设置面板保存后立即通知歌词层刷新（不等文件轮询）。</summary>
    public static event Action SettingsChanged;

    public static string ConfigDirectory => Path.Combine(Paths.ConfigPath, "LimbusCompany");

    public static bool SuppressOriginal =>
        Settings != null && Settings.Enabled && Settings.ReplaceOriginal;

    public static void Initialize()
    {
        try
        {
            Log = Logger.CreateLogSource("Jukebox LimbusCompany");
            Settings = LimbusCompanyLyricsSettings.Load(ConfigDirectory);
            Log.LogInfo($"配置已加载: {Path.Combine(ConfigDirectory, "lyric_config.json")}");
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log.LogError($"LimbusCompanyLyrics 读取配置失败，使用默认值: {e}");
            Settings = new LimbusCompanyLyricsSettings();
        }
    }

    public static void Reload()
    {
        try
        {
            Settings = LimbusCompanyLyricsSettings.Load(ConfigDirectory);
            LimbusCompanyLyrics.Log.LogInfo("LimbusCompanyLyrics 配置已重新加载。");
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log.LogWarning($"LimbusCompanyLyrics 重新加载配置失败: {e.Message}");
        }
    }

    /// <summary>Switches the UI language ("en" / "zh") and writes it back to lyric_config.json.</summary>
    public static void SetLanguage(string language)
    {
        if (Settings == null)
            return;

        Settings.Language = LimbusCompanyLyricsSettings.NormalizeLanguage(language);
        SaveAllSettings();
    }

    /// <summary>Writes the settings exposed by the in-game settings panel back to lyric_config.json.</summary>
    public static void SaveAllSettings()
    {
        var s = Settings;
        if (s == null || string.IsNullOrEmpty(s.FilePath) || !File.Exists(s.FilePath))
            return;

        try
        {
            var root = JObject.Parse(File.ReadAllText(s.FilePath, Encoding.UTF8));
            var settings = root["settings"] as JObject ?? root;

            settings["language"] = s.Language;
            settings["bgm_track"] = s.BgmTrack;
            settings["bgm_key_toggle"] = s.BgmKeyToggle;
            settings["bgm_key_prev"] = s.BgmKeyPrev;
            settings["bgm_key_next"] = s.BgmKeyNext;
            settings["bgm_key_replace"] = s.BgmKeyReplace;
            settings["bgm_key_restore"] = s.BgmKeyRestore;
            settings["bgm_key_lyrics"] = s.BgmKeyLyrics;
            settings["bgm_key_now_playing"] = s.BgmKeyNowPlaying;
            settings["now_playing_enabled"] = s.NowPlayingEnabled;
            settings["cover_pos_x"] = s.CoverPosX;
            settings["cover_pos_y"] = s.CoverPosY;
            settings["title_pos_x"] = s.TitlePosX;
            settings["title_pos_y"] = s.TitlePosY;
            settings["cover_size"] = s.CoverSize;
            settings["enabled"] = s.Enabled;
            settings["replace_original"] = s.ReplaceOriginal;
            settings["glow_enabled"] = s.GlowEnabled;
            settings["random_position"] = s.RandomPosition;
            settings["random_angle"] = s.RandomAngle;
            settings["translation_enabled"] = s.TranslationEnabled;
            settings["translation_scale"] = s.TranslationScale;
            settings["translation_font_size"] = s.TranslationFontSize;
            settings["translation_color"] = s.TranslationColor;
            settings["font_size"] = s.FontSize;
            settings["font_family"] = s.FontFamily;
            settings["spacing"] = s.Spacing;
            settings["text_color"] = s.TextColor;
            settings["stroke_color"] = s.StrokeColor;
            settings["stroke_width"] = s.StrokeWidth;
            settings["glow_color"] = s.GlowColor;
            settings["glow_size"] = s.GlowSize;
            settings["glow_alpha"] = s.GlowAlpha;
            settings["shake_intensity"] = s.ShakeIntensity;
            settings["shake_speed"] = s.ShakeSpeed;
            settings["fade_speed"] = s.FadeSpeed;
            settings["rise_speed"] = s.RiseSpeed;
            settings["typewriter_enabled"] = s.TypewriterEnabled;
            settings["typewriter_speed"] = s.TypewriterSpeed;
            settings["angle_min"] = s.AngleMin;
            settings["angle_max"] = s.AngleMax;
            settings["delay"] = s.Delay;
            settings["pos_x_min"] = s.PosXMin;
            settings["pos_x_max"] = s.PosXMax;
            settings["pos_y_min"] = s.PosYMin;
            settings["pos_y_max"] = s.PosYMax;
            settings["position_anchor"] = s.PositionAnchor;
            settings["zoom"] = s.Zoom;
            settings["anti_overlap_mode"] = s.AntiOverlapMode;
            settings["avoid_window"] = s.AvoidWindow;
            settings["avoid_gap"] = s.AvoidGap;
            settings["avoid_step"] = s.AvoidStep;
            settings["custom_offset_x"] = s.CustomOffsetX;
            settings["custom_offset_y"] = s.CustomOffsetY;
            settings["visualizer_enabled"] = s.VisualizerEnabled;
            settings["visualizer_amplitude"] = s.VisualizerAmplitude;
            settings["visualizer_size"] = s.VisualizerSize;
            settings["visualizer_position"] = s.VisualizerPosition;
            settings["visualizer_bars"] = s.VisualizerBars;
            settings["visualizer_gradient_range"] = s.VisualizerGradientRange;
            settings["visualizer_gradient_speed"] = s.VisualizerGradientSpeed;
            settings["visualizer_gradient_mode"] = s.VisualizerGradientMode;
            settings["visualizer_color_a"] = s.VisualizerColorA;
            settings["visualizer_color_b"] = s.VisualizerColorB;
            settings["visualizer_colors"] = s.VisualizerColors;
            settings["visualizer_mirror_x"] = s.VisualizerMirrorX;
            settings["perspective_enabled"] = s.PerspectiveEnabled;
            settings["persp_x_strength"] = s.PerspXStrength;
            settings["persp_y_strength"] = s.PerspYStrength;

            File.WriteAllText(s.FilePath, root.ToString(Formatting.Indented), new UTF8Encoding(false));

            // 立即通知歌词层刷新，同时让文件轮询也保持兜底
            SettingsChanged?.Invoke();
            s.LastWriteUtc = DateTime.MinValue;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log.LogWarning($"LimbusCompany 保存设置失败: {e.Message}");
        }
    }
}

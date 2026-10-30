using System;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 与 E:\084music\lyric_config.json 相同的 settings 结构，外加几个本 mod 用的扩展项。
/// </summary>
public class LimbusCompanyLyricsSettings
{
    public string TextColor = "#fffeef";
    public string StrokeColor = "#d8a523";
    public string GlowColor = "#d8a523";
    public bool GlowEnabled = true;
    public float GlowSize = 13f;
    public float GlowAlpha = 23f;

    public string FontFamily = "等线 Light";
    public float FontSize = 40f;
    public float StrokeWidth = 0.6f;
    public float Spacing = 4f;

    public float ShakeIntensity = 1f;
    public float ShakeSpeed = 119f;
    public float FadeSpeed = 6f;
    public float RiseSpeed = 0f;

    // ---- 逐字显示 ----
    public bool TypewriterEnabled = false;
    public float TypewriterSpeed = 12f;

    public float AngleMin = -10f;
    public float AngleMax = 10f;
    public int Delay = 0;

    public bool PerspectiveEnabled = true;
    public float PerspXStrength = 10f;
    public float PerspYStrength = 0f;

    public float PosXMin = 0f;
    public float PosXMax = 100f;
    public float PosYMin = 0f;
    public float PosYMax = 100f;

    /// <summary>random（默认）或固定锚点：top-left / top / top-right / left / center / right / bottom-left / bottom / bottom-right。</summary>
    public string PositionAnchor = "random";
    public float Zoom = 100f;

    // ---- 防交错 ----
    /// <summary>strict / auto / mirror / below / custom</summary>
    public string AntiOverlapMode = "strict";

    public float AvoidWindow = 6f;
    public float AvoidGap = 30f;

    /// <summary>车道步长，屏幕高度的百分比。</summary>
    public float AvoidStep = 10f;

    /// <summary>custom 模式下翻译歌词相对主歌词的偏移（像素）。</summary>
    public float CustomOffsetX = 0f;

    public float CustomOffsetY = -140f;

    // ---- 音频可视化 ----
    public bool VisualizerEnabled = false;

    /// <summary>波动大小倍数。</summary>
    public float VisualizerAmplitude = 1f;

    /// <summary>波动最大占屏幕对应方向的百分比（5-50）。</summary>
    public float VisualizerSize = 50f;

    /// <summary>bottom / top / left / right</summary>
    public string VisualizerPosition = "bottom";

    /// <summary>柱状条数量（频谱密度）。</summary>
    public int VisualizerBars = 48;

    /// <summary>渐变范围（0=整排同色，1=整排铺满色相段）；色相随时间流动，不随音量变化。</summary>
    public float VisualizerGradientRange = 1f;

    /// <summary>fixed2（固定双色）/ range（x~y 动态）/ multi（多 RGB 色标）。</summary>
    public string VisualizerGradientMode = "range";

    /// <summary>起点颜色，RGB 文本，例如 "216,165,35"。</summary>
    public string VisualizerColorA = "216,165,35";

    /// <summary>终点颜色，RGB 文本，例如 "0,229,255"。</summary>
    public string VisualizerColorB = "0,229,255";

    /// <summary>多色渐变色标，空格分隔多个 RGB，例如 "255,0,0 0,255,0 0,0,255"。</summary>
    public string VisualizerColors = "255,0,0 255,165,0 255,255,0 0,255,0 0,140,255 160,0,255";

    /// <summary>X 轴镜像（左右对称）。</summary>
    public bool VisualizerMirrorX = false;

    // ---- 本 mod 扩展项 ----
    public bool Enabled = true;
    public bool ReplaceOriginal = true;
    public bool RandomPosition = true;
    public bool RandomAngle = true;

    // ---- 双语歌词（原歌词 + 翻译 sidecar） ----
    public bool TranslationEnabled = true;
    public float TranslationScale = 0.75f;

    /// <summary>绝对字号；0 表示沿用 FontSize * TranslationScale。</summary>
    public float TranslationFontSize = 0f;

    public string TranslationColor = "#d8d8d8";

    /// <summary>"en" (default) or "zh".</summary>
    public string Language = "en";

    // ---- 运行时状态 ----
    public string FilePath;
    public DateTime LastWriteUtc;

    public static LimbusCompanyLyricsSettings Load(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "lyric_config.json");

        if (!File.Exists(path))
        {
            const string externalConfig = @"E:\084music\lyric_config.json";
            try
            {
                if (File.Exists(externalConfig))
                    File.Copy(externalConfig, path);
                else
                    File.WriteAllText(path, ReadDefaultJson(), new UTF8Encoding(false));
            }
            catch (Exception)
            {
                File.WriteAllText(path, ReadDefaultJson(), new UTF8Encoding(false));
            }
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        var cfg = new LimbusCompanyLyricsSettings
        {
            FilePath = path,
            LastWriteUtc = File.GetLastWriteTimeUtc(path)
        };

        JToken settingsToken;
        try
        {
            var root = JObject.Parse(json);
            settingsToken = root["settings"] ?? root;
        }
        catch (Exception)
        {
            return cfg;
        }

        cfg.Apply(settingsToken);
        return cfg;
    }

    private void Apply(JToken s)
    {
        TextColor = GetString(s, "text_color", TextColor);
        StrokeColor = GetString(s, "stroke_color", StrokeColor);
        GlowColor = GetString(s, "glow_color", GlowColor);
        GlowEnabled = GetBool(s, "glow_enabled", GlowEnabled);
        GlowSize = GetFloat(s, "glow_size", GlowSize);
        GlowAlpha = GetFloat(s, "glow_alpha", GlowAlpha);

        FontFamily = GetString(s, "font_family", FontFamily);
        FontSize = Mathf.Max(1f, GetFloat(s, "font_size", FontSize));
        StrokeWidth = Mathf.Max(0f, GetFloat(s, "stroke_width", StrokeWidth));
        Spacing = GetFloat(s, "spacing", Spacing);

        ShakeIntensity = Mathf.Max(0f, GetFloat(s, "shake_intensity", ShakeIntensity));
        ShakeSpeed = Mathf.Max(1f, GetFloat(s, "shake_speed", ShakeSpeed));
        FadeSpeed = Mathf.Max(0.1f, GetFloat(s, "fade_speed", FadeSpeed));
        RiseSpeed = GetFloat(s, "rise_speed", RiseSpeed);
        TypewriterEnabled = GetBool(s, "typewriter_enabled", TypewriterEnabled);
        TypewriterSpeed = Mathf.Clamp(GetFloat(s, "typewriter_speed", TypewriterSpeed), 1f, 200f);

        AngleMin = GetFloat(s, "angle_min", AngleMin);
        AngleMax = GetFloat(s, "angle_max", AngleMax);
        Delay = (int)Mathf.Max(0f, GetFloat(s, "delay", Delay));

        PerspectiveEnabled = GetBool(s, "perspective_enabled", PerspectiveEnabled);
        PerspXStrength = GetFloat(s, "persp_x_strength", PerspXStrength);
        PerspYStrength = GetFloat(s, "persp_y_strength", PerspYStrength);

        PosXMin = GetFloat(s, "pos_x_min", PosXMin);
        PosXMax = GetFloat(s, "pos_x_max", PosXMax);
        PosYMin = GetFloat(s, "pos_y_min", PosYMin);
        PosYMax = GetFloat(s, "pos_y_max", PosYMax);
        PositionAnchor = NormalizeAnchor(GetString(s, "position_anchor", PositionAnchor));
        Zoom = Mathf.Clamp(GetFloat(s, "zoom", Zoom), 10f, 400f);
        AntiOverlapMode = NormalizeMode(GetString(s, "anti_overlap_mode", AntiOverlapMode));
        AvoidWindow = Mathf.Clamp(GetFloat(s, "avoid_window", AvoidWindow), 0f, 30f);
        AvoidGap = Mathf.Clamp(GetFloat(s, "avoid_gap", AvoidGap), 0f, 300f);
        AvoidStep = Mathf.Clamp(GetFloat(s, "avoid_step", AvoidStep), 2f, 50f);
        CustomOffsetX = Mathf.Clamp(GetFloat(s, "custom_offset_x", CustomOffsetX), -2000f, 2000f);
        CustomOffsetY = Mathf.Clamp(GetFloat(s, "custom_offset_y", CustomOffsetY), -2000f, 2000f);
        VisualizerEnabled = GetBool(s, "visualizer_enabled", VisualizerEnabled);
        VisualizerAmplitude = Mathf.Clamp(GetFloat(s, "visualizer_amplitude", VisualizerAmplitude), 0.1f, 5f);
        VisualizerSize = Mathf.Clamp(GetFloat(s, "visualizer_size", VisualizerSize), 5f, 50f);
        VisualizerPosition = NormalizePosition(GetString(s, "visualizer_position", VisualizerPosition));
        VisualizerBars = Mathf.Clamp((int)GetFloat(s, "visualizer_bars", VisualizerBars), 8, 128);
        var legacyGradient = GetBool(s, "visualizer_gradient", true);
        VisualizerGradientRange = Mathf.Clamp(
            GetFloat(s, "visualizer_gradient_range", legacyGradient ? VisualizerGradientRange : 0f), 0f, 1f);
        VisualizerGradientMode = NormalizeGradientMode(GetString(s, "visualizer_gradient_mode", VisualizerGradientMode));
        VisualizerColorA = GetString(s, "visualizer_color_a", VisualizerColorA);
        VisualizerColorB = GetString(s, "visualizer_color_b", VisualizerColorB);
        VisualizerColors = GetString(s, "visualizer_colors", VisualizerColors);

        // 兼容旧版十六进制颜色字段。
        if (s["visualizer_color_a"] == null)
            VisualizerColorA = LegacyHexToRgb(GetString(s, "visualizer_color_from", null), VisualizerColorA);
        if (s["visualizer_color_b"] == null)
            VisualizerColorB = LegacyHexToRgb(GetString(s, "visualizer_color_to", null), VisualizerColorB);
        VisualizerMirrorX = GetBool(s, "visualizer_mirror_x", VisualizerMirrorX);

        Enabled = GetBool(s, "enabled", Enabled);
        ReplaceOriginal = GetBool(s, "replace_original", ReplaceOriginal);
        RandomPosition = GetBool(s, "random_position", RandomPosition);
        RandomAngle = GetBool(s, "random_angle", RandomAngle);
        TranslationEnabled = GetBool(s, "translation_enabled", TranslationEnabled);
        TranslationScale = Mathf.Clamp(GetFloat(s, "translation_scale", TranslationScale), 0.1f, 3f);
        TranslationFontSize = Mathf.Clamp(GetFloat(s, "translation_font_size", TranslationFontSize), 0f, 400f);
        TranslationColor = GetString(s, "translation_color", TranslationColor);
        Language = NormalizeLanguage(GetString(s, "language", Language));
    }

    public static string NormalizeGradientMode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "range";

        switch (value.Trim().ToLowerInvariant())
        {
            case "fixed2":
            case "fixed":
            case "two":
                return "fixed2";
            case "multi":
            case "multiple":
                return "multi";
            default:
                return "range";
        }
    }

    private static string LegacyHexToRgb(string hex, string fallback)
    {
        if (string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out var c))
            return fallback;

        return $"{Mathf.RoundToInt(c.r * 255f)},{Mathf.RoundToInt(c.g * 255f)},{Mathf.RoundToInt(c.b * 255f)}";
    }

    public static string NormalizeAnchor(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "random";

        switch (value.Trim().ToLowerInvariant())
        {
            case "top-left":
            case "top":
            case "top-right":
            case "left":
            case "center":
            case "right":
            case "bottom-left":
            case "bottom":
            case "bottom-right":
                return value.Trim().ToLowerInvariant();
            case "top-center":
            case "top_center":
                return "top";
            case "bottom-center":
            case "bottom_center":
                return "bottom";
            case "middle-left":
                return "left";
            case "middle":
                return "center";
            case "middle-right":
                return "right";
            default:
                return "random";
        }
    }

    public static string NormalizePosition(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "bottom";

        switch (value.Trim().ToLowerInvariant())
        {
            case "top":
            case "left":
            case "right":
                return value.Trim().ToLowerInvariant();
            default:
                return "bottom";
        }
    }

    public static string NormalizeMode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "strict";

        switch (value.Trim().ToLowerInvariant())
        {
            case "strict":
            case "mirror":
            case "below":
            case "custom":
                return value.Trim().ToLowerInvariant();
            default:
                return "auto";
        }
    }

    public static string NormalizeLanguage(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "en";

        value = value.Trim().ToLowerInvariant();
        return value.StartsWith("zh") || value == "chinese" || value == "中文" ? "zh" : "en";
    }

    public bool HasFileChanged()
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            return false;

        var stamp = File.GetLastWriteTimeUtc(FilePath);
        if (stamp == LastWriteUtc)
            return false;

        LastWriteUtc = stamp;
        return true;
    }

    private static string ReadDefaultJson()
    {
        var asm = typeof(LimbusCompanyLyricsSettings).Assembly;
        using var stream = asm.GetManifestResourceStream("Jukebox.limbuscompany.default.json");
        if (stream == null)
            return "{ \"settings\": { } }";

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static float GetFloat(JToken s, string key, float fallback)
    {
        var t = s[key];
        if (t == null || t.Type == JTokenType.Null)
            return fallback;

        try
        {
            return t.Value<float>();
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static bool GetBool(JToken s, string key, bool fallback)
    {
        var t = s[key];
        if (t == null || t.Type == JTokenType.Null)
            return fallback;

        try
        {
            return t.Value<bool>();
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static string GetString(JToken s, string key, string fallback)
    {
        var t = s[key];
        if (t == null || t.Type == JTokenType.Null)
            return fallback;

        var value = t.Value<string>();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}

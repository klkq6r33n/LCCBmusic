using System;
using Jukebox.UI.Subtitles;
using JukeboxCore.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Jukebox.LimbusCompany;

/// <summary>
/// Shared font resolution for the lyric layer and the in-menu language button.
/// </summary>
public static class LimbusCompanyFonts
{
    private static TMP_FontAsset _cached;
    private static TMP_FontAsset _latin;

    /// <summary>
    /// 英文歌词使用的字体：优先 Jukebox 字幕面板的字体（即游戏 UI/官方字库），
    /// 其次在已加载的 TMP 字体资源中找 ULTRAKILL/VCR/OSD 字样。
    /// </summary>
    public static TMP_FontAsset ResolveLatin()
    {
        if (_latin != null)
            return _latin;

        TMP_FontAsset any = null;
        TMP_FontAsset matched = null;

        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font == null)
                continue;

            any ??= font;

            var name = font.name ?? string.Empty;
            if (name.IndexOf("vcr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("ultrakill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("osd", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matched = font;
                break;
            }
        }

        if (matched != null)
        {
            _latin = matched;
            return _latin;
        }

        // 关卡内 Jukebox 菜单未加载时，主动从 Addressables 取 ULTRAKILL 官方字库。
        try
        {
            var ulkFont = Addressables
                .LoadAssetAsync<TMP_FontAsset>("Assets/Jukebox/Fonts/VCR_OSD_MONO.asset")
                .WaitForCompletion();
            if (ulkFont != null)
            {
                _latin = ulkFont;
                LimbusCompanyLyrics.Log?.LogInfo("LimbusCompany 英文字体: VCR_OSD_MONO (ULTRAKILL)");
                return _latin;
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("LimbusCompany 加载 ULTRAKILL 字体失败: " + e.Message);
        }

        var panel = UnityEngine.Object.FindObjectOfType<SubtitlesPanel>();
        if (panel != null && panel.text != null && panel.text.font != null)
        {
            _latin = panel.text.font;
            return _latin;
        }

        _latin = any ?? TMP_Settings.defaultFontAsset;
        return _latin;
    }

    /// <summary>菜单字体：英文使用 ULTRAKILL 官方字体，含中日韩字符时使用 CJK 字体。</summary>
    public static TMP_FontAsset MenuFont(string text)
    {
        return IsCjkText(text) ? Resolve() : ResolveLatin();
    }

    /// <summary>是否包含中日韩字符（含假名）。</summary>
    public static bool IsCjkText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var c in text)
        {
            if ((c >= 0x2E80 && c <= 0x9FFF) ||
                (c >= 0xAC00 && c <= 0xD7AF) ||
                (c >= 0xF900 && c <= 0xFAFF) ||
                (c >= 0xFF00 && c <= 0xFFEF))
                return true;
        }

        return false;
    }

    public static TMP_FontAsset Resolve()
    {
        if (_cached != null)
            return _cached;

        var panel = UnityEngine.Object.FindObjectOfType<SubtitlesPanel>();
        if (panel != null && panel.text != null && panel.text.font != null && HasAsianGlyphs(panel.text.font))
            _cached = panel.text.font;

        if (_cached == null)
            _cached = CreateSystemFont();

        if (_cached == null)
            _cached = TMP_Settings.defaultFontAsset;

        return _cached;
    }

    private static TMP_FontAsset CreateSystemFont()
    {
        var s = LimbusCompanyLyrics.Settings;
        string[] installed;
        try
        {
            installed = Font.GetOSInstalledFontNames();
        }
        catch (Exception)
        {
            installed = Array.Empty<string>();
        }

        var preferred = new[]
        {
            s != null ? s.FontFamily : null,
            "Yu Gothic UI",
            "Yu Gothic",
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "Noto Sans JP",
            "Noto Sans SC",
            "Noto Sans CJK JP",
            "Noto Sans CJK SC",
            "等线 Light",
            "等线",
            "DengXian",
            "MS Gothic",
            "Meiryo",
            "Arial Unicode MS",
            "Segoe UI"
        };

        TMP_FontAsset fallback = null;

        foreach (var name in preferred)
        {
            if (string.IsNullOrEmpty(name) || !ContainsName(installed, name))
                continue;

            try
            {
                var osFont = Font.CreateDynamicFontFromOSFont(name, 96);
                if (osFont == null)
                    continue;

                var asset = TMP_FontAsset.CreateFontAsset(osFont);
                if (asset == null)
                    continue;

                asset.name = "JukeboxLimbusCompany-" + name;

                if (HasAsianGlyphs(asset))
                {
                    LimbusCompanyLyrics.Log.LogInfo("LimbusCompany 字体: " + name);
                    return asset;
                }

                fallback ??= asset;
            }
            catch (Exception e)
            {
                LimbusCompanyLyrics.Log.LogWarning($"LimbusCompany 加载系统字体失败 ({name}): {e.Message}");
            }
        }

        return fallback;
    }

    private static bool ContainsName(string[] installed, string name)
    {
        foreach (var item in installed)
        {
            if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasAsianGlyphs(TMP_FontAsset font)
    {
        try
        {
            return font.HasCharacter('あ', false, false) && font.HasCharacter('中', false, false);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

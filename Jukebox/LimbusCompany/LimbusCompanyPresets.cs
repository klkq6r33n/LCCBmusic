using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 玩家自建预设：把当前歌词/频谱/NP 布局保存到 lyric_config.json 的 user_presets，
/// 之后可在 F4 设置面板或 F5 菜单中一键应用。
/// </summary>
public static class LimbusCompanyPresets
{
    public const string RootKey = "user_presets";

    private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
    {
        nameof(LimbusCompanyLyricsSettings.FilePath),
        nameof(LimbusCompanyLyricsSettings.LastWriteUtc),
        nameof(LimbusCompanyLyricsSettings.Language),
        nameof(LimbusCompanyLyricsSettings.BgmTrack),
        nameof(LimbusCompanyLyricsSettings.BgmKeyToggle),
        nameof(LimbusCompanyLyricsSettings.BgmKeyPrev),
        nameof(LimbusCompanyLyricsSettings.BgmKeyNext),
        nameof(LimbusCompanyLyricsSettings.BgmKeyReplace),
        nameof(LimbusCompanyLyricsSettings.BgmKeyRestore),
        nameof(LimbusCompanyLyricsSettings.BgmKeyLyrics),
        nameof(LimbusCompanyLyricsSettings.BgmKeyNowPlaying),
        nameof(LimbusCompanyLyricsSettings.BgmKeyLanguage),
        nameof(LimbusCompanyLyricsSettings.Enabled),
        nameof(LimbusCompanyLyricsSettings.ReplaceOriginal)
    };

    public static readonly List<string> Names = new();

    public static void Refresh()
    {
        Names.Clear();

        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || string.IsNullOrEmpty(settings.FilePath) || !File.Exists(settings.FilePath))
            return;

        try
        {
            var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
            if (root[RootKey] is not JObject presets)
                return;

            foreach (var property in presets.Properties())
                Names.Add(property.Name);
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取预设失败: " + e.Message);
        }
    }

    public static bool Save(string name)
    {
        name = SanitizeName(name);
        if (string.IsNullOrEmpty(name))
            return false;

        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || string.IsNullOrEmpty(settings.FilePath) || !File.Exists(settings.FilePath))
            return false;

        try
        {
            var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
            if (root[RootKey] is not JObject presets)
            {
                presets = new JObject();
                root[RootKey] = presets;
            }

            presets[name] = Capture(settings);
            File.WriteAllText(settings.FilePath, root.ToString(Formatting.Indented), new UTF8Encoding(false));
            Refresh();
            LimbusCompanyLyrics.Log?.LogInfo("已保存预设: " + name);
            return true;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("保存预设失败: " + e.Message);
            return false;
        }
    }

    public static bool Apply(string name)
    {
        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || string.IsNullOrEmpty(settings.FilePath) || !File.Exists(settings.FilePath))
            return false;

        try
        {
            var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
            if (root[RootKey] is not JObject presets || presets[name] is not JObject preset)
                return false;

            ApplyFields(settings, preset);
            LimbusCompanyLyrics.SaveAllSettings();
            LimbusCompanyLyrics.Log?.LogInfo("已应用预设: " + name);
            return true;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("应用预设失败: " + e.Message);
            return false;
        }
    }

    public static bool Delete(string name)
    {
        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || string.IsNullOrEmpty(settings.FilePath) || !File.Exists(settings.FilePath))
            return false;

        try
        {
            var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
            if (root[RootKey] is not JObject presets || presets[name] == null)
                return false;

            presets.Remove(name);
            File.WriteAllText(settings.FilePath, root.ToString(Formatting.Indented), new UTF8Encoding(false));
            Refresh();
            return true;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("删除预设失败: " + e.Message);
            return false;
        }
    }

    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        name = name.Trim();
        return name.Length <= 20 ? name : name.Substring(0, 20);
    }

    private static JObject Capture(LimbusCompanyLyricsSettings settings)
    {
        var result = new JObject();
        foreach (var field in typeof(LimbusCompanyLyricsSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Excluded.Contains(field.Name))
                continue;

            var value = field.GetValue(settings);
            switch (value)
            {
                case string text:
                    result[field.Name] = text;
                    break;
                case bool flag:
                    result[field.Name] = flag;
                    break;
                case int number:
                    result[field.Name] = number;
                    break;
                case float number:
                    result[field.Name] = number;
                    break;
                case double number:
                    result[field.Name] = number;
                    break;
            }
        }

        return result;
    }

    private static void ApplyFields(LimbusCompanyLyricsSettings settings, JObject preset)
    {
        foreach (var field in typeof(LimbusCompanyLyricsSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Excluded.Contains(field.Name))
                continue;

            var token = preset[field.Name];
            if (token == null || token.Type == JTokenType.Null)
                continue;

            try
            {
                if (field.FieldType == typeof(string))
                    field.SetValue(settings, token.Value<string>());
                else if (field.FieldType == typeof(bool))
                    field.SetValue(settings, token.Value<bool>());
                else if (field.FieldType == typeof(int))
                    field.SetValue(settings, token.Value<int>());
                else if (field.FieldType == typeof(float))
                    field.SetValue(settings, token.Value<float>());
                else if (field.FieldType == typeof(double))
                    field.SetValue(settings, token.Value<double>());
            }
            catch (Exception)
            {
                // 忽略单个损坏字段，其余字段继续应用。
            }
        }
    }
}

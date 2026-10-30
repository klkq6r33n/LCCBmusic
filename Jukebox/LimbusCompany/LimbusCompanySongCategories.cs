using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JukeboxCore.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 玩家自定义歌曲分类：每个分类对应 Cybergrind/Music 下的一个文件夹。
/// 保存在 lyric_config.json 的 song_categories 数组里（ULTRAKILL-OST 始终存在）。
/// </summary>
public static class LimbusCompanySongCategories
{
    public const string RootKey = "song_categories";
    public const string OstName = "ULTRAKILL-OST";

    public static readonly List<string> Categories = new();

    public static void Refresh()
    {
        Categories.Clear();
        Categories.Add(OstName);

        var settings = LimbusCompanyLyrics.Settings;
        if (settings != null && !string.IsNullOrEmpty(settings.FilePath) && File.Exists(settings.FilePath))
        {
            try
            {
                var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
                if (root[RootKey] is JArray array)
                {
                    foreach (var token in array)
                    {
                        var name = SanitizeName(token.Value<string>());
                        if (string.IsNullOrEmpty(name) || Contains(name))
                            continue;

                        Categories.Add(name);
                    }
                }
            }
            catch (Exception e)
            {
                LimbusCompanyLyrics.Log?.LogWarning("读取歌曲分类失败: " + e.Message);
            }
        }

        foreach (var category in Categories)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(PathsUtils.MusicPath, category));
            }
            catch (Exception)
            {
                // 游戏目录不可写时忽略。
            }
        }
    }

    public static bool Add(string rawName)
    {
        var name = SanitizeName(rawName);
        if (string.IsNullOrEmpty(name) || Contains(name))
            return false;

        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || string.IsNullOrEmpty(settings.FilePath) || !File.Exists(settings.FilePath))
            return false;

        try
        {
            Directory.CreateDirectory(Path.Combine(PathsUtils.MusicPath, name));

            var root = JObject.Parse(File.ReadAllText(settings.FilePath, Encoding.UTF8));
            var array = root[RootKey] as JArray ?? new JArray();

            if (!ContainsValue(array, OstName))
                array.Insert(0, OstName);
            if (!ContainsValue(array, name))
                array.Add(name);

            root[RootKey] = array;
            File.WriteAllText(settings.FilePath, root.ToString(Formatting.Indented), new UTF8Encoding(false));
            Refresh();
            LimbusCompanyLyrics.Log?.LogInfo("已添加歌曲分类: " + name);
            return true;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("添加歌曲分类失败: " + e.Message);
            return false;
        }
    }

    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        name = name.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        name = name.Trim(' ', '.');
        if (name == "." || name == "..")
            return string.Empty;

        if (name.Equals("personal", StringComparison.OrdinalIgnoreCase) || name == "个人")
            return string.Empty;

        return name.Length <= 32 ? name : name.Substring(0, 32);
    }

    private static bool Contains(string name)
    {
        foreach (var category in Categories)
        {
            if (string.Equals(category, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool ContainsValue(JArray array, string name)
    {
        foreach (var token in array)
        {
            if (string.Equals(token.Value<string>(), name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

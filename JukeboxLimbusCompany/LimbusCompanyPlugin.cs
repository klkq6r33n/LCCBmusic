using BepInEx;
using Jukebox.LimbusCompany;
using UnityEngine;

namespace JukeboxLimbusCompany;

/// <summary>
/// 独立扩展 DLL：依赖 CybergrindMusicExplorer（Jukebox）本体，
/// 只负责挂载 LimbusCompany 歌词层与 F4/F5 界面，不替换游戏所需的 Jukebox 组件。
/// </summary>
[BepInProcess("ULTRAKILL.exe")]
[BepInPlugin(Guid, Name, Version)]
[BepInDependency("dev.flazhik.jukebox")]
[BepInDependency("greycsont.ultrakill.fffffff")]
public class LimbusCompanyPlugin : BaseUnityPlugin
{
    public const string Guid = "dev.6r3n.jukeboxlimbuscompany";
    public const string Name = "Jukebox LimbusCompany";
    public const string Version = LimbusCompanyLyrics.Version;

    private static GameObject _root;

    private void Awake()
    {
        LimbusCompanyLyrics.Initialize();

        // 插件对象会在 Start 之前被销毁，保持常驻。
        gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Start()
    {
        if (_root != null)
            return;

        _root = new GameObject("LimbusCompanyLyrics");
        DontDestroyOnLoad(_root);
        _root.hideFlags = HideFlags.HideAndDontSave;
        _root.AddComponent<LimbusCompanyLyricsOverlay>();
        _root.AddComponent<LimbusCompanyLocalization>();
    }
}

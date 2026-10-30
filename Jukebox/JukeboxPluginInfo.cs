namespace Jukebox;

public static class JukeboxPluginInfo
{
    // 独立 mod（单独文件夹/包），但沿用 Jukebox 的插件 GUID：
    //   - Addressables 资源包按程序集名 "Jukebox" 绑定脚本，程序集名不能改；
    //   - CGMEDownloader 与 Thorn 扩展都依赖 dev.flazhik.jukebox，沿用 GUID 才能继续工作。
    // 使用时必须在 r2modman 里禁用原 Flazhik-CybergrindMusicExplorer（同一 GUID 不能同时加载）。
    public const string Guid = "dev.flazhik.jukebox";
    public const string Name = "Jukebox LimbusCompany";
    public const string Version = "2.3.2";
}

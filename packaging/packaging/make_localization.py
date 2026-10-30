import json

SRC = r"D:\084json\Jukebox084\packaging\extracted_strings.json"
OUT = r"D:\084json\Jukebox084\Jukebox\LimbusCompany\localization.json"

strings = json.load(open(SRC, encoding="utf-8"))

T = {}

T[0] = "周围敌人不超过 4 个时播放平静主题。除非周围有超过 1 个 Insurrectionist 或超过 2 个 Mindflayer。"
T[4] = (
    "1. 打开包含 mod 可执行文件的文件夹。<color=#FAEDB8>点击此处打开</color>\n"
    "2. 从<color=#FAEDB8>此处</color>下载 <b>yt-dlp</b> 并移动到刚才打开的文件夹\n"
    "3. 从<color=#FAEDB8>此处</color>下载 <b>ffmpeg</b>，把压缩包里的 ffmpeg.exe 解压到刚才打开的文件夹\n"
    "4. 从<color=#FAEDB8>此处</color>下载 <b>ffprobe</b>，把压缩包里的 ffprobe.exe 解压到刚才打开的文件夹\n\n"
    "全部完成后按「重启」。按「关闭」关闭此窗口"
)
T[7] = "2.3.0 更新"
T[12] = (
    "<color=red>root@jukebox:/Credits#</color> cd ../SpecialThanks\n"
    "<color=red>root@jukebox:/SpecialThanks#</color> ./SpecialThanks.sh\n\n"
    "特别感谢 UntotenTheo、Net 与 ayladotcom"
)
T[13] = (
    "<size=24>自定义曲目</size>\n\n"
    "那么，自定义曲目呢？\n\n"
    "做法和前奏/循环一样：在文件名里加后缀。\n"
    "任何曲目默认都被视为「战斗主题」。要给它加平静变体，就再放一个同名曲目，加 <b><i>_calm</i></b> 后缀。\n\n"
    "<b>Music.mp3            - 该曲目的战斗主题\n"
    "Music_calm.mp3 - 该曲目的平静主题</b>\n\n"
    "那分段曲目呢？它们本来就有两个文件！怎么给它们的前奏和循环分别做平静变体？可以只给循环或只给前奏加平静主题吗？\n\n"
    "当然可以，随你定制。\n\n"
    "<b>Music_calmintro.mp3  - 前奏部分的平静变体\n"
    "Music_intro.mp3      - 前奏部分的战斗变体\n"
    "Music_calmloop.mp3   - 主体部分的平静变体\n"
    "Music.mp3 - 主体部分的战斗变体</b>\n\n"
    "而且，这些文件在终端里会被视为同一首曲目"
)
T[16] = "有新的 <color=#FAEDB8>yt-dlp</color> 版本可用，按「更新」从官方来源下载。此步骤是必需的，否则下载器可能无法正常工作。"
T[17] = "始终播放前奏部分"
T[19] = "确定要把所有歌曲从播放列表中移除吗？"
T[22] = "哟？"
T[23] = "返回"
T[24] = "平静与战斗主题"
T[25] = "取消"
T[26] = "经典面板"
T[27] = "关闭"
T[28] = "操作"
T[29] = "需要 COOKIES"
T[30] = "制作人员"
T[31] = "当前歌曲"
T[32] = "CYBERGRIND 音乐浏览器"
T[33] = "平静与战斗主题"
T[34] = "切换播放列表"
T[35] = "清空播放列表"
T[36] = "关闭"
T[38] = "包含平静主题"
T[39] = "包含前奏与循环部分"
T[40] = "自定义"
T[41] = "自定义曲目音量提升"
T[42] = "禁用播放器"
T[43] = "全部下载"
T[44] = "下载器"
T[45] = "下载器 /// 免责声明"
T[46] = "下载器 /// 正在更新 YT-DLP"
T[47] = "默认"
T[48] = "Discord 与 Steam 集成"
T[49] = "显示歌词字幕"
T[52] = "将播放列表下载到独立文件夹"
T[53] = "下载器"
T[54] = "平静主题触发的敌人数量阈值"
T[55] = "启用平静主题"
T[56] = "启用曲目试听"
T[61] = "通用"
T[62] = "帮助与制作人员"
T[63] = "HUD"
T[64] = "你好，我是 Flazhik，这些内容都是我做的"
T[65] = "安装"
T[66] = "前奏"
T[67] = "前奏与循环"
T[69] = "前奏与循环"
T[70] = "加入我的 Discord 服务器"
T[71] = "JUKEBOX 面板"
T[73] = "大"
T[74] = "载入全部曲目"
T[75] = "手动下载"
T[78] = "下一首"
T[79] = "无"
T[88] = "Nicy：QA、支持、创意，以及 <color=#6500c5>ourple</color> 的大使"
T[89] = "正在播放"
T[90] = (
    "现在你可以直接从 <color=red>YouTube</color> 下载歌曲，只需提供歌曲或播放列表的 URL。"
    "下载的歌曲会保存在 Cybergrind 终端的 <b>YouTube</b> 文件夹里，刷新页面后就能加入播放列表。\n\n"
    "<b>下载器依赖 ffmpeg、yt-dlp 等第三方软件，届时会提示你自动下载安装。你也可以拒绝，但那样此功能将不可用</b>\n\n"
    "流程应该足够直观，不过有几点要注意：\n\n"
    "1. YouTube 可能要求你登录\n\n"
    "取决于你下载歌曲时使用的网络，YouTube 可能拒绝你的请求，除非你已登录。"
    "mod 会提示你，并提供解决说明，或者你也可以 <color=#FAEDB8>点击这里</color> 查看说明。 \n\n"
    "2. 不管我怎么试，总有一堆曲目下载不下来，为什么？\n\n"
    "原因可能很多：年龄限制、敏感内容。见鬼，有些视频就是被永久删除了，你也拿不到。\n\n"
    "3. 好像一首都没法下载\n\n"
    "见第 1 条，多半是需要刷新你的 cookies"
)
T[91] = "打开播放菜单"
T[92] = "打开此菜单"
T[93] = "在文件资源管理器中打开文件夹"
T[94] = "周围此类敌人 ≤ N 时播放平静主题"
T[95] = "播放"
T[96] = "预处理中……"
T[97] = "弹反欢呼声"
T[99] = "播放"
T[100] = "播放列表"
T[101] = "播放列表编辑"
T[102] = "防止重复曲目"
T[103] = "镭射"
T[104] = "随机播放"
T[105] = "普通"
T[106] = "重启"
T[109] = "刷新"
T[110] = "从上次中断处继续播放"
T[111] = "保存中……"
T[112] = "小"
T[113] = "为每首曲目单独保存音量提升"
T[114] = "计分板音乐"
T[115] = "选择音乐类型"
T[116] = "选择文件"
T[117] = "选择播放列表"
T[118] = "在 HUD 中显示当前曲目"
T[119] = "常驻显示当前曲目面板"
T[120] = (
    "和原版 ULTRAKILL 配乐类似，你可以往播放列表里添加分段曲目，让它拥有前奏和循环部分。\n\n"
    "如果你有两个同名曲目，分别带 <b><i>_intro</i></b> 和 <b><i>_loop</i></b> 后缀，"
    "它们会被视为一首带前奏与循环的曲目。\n<b>\n"
    "Music_intro.mp3      - 前奏部分\n"
    "Music_loop.mp3 - 主循环部分\n"
    "</b>"
)
T[122] = "出错了"
T[123] = "音效"
T[124] = "原声带"
T[129] = "终端音乐"
T[130] = "主题"
T[131] = "本菜单让你在游戏中切换歌曲。进入 Cyber Grind 开始游戏后，歌曲列表才会出现。"
T[132] = (
    "本节允许你直接从 <color=red>YouTube</color> 下载曲目，但需要安装 "
    "<color=#FAEDB8>yt-dlp</color> 和 <color=#FAEDB8>ffmpeg</color> 才能正常工作。\n\n"
    "这些工具会从官方来源下载，并自动安装到 mod 可执行文件所在的文件夹。\n\n"
    "之后，下载器区域就会直接出现。"
)
T[133] = (
    "本次更新加入字幕支持、<color=#90E3D1>Thorn</color> 集成（3 个模块，见 <b>Thorn 菜单</b> > <b>HUD</b>），"
    "以及大量错误修复。"
)
T[136] = "上/下/左/右"
T[137] = "更新"
T[139] = "波次完成"
T[140] = "你死了"
T[141] = (
    "你知道原版 ULTRAKILL 战役里，在没有敌人时会播放一段轻柔的平静主题，"
    "战斗开始后切换成激烈的变奏吗？\n"
    "现在你可以在 Cyber Grind 里为原版 ULTRAKILL 原声启用这个机制，也可以自己制作曲目。\n\n"
    "不过和战役模式不同，你可以自己决定何时播放平静主题（否则会很蠢，因为 Cyber Grind 里几乎总有敌人）。\n"
    "打开 <b>Themes</b> 页，你会看到三个控件：\n\n"
    "1. <b>Enable calm themes</b>：不想要这个功能就关掉\n"
    "2. <b>Enemies threshold for calm theme to play</b>：除非当前存活敌人数小于等于该值，否则播放战斗主题\n"
    "3. <b>Play battle theme when there's <= N of these enemies around</b>："
    "敌人数量阈值是控制平静主题的可靠方式，但假如剩下的两个敌人是 Mindflayer 和 Insurrectionist 呢？"
    "这真的是切到平静主题的好时机吗？\n"
    "所以你可以单独调整每种敌人的阈值，或者直接禁用。\n\n"
    "<b>注意！</b> 普通敌人和它们的镭射版本差别很大，所以镭射敌人有各自独立的页签。"
)
T[143] = (
    "YouTube 报出 <i>Sign in to confirm you're not a bot</i> 错误\n\n"
    "这通常发生在 YouTube 检测到你的网络有高活动量时——使用 VPN 时很常见\n\n"
    "有解决办法，<b>但我非常建议使用一个全新的专用 Google 账号</b>\n\n"
    "1. 在浏览器中打开 YouTube，确保已登录（而且务必是新打开的 YouTube 标签页）\n"
    "2. 打开 cookie 导出工具，把 cookies 以 Netscape 格式导出为 <b>cookies.txt</b>，"
    "放到<color=#FAEDB8>此文件夹</color>（点击打开）\n"
    "3. 如果没装 cookie 导出工具，按浏览器选择：\n\n"
    "Chrome：<color=#FAEDB8>Get cookies.txt locally</color>\n"
    "Firefox：<color=#FAEDB8>Export Cookies</color>\n\n"
    "如果一切正确，下次输入视频/播放列表 URL 时下载器应该就能工作"
)
T[144] = "[ 空 ]"

EXTRA = {
    "RESET": "重置",
    "Now playing Hud": "正在播放 HUD",
    "Volume boost": "音量提升",
}

result = {}
for index, value in T.items():
    if 0 <= index < len(strings):
        result[strings[index]] = value
result.update(EXTRA)

json.dump(result, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
print("entries", len(result))

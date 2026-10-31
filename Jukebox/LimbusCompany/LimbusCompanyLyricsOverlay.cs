using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Jukebox.Components;
using Jukebox.UI.Subtitles;
using JukeboxCore.Logging;
using JukeboxCore.Songs.Models;
using JukeboxCore.Subtitles;
using JukeboxCore.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 独立歌词层：订阅 JukeboxMusicPlayer.OnSubtitleChanged，用 TMP 按 LimbusCompany 演出风格渲染。
/// </summary>
public class LimbusCompanyLyricsOverlay : MonoBehaviour
{
    private Canvas _canvas;
    private GameObject _visRoot;
    private readonly Image[] _visBars = new Image[256];
    private readonly float[] _spectrum = new float[512];
    private CanvasGroup _group;
    private RectTransform _rect;
    private TextMeshProUGUI _text;
    private TMP_FontAsset _font;
    private SubtitlesPanel[] _panels = Array.Empty<SubtitlesPanel>();
    private float _nextPanelScan;

    private JukeboxMusicPlayer _player;
    private float _nextPlayerScan;

    private string _pendingText;
    private float _pendingAt;
    private string _shownText;

    private float _alpha;
    private float _targetAlpha;
    private float _elapsed;
    private float _lineAngle;
    private Vector2 _basePosition;

    private Vector2 _halfExtents;
    private Vector2 _lastBasePosition;
    private Vector2 _lastHalfExtents;
    private bool _hasLastPosition;
    private TMP_Text[] _allTexts = Array.Empty<TMP_Text>();
    private float _nextTextScan;
    private string _lastOtherText;
    private float _nextPanelLog;
    private int _lastPanelCount = -1;

    private Vector2 _mainCurrentPosition;

    private float _typeElapsed;
    private bool _typing;

    private sealed class RecentPlacement
    {
        public Vector2 Position;
        public Vector2 Half;
        public float Angle;
        public float Time;
    }

    private readonly List<RecentPlacement> _recent = new();

    /// <summary>上一句歌词的“残影”：新句出现时旧句仍在它自己的位置独立渐隐。</summary>
    private sealed class GhostLine
    {
        public GameObject Go;
        public RectTransform Rect;
        public TextMeshProUGUI Text;
        public CanvasGroup Group;
        public float Alpha;
        public Vector2 Half;
        public float Angle;
        public float Elapsed;
        public Vector2 BasePosition;
    }

    private readonly List<GhostLine> _ghosts = new();
    private const int MaxGhosts = 6;

    private struct AvoidBox
    {
        public Vector2 Position;
        public Vector2 Half;
        public float Angle;
    }

    // ---- 翻译歌词（第二行，独立位置） ----
    private CanvasGroup _transGroup;
    private RectTransform _transRect;
    private TextMeshProUGUI _transText;
    private float _transAlpha;
    private float _transTargetAlpha;
    private string _transShown;
    private Vector2 _transBasePosition;
    private Vector2 _transHalfExtents;
    private float _transAngle;
    private SubtitleTrack _translationTrack;
    private int _translationIndex = -1;
    private JukeboxSong _currentSong;
    private float _transTypeElapsed;
    private bool _transTyping;
    private float _transRelativeY;

    private float _nextConfigCheck;
    private float _nextOverlapCheck;

    // ---- 关卡 BGM 取代 ----
    public static LimbusCompanyLyricsOverlay Instance { get; private set; }

    private AudioSource _bgmSource;
    private Coroutine _bgmLoadRoutine;
    private readonly List<AudioSource> _pausedThemes = new();

    private struct ThemeState
    {
        public bool Mute;
        public float Volume;
    }

    private readonly Dictionary<AudioSource, ThemeState> _themeMuteState = new();
    private AudioSource[] _musicThemesCache = Array.Empty<AudioSource>();
    private float _nextMusicThemeScan;
    private readonly List<AudioSource> _sceneThemeSources = new();
    private float _nextSceneThemeScan;
    private float _nextManagerFind;

    private object _musicManagerInstance;
    private FieldInfo _musicManagerOffField;
    private bool _musicManagerOffSaved;
    private bool _musicManagerOffOriginal;
    public bool BgmReplacing { get; private set; }
    public string BgmStatus { get; private set; } = "";

    private GameObject _bgmPanel;
    private RectTransform _bgmPanelRect;
    private GameObject _bgmUiRoot;
    private RectTransform _bgmContent;
    private TextMeshProUGUI _bgmStatus;
    private int _presetPage;
    private TextMeshProUGUI _presetStatusText;
    private TMP_InputField _presetNameInput;
    private const int PresetsPerPage = 8;
    private Slider _npSizeSlider;
    private TextMeshProUGUI _npSizeValueText;
    private Slider _bgmSeekSlider;
    private TextMeshProUGUI _bgmTimeText;
    private TextMeshProUGUI _bgmPauseLabel;
    private string _bgmLastTimeText;
    private string _bgmLastPauseText;
    private Image _menuPreviewCoverImage;
    private Image _menuPreviewFrameImage;
    private TextMeshProUGUI _menuPreviewTitleText;
    private TextMeshProUGUI _menuPreviewArtistText;
    private Sprite _menuPreviewSprite;
    private Texture2D _menuPreviewTexture;
    private string _menuPreviewPath;
    private string _menuPreviewTitle;
    private string _menuPreviewArtist;
    private string _bgmLastStatusText;
    private bool _bgmPanelOpen;
    private string[] _bgmAllTracks = Array.Empty<string>();
    private string[] _bgmTracks = Array.Empty<string>();
    private int _bgmIndex;
    private string _bgmSelectedCategory; // null/空 = 个人
    private int _categoryPage;
    private bool _addingCategory;
    private bool _confirmDeleteCategory;
    private string _categoryPendingDelete;
    private TMP_InputField _categoryNameInput;
    private TextMeshProUGUI _categoryStatusText;
    private const int CategoriesPerPage = 3;
    private const string OstFolderName = LimbusCompanySongCategories.OstName;
    private int _bgmRebindStage; // 0 none, 1 choose action, 2 wait key
    private int _bgmRebindAction;
    private int _bgmPage; // 0 music, 1 settings, 2 display
    private int _bgmListOffset;
    private LimbusCompanyLocalization _settingsUi;
    private bool _bgmPaused;
    private const int TracksPerPage = 8;
    private const string BgmPauseStateKey = "lccb_f5_menu";

    private GameObject _npRoot;
    private Image _npRowImage;
    private Image _npFrameImage;
    private Image _npCoverImage;
    private TextMeshProUGUI _npTitle;
    private TextMeshProUGUI _npArtist;
    private Sprite _npSprite;
    private Texture2D _npSpriteTexture;
    private bool _npSpriteOwned;
    private string _npSpritePath;
    private string _bgmCurrentPath;
    private SubtitleTrack _bgmSubtitleTrack = SubtitleTrack.Empty;
    private SubtitleTrack _bgmTranslationTrack = SubtitleTrack.Empty;
    private int _bgmSubtitleIndex = -1;
    private int _bgmTranslationIndex = -1;
    private string _bgmSubtitleText;
    private string _bgmTranslationText;
    private string _npLastTitleText;
    private string _npLastArtistText;
    private float _npLastFontSize = -1f;
    private int _npAdjustTarget; // 0 none, 1 row position/size
    private bool _npMouseAdjust;
    private GameObject _npAdjustHint;

    private static readonly Key[] DigitKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3,
        Key.Digit4, Key.Digit5, Key.Digit6,
        Key.Digit7, Key.Digit8
    };

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LimbusCompanyLyrics.SettingsChanged += OnSettingsChanged;
        SceneManager.sceneLoaded += OnSceneLoadedForBgm;
    }

    private void OnDisable()
    {
        LimbusCompanyLyrics.SettingsChanged -= OnSettingsChanged;
        SceneManager.sceneLoaded -= OnSceneLoadedForBgm;
        CloseBgmPanel();
    }

    private void OnDestroy()
    {
        ResumeFromBgmMenu();
        _settingsUi?.CloseStandalone();

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoadedForBgm(Scene scene, LoadSceneMode mode)
    {
        // 切换场景时关闭 F5 菜单并恢复游戏，避免暂停状态残留。
        if (_bgmPanelOpen)
            CloseBgmPanel();

        // 兜底：场景切换后无论菜单状态如何都解除玩家输入锁，
        // 防止上一场景遗留的 F5 暂停状态让键盘失效。
        ResumeFromBgmMenu(true);

        // 切关卡时结束取代，避免继续静音新关卡的 BGM。
        if (BgmReplacing)
            StopBgmReplacement();
    }

    private void OnSettingsChanged()
    {
        ApplyStyle();
    }

    private void LateUpdate()
    {
        // 游戏可能在 Update/LateUpdate 里重新锁鼠标；这里在最后强行解除并显示。
        if (_bgmPanelOpen)
        {
            ForceCursorForMenu();

            // 面板若因场景切换/异常被隐藏，仍然走正常关闭流程。
            if (_bgmPanel == null || !_bgmPanel.activeSelf)
                CloseBgmPanel();
        }
        else
        {
            // 看门狗：菜单已不在前台却仍有本 mod 的暂停状态时强制恢复，避免键盘失效。
            var stateActive = false;
            try
            {
                stateActive = GameStateManager.Instance != null &&
                              GameStateManager.Instance.IsStateActive(BgmPauseStateKey);
            }
            catch (Exception)
            {
                // 忽略检测失败。
            }

            if (_bgmPaused || stateActive)
            {
                LimbusCompanyLyrics.Log?.LogWarning("检测到 F5 暂停状态残留，强制恢复游戏输入。");
                ResumeFromBgmMenu(true);
            }
        }
    }

    private static void ForceCursorForMenu()
    {
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;
    }

    private Coroutine _cursorRoutine;

    private IEnumerator ForceCursorLoop()
    {
        while (_bgmPanelOpen)
        {
            ForceCursorForMenu();
            yield return new WaitForEndOfFrame();
        }

        _cursorRoutine = null;
    }

    private void Update()
    {
        try
        {
            HandleBgmPanelKeys();
            RefreshBgmPanelTextIfOpen();
            PollPlayer();
            PollConfig();
            PollPanelSuppression();
            UpdateBgmLyrics();
            Animate();
            PollTranslation();
            if (BgmReplacing)
                KeepGameMusicPaused();
            UpdateVisualizer();
            UpdateNowPlayingPanel();
            if (_bgmPanelOpen)
                UpdateBgmMenuScale();
            if (_bgmPanelOpen && _bgmPage == 0)
                UpdateBgmPlaybackUi();
            if (_bgmPanelOpen && _bgmPage == 2)
                UpdateNpSizeUi();
            CheckOverlapDiagnostics();

            if (_bgmPanelOpen)
                ForceCursorForMenu();
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log.LogError($"LimbusCompanyLyrics 歌词层异常: {e}");
            enabled = false;
        }
    }

    // ------------------------------------------------------------------ setup

    private void BuildUi()
    {
        if (_canvas != null)
            return;

        var canvasGo = new GameObject("LimbusCompanyLyricsCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // F5 鼠标菜单需要射线检测；EventSystem 通常由游戏自带。
        canvasGo.AddComponent<GraphicRaycaster>();

        // 波形图放进独立画布：排序值低于游戏原版 UI，避免盖住原版界面。
        var visCanvasGo = new GameObject("LimbusCompanyVisualizerCanvas", typeof(RectTransform));
        visCanvasGo.transform.SetParent(transform, false);
        var visCanvas = visCanvasGo.AddComponent<Canvas>();
        visCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        visCanvas.sortingOrder = -1000;
        var visScaler = visCanvasGo.AddComponent<CanvasScaler>();
        visScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        visScaler.referenceResolution = new Vector2(1920f, 1080f);
        visScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        visScaler.matchWidthOrHeight = 0.5f;

        BuildVisualizer(visCanvasGo.transform);

        var textGo = new GameObject("LyricText", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        _rect = textGo.GetComponent<RectTransform>();
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.sizeDelta = new Vector2(1800f, 600f);

        _group = textGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.raycastTarget = false;
        _text.richText = false;
        _text.enableWordWrapping = false;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.alignment = TextAlignmentOptions.Center;
        _text.enableAutoSizing = false;
        _text.text = string.Empty;

        var transGo = new GameObject("TranslationText", typeof(RectTransform));
        transGo.transform.SetParent(canvasGo.transform, false);
        _transRect = transGo.GetComponent<RectTransform>();
        _transRect.anchorMin = new Vector2(0.5f, 0.5f);
        _transRect.anchorMax = new Vector2(0.5f, 0.5f);
        _transRect.pivot = new Vector2(0.5f, 0.5f);
        _transRect.anchoredPosition = Vector2.zero;
        _transRect.sizeDelta = new Vector2(1800f, 600f);

        _transGroup = transGo.AddComponent<CanvasGroup>();
        _transGroup.alpha = 0f;
        _transGroup.interactable = false;
        _transGroup.blocksRaycasts = false;

        _transText = transGo.AddComponent<TextMeshProUGUI>();
        _transText.raycastTarget = false;
        _transText.richText = false;
        _transText.enableWordWrapping = false;
        _transText.overflowMode = TextOverflowModes.Overflow;
        _transText.alignment = TextAlignmentOptions.Center;
        _transText.enableAutoSizing = false;
        _transText.text = string.Empty;

        EnsureFont();
        ApplyStyle();
        BuildBgmPanel();
        BuildNowPlayingPanel();
    }

    private void EnsureFont()
    {
        if (_text == null || _font != null)
            return;

        var panel = FindObjectOfType<SubtitlesPanel>();
        if (panel != null && panel.text != null && panel.text.font != null)
        {
            var panelFont = panel.text.font;
            if (HasAsianGlyphs(panelFont))
                _font = panelFont;
        }

        if (_font == null)
            _font = CreateSystemFont();

        if (_font == null)
            _font = TMP_Settings.defaultFontAsset;

        if (_font == null)
        {
            LimbusCompanyLyrics.Log.LogError("LimbusCompanyLyrics 找不到可用字体，歌词层无法显示。");
            return;
        }

        var latin = LimbusCompanyFonts.ResolveLatin();
        _text.font = latin ?? _font;
        if (_transText != null)
            _transText.font = latin ?? _font;
    }

    private static void ApplyFontFor(TMP_Text text, string content)
    {
        if (text == null)
            return;

        var font = LimbusCompanyFonts.IsCjkText(content)
            ? LimbusCompanyFonts.Resolve()
            : LimbusCompanyFonts.ResolveLatin();

        if (font != null && text.font != font)
            text.font = font;
    }

    private TMP_FontAsset CreateSystemFont()
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

                asset.name = "LimbusCompany-" + name;

                if (HasAsianGlyphs(asset))
                {
                    LimbusCompanyLyrics.Log.LogInfo("LimbusCompanyLyrics 歌词字体: " + name);
                    return asset;
                }

                fallback ??= asset;
            }
            catch (Exception e)
            {
                LimbusCompanyLyrics.Log.LogWarning($"LimbusCompanyLyrics 加载系统字体失败 ({name}): {e.Message}");
            }
        }

        return fallback;
    }

    // ------------------------------------------------------------------ audio visualizer

    private void BuildVisualizer(Transform parent)
    {
        _visRoot = new GameObject("AudioVisualizer", typeof(RectTransform));
        var rootRect = _visRoot.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        for (var i = 0; i < _visBars.Length; i++)
        {
            var bar = new GameObject("Bar" + i, typeof(RectTransform));
            var rect = bar.GetComponent<RectTransform>();
            rect.SetParent(rootRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var image = bar.AddComponent<Image>();
            image.color = new Color(0.776f, 0.635f, 0.29f, 0.55f);
            image.raycastTarget = false;

            var outline = bar.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            _visBars[i] = image;
        }

        _visRoot.SetActive(false);
        _visRoot.transform.SetSiblingIndex(0);
    }

    // ------------------------------------------------------------------ level BGM replacement

    private GameObject _tempEventSystem;

    private void EnsureMouseEventSystem()
    {
        if (EventSystem.current != null || _tempEventSystem != null)
            return;

        try
        {
            var go = new GameObject("LimbusEventSystem");
            go.transform.SetParent(transform, false);
            _tempEventSystem = go;
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.GetType().GetMethod("AssignDefaultActions")?.Invoke(module, null);
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("创建 UI EventSystem 失败: " + e.Message);
        }
    }

    private void DestroyTempEventSystem()
    {
        if (_tempEventSystem == null)
            return;

        Destroy(_tempEventSystem);
        _tempEventSystem = null;
    }

    private void BuildBgmPanel()
    {
        if (_canvas == null || _bgmPanel != null)
            return;

        var go = new GameObject("LimbusBgmPanel", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(_canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1220f, 820f);
        rect.anchoredPosition = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);
        // 空白处不拦截点击：让下层曲绘行在菜单打开时仍可拖动（按钮本身仍可点击）。
        image.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.847f, 0.647f, 0.137f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        _bgmPanelRect = rect;
        _bgmPanel = go;
        go.SetActive(false);
    }

    private static bool IsZh => LimbusCompanyLyrics.Settings != null &&
                                LimbusCompanyLyrics.Settings.Language == "zh";

    private RectTransform CreateUiObject(string name, RectTransform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private TextMeshProUGUI CreateUiLabel(RectTransform parent, string text, Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, bool wrap = false)
    {
        var rect = CreateUiObject("Label", parent, position, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = LimbusCompanyFonts.MenuFont(text);
        label.fontSize = fontSize;
        label.color = new Color(1f, 0.996f, 0.937f, 1f);
        label.alignment = alignment;
        label.enableWordWrapping = wrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private Button CreateUiButton(RectTransform parent, string text, Vector2 position, Vector2 size,
        Action onClick, float fontSize = 16f, Color? tint = null)
    {
        var rect = CreateUiObject("Button", parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = tint ?? new Color(0.09f, 0.09f, 0.10f, 1f);

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.776f, 0.635f, 0.29f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        var labelRect = CreateUiObject("Label", rect, Vector2.zero, size);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = LimbusCompanyFonts.MenuFont(text);
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = new Color(0.937f, 0.902f, 0.812f, 1f);
        label.text = text;
        return button;
    }

    private void RebuildBgmPanelUi()
    {
        if (_bgmPanel == null)
            return;

        _settingsUi?.CloseStandalone();
        _bgmSeekSlider = null;
        _bgmTimeText = null;
        _bgmPauseLabel = null;
        _npSizeSlider = null;
        _npSizeValueText = null;

        if (_bgmUiRoot != null)
        {
            _bgmUiRoot.SetActive(false);
            Destroy(_bgmUiRoot);
            _bgmUiRoot = null;
        }

        var root = CreateUiObject("BgmUi", _bgmPanelRect, Vector2.zero, new Vector2(1220f, 820f));
        _bgmUiRoot = root.gameObject;

        CreateUiLabel(root, "JUKEBOX LIMBUSCOMPANY - F5", new Vector2(-180f, 378f), new Vector2(720f, 36f), 24f,
            TextAlignmentOptions.Center);
        CreateUiButton(root, "X", new Vector2(570f, 378f), new Vector2(44f, 34f), CloseBgmPanel, 18f);

        CreateUiButton(root, IsZh ? "音乐" : "MUSIC", new Vector2(-530f, 334f), new Vector2(150f, 32f),
            () => SwitchBgmPage(0), 15f);
        CreateUiButton(root, IsZh ? "歌词设置" : "SETTINGS", new Vector2(-370f, 334f), new Vector2(150f, 32f),
            () => SwitchBgmPage(1), 15f);
        CreateUiButton(root, IsZh ? "预设" : "PRESETS", new Vector2(-210f, 334f), new Vector2(150f, 32f),
            () => SwitchBgmPage(3), 15f);
        CreateUiButton(root, IsZh ? "显示" : "DISPLAY", new Vector2(-50f, 334f), new Vector2(150f, 32f),
            () => SwitchBgmPage(2), 15f);

        // 左侧：功能与设置；右侧：曲绘在上，曲名、作者在下。
        _bgmContent = CreateUiObject("Content", root, new Vector2(-290f, -20f), new Vector2(640f, 660f));

        switch (_bgmPage)
        {
            case 1:
                BuildBgmSettingsPage(_bgmContent);
                break;
            case 2:
                BuildBgmDisplayPage(_bgmContent);
                break;
            case 3:
                BuildBgmPresetsPage(_bgmContent);
                break;
            default:
                BuildBgmMusicPage(_bgmContent);
                break;
        }

        BuildBgmPreview(root);

        _bgmStatus = CreateUiLabel(root, string.Empty, new Vector2(-290f, -390f), new Vector2(640f, 30f), 15f,
            TextAlignmentOptions.Center, true);
        UpdateBgmPanelStatus();
    }

    private void BuildBgmPreview(RectTransform root)
    {
        var panelRect = CreateUiObject("PreviewPanel", root, new Vector2(330f, -20f), new Vector2(420f, 660f));
        var panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.045f, 0.045f, 0.055f, 0.9f);
        panelImage.raycastTarget = true;
        var panelOutline = panelRect.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.776f, 0.635f, 0.29f, 0.7f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        CreateUiLabel(root, IsZh ? "曲目预览" : "TRACK PREVIEW", new Vector2(330f, 300f),
            new Vector2(380f, 30f), 18f, TextAlignmentOptions.Center);

        var frameRect = CreateUiObject("PreviewFrame", root, new Vector2(330f, 120f), new Vector2(260f, 260f));
        _menuPreviewFrameImage = frameRect.gameObject.AddComponent<Image>();
        _menuPreviewFrameImage.color = new Color(0.02f, 0.02f, 0.02f, 0.78f);
        _menuPreviewFrameImage.raycastTarget = false;
        var frameOutline = frameRect.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.92f, 0.92f, 0.92f, 0.9f);
        frameOutline.effectDistance = new Vector2(3f, -3f);
        frameOutline.useGraphicAlpha = false;

        var coverRect = CreateUiObject("PreviewCover", frameRect, Vector2.zero, new Vector2(244f, 244f));
        _menuPreviewCoverImage = coverRect.gameObject.AddComponent<Image>();
        _menuPreviewCoverImage.preserveAspect = true;
        _menuPreviewCoverImage.raycastTarget = false;
        _menuPreviewCoverImage.enabled = false;

        _menuPreviewTitleText = CreateUiLabel(root, string.Empty, new Vector2(330f, -70f),
            new Vector2(370f, 110f), 24f, TextAlignmentOptions.Center, true);
        _menuPreviewArtistText = CreateUiLabel(root, string.Empty, new Vector2(330f, -170f),
            new Vector2(370f, 70f), 18f, TextAlignmentOptions.Center, true);

        UpdateBgmPreview();
    }

    private void UpdateBgmPreview()
    {
        if (_menuPreviewTitleText == null)
            return;

        if (_bgmTracks.Length == 0)
        {
            _menuPreviewTitle = "-";
            _menuPreviewArtist = string.Empty;
            _menuPreviewSprite = null;
        }
        else
        {
            EnsureMenuPreviewInfo(_bgmTracks[Mathf.Clamp(_bgmIndex, 0, _bgmTracks.Length - 1)]);
        }

        _menuPreviewTitleText.font = LimbusCompanyFonts.MenuFont(_menuPreviewTitle);
        _menuPreviewArtistText.font = LimbusCompanyFonts.MenuFont(_menuPreviewArtist);
        _menuPreviewTitleText.text = _menuPreviewTitle ?? string.Empty;
        _menuPreviewArtistText.text = _menuPreviewArtist ?? string.Empty;

        if (_menuPreviewCoverImage != null)
        {
            _menuPreviewCoverImage.sprite = _menuPreviewSprite;
            _menuPreviewCoverImage.enabled = _menuPreviewSprite != null;
        }

        if (_menuPreviewFrameImage != null)
        {
            _menuPreviewFrameImage.color = _menuPreviewSprite != null
                ? new Color(0.02f, 0.02f, 0.02f, 0.78f)
                : new Color(0.02f, 0.02f, 0.02f, 0f);
        }
    }

    private void EnsureMenuPreviewInfo(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || relativePath == _menuPreviewPath)
            return;

        _menuPreviewPath = relativePath;
        _menuPreviewTitle = Path.GetFileNameWithoutExtension(relativePath);
        _menuPreviewArtist = string.Empty;

        if (_menuPreviewSprite != null)
        {
            Destroy(_menuPreviewSprite);
            _menuPreviewSprite = null;
        }
        if (_menuPreviewTexture != null)
        {
            Destroy(_menuPreviewTexture);
            _menuPreviewTexture = null;
        }

        var fullPath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(PathsUtils.MusicPath, relativePath);

        var cover = LoadFolderCover(fullPath);
        var titleFromTag = false;

        try
        {
            using var tagFile = TagLib.File.Create(fullPath);
            var tag = tagFile.Tag;
            if (tag != null)
            {
                if (!string.IsNullOrEmpty(tag.Title))
                {
                    _menuPreviewTitle = tag.Title;
                    titleFromTag = true;
                }

                var performer = tag.FirstPerformer;
                if (string.IsNullOrEmpty(performer))
                    performer = tag.FirstAlbumArtist;
                if (string.IsNullOrEmpty(performer))
                    performer = tag.FirstComposer;
                if (!string.IsNullOrEmpty(performer))
                    _menuPreviewArtist = performer;

                var pictures = tag.Pictures;
                LimbusCompanyLyrics.Log?.LogInfo(
                    $"预览标签: {Path.GetFileName(fullPath)} pictures={pictures?.Length ?? 0} folderCover={(cover != null)}");

                if (cover == null && pictures != null && pictures.Length > 0)
                {
                    cover = CreateSprite(pictures[0].Data.Data);
                    LimbusCompanyLyrics.Log?.LogInfo("预览曲绘(ID3): " + Path.GetFileName(fullPath)
                        + " bytes=" + pictures[0].Data.Data.Length + " sprite=" + (cover != null));
                }
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取预览标签失败: " + e.Message);
        }

        ApplyNameFallbacks(fullPath, ref _menuPreviewTitle, ref _menuPreviewArtist, titleFromTag);

        _menuPreviewSprite = cover;
        _menuPreviewTexture = cover?.texture;
    }

    private void SwitchBgmPage(int page)
    {
        _bgmPage = page;
        RebuildBgmPanelUi();
    }

    private void BuildBgmMusicPage(RectTransform parent)
    {
        BuildBgmCategoryBar(parent);
        BuildBgmPlaybackControls(parent);

        if (_bgmTracks.Length == 0)
        {
            string message;
            if (string.IsNullOrEmpty(_bgmSelectedCategory))
            {
                message = IsZh
                    ? "没有个人歌曲。\n把音频放进 Cybergrind/Music（分类文件夹之外）。"
                    : "No personal songs.\nPut audio in Cybergrind/Music (outside category folders).";
            }
            else if (string.Equals(_bgmSelectedCategory, OstFolderName, StringComparison.OrdinalIgnoreCase))
            {
                message = IsZh
                    ? "ULTRAKILL-OST 文件夹为空。\n把官方歌曲放进 Cybergrind/Music/ULTRAKILL-OST。"
                    : "ULTRAKILL-OST is empty.\nPut official songs in Cybergrind/Music/ULTRAKILL-OST.";
            }
            else
            {
                message = IsZh
                    ? $"分类 {_bgmSelectedCategory} 为空。\n把歌曲放进 Cybergrind/Music/{_bgmSelectedCategory}。"
                    : $"Category {_bgmSelectedCategory} is empty.\nPut songs in Cybergrind/Music/{_bgmSelectedCategory}.";
            }

            CreateUiLabel(parent, message, new Vector2(0f, 120f), new Vector2(600f, 80f), 17f,
                TextAlignmentOptions.Center, true);
            CreateUiButton(parent, IsZh ? "归还原 BGM" : "RESTORE BGM", new Vector2(0f, -300f),
                new Vector2(260f, 44f), RestoreBgmFromUi, 16f);
            return;
        }

        var pages = Mathf.Max(1, (_bgmTracks.Length + TracksPerPage - 1) / TracksPerPage);
        var page = Mathf.Clamp(_bgmListOffset / TracksPerPage, 0, pages - 1);
        _bgmListOffset = page * TracksPerPage;
        var start = _bgmListOffset;

        for (var i = 0; i < TracksPerPage; i++)
        {
            var index = start + i;
            if (index >= _bgmTracks.Length)
                break;

            var captured = index;
            var selected = index == _bgmIndex;
            CreateUiButton(parent, TrackLabel(_bgmTracks[index]),
                new Vector2(0f, 250f - i * 46f), new Vector2(600f, 40f),
                () => SelectBgmTrack(captured), 15f,
                selected ? new Color(0.24f, 0.20f, 0.08f, 1f) : (Color?)null);
        }

        CreateUiButton(parent, IsZh ? "上一页" : "< PAGE", new Vector2(-190f, -248f), new Vector2(160f, 40f),
            () => ChangeBgmTrackPage(-1), 15f);
        CreateUiLabel(parent, (page + 1) + " / " + pages, new Vector2(0f, -248f), new Vector2(160f, 30f), 16f,
            TextAlignmentOptions.Center);
        CreateUiButton(parent, IsZh ? "下一页" : "PAGE >", new Vector2(190f, -248f), new Vector2(160f, 40f),
            () => ChangeBgmTrackPage(1), 15f);

        CreateUiButton(parent, IsZh ? "取代关卡 BGM" : "REPLACE LEVEL BGM", new Vector2(-140f, -300f),
            new Vector2(260f, 44f), ApplySelectedBgmTrack, 16f);
        CreateUiButton(parent, IsZh ? "归还原 BGM" : "RESTORE BGM", new Vector2(140f, -300f),
            new Vector2(220f, 44f), RestoreBgmFromUi, 16f);
    }

    /// <summary>当前窗口在 Canvas 单位下的真实大小（而不是固定参考分辨率）。</summary>
    private void GetWindowCanvasSize(out float width, out float height)
    {
        width = 1920f;
        height = 1080f;

        if (_canvas == null)
            return;

        var scale = _canvas.scaleFactor;
        if (scale <= 0.0001f)
            scale = 1f;

        var screenWidth = Screen.width / scale;
        var screenHeight = Screen.height / scale;
        if (screenWidth > 1f && screenHeight > 1f)
        {
            width = screenWidth;
            height = screenHeight;
            return;
        }

        var rect = _canvas.GetComponent<RectTransform>().rect;
        if (rect.width > 1f)
            width = rect.width;
        if (rect.height > 1f)
            height = rect.height;
    }

    internal float WindowCanvasScale
    {
        get
        {
            var scale = _canvas != null ? _canvas.scaleFactor : 1f;
            return scale > 0.0001f ? scale : 1f;
        }
    }

    /// <summary>菜单按当前窗口大小自动缩放，避免小窗口/窄窗口被裁切。</summary>
    private void UpdateBgmMenuScale()
    {
        if (_bgmPanelRect == null || _canvas == null)
            return;

        GetWindowCanvasSize(out var width, out var height);

        var scale = Mathf.Min(1f, Mathf.Min((width - 40f) / 1220f, (height - 40f) / 820f));
        scale = Mathf.Clamp(scale, 0.5f, 1f);

        var target = new Vector3(scale, scale, 1f);
        if ((_bgmPanelRect.localScale - target).sqrMagnitude > 0.0001f)
            _bgmPanelRect.localScale = target;
    }

    private void BuildBgmCategoryBar(RectTransform parent)
    {
        LimbusCompanySongCategories.Refresh();

        if (_confirmDeleteCategory)
        {
            BuildDeleteBgmCategoryRow(parent);
            return;
        }

        if (_addingCategory)
        {
            BuildAddBgmCategoryRow(parent);
            return;
        }

        var all = new List<string> { null };
        foreach (var category in LimbusCompanySongCategories.Categories)
        {
            if (!string.IsNullOrEmpty(category))
                all.Add(category);
        }

        var pages = Mathf.Max(1, (all.Count + CategoriesPerPage - 1) / CategoriesPerPage);
        _categoryPage = Mathf.Clamp(_categoryPage, 0, pages - 1);
        var start = _categoryPage * CategoriesPerPage;
        var centers = new[] { -210f, -60f, 90f };

        for (var i = 0; i < CategoriesPerPage; i++)
        {
            var index = start + i;
            if (index >= all.Count)
                break;

            var category = all[index];
            var selected = string.IsNullOrEmpty(category)
                ? string.IsNullOrEmpty(_bgmSelectedCategory)
                : string.Equals(category, _bgmSelectedCategory, StringComparison.OrdinalIgnoreCase);

            CreateUiButton(parent,
                string.IsNullOrEmpty(category) ? (IsZh ? "个人" : "PERSONAL") : CategoryLabel(category),
                new Vector2(centers[i], 288f), new Vector2(140f, 30f),
                () => SelectBgmCategory(category), 13f,
                selected ? new Color(0.24f, 0.20f, 0.08f, 1f) : (Color?)null);
        }

        if (pages > 1)
        {
            CreateUiButton(parent, "<", new Vector2(-300f, 288f), new Vector2(30f, 30f),
                () => ChangeCategoryPage(-1), 16f);
            CreateUiButton(parent, ">", new Vector2(185f, 288f), new Vector2(30f, 30f),
                () => ChangeCategoryPage(1), 16f);
        }

        var canDelete = !string.IsNullOrEmpty(_bgmSelectedCategory) &&
                        !string.Equals(_bgmSelectedCategory, OstFolderName, StringComparison.OrdinalIgnoreCase);
        if (canDelete)
        {
            CreateUiButton(parent, IsZh ? "删除" : "DEL", new Vector2(235f, 288f), new Vector2(55f, 30f),
                () =>
                {
                    _confirmDeleteCategory = true;
                    _categoryPendingDelete = _bgmSelectedCategory;
                    RebuildBgmPanelUi();
                }, 13f, new Color(0.32f, 0.09f, 0.09f, 1f));
        }

        CreateUiButton(parent, "+", new Vector2(290f, 288f), new Vector2(40f, 30f),
            () => { _addingCategory = true; _categoryNameInput = null; RebuildBgmPanelUi(); }, 18f);
    }

    private void BuildDeleteBgmCategoryRow(RectTransform parent)
    {
        var name = CategoryLabel(_categoryPendingDelete ?? string.Empty);
        var warning = IsZh
            ? $"删除 {name} 及其所有歌曲？不可恢复。"
            : $"Delete {name} and ALL its songs? Permanent.";

        CreateUiLabel(parent, warning, new Vector2(-50f, 288f), new Vector2(340f, 30f), 12.5f,
            TextAlignmentOptions.Center);
        CreateUiButton(parent, IsZh ? "删除" : "DELETE", new Vector2(180f, 288f), new Vector2(80f, 30f),
            ConfirmDeleteBgmCategory, 13f, new Color(0.5f, 0.1f, 0.1f, 1f));
        CreateUiButton(parent, IsZh ? "取消" : "CANCEL", new Vector2(262f, 288f), new Vector2(80f, 30f),
            () =>
            {
                _confirmDeleteCategory = false;
                _categoryPendingDelete = null;
                RebuildBgmPanelUi();
            }, 13f);
    }

    private void ConfirmDeleteBgmCategory()
    {
        var name = _categoryPendingDelete;
        if (string.IsNullOrEmpty(name))
            return;

        if (BgmReplacing && !string.IsNullOrEmpty(_bgmCurrentPath))
        {
            try
            {
                var folder = Path.GetFullPath(Path.Combine(PathsUtils.MusicPath, name)) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(_bgmCurrentPath).StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    StopBgmReplacement();
            }
            catch (Exception)
            {
                // 路径比较失败时仍继续删除分类。
            }
        }

        LimbusCompanySongCategories.Remove(name, true);
        _confirmDeleteCategory = false;
        _categoryPendingDelete = null;
        _bgmSelectedCategory = null;
        _categoryPage = 0;
        RefreshBgmTracks();
        RebuildBgmPanelUi();
    }

    private void BuildAddBgmCategoryRow(RectTransform parent)
    {
        _categoryNameInput = CreateUiInput(parent, string.Empty, new Vector2(-80f, 288f), new Vector2(300f, 30f));
        CreateUiButton(parent, IsZh ? "添加" : "ADD", new Vector2(140f, 288f), new Vector2(90f, 30f),
            ConfirmAddBgmCategory, 14f);
        CreateUiButton(parent, IsZh ? "取消" : "CANCEL", new Vector2(240f, 288f), new Vector2(100f, 30f),
            () => { _addingCategory = false; _categoryNameInput = null; _categoryStatusText = null; RebuildBgmPanelUi(); },
            14f);

        _categoryStatusText = CreateUiLabel(parent, string.Empty, new Vector2(0f, 312f),
            new Vector2(600f, 22f), 12f, TextAlignmentOptions.Center);
    }

    private void ConfirmAddBgmCategory()
    {
        var raw = _categoryNameInput != null ? _categoryNameInput.text : string.Empty;
        var name = LimbusCompanySongCategories.SanitizeName(raw);

        if (string.IsNullOrEmpty(name))
        {
            SetCategoryStatus(IsZh ? "名称无效或为空。" : "Invalid or empty name.");
            return;
        }

        if (!LimbusCompanySongCategories.Add(raw))
        {
            SetCategoryStatus(IsZh ? "分类已存在或无法创建。" : "Category exists or cannot be created.");
            return;
        }

        _addingCategory = false;
        _categoryNameInput = null;
        _categoryStatusText = null;
        _bgmSelectedCategory = name;
        _categoryPage = 0;
        ApplyBgmSourceFilter();
        _bgmListOffset = 0;
        RebuildBgmPanelUi();
    }

    private void SetCategoryStatus(string text)
    {
        if (_categoryStatusText == null)
            return;

        _categoryStatusText.font = LimbusCompanyFonts.MenuFont(text);
        _categoryStatusText.text = text;
    }

    private void ChangeCategoryPage(int delta)
    {
        var total = LimbusCompanySongCategories.Categories.Count + 1;
        var pages = Mathf.Max(1, (total + CategoriesPerPage - 1) / CategoriesPerPage);
        _categoryPage = Mathf.Clamp(_categoryPage + delta, 0, pages - 1);
        RebuildBgmPanelUi();
    }

    private static string CategoryLabel(string category)
    {
        if (string.IsNullOrEmpty(category))
            return "PERSONAL";

        return category.Length <= 14 ? category : category.Substring(0, 13) + "...";
    }

    private void BuildBgmPlaybackControls(RectTransform parent)
    {
        var barRect = CreateUiObject("SeekBar", parent, new Vector2(0f, -120f), new Vector2(600f, 16f));
        var background = barRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.16f, 0.18f, 1f);
        background.raycastTarget = true;

        var fillArea = CreateUiObject("FillArea", barRect, Vector2.zero, new Vector2(600f, 16f));
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        var fill = CreateUiObject("Fill", fillArea, Vector2.zero, new Vector2(600f, 16f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.847f, 0.647f, 0.137f, 1f);
        fillImage.raycastTarget = false;

        var slider = barRect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = null;
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.onValueChanged.AddListener(OnBgmSeekChanged);
        _bgmSeekSlider = slider;

        _bgmTimeText = CreateUiLabel(parent, "--:-- / --:--", new Vector2(0f, -148f),
            new Vector2(600f, 24f), 14f, TextAlignmentOptions.Center);

        var pause = CreateUiButton(parent, IsZh ? "暂停" : "PAUSE", new Vector2(0f, -186f),
            new Vector2(180f, 36f), ToggleBgmPlayback, 15f);
        _bgmPauseLabel = pause.GetComponentInChildren<TextMeshProUGUI>();

        UpdateBgmPlaybackUi();
    }

    private void OnBgmSeekChanged(float value)
    {
        if (_bgmSource == null || _bgmSource.clip == null)
            return;

        _bgmSource.time = Mathf.Clamp(value, 0f, _bgmSource.clip.length);
    }

    private void ToggleBgmPlayback()
    {
        if (_bgmSource == null || _bgmSource.clip == null)
            return;

        if (_bgmSource.isPlaying)
            _bgmSource.Pause();
        else
            _bgmSource.UnPause();

        UpdateBgmPlaybackUi();
    }

    private void UpdateBgmPlaybackUi()
    {
        if (_bgmSeekSlider == null)
            return;

        var source = _bgmSource;
        var hasClip = source != null && source.clip != null;

        if (hasClip)
        {
            var length = Mathf.Max(0.01f, source.clip.length);
            if (!Mathf.Approximately(_bgmSeekSlider.maxValue, length))
                _bgmSeekSlider.maxValue = length;

            var time = Mathf.Clamp(source.time, 0f, length);
            _bgmSeekSlider.SetValueWithoutNotify(time);

            var text = FormatPlaybackTime(time) + " / " + FormatPlaybackTime(length);
            if (text != _bgmLastTimeText && _bgmTimeText != null)
            {
                _bgmLastTimeText = text;
                _bgmTimeText.text = text;
            }
        }
        else
        {
            _bgmSeekSlider.maxValue = 1f;
            _bgmSeekSlider.SetValueWithoutNotify(0f);
            if (_bgmTimeText != null && _bgmLastTimeText != "--:-- / --:--")
            {
                _bgmLastTimeText = "--:-- / --:--";
                _bgmTimeText.text = _bgmLastTimeText;
            }
        }

        var playing = hasClip && source.isPlaying;
        var label = playing ? (IsZh ? "暂停" : "PAUSE") : (IsZh ? "继续" : "RESUME");
        if (_bgmPauseLabel != null && label != _bgmLastPauseText)
        {
            _bgmLastPauseText = label;
            _bgmPauseLabel.font = LimbusCompanyFonts.MenuFont(label);
            _bgmPauseLabel.text = label;
        }
    }

    private static string FormatPlaybackTime(float seconds)
    {
        if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            return "--:--";

        var total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private void BuildBgmSettingsPage(RectTransform parent)
    {
        var host = CreateUiObject("SettingsHost", parent, new Vector2(0f, -10f), new Vector2(560f, 660f));
        _settingsUi ??= GetComponent<LimbusCompanyLocalization>();
        _settingsUi?.OpenStandalone(host);
    }

    private void BuildBgmDisplayPage(RectTransform parent)
    {
        var s = LimbusCompanyLyrics.Settings;

        CreateUiLabel(parent, IsZh ? "界面语言" : "LANGUAGE", new Vector2(0f, 260f), new Vector2(280f, 30f), 16f,
            TextAlignmentOptions.Center);
        CreateUiButton(parent, "中文", new Vector2(0f, 212f), new Vector2(240f, 40f),
            () => SetUiLanguage("zh"), 16f, IsZh ? new Color(0.24f, 0.20f, 0.08f, 1f) : (Color?)null);
        CreateUiButton(parent, "ENGLISH", new Vector2(0f, 162f), new Vector2(240f, 40f),
            () => SetUiLanguage("en"), 16f, !IsZh ? new Color(0.24f, 0.20f, 0.08f, 1f) : (Color?)null);

        var lyricsOn = s == null || s.Enabled;
        var npOn = s != null && s.NowPlayingEnabled;
        CreateUiButton(parent, (IsZh ? "歌词显示：" : "LYRICS: ") + (lyricsOn ? "ON" : "OFF"),
            new Vector2(0f, 100f), new Vector2(240f, 40f), ToggleLyricsDisplay, 16f);
        CreateUiButton(parent, (IsZh ? "曲绘 / 歌名：" : "NOW PLAYING: ") + (npOn ? "ON" : "OFF"),
            new Vector2(0f, 50f), new Vector2(240f, 40f), ToggleNowPlaying, 16f);

        CreateUiLabel(parent, IsZh ? "曲绘 / 文字大小" : "COVER / TEXT SIZE",
            new Vector2(0f, -10f), new Vector2(320f, 28f), 15f, TextAlignmentOptions.Center);
        _npSizeSlider = CreateUiSlider(parent, new Vector2(0f, -40f), new Vector2(320f, 18f),
            10f, 120f, s?.CoverSize ?? 32f, OnNpSizeChanged);
        _npSizeValueText = CreateUiLabel(parent, string.Empty, new Vector2(0f, -66f),
            new Vector2(200f, 24f), 13f, TextAlignmentOptions.Center);
        CreateUiButton(parent, IsZh ? "重置位置与大小" : "RESET POSITION & SIZE",
            new Vector2(0f, -106f), new Vector2(280f, 34f), ResetNowPlayingLayout, 14f);

        UpdateNpSizeUi();
    }

    private Slider CreateUiSlider(RectTransform parent, Vector2 position, Vector2 size,
        float min, float max, float value, Action<float> onChange)
    {
        var barRect = CreateUiObject("Slider", parent, position, size);
        var background = barRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.16f, 0.18f, 1f);
        background.raycastTarget = true;

        var fillArea = CreateUiObject("FillArea", barRect, Vector2.zero, size);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        var fill = CreateUiObject("Fill", fillArea, Vector2.zero, size);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.847f, 0.647f, 0.137f, 1f);
        fillImage.raycastTarget = false;

        var slider = barRect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = null;
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
        slider.onValueChanged.AddListener(v => onChange(v));
        return slider;
    }

    private void OnNpSizeChanged(float value)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverSize = Mathf.Clamp(value, 10f, 120f);
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateBgmPanelStatus();
    }

    private void UpdateNpSizeUi()
    {
        if (_npSizeSlider == null)
            return;

        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        var value = Mathf.Clamp(s.CoverSize, 10f, 120f);
        _npSizeSlider.SetValueWithoutNotify(value);

        if (_npSizeValueText != null)
        {
            var text = Mathf.RoundToInt(value).ToString();
            if (_npSizeValueText.text != text)
                _npSizeValueText.text = text;
        }
    }

    private void ResetNowPlayingLayout()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverPosX = 2f;
        s.CoverPosY = 90f;
        s.CoverSize = 32f;
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateNpSizeUi();
        UpdateBgmPanelStatus();
    }

    private void BuildBgmPresetsPage(RectTransform parent)
    {
        LimbusCompanyPresets.Refresh();
        var names = LimbusCompanyPresets.Names;

        CreateUiLabel(parent, IsZh ? "预设" : "PRESETS", new Vector2(0f, 260f), new Vector2(600f, 32f), 20f,
            TextAlignmentOptions.Center);

        _presetNameInput = CreateUiInput(parent, string.Empty, new Vector2(-90f, 205f), new Vector2(380f, 36f));
        CreateUiButton(parent, IsZh ? "保存当前" : "SAVE CURRENT", new Vector2(225f, 205f),
            new Vector2(150f, 36f), SavePresetFromUi, 15f);

        _presetStatusText = CreateUiLabel(parent, string.Empty, new Vector2(0f, 160f),
            new Vector2(600f, 26f), 13f, TextAlignmentOptions.Center);

        if (names.Count == 0)
        {
            CreateUiLabel(parent, IsZh ? "还没有预设。输入名称后点“保存当前”。" : "No presets yet. Type a name and click SAVE CURRENT.",
                new Vector2(0f, 60f), new Vector2(600f, 40f), 15f, TextAlignmentOptions.Center);
        }
        else
        {
            var pages = Mathf.Max(1, (names.Count + PresetsPerPage - 1) / PresetsPerPage);
            _presetPage = Mathf.Clamp(_presetPage, 0, pages - 1);
            var start = _presetPage * PresetsPerPage;

            for (var i = 0; i < PresetsPerPage; i++)
            {
                var index = start + i;
                if (index >= names.Count)
                    break;

                var presetName = names[index];
                CreateUiButton(parent, presetName, new Vector2(-60f, 110f - i * 44f), new Vector2(440f, 36f),
                    () => ApplyPresetFromUi(presetName), 14f);
                CreateUiButton(parent, IsZh ? "删除" : "DEL", new Vector2(245f, 110f - i * 44f),
                    new Vector2(60f, 36f), () => DeletePresetFromUi(presetName), 12.5f,
                    new Color(0.32f, 0.09f, 0.09f, 1f));
            }

            CreateUiButton(parent, IsZh ? "上一页" : "< PAGE", new Vector2(-190f, -260f),
                new Vector2(160f, 36f), () => ChangePresetPage(-1), 14f);
            CreateUiLabel(parent, (_presetPage + 1) + " / " + pages, new Vector2(0f, -260f),
                new Vector2(160f, 28f), 14f, TextAlignmentOptions.Center);
            CreateUiButton(parent, IsZh ? "下一页" : "PAGE >", new Vector2(190f, -260f),
                new Vector2(160f, 36f), () => ChangePresetPage(1), 14f);
        }

        CreateUiLabel(parent,
            IsZh ? "点击预设名称应用；X 删除；用相同名称保存会覆盖。" : "Click a preset to apply; X deletes; saving with the same name overwrites.",
            new Vector2(0f, -305f), new Vector2(600f, 32f), 12.5f, TextAlignmentOptions.Center, true);
    }

    private TMP_InputField CreateUiInput(RectTransform parent, string value, Vector2 position, Vector2 size)
    {
        var rect = CreateUiObject("Input", parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.09f, 0.09f, 0.10f, 1f);

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.776f, 0.635f, 0.29f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        var textRect = CreateUiObject("Text", rect, Vector2.zero, size);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = LimbusCompanyFonts.MenuFont(value);
        text.fontSize = 15f;
        text.color = new Color(0.937f, 0.902f, 0.812f, 1f);
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        var input = rect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.textComponent = text;
        input.textViewport = textRect;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = value;
        input.onValueChanged.AddListener(v => text.font = LimbusCompanyFonts.MenuFont(v));
        return input;
    }

    private void SavePresetFromUi()
    {
        var name = _presetNameInput != null ? _presetNameInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            SetPresetStatus(IsZh ? "请输入预设名称。" : "Enter a preset name.");
            return;
        }

        if (LimbusCompanyPresets.Save(name))
        {
            _presetPage = 0;
            RebuildBgmPanelUi();
        }
        else
        {
            SetPresetStatus(IsZh ? "保存失败。" : "Save failed.");
        }
    }

    private void ApplyPresetFromUi(string name)
    {
        if (LimbusCompanyPresets.Apply(name))
        {
            RebuildBgmPanelUi();
        }
        else
        {
            SetPresetStatus(IsZh ? "应用失败。" : "Apply failed.");
        }
    }

    private void DeletePresetFromUi(string name)
    {
        LimbusCompanyPresets.Delete(name);
        _presetPage = 0;
        RebuildBgmPanelUi();
    }

    private void ChangePresetPage(int delta)
    {
        LimbusCompanyPresets.Refresh();
        var pages = Mathf.Max(1, (LimbusCompanyPresets.Names.Count + PresetsPerPage - 1) / PresetsPerPage);
        _presetPage = Mathf.Clamp(_presetPage + delta, 0, pages - 1);
        RebuildBgmPanelUi();
    }

    private void SetPresetStatus(string text)
    {
        if (_presetStatusText == null)
            return;

        _presetStatusText.font = LimbusCompanyFonts.MenuFont(text);
        _presetStatusText.text = text;
    }

    private void SelectBgmTrack(int index)
    {
        if (index < 0 || index >= _bgmTracks.Length)
            return;

        _bgmIndex = index;
        var s = LimbusCompanyLyrics.Settings;
        if (s != null)
        {
            s.BgmTrack = _bgmTracks[index];
            LimbusCompanyLyrics.SaveAllSettings();
        }

        RebuildBgmPanelUi();
    }

    private void ChangeBgmTrackPage(int delta)
    {
        if (_bgmTracks.Length == 0)
            return;

        var pages = Mathf.Max(1, (_bgmTracks.Length + TracksPerPage - 1) / TracksPerPage);
        var page = Mathf.Clamp(_bgmListOffset / TracksPerPage + delta, 0, pages - 1);
        _bgmListOffset = page * TracksPerPage;
        RebuildBgmPanelUi();
    }

    private void RestoreBgmFromUi()
    {
        StopBgmReplacement();
        RebuildBgmPanelUi();
    }

    private void SetUiLanguage(string language)
    {
        LimbusCompanyLyrics.SetLanguage(language);
        RebuildBgmPanelUi();
    }

    internal RectTransform NowPlayingCanvasRect =>
        _canvas != null ? _canvas.GetComponent<RectTransform>() : null;

    internal Vector2 GetNowPlayingAnchorPercent()
    {
        var s = LimbusCompanyLyrics.Settings;
        return new Vector2(s?.CoverPosX ?? 84f, s?.CoverPosY ?? 18f);
    }

    internal void SetNowPlayingAnchorPercent(float x, float y, bool save)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverPosX = Mathf.Clamp(x, -100f, 200f);
        s.CoverPosY = Mathf.Clamp(y, -100f, 200f);

        if (save)
        {
            LimbusCompanyLyrics.SaveAllSettings();
            UpdateBgmPanelStatus();
        }
    }

    internal void SaveNowPlayingSettings()
    {
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateBgmPanelStatus();
    }

    internal void ScrollNowPlayingSize(float scroll)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || Mathf.Approximately(scroll, 0f))
            return;

        s.CoverSize = Mathf.Clamp(s.CoverSize + (scroll > 0f ? 2f : -2f), 10f, 120f);
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateBgmPanelStatus();
    }

    private void EnterNowPlayingMouseAdjust()
    {
        if (!_bgmPanelOpen || _bgmPanel == null)
            return;

        _npMouseAdjust = true;
        _bgmPanel.SetActive(false);
        BuildNowPlayingAdjustHint();
    }

    private void BuildNowPlayingAdjustHint()
    {
        if (_npAdjustHint != null || _canvas == null)
            return;

        var canvasRect = _canvas.GetComponent<RectTransform>();
        var rect = CreateUiObject("NowPlayingAdjustHint", canvasRect, new Vector2(-700f, 480f),
            new Vector2(520f, 82f));

        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);
        image.raycastTarget = true;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.776f, 0.635f, 0.29f, 1f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreateUiLabel(rect,
            IsZh
                ? "拖动曲绘行移动 · 滚轮缩放 · Esc 完成"
                : "Drag the cover row · scroll to resize · Esc to finish",
            new Vector2(-55f, 16f), new Vector2(380f, 30f), 14f, TextAlignmentOptions.Center);
        CreateUiButton(rect, IsZh ? "完成" : "DONE", new Vector2(200f, -17f), new Vector2(110f, 30f),
            ExitNowPlayingMouseAdjust, 14f);

        _npAdjustHint = rect.gameObject;
    }

    private void CleanupNowPlayingAdjustHint()
    {
        if (_npAdjustHint == null)
            return;

        _npAdjustHint.SetActive(false);
        Destroy(_npAdjustHint);
        _npAdjustHint = null;
    }

    private void ExitNowPlayingMouseAdjust()
    {
        if (!_npMouseAdjust)
            return;

        _npMouseAdjust = false;
        CleanupNowPlayingAdjustHint();
        LimbusCompanyLyrics.SaveAllSettings();

        // 完成后直接关闭菜单并恢复游戏，让玩家立刻看到调整结果。
        CloseBgmPanel();
    }

    private void NudgeNowPlaying(int dx, int dy)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverPosX = Mathf.Clamp(s.CoverPosX + dx, 0f, 100f);
        s.CoverPosY = Mathf.Clamp(s.CoverPosY - dy, 0f, 100f);
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateBgmPanelStatus();
    }

    private void ResizeNowPlayingCover(float delta)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverSize = Mathf.Clamp(s.CoverSize + delta, 10f, 120f);
        LimbusCompanyLyrics.SaveAllSettings();
        UpdateBgmPanelStatus();
    }

    private void ResetNowPlayingPosition()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.CoverPosX = 2f;
        s.CoverPosY = 90f;
        s.CoverSize = 32f;
        LimbusCompanyLyrics.SaveAllSettings();
        RebuildBgmPanelUi();
    }

    private void BeginMouseRebind(int action)
    {
        _bgmRebindAction = action;
        _bgmRebindStage = 2;
        UpdateBgmPanelStatus();
    }

    private static string ActionName(int action, bool zh)
    {
        if (action < 0 || action >= ActionNames.Length)
            return "-";

        return ActionNames[action][zh ? 0 : 1];
    }

    private static Key GetBinding(int action, LimbusCompanyLyricsSettings s)
    {
        switch (action)
        {
            case 0:
                return ParseKey(s?.BgmKeyToggle, Key.F5);
            case 1:
                return ParseKey(s?.BgmKeyPrev, Key.LeftBracket);
            case 2:
                return ParseKey(s?.BgmKeyNext, Key.RightBracket);
            case 3:
                return ParseKey(s?.BgmKeyReplace, Key.Enter);
            case 4:
                return ParseKey(s?.BgmKeyRestore, Key.Backspace);
            case 5:
                return ParseKey(s?.BgmKeyLyrics, Key.L);
            case 6:
                return ParseKey(s?.BgmKeyNowPlaying, Key.N);
            default:
                return ParseKey(s?.BgmKeyLanguage, Key.G);
        }
    }

    private void HandleBgmPanelKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        var settings = LimbusCompanyLyrics.Settings;

        if (Pressed(keyboard, ParseKey(settings?.BgmKeyToggle, Key.F5)))
        {
            if (_bgmPanelOpen)
            {
                CloseBgmPanel();
                return;
            }

            // 手动兜底：如果上次关闭后仍残留暂停/输入锁，F5 直接恢复而不是再次打开。
            var stateActive = false;
            try
            {
                stateActive = GameStateManager.Instance != null &&
                              GameStateManager.Instance.IsStateActive(BgmPauseStateKey);
            }
            catch (Exception)
            {
                // 忽略检测失败。
            }

            if (_bgmPaused || stateActive)
            {
                LimbusCompanyLyrics.Log?.LogWarning("F5：恢复正常游戏输入。");
                ResumeFromBgmMenu(true);
                return;
            }

            // 血宫内只使用 F4（CGME 菜单）；F5 只在关卡中可用。
            if (CanOpenBgmMenu())
                ToggleBgmPanel();
            return;
        }

        if (_bgmPanelOpen && keyboard.escapeKey.wasPressedThisFrame)
            CloseBgmPanel();
    }

    private static bool IsCyberGrindScene()
    {
        try
        {
            // ULTRAKILL 的场景资源名是哈希；SceneHelper.CurrentScene 才是 "Endless" 等友好名称。
            if (SceneHelper.CurrentScene == "Endless")
                return true;
        }
        catch (Exception)
        {
            // 忽略，继续用其它判据。
        }

        try
        {
            if (EndlessGrid.Instance != null)
                return true;
        }
        catch (Exception)
        {
            // 忽略。
        }

        var scene = SceneManager.GetActiveScene().name;
        return !string.IsNullOrEmpty(scene) &&
               scene.IndexOf("Endless", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool CanOpenBgmMenu()
    {
        try
        {
            if (IsCyberGrindScene())
                return false;

            var scene = SceneHelper.CurrentScene;
            if (scene == "Main Menu" || scene == "Bootstrap")
                return false;

            if (NewMovement.Instance == null)
                return false;

            return OptionsManager.Instance == null || !OptionsManager.Instance.paused;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>开启 F5 菜单时暂停游戏并解锁鼠标（与 CGME 菜单相同的暂停方式）。</summary>
    private void PauseForBgmMenu()
    {
        if (_bgmPaused)
            return;

        try
        {
            if (NewMovement.Instance != null)
                NewMovement.Instance.enabled = false;
            if (CameraController.Instance != null)
                CameraController.Instance.activated = false;
            if (GunControl.Instance != null)
                GunControl.Instance.activated = false;

            GameStateManager.Instance.RegisterState(new GameState(BgmPauseStateKey, new[] { _bgmPanel })
            {
                cursorLock = LockMode.Unlock,
                cameraInputLock = LockMode.Lock,
                playerInputLock = LockMode.Lock
            });

            if (OptionsManager.Instance != null)
                OptionsManager.Instance.paused = true;

            _bgmPaused = true;
            ForceCursorForMenu();
            LimbusCompanyLyrics.Log?.LogInfo("F5 菜单已暂停游戏输入。");
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("F5 菜单暂停失败，仅解锁鼠标: " + e.Message);
            if (NewMovement.Instance != null)
                NewMovement.Instance.enabled = true;
            if (CameraController.Instance != null)
                CameraController.Instance.activated = true;
            if (GunControl.Instance != null)
                GunControl.Instance.activated = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ResumeFromBgmMenu(bool force = false)
    {
        if (!_bgmPaused && !force)
            return;

        try
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.PopState(BgmPauseStateKey);
            if (OptionsManager.Instance != null)
                OptionsManager.Instance.paused = false;

            var timeController = TimeController.Instance;
            if (timeController != null && timeController.timeScale > 0f)
                Time.timeScale = timeController.timeScale * timeController.timeScaleModifier;

            if (CameraController.Instance != null)
                CameraController.Instance.activated = true;
            if (NewMovement.Instance != null)
                NewMovement.Instance.enabled = true;
            if (GunControl.Instance != null)
                GunControl.Instance.activated = true;
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("F5 菜单恢复游戏失败: " + e.Message);
        }

        if (_bgmPaused)
            LimbusCompanyLyrics.Log?.LogInfo("F5 菜单已恢复游戏输入。");
        _bgmPaused = false;
    }

    private void ToggleLanguageFromKey()
    {
        LimbusCompanyLyrics.SetLanguage(IsZh ? "en" : "zh");
        RefreshBgmUiForChange();
    }

    private static bool Pressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
            return false;

        var control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    private static Key ParseKey(string name, Key fallback)
    {
        if (!string.IsNullOrEmpty(name) && Enum.TryParse(name, true, out Key parsed) && parsed != Key.None)
            return parsed;

        return fallback;
    }

    private static string KeyLabel(Key key)
    {
        switch (key)
        {
            case Key.LeftBracket:
                return "[";
            case Key.RightBracket:
                return "]";
            case Key.Backspace:
                return "Backspace";
            case Key.Enter:
                return "Enter";
            case Key.Escape:
                return "Esc";
            case Key.LeftArrow:
                return "<-";
            case Key.RightArrow:
                return "->";
            case Key.UpArrow:
                return "^";
            case Key.DownArrow:
                return "v";
            case Key.None:
                return "-";
            default:
                return key.ToString();
        }
    }

    private void HandleRebindChoice(Keyboard keyboard, LimbusCompanyLyricsSettings settings)
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            _bgmRebindStage = 0;
            RefreshBgmUiForChange();
            return;
        }

        for (var i = 0; i < DigitKeys.Length; i++)
        {
            if (!keyboard[DigitKeys[i]].wasPressedThisFrame)
                continue;

            _bgmRebindAction = i;
            _bgmRebindStage = 2;
            RefreshBgmUiForChange();
            return;
        }
    }

    private void HandleRebindKey(Keyboard keyboard, LimbusCompanyLyricsSettings settings)
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            _bgmRebindStage = 0;
            _bgmRebindAction = 0;
            RefreshBgmUiForChange();
            return;
        }

        foreach (var control in keyboard.allKeys)
        {
            if (control == null || control.keyCode == Key.None || !control.wasPressedThisFrame)
                continue;

            if (settings != null)
            {
                var name = control.keyCode.ToString();
                switch (_bgmRebindAction)
                {
                    case 0:
                        settings.BgmKeyToggle = name;
                        break;
                    case 1:
                        settings.BgmKeyPrev = name;
                        break;
                    case 2:
                        settings.BgmKeyNext = name;
                        break;
                    case 3:
                        settings.BgmKeyReplace = name;
                        break;
                    case 4:
                        settings.BgmKeyRestore = name;
                        break;
                    case 5:
                        settings.BgmKeyLyrics = name;
                        break;
                    case 6:
                        settings.BgmKeyNowPlaying = name;
                        break;
                    case 7:
                        settings.BgmKeyLanguage = name;
                        break;
                }

                LimbusCompanyLyrics.SaveAllSettings();
            }

            _bgmRebindStage = 0;
            _bgmRebindAction = 0;
            RefreshBgmUiForChange();
            return;
        }
    }

    private void ToggleLyricsDisplay()
    {
        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null)
            return;

        settings.Enabled = !settings.Enabled;
        LimbusCompanyLyrics.SaveAllSettings();

        if (!settings.Enabled)
        {
            _alpha = 0f;
            _targetAlpha = 0f;
            _typing = false;
            _pendingText = null;
            _shownText = null;
            HideGhosts();
            if (_text != null)
                _text.text = string.Empty;
        }

        RefreshBgmUiForChange();
    }

    private void ToggleNowPlaying()
    {
        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null)
            return;

        settings.NowPlayingEnabled = !settings.NowPlayingEnabled;
        LimbusCompanyLyrics.SaveAllSettings();
        RefreshBgmUiForChange();
    }

    private void HandleNowPlayingAdjust(Keyboard keyboard, LimbusCompanyLyricsSettings settings)
    {
        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            _npAdjustTarget = 0;
            LimbusCompanyLyrics.SaveAllSettings();
            RefreshBgmUiForChange();
            return;
        }

        var step = keyboard.leftShiftKey.isPressed ? 0.25f : 1f;
        var dx = 0f;
        var dy = 0f;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            dx = -step;
        if (keyboard.rightArrowKey.wasPressedThisFrame)
            dx = step;
        if (keyboard.upArrowKey.wasPressedThisFrame)
            dy = step;
        if (keyboard.downArrowKey.wasPressedThisFrame)
            dy = -step;

        if (settings != null && (dx != 0f || dy != 0f))
        {
            settings.CoverPosX = Mathf.Clamp(settings.CoverPosX + dx, 0f, 100f);
            settings.CoverPosY = Mathf.Clamp(settings.CoverPosY - dy, 0f, 100f);
            LimbusCompanyLyrics.SaveAllSettings();
            UpdateBgmPanelStatus();
        }

        if (settings == null)
            return;

        var resize = 0f;
        if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
            resize = -2f;
        if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
            resize = 2f;

        if (resize != 0f)
        {
            settings.CoverSize = Mathf.Clamp(settings.CoverSize + resize, 80f, 600f);
            LimbusCompanyLyrics.SaveAllSettings();
            UpdateBgmPanelStatus();
        }
    }

    private float _nextBgmPanelRefresh;

    private void RefreshBgmPanelTextIfOpen()
    {
        if (!_bgmPanelOpen || Time.unscaledTime < _nextBgmPanelRefresh)
            return;

        _nextBgmPanelRefresh = Time.unscaledTime + 0.5f;
        UpdateBgmPanelStatus();
    }

    private void ToggleBgmPanel()
    {
        if (_bgmPanel == null)
            BuildBgmPanel();

        if (_bgmPanelOpen)
        {
            CloseBgmPanel();
            return;
        }

        // 血宫内禁用 F5 菜单（F4 仍由 CGME 使用）。
        if (!CanOpenBgmMenu())
            return;

        _bgmPanelOpen = true;
        _bgmPage = 0;
        _bgmListOffset = 0;
        _addingCategory = false;
        _confirmDeleteCategory = false;
        _categoryPendingDelete = null;
        _categoryNameInput = null;
        _categoryStatusText = null;
        RefreshBgmTracks();
        EnsureMouseEventSystem();

        if (_cursorRoutine == null)
            _cursorRoutine = StartCoroutine(ForceCursorLoop());

        if (_bgmPanel != null)
            _bgmPanel.SetActive(true);

        // 曲绘行置于菜单上层：用曲绘本身作为拖动/缩放把手，避免被菜单遮挡。
        if (_npRoot != null)
            _npRoot.transform.SetAsLastSibling();

        RebuildBgmPanelUi();
        PauseForBgmMenu();
    }

    private void CloseBgmPanel()
    {
        // 关闭面板时退出改键/调整模式，避免下次打开时状态残留。
        var wasEditing = _bgmRebindStage != 0 || _npAdjustTarget != 0 || _npMouseAdjust;
        _bgmPanelOpen = false;
        _bgmRebindStage = 0;
        _npAdjustTarget = 0;
        _npMouseAdjust = false;

        try
        {
            CleanupNowPlayingAdjustHint();
            _settingsUi?.CloseStandalone();

            if (_bgmPanel != null)
                _bgmPanel.SetActive(false);

            if (_cursorRoutine != null)
            {
                StopCoroutine(_cursorRoutine);
                _cursorRoutine = null;
            }
        }
        finally
        {
            // 强制恢复：即使上面任何一步抛异常，也一定解除暂停/输入锁。
            ResumeFromBgmMenu(true);
            DestroyTempEventSystem();

            if (wasEditing)
                LimbusCompanyLyrics.SaveAllSettings();
        }
    }

    private void RefreshBgmTracks()
    {
        var tracks = new List<string>();
        try
        {
            var root = PathsUtils.MusicPath;
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension == ".mp3" || extension == ".ogg" || extension == ".wav")
                        tracks.Add(file.Substring(root.Length + 1));
                }
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取 Cybergrind/Music 失败: " + e.Message);
        }

        tracks.Sort(StringComparer.OrdinalIgnoreCase);
        _bgmAllTracks = tracks.ToArray();

        LimbusCompanySongCategories.Refresh();

        var settings = LimbusCompanyLyrics.Settings;
        if (!string.IsNullOrEmpty(_bgmSelectedCategory) && !IsKnownCategory(_bgmSelectedCategory))
            _bgmSelectedCategory = null;

        if (string.IsNullOrEmpty(_bgmSelectedCategory))
            _bgmSelectedCategory = CategoryOf(settings?.BgmTrack);

        ApplyBgmSourceFilter();

        // 当前分类为空时自动切到有歌曲的分类。
        if (_bgmTracks.Length == 0 && _bgmAllTracks.Length > 0)
        {
            _bgmSelectedCategory = CategoryOf(_bgmAllTracks[0]);
            ApplyBgmSourceFilter();
        }
    }

    private static bool IsKnownCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return true;

        foreach (var known in LimbusCompanySongCategories.Categories)
        {
            if (string.Equals(known, category, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>返回歌曲所属的自定义分类；不在任何分类文件夹内则返回 null（个人）。</summary>
    private static string CategoryOf(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        string best = null;
        foreach (var category in LimbusCompanySongCategories.Categories)
        {
            if (!IsUnderCategory(relativePath, category))
                continue;

            if (best == null || category.Length > best.Length)
                best = category;
        }

        return best;
    }

    private static bool IsUnderCategory(string relativePath, string category)
    {
        return relativePath.StartsWith(category + "\\", StringComparison.OrdinalIgnoreCase) ||
               relativePath.StartsWith(category + "/", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyBgmSourceFilter()
    {
        var filtered = new List<string>();
        foreach (var track in _bgmAllTracks)
        {
            var category = CategoryOf(track);
            if (string.IsNullOrEmpty(_bgmSelectedCategory))
            {
                if (category == null)
                    filtered.Add(track);
            }
            else if (category != null &&
                     string.Equals(category, _bgmSelectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(track);
            }
        }

        _bgmTracks = filtered.ToArray();

        _bgmIndex = 0;
        if (_bgmTracks.Length == 0)
            return;

        var settings = LimbusCompanyLyrics.Settings;
        var index = settings != null ? Array.IndexOf(_bgmTracks, settings.BgmTrack) : -1;
        if (index >= 0)
            _bgmIndex = index;
        // 只是切换分类浏览时不覆盖已保存的曲目，切回另一分类时还能恢复选择。
    }

    private void SelectBgmCategory(string category)
    {
        _bgmSelectedCategory = string.IsNullOrEmpty(category) ? null : category;
        ApplyBgmSourceFilter();
        _bgmListOffset = 0;
        _categoryPage = 0;
        RebuildBgmPanelUi();
    }

    private void CycleBgmTrack(int delta)
    {
        if (_bgmTracks.Length == 0)
            return;

        _bgmIndex = (_bgmIndex + delta + _bgmTracks.Length) % _bgmTracks.Length;
        var settings = LimbusCompanyLyrics.Settings;
        if (settings != null)
            settings.BgmTrack = _bgmTracks[_bgmIndex];
        RefreshBgmUiForChange();
    }

    private void ApplySelectedBgmTrack()
    {
        if (_bgmTracks.Length == 0)
            return;

        var track = _bgmTracks[_bgmIndex];
        LimbusCompanyLyrics.Log?.LogInfo("点击取代关卡 BGM: " + track);
        var settings = LimbusCompanyLyrics.Settings;
        if (settings != null)
        {
            settings.BgmTrack = track;
            LimbusCompanyLyrics.SaveAllSettings();
        }

        StartBgmReplacement(track);
        RefreshBgmUiForChange();
    }

    private static readonly string[][] ActionNames =
    {
        new[] { "面板开关", "Open/close panel" },
        new[] { "上一首", "Previous track" },
        new[] { "下一首", "Next track" },
        new[] { "取代 BGM", "Replace BGM" },
        new[] { "归还 BGM", "Restore BGM" },
        new[] { "歌词显示", "Lyrics display" },
        new[] { "曲绘框", "Now playing frame" },
        new[] { "界面语言", "UI language" }
    };

    /// <summary>鼠标菜单状态变化后刷新：面板开启时整页重建，否则只更新状态栏。</summary>
    private void RefreshBgmUiForChange()
    {
        if (_bgmPanelOpen && _bgmPanel != null)
            RebuildBgmPanelUi();
        else
            UpdateBgmPanelStatus();
    }

    private void UpdateBgmPanelStatus()
    {
        if (_bgmStatus == null)
            return;

        var s = LimbusCompanyLyrics.Settings;
        var zh = IsZh;
        string text;

        if (_bgmRebindStage == 2)
        {
            text = (zh ? "请按下新的按键：" : "Press a new key: ") + ActionName(_bgmRebindAction, zh)
                   + (zh ? "   [Esc] 取消" : "   [Esc] cancel");
        }
        else if (_bgmRebindStage == 1)
        {
            text = zh
                ? "按 1-8 选择要改的按键（1 开关 / 2 上一首 / 3 下一首 / 4 取代 / 5 归还 / 6 歌词 / 7 曲绘 / 8 语言）"
                : "Press 1-8 to choose a binding (1 toggle / 2 prev / 3 next / 4 replace / 5 restore / 6 lyrics / 7 cover / 8 language)";
        }
        else if (_npAdjustTarget != 0)
        {
            text = (zh ? "调整曲绘行" : "Adjusting the cover row")
                   + (zh ? "：方向键移动，Shift 微调，[-]/[+] 缩放，P/Esc 完成" : ": arrows move, Shift = fine, [-]/[+] resize, P/Esc done")
                   + $"   x={s?.CoverPosX:F0}%  y={s?.CoverPosY:F0}%  size={s?.CoverSize:F0}";
        }
        else
        {
            var selected = _bgmTracks.Length > 0
                ? ShortName(_bgmTracks[Mathf.Clamp(_bgmIndex, 0, _bgmTracks.Length - 1)])
                : "-";
            text = (zh ? "选中：" : "Track: ") + selected
                   + "   |   " + BgmStatusText(zh)
                   + "   |   " + (zh ? "歌词" : "Lyrics") + ": " + (s == null || s.Enabled ? "ON" : "OFF")
                   + "   |   " + (zh ? "曲绘" : "Cover") + ": " + (s != null && s.NowPlayingEnabled ? "ON" : "OFF");
        }

        if (text != _bgmLastStatusText)
        {
            _bgmLastStatusText = text;
            _bgmStatus.font = LimbusCompanyFonts.MenuFont(text);
            _bgmStatus.text = text;
        }
    }

    private string BgmStatusText(bool zh)
    {
        if (BgmReplacing)
        {
            var current = _bgmTracks.Length > 0
                ? _bgmTracks[_bgmIndex]
                : LimbusCompanyLyrics.Settings?.BgmTrack;
            return (zh ? "正在取代：" : "Replacing: ") + ShortName(current);
        }

        if (!string.IsNullOrEmpty(BgmStatus))
            return BgmStatus;

        return zh ? "当前为关卡原 BGM" : "Level BGM active";
    }

    // ------------------------------------------------------------------ now playing (ULK-style frame)

    private void BuildNowPlayingPanel()
    {
        if (_canvas == null || _npRoot != null)
            return;

        var rowGo = new GameObject("LimbusNowPlaying", typeof(RectTransform));
        var rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.SetParent(_canvas.transform, false);
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(500f, 220f);
        rowRect.anchoredPosition = Vector2.zero;

        // 透明点击层：仅在 F5 的“显示”页启用，用于鼠标拖动整行。
        _npRowImage = rowGo.AddComponent<Image>();
        _npRowImage.color = new Color(0f, 0f, 0f, 0f);
        _npRowImage.raycastTarget = false;

        var drag = rowGo.AddComponent<NowPlayingDragHandler>();
        drag.Owner = this;

        // 曲绘框（只框住曲绘，不框整行）。
        var frameGo = new GameObject("CoverFrame", typeof(RectTransform));
        var frameRect = frameGo.GetComponent<RectTransform>();
        frameRect.SetParent(rowRect, false);
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(220f, 220f);
        frameRect.anchoredPosition = Vector2.zero;

        _npFrameImage = frameGo.AddComponent<Image>();
        _npFrameImage.color = new Color(0.02f, 0.02f, 0.02f, 0.78f);
        _npFrameImage.raycastTarget = false;

        var outline = frameGo.AddComponent<Outline>();
        // ULTRAKILL HUD 风格：浅色细边框 + 深色半透明底。
        outline.effectColor = new Color(0.92f, 0.92f, 0.92f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        // 镂空时框体填充完全透明，描边仍需独立可见。
        outline.useGraphicAlpha = false;

        var coverGo = new GameObject("NowPlayingCover", typeof(RectTransform));
        var coverRect = coverGo.GetComponent<RectTransform>();
        coverRect.SetParent(frameRect, false);
        coverRect.anchorMin = new Vector2(0.5f, 0.5f);
        coverRect.anchorMax = new Vector2(0.5f, 0.5f);
        coverRect.pivot = new Vector2(0.5f, 0.5f);
        coverRect.sizeDelta = new Vector2(208f, 208f);
        coverRect.anchoredPosition = Vector2.zero;

        _npCoverImage = coverGo.AddComponent<Image>();
        _npCoverImage.preserveAspect = true;
        _npCoverImage.raycastTarget = false;
        _npCoverImage.enabled = false;

        _npTitle = CreateNowPlayingText(rowRect, "NowPlayingTitle", new Color(1f, 0.996f, 0.937f, 1f));
        _npArtist = CreateNowPlayingText(rowRect, "NowPlayingArtist", new Color(0.82f, 0.82f, 0.84f, 1f));

        _npRoot = rowGo;
        rowGo.SetActive(false);
    }

    private TextMeshProUGUI CreateNowPlayingText(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 220f);
        rect.anchoredPosition = Vector2.zero;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = LimbusCompanyFonts.Resolve();
        text.fontSize = 24f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.richText = false;
        text.raycastTarget = false;
        text.text = string.Empty;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void UpdateNowPlayingPanel()
    {
        if (_npRoot == null)
            return;

        var settings = LimbusCompanyLyrics.Settings;
        if (settings == null || !settings.NowPlayingEnabled)
        {
            if (_npRoot.activeSelf)
                _npRoot.SetActive(false);
            return;
        }

        var song = _player?.State?.Song;
        var songPlaying = _player?.Source != null && _player.Source.isPlaying && song != null;
        var replacementPlaying = BgmReplacing && !string.IsNullOrEmpty(_bgmCurrentPath);

        if (!songPlaying && !replacementPlaying)
        {
            if (_npRoot.activeSelf)
                _npRoot.SetActive(false);
            return;
        }

        if (!_npRoot.activeSelf)
            _npRoot.SetActive(true);

        GetWindowCanvasSize(out var width, out var height);

        string title;
        string artist;

        if (replacementPlaying)
        {
            EnsureBgmTrackInfo(_bgmCurrentPath);
            title = _bgmInfoTitle;
            artist = _bgmInfoArtist;
        }
        else
        {
            var metadata = song.Metadata;
            title = metadata.Title;
            artist = metadata.Artist;

            var icon = metadata.Icon;
            // Jukebox 对没有内嵌封面的曲目会使用 music_note 占位图；此时保持镂空。
            if (icon != null && icon.name.IndexOf("music_note", StringComparison.OrdinalIgnoreCase) >= 0)
                icon = null;

            SetNowPlayingSprite(icon, false, "game:" + title);
        }

        title ??= string.Empty;
        artist ??= string.Empty;

        // cover_size 现在是文字大小；曲绘只比文字略大一圈，而不是把文字放大到曲绘尺寸。
        // 按画布高度相对 1080p 缩放，使不同宽高比下曲绘行的视觉占比一致。
        var canvasScale = Mathf.Clamp(height / 1080f, 0.65f, 1.5f);
        var fontSize = Mathf.Clamp(Mathf.Clamp(settings.CoverSize, 10f, 120f) * canvasScale, 8f, 180f);
        var size = fontSize * 1.3f;
        var changed = title != _npLastTitleText || artist != _npLastArtistText ||
                      !Mathf.Approximately(fontSize, _npLastFontSize);
        if (changed)
        {
            _npLastTitleText = title;
            _npLastArtistText = artist;
            _npLastFontSize = fontSize;

            _npTitle.font = LimbusCompanyFonts.MenuFont(title);
            _npArtist.font = LimbusCompanyFonts.MenuFont(artist);
            _npTitle.text = title;
            _npArtist.text = artist;
            _npTitle.fontSize = fontSize;
            _npArtist.fontSize = fontSize;
            _npTitle.gameObject.SetActive(title.Length > 0);
            _npArtist.gameObject.SetActive(artist.Length > 0);
            _npTitle.ForceMeshUpdate();
            _npArtist.ForceMeshUpdate();
        }

        var gap = Mathf.Max(6f, size * 0.30f);
        var titleWidth = title.Length > 0 ? Mathf.Min(_npTitle.preferredWidth, width * 0.45f) : 0f;
        var artistWidth = artist.Length > 0 ? Mathf.Min(_npArtist.preferredWidth, width * 0.30f) : 0f;
        var totalWidth = size + (titleWidth > 0f ? gap + titleWidth : 0f) +
                         (artistWidth > 0f ? gap + artistWidth : 0f);

        // cover_pos_x 表示整行左边缘（默认左下角）；使用与菜单相同的防出屏方式：
        // 始终把整行限制在屏幕安全区内，并同步回内存位置，拖动不会从屏幕外开始。
        const float margin = 12f;
        var anchorX = Mathf.Clamp(settings.CoverPosX, -100f, 200f) / 100f * width;
        var centerY = (0.5f - Mathf.Clamp(settings.CoverPosY, -100f, 200f) / 100f) * height;

        var maxLeft = Mathf.Max(margin, width - totalWidth - margin);
        var left = Mathf.Clamp(anchorX, margin, maxLeft);
        var minY = -height / 2f + size / 2f + margin;
        var maxY = height / 2f - size / 2f - margin;
        centerY = Mathf.Clamp(centerY, minY, maxY);

        settings.CoverPosX = width > 1f ? left / width * 100f : 0f;
        settings.CoverPosY = height > 1f ? (0.5f - centerY / height) * 100f : 0f;

        var rowRect = _npRoot.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(totalWidth, size);
        rowRect.anchoredPosition = new Vector2(left + totalWidth / 2f, centerY);

        var frameRect = _npFrameImage.GetComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(size, size);
        frameRect.anchoredPosition = new Vector2(-totalWidth / 2f + size / 2f, 0f);

        var pad = Mathf.Clamp(size * 0.06f, 1.5f, 6f);
        var coverRect = _npCoverImage.GetComponent<RectTransform>();
        coverRect.sizeDelta = new Vector2(size - pad * 2f, size - pad * 2f);
        coverRect.anchoredPosition = Vector2.zero;

        var x = -totalWidth / 2f + size;
        var titleRect = _npTitle.GetComponent<RectTransform>();
        if (titleWidth > 0f)
        {
            titleRect.sizeDelta = new Vector2(titleWidth, size);
            titleRect.anchoredPosition = new Vector2(x + gap + titleWidth / 2f, 0f);
            x += gap + titleWidth;
        }

        var artistRect = _npArtist.GetComponent<RectTransform>();
        if (artistWidth > 0f)
        {
            artistRect.sizeDelta = new Vector2(artistWidth, size);
            artistRect.anchoredPosition = new Vector2(x + gap + artistWidth / 2f, 0f);
        }

        // 没有曲绘时，框体透明，只留下 ULK 风格的描边。
        if (_npFrameImage != null)
        {
            var hasCover = _npCoverImage != null && _npCoverImage.enabled && _npCoverImage.sprite != null;
            _npFrameImage.color = hasCover
                ? new Color(0.02f, 0.02f, 0.02f, 0.78f)
                : new Color(0.02f, 0.02f, 0.02f, 0f);
        }

        // F5 菜单打开时，用曲绘小方框作为拖动/滚轮缩放的把手（防止整行挡住菜单）。
        if (_npRowImage != null)
            _npRowImage.raycastTarget = false;
        if (_npFrameImage != null)
            _npFrameImage.raycastTarget = _bgmPanelOpen;
    }

    private string _bgmInfoPath;
    private string _bgmInfoTitle;
    private string _bgmInfoArtist;

    private void EnsureBgmTrackInfo(string path)
    {
        if (string.IsNullOrEmpty(path) || path == _bgmInfoPath)
            return;

        _bgmInfoPath = path;
        _bgmInfoTitle = Path.GetFileNameWithoutExtension(path);
        _bgmInfoArtist = string.Empty;

        var cover = LoadFolderCover(path);
        var titleFromTag = false;

        try
        {
            using var tagFile = TagLib.File.Create(path);
            var tag = tagFile.Tag;
            if (tag != null)
            {
                if (!string.IsNullOrEmpty(tag.Title))
                {
                    _bgmInfoTitle = tag.Title;
                    titleFromTag = true;
                }

                var performer = tag.FirstPerformer;
                if (string.IsNullOrEmpty(performer))
                    performer = tag.FirstAlbumArtist;
                if (string.IsNullOrEmpty(performer))
                    performer = tag.FirstComposer;
                if (!string.IsNullOrEmpty(performer))
                    _bgmInfoArtist = performer;

                var pictures = tag.Pictures;
                LimbusCompanyLyrics.Log?.LogInfo(
                    $"曲绘标签: {Path.GetFileName(path)} pictures={pictures?.Length ?? 0} folderCover={(cover != null)}");

                if (cover == null && pictures != null && pictures.Length > 0)
                {
                    cover = CreateSprite(pictures[0].Data.Data);
                    LimbusCompanyLyrics.Log?.LogInfo("曲绘来源(ID3): " + Path.GetFileName(path)
                        + " bytes=" + pictures[0].Data.Data.Length + " sprite=" + (cover != null));
                }
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取音乐标签失败: " + e.Message);
        }

        ApplyNameFallbacks(path, ref _bgmInfoTitle, ref _bgmInfoArtist, titleFromTag);

        SetNowPlayingSprite(cover, true, path);
    }

    /// <summary>标签缺少作者/标题时，从「作者 - 曲名」文件名或歌曲文件夹名补全。</summary>
    private static void ApplyNameFallbacks(string fullPath, ref string title, ref string artist, bool titleFromTag)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        var folder = Path.GetFileName(Path.GetDirectoryName(fullPath) ?? string.Empty);

        string nameArtist = null;
        string nameTitle = null;

        if (TrySplitArtistTitle(fileName, out var fileArtist, out var fileTitle))
        {
            nameArtist = fileArtist;
            nameTitle = fileTitle;
        }
        else if (TrySplitArtistTitle(folder, out var folderArtist, out _))
        {
            nameArtist = folderArtist;
        }

        if (!titleFromTag && !string.IsNullOrEmpty(nameTitle))
            title = nameTitle;

        if (string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(nameArtist))
            artist = nameArtist;
    }

    private static bool TrySplitArtistTitle(string text, out string artist, out string title)
    {
        artist = null;
        title = null;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var separator in new[] { " - ", " – ", " — ", " － " })
        {
            var index = text.IndexOf(separator, StringComparison.Ordinal);
            if (index <= 0 || index + separator.Length >= text.Length)
                continue;

            var left = text.Substring(0, index).Trim();
            var right = text.Substring(index + separator.Length).Trim();
            if (left.Length == 0 || right.Length == 0)
                continue;

            artist = left;
            title = right;
            return true;
        }

        return false;
    }

    private void SetNowPlayingSprite(Sprite sprite, bool owned, string path)
    {
        if (ReferenceEquals(_npSprite, sprite) && _npSpritePath == path)
            return;

        if (_npSpriteOwned && _npSprite != null)
            Destroy(_npSprite);
        if (_npSpriteOwned && _npSpriteTexture != null)
            Destroy(_npSpriteTexture);

        _npSprite = sprite;
        _npSpriteTexture = owned ? sprite?.texture : null;
        _npSpriteOwned = owned && sprite != null;
        _npSpritePath = path;

        if (_npCoverImage != null)
        {
            _npCoverImage.sprite = sprite;
            _npCoverImage.enabled = sprite != null;
        }
    }

    private static readonly string[] CoverFileNames =
    {
        "cover", "folder", "front", "album", "artwork", "albumart", "thumb", "jacket", "pak"
    };

    /// <summary>在歌曲文件夹里寻找封面：同名图片 → 常见封面名 → 目录内任意图片。</summary>
    private Sprite LoadFolderCover(string audioPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(audioPath);
            var baseName = Path.GetFileNameWithoutExtension(audioPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return null;

            var extensions = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var extension in extensions)
            {
                var direct = Path.Combine(directory, baseName + extension);
                if (File.Exists(direct))
                {
                    var sprite = CreateSpriteFromFile(direct);
                    if (sprite != null)
                    {
                        LimbusCompanyLyrics.Log?.LogInfo("曲绘来源(同名): " + direct);
                        return sprite;
                    }
                }
            }

            foreach (var name in CoverFileNames)
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(directory, name + extension);
                    if (!File.Exists(candidate))
                        continue;

                    var sprite = CreateSpriteFromFile(candidate);
                    if (sprite != null)
                    {
                        LimbusCompanyLyrics.Log?.LogInfo("曲绘来源(封面名): " + candidate);
                        return sprite;
                    }
                }
            }

            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(directory, "*" + extension);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    var sprite = CreateSpriteFromFile(file);
                    if (sprite != null)
                    {
                        LimbusCompanyLyrics.Log?.LogInfo("曲绘来源(目录图片): " + file);
                        return sprite;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取曲绘文件失败: " + e.Message);
        }

        return null;
    }

    private static Sprite CreateSpriteFromFile(string path)
    {
        try
        {
            return CreateSprite(File.ReadAllBytes(path));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Sprite CreateSprite(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        Texture2D texture = null;
        try
        {
            texture = new Texture2D(2, 2);
            if (!texture.LoadImage(data))
            {
                LimbusCompanyLyrics.Log?.LogWarning("曲绘解码失败: LoadImage=false, bytes=" + (data?.Length ?? 0));
                Destroy(texture);
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            LimbusCompanyLyrics.Log?.LogInfo($"曲绘解码成功: {texture.width}x{texture.height}, bytes={data.Length}");
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("曲绘解码异常: " + e.Message);
            if (texture != null)
                Destroy(texture);
            return null;
        }
    }

    private static string TrackLabel(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return "-";

        var label = relativePath.Replace('\\', '/');
        return label.Length <= 64 ? label : label.Substring(0, 61) + "...";
    }

    private static string ShortName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "-";

        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
            name = path;
        return name.Length <= 56 ? name : name.Substring(0, 53) + "...";
    }

    private void LoadBgmSubtitleTracks(string path)
    {
        _bgmSubtitleTrack = SubtitleTrack.Empty;
        _bgmTranslationTrack = SubtitleTrack.Empty;
        _bgmSubtitleIndex = -1;
        _bgmTranslationIndex = -1;
        _bgmSubtitleText = null;
        _bgmTranslationText = null;

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var mainFile = new FileInfo(path);
            var srt = FindMainSrt(mainFile);
            if (srt != null)
                _bgmSubtitleTrack = SubtitleParser.FromFile(srt);

            var transPath = FindTranslationSidecar(srt ?? mainFile);
            if (transPath == null && srt != null)
                transPath = FindTranslationSidecar(mainFile);
            if (transPath != null)
                _bgmTranslationTrack = SubtitleParser.FromFile(new FileInfo(transPath));

            LimbusCompanyLyrics.Log?.LogInfo(
                $"关卡歌词: {srt?.FullName ?? "(无)"} (主 {_bgmSubtitleTrack.Lines.Count} 行 / 翻译 {_bgmTranslationTrack.Lines.Count} 行)");
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取关卡歌词失败: " + e.Message);
        }
    }

    /// <summary>兼容文件夹形式的歌曲：优先同名 .srt，其次文件夹同名 .srt，最后目录内任意主字幕。</summary>
    private static FileInfo FindMainSrt(FileInfo audio)
    {
        var direct = SubtitleFiles.MainSidecar(audio);
        if (direct != null && direct.Exists)
            return direct;

        var directory = audio?.DirectoryName;
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        var folderName = new DirectoryInfo(directory).Name;
        var folderSrt = Path.Combine(directory, folderName + ".srt");
        if (File.Exists(folderSrt))
            return new FileInfo(folderSrt);

        foreach (var candidate in Directory.GetFiles(directory, "*.srt"))
        {
            var name = Path.GetFileNameWithoutExtension(candidate).ToLowerInvariant();
            if (name.EndsWith(".zh") || name.EndsWith(".cn") || name.EndsWith(".chs") ||
                name.EndsWith(".cht") || name.EndsWith(".ja") || name.EndsWith(".jp") ||
                name.EndsWith(".trans") || name.EndsWith(".translation") ||
                name.EndsWith(".中文") || name.EndsWith(".翻译") ||
                name.EndsWith("_zh") || name.EndsWith("_cn") || name.EndsWith("_ja") ||
                name.EndsWith("_trans") || name.EndsWith("_translation"))
                continue;

            return new FileInfo(candidate);
        }

        return null;
    }

    /// <summary>用 F5 取代关卡 BGM 时，按音频进度显示同目录的 .srt 歌词。</summary>
    private void UpdateBgmLyrics()
    {
        if (!BgmReplacing || _bgmSource == null || !_bgmSource.isPlaying)
            return;

        var s = LimbusCompanyLyrics.Settings;
        if (s == null || !s.Enabled)
            return;

        if (_bgmSubtitleTrack == null || _bgmSubtitleTrack.Lines.Count == 0)
            return;

        var index = _bgmSubtitleTrack.IndexAt(_bgmSource.time, _bgmSubtitleIndex);
        _bgmSubtitleIndex = index;
        var text = index >= 0 ? _bgmSubtitleTrack.Lines[index].Text : string.Empty;

        // 文本变化，或歌词刚被重新启用时，重新送进歌词层。
        if (text != _bgmSubtitleText || (text.Length > 0 && _shownText == null))
        {
            _bgmSubtitleText = text;
            OnSubtitleChanged(text);
        }
    }

    /// <summary>关卡 BGM 取代时的翻译歌词。</summary>
    private void UpdateBgmTranslation(LimbusCompanyLyricsSettings s)
    {
        var available = s.Enabled && s.TranslationEnabled && _bgmSource != null &&
                        _bgmSource.isPlaying && _bgmTranslationTrack != null &&
                        _bgmTranslationTrack.Lines.Count > 0;

        if (!available)
        {
            _bgmTranslationText = null;
            _transTargetAlpha = 0f;
            return;
        }

        var index = _bgmTranslationTrack.IndexAt(_bgmSource.time, _bgmTranslationIndex);
        _bgmTranslationIndex = index;

        if (index < 0)
        {
            _bgmTranslationText = null;
            _transTargetAlpha = 0f;
            return;
        }

        var text = _bgmTranslationTrack.Lines[index].Text;
        if (text != _bgmTranslationText)
        {
            _bgmTranslationText = text;
            ShowTranslation(text);
        }
    }

    private AudioSource EnsureBgmSource()
    {
        if (_bgmSource != null)
            return _bgmSource;

        var go = new GameObject("LimbusCompanyBgmSource");
        go.transform.SetParent(transform, false);
        _bgmSource = go.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.volume = 1f;

        foreach (var theme in GetGameMusicThemes())
        {
            if (theme != null && theme.outputAudioMixerGroup != null)
            {
                _bgmSource.outputAudioMixerGroup = theme.outputAudioMixerGroup;
                break;
            }
        }

        return _bgmSource;
    }

    /// <summary>用 Cybergrind/Music 中的曲目取代当前关卡 BGM。</summary>
    public bool StartBgmReplacement(string relativePath)
    {
        LimbusCompanyLyrics.Log?.LogInfo("请求取代关卡 BGM: " + (relativePath ?? "(null)"));
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            BgmStatus = "no track selected";
            return false;
        }

        var fullPath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(PathsUtils.MusicPath, relativePath);

        if (!File.Exists(fullPath))
        {
            BgmStatus = "file not found: " + relativePath;
            LimbusCompanyLyrics.Log?.LogWarning("BGM 取代失败，文件不存在: " + fullPath);
            return false;
        }

        StopBgmReplacement();
        EnsureBgmSource();
        BgmReplacing = true;
        _bgmCurrentPath = fullPath;
        UpdateNowPlayingPanel();
        LoadBgmSubtitleTracks(fullPath);
        BgmStatus = "loading: " + relativePath;
        PauseGameMusic();

        _bgmLoadRoutine = StartCoroutine(LoadBgmClip(fullPath, relativePath));
        return true;
    }

    private IEnumerator LoadBgmClip(string fullPath, string displayName)
    {
        using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(fullPath).AbsoluteUri, AudioTypeFor(fullPath)))
        {
            yield return request.SendWebRequest();

            if (!BgmReplacing)
                yield break;

            if (request.isNetworkError || request.isHttpError)
            {
                BgmStatus = "load failed: " + request.error;
                LimbusCompanyLyrics.Log?.LogWarning("BGM 读取失败: " + request.error);
                StopBgmReplacement();
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                BgmStatus = "load failed (empty clip)";
                StopBgmReplacement();
                yield break;
            }

            var source = EnsureBgmSource();
            var old = source.clip;
            // 加前缀，避免 USTManager 按 clip 名把 F5 替代曲误当成游戏音乐再替换一次。
            if (!clip.name.StartsWith("LCCB_", StringComparison.Ordinal))
                clip.name = "LCCB_" + clip.name;
            source.clip = clip;
            source.loop = true;
            source.volume = 1f;
            source.Play();
            if (old != null)
                Destroy(old);

            BgmStatus = "replacing: " + displayName;
            LimbusCompanyLyrics.Log?.LogInfo("BGM 取代: " + fullPath);
        }

        _bgmLoadRoutine = null;
    }

    /// <summary>归还关卡 BGM，并从取代前的进度继续。</summary>
    public void StopBgmReplacement()
    {
        BgmReplacing = false;
        _bgmCurrentPath = null;
        // 下次取代时重新读取标签/曲绘（旧曲绘可能已随曲目切换销毁）。
        _bgmInfoPath = null;

        // 结束关卡歌词/翻译的显示。
        _bgmSubtitleTrack = SubtitleTrack.Empty;
        _bgmTranslationTrack = SubtitleTrack.Empty;
        _bgmSubtitleIndex = -1;
        _bgmTranslationIndex = -1;
        _bgmSubtitleText = null;
        _bgmTranslationText = null;
        _translationIndex = -1;
        _transTargetAlpha = 0f;
        OnSubtitleChanged(string.Empty);

        if (_bgmLoadRoutine != null)
        {
            StopCoroutine(_bgmLoadRoutine);
            _bgmLoadRoutine = null;
        }

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            var clip = _bgmSource.clip;
            _bgmSource.clip = null;
            if (clip != null)
                Destroy(clip);
        }

        foreach (var pair in _themeMuteState)
        {
            if (pair.Key == null)
                continue;

            pair.Key.mute = pair.Value.Mute;
            pair.Key.volume = pair.Value.Volume;
        }
        _themeMuteState.Clear();
        _sceneThemeSources.Clear();

        if (_musicManagerOffSaved && _musicManagerInstance != null && _musicManagerOffField != null)
        {
            try
            {
                _musicManagerOffField.SetValue(_musicManagerInstance, _musicManagerOffOriginal);
            }
            catch (Exception)
            {
                // 忽略反射失败。
            }
        }
        _musicManagerOffSaved = false;

        _pausedThemes.RemoveAll(src => src == null);
        foreach (var source in _pausedThemes)
        {
            if (source != null)
                source.UnPause();
        }
        _pausedThemes.Clear();

        BgmStatus = "restored level BGM";
    }

    private void PauseGameMusic()
    {
        RefreshSceneThemeSources(true);
        ForceMusicManagerOff();

        var themes = GetGameMusicThemes();
        LimbusCompanyLyrics.Log?.LogInfo(
            $"暂停关卡 BGM 音源: MusicManager={themes.Length}, 场景内主题音源={_sceneThemeSources.Count}");

        foreach (var source in themes)
        {
            LimbusCompanyLyrics.Log?.LogInfo(
                $"  MusicManager 音源: {source.gameObject.name} clip={source.clip?.name ?? "(null)"}");
            PauseGameTheme(source);
        }
        foreach (var source in _sceneThemeSources)
        {
            LimbusCompanyLyrics.Log?.LogInfo(
                $"  场景主题音源: {source.gameObject.name} clip={source.clip?.name ?? "(null)"}");
            PauseGameTheme(source);
        }
    }

    private void KeepGameMusicPaused()
    {
        _pausedThemes.RemoveAll(src => src == null);

        // 每帧把 MusicManager.off 置为 true：MusicManager.Update 会把所有主题音量清零，
        // 这样即使某个多段音源没有被上面的列表收集到，也不会发声。
        ForceMusicManagerOff();

        // 取代期间每帧重新收集 MusicManager 音源，并定期扫描场景里
        // 不在 MusicManager 下的 CleanTheme / BattleTheme / BossTheme 音源。
        RefreshSceneThemeSources();
        foreach (var source in GetGameMusicThemes(true))
            PauseGameTheme(source);
        foreach (var source in _sceneThemeSources)
            PauseGameTheme(source);
    }

    /// <summary>取代期间强制关闭 MusicManager，阻止它继续驱动多段关卡 BGM。</summary>
    private void ForceMusicManagerOff()
    {
        try
        {
            if (_musicManagerInstance == null || _musicManagerOffField == null)
            {
                var type = Type.GetType("MusicManager, Assembly-CSharp");
                if (type == null)
                    return;

                PropertyInfo instanceProperty = null;
                for (var current = type; current != null && instanceProperty == null; current = current.BaseType)
                    instanceProperty = current.GetProperty("Instance",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                _musicManagerInstance = instanceProperty?.GetValue(null);
                if (_musicManagerInstance == null)
                    _musicManagerInstance = FindObjectOfType(type);

                _musicManagerOffField = type.GetField("off",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
        catch (Exception)
        {
            return;
        }

        if (_musicManagerInstance == null || _musicManagerOffField == null)
            return;

        try
        {
            if (!_musicManagerOffSaved)
            {
                _musicManagerOffOriginal = (bool)_musicManagerOffField.GetValue(_musicManagerInstance);
                _musicManagerOffSaved = true;
            }

            _musicManagerOffField.SetValue(_musicManagerInstance, true);
        }
        catch (Exception)
        {
            // 忽略反射失败。
        }
    }

    /// <summary>扫描场景里以 CleanTheme/BattleTheme/BossTheme 为 clip 的音源（多段关卡 BGM 可能不在 MusicManager 下）。</summary>
    private void RefreshSceneThemeSources(bool force = false)
    {
        _sceneThemeSources.RemoveAll(src => src == null);

        if (!force && Time.unscaledTime < _nextSceneThemeScan)
            return;

        _nextSceneThemeScan = Time.unscaledTime + 0.5f;

        try
        {
            foreach (var source in FindObjectsOfType<AudioSource>())
            {
                if (source == null || source.clip == null)
                    continue;

                var clipName = source.clip.name ?? string.Empty;
                if (!clipName.Equals("CleanTheme", StringComparison.OrdinalIgnoreCase) &&
                    !clipName.Equals("BattleTheme", StringComparison.OrdinalIgnoreCase) &&
                    !clipName.Equals("BossTheme", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!_sceneThemeSources.Contains(source))
                    _sceneThemeSources.Add(source);
            }
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("扫描场景音乐音源失败: " + e.Message);
        }
    }

    private void PauseGameTheme(AudioSource source)
    {
        if (source == null)
            return;

        // 静音 + 音量归零，挡住 USTManager / MusicChanger 在同一帧重新 Play 的多段 BGM。
        if (!_themeMuteState.ContainsKey(source))
            _themeMuteState[source] = new ThemeState { Mute = source.mute, Volume = source.volume };
        source.mute = true;
        source.volume = 0f;

        // 只记录真正在播放的音源，归还时不会把原本就暂停的音乐错误地唤醒。
        if (source.isPlaying)
        {
            source.Pause();
            if (!_pausedThemes.Contains(source))
                _pausedThemes.Add(source);
        }
    }

    private static AudioType AudioTypeFor(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".ogg":
                return AudioType.OGGVORBIS;
            case ".wav":
                return AudioType.WAV;
            default:
                return AudioType.MPEG;
        }
    }

    /// <summary>反射取得 MusicManager 的 battle/clean/boss AudioSource。</summary>
    private AudioSource[] GetGameMusicThemes(bool force = false)
    {
        if (!force && Time.unscaledTime < _nextMusicThemeScan)
            return _musicThemesCache;

        _nextMusicThemeScan = Time.unscaledTime + 0.25f;

        try
        {
            var type = Type.GetType("MusicManager, Assembly-CSharp");
            if (type == null)
                return _musicThemesCache = Array.Empty<AudioSource>();

            PropertyInfo instanceProperty = null;
            for (var current = type; current != null && instanceProperty == null; current = current.BaseType)
                instanceProperty = current.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var instance = instanceProperty?.GetValue(null);
            if (instance == null && Time.unscaledTime >= _nextManagerFind)
            {
                // Instance 尚未建立时，退回到场景中查找 MusicManager。
                _nextManagerFind = Time.unscaledTime + 1f;
                instance = FindObjectOfType(type);
            }
            if (instance == null)
                return _musicThemesCache = Array.Empty<AudioSource>();

            var field = type.GetField("allThemes",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var themes = field?.GetValue(instance) as AudioSource[];

            var list = new List<AudioSource>();
            if (themes != null)
            {
                foreach (var theme in themes)
                    if (theme != null && !list.Contains(theme))
                        list.Add(theme);
            }

            // allThemes 可能为空/尚未初始化；显式读取 clean / battle / boss / target 四段音源。
            foreach (var themeName in new[] { "cleanTheme", "battleTheme", "bossTheme", "targetTheme" })
            {
                var themeField = type.GetField(themeName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (themeField?.GetValue(instance) is AudioSource themeSource &&
                    themeSource != null && !list.Contains(themeSource))
                    list.Add(themeSource);
            }

            // 多段 BGM / USTManager 可能创建或替换 MusicManager 下的其它 AudioSource。
            if (instance is Component component)
            {
                foreach (var source in component.GetComponentsInChildren<AudioSource>(true))
                    if (source != null && !list.Contains(source))
                        list.Add(source);
            }

            return _musicThemesCache = list.ToArray();
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log?.LogWarning("读取关卡音乐失败: " + e.Message);
            return _musicThemesCache = Array.Empty<AudioSource>();
        }
    }

    private AudioSource GetVisualizerSource(LimbusCompanyLyricsSettings s)
    {
        if (s == null || !s.VisualizerEnabled)
            return null;

        if (BgmReplacing && _bgmSource != null && _bgmSource.isPlaying)
            return _bgmSource;

        if (_player != null && _player.Source != null && _player.Source.isPlaying)
            return _player.Source;

        AudioSource best = null;
        var bestVolume = -1f;
        foreach (var source in GetGameMusicThemes())
        {
            if (source == null || !source.isPlaying)
                continue;

            if (source.volume > bestVolume)
            {
                best = source;
                bestVolume = source.volume;
            }
        }

        return best;
    }

    private void UpdateVisualizer()
    {
        if (_visRoot == null)
            return;

        var s = LimbusCompanyLyrics.Settings;
        var spectrumSource = GetVisualizerSource(s);
        var show = s != null && s.VisualizerEnabled && spectrumSource != null;
        if (!show)
        {
            if (_visRoot.activeSelf)
                _visRoot.SetActive(false);
            return;
        }

        if (!_visRoot.activeSelf)
            _visRoot.SetActive(true);

        spectrumSource.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        var rootRect = _visRoot.GetComponent<RectTransform>();
        var size = rootRect.rect.size;
        var width = size.x > 0f ? size.x : Screen.width;
        var height = size.y > 0f ? size.y : Screen.height;

        var position = LimbusCompanyLyricsSettings.NormalizePosition(s.VisualizerPosition);
        var horizontal = position == "bottom" || position == "top";
        var maxExtent = (horizontal ? height : width) * Mathf.Max(1f, s.VisualizerSize) / 100f;
        var amplitude = Mathf.Clamp(s.VisualizerAmplitude, 0.1f, 5f);
        var count = Mathf.Clamp(s.VisualizerBars, 8, _visBars.Length);
        const float spreadMargin = 16f;
        var span = (horizontal ? width : height) - spreadMargin * 2f;
        var step = span / count;
        var thickness = Mathf.Max(1.5f, step * 0.62f);

        for (var i = 0; i < _visBars.Length; i++)
        {
            var used = i < count;
            if (_visBars[i].gameObject.activeSelf != used)
                _visBars[i].gameObject.SetActive(used);
            if (!used)
                continue;

            var band = s.VisualizerMirrorX ? Mathf.Min(i, count - 1 - i) : i;
            var start = band * _spectrum.Length / count;
            var end = Mathf.Max(start + 1, (band + 1) * _spectrum.Length / count);
            float sum = 0f;
            for (var j = start; j < end; j++)
                sum += _spectrum[j];

            var average = sum / (end - start);
            var value = Mathf.Clamp01(Mathf.Sqrt(average * 100f) * amplitude);
            var extent = value * maxExtent;

            var bar = _visBars[i];
            var range = Mathf.Clamp(s.VisualizerGradientRange, 0f, 1f);
            var mode = LimbusCompanyLyricsSettings.NormalizeGradientMode(s.VisualizerGradientMode);
            var colorA = ParseColor(s.VisualizerColorA, new Color(0.847f, 0.647f, 0.137f));
            var colorB = ParseColor(s.VisualizerColorB, new Color(0f, 0.898f, 1f));
            var spread = count > 1 ? (float)band / (count - 1) : 0f;

            if (range <= 0.001f)
            {
                colorA.a = 0.9f;
                bar.color = colorA;
            }
            else
            {
                // 所有模式都像区间渐变一样：每根柱子有自己的相位，颜色随时间变化。
                var speed = Mathf.Clamp(s.VisualizerGradientSpeed, 0f, 2f);
                var phase = Mathf.Repeat(Time.unscaledTime * speed + spread * range, 1f);

                if (mode == "multi")
                {
                    // 多个 RGB 色标，空格分隔；整排按顺序流动经过这些颜色。
                    var stops = ParseColorList(s.VisualizerColors);
                    Color custom;
                    if (stops.Count == 0)
                        custom = colorA;
                    else if (stops.Count == 1)
                        custom = stops[0];
                    else
                        custom = SampleColorStops(stops, phase);

                    custom.a = 0.9f;
                    bar.color = custom;
                }
                else
                {
                    // fixed2：在两个颜色之间单向流动（循环）。
                    // range：在两个颜色之间来回渐变。
                    var t = mode == "fixed2"
                        ? phase
                        : (phase <= 0.5f ? phase * 2f : (1f - phase) * 2f);

                    var custom = Color.Lerp(colorA, colorB, t);
                    custom.a = 0.9f;
                    bar.color = custom;
                }
            }

            var rect = bar.rectTransform;
            if (horizontal)
            {
                var x = -width * 0.5f + spreadMargin + step * (i + 0.5f);
                if (position == "bottom")
                {
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(x, -height * 0.5f);
                }
                else
                {
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(x, height * 0.5f);
                }

                rect.sizeDelta = new Vector2(thickness, extent);
            }
            else
            {
                var y = -height * 0.5f + spreadMargin + step * (i + 0.5f);
                if (position == "left")
                {
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = new Vector2(-width * 0.5f, y);
                }
                else
                {
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = new Vector2(width * 0.5f, y);
                }

                rect.sizeDelta = new Vector2(extent, thickness);
            }
        }
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

    // ------------------------------------------------------------------ style

    private void ApplyStyle()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _text == null)
            return;

        ApplyMainStyle();
        ApplyTransStyle();

        // 字号变化后重新量取尺寸并重新钳制/避让
        if (_shownText != null)
        {
            _text.ForceMeshUpdate(false, false);
            _halfExtents = new Vector2(Mathf.Max(0f, _text.preferredWidth) * 0.5f,
                Mathf.Max(0f, _text.preferredHeight) * 0.5f);

            var canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>().rect : new Rect();
            var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
            var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;
            _basePosition = RandomMainPosition(s, width, height);
            _mainCurrentPosition = _basePosition;
            _rect.anchoredPosition = _basePosition;
        }

        if (_transShown != null && _transText != null)
        {
            _transText.ForceMeshUpdate(false, false);
            _transHalfExtents = new Vector2(Mathf.Max(0f, _transText.preferredWidth) * 0.5f,
                Mathf.Max(0f, _transText.preferredHeight) * 0.5f);
            PlaceTranslation();
        }
    }

    private void ApplyMainStyle()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _text == null)
            return;

        var baseSize = Mathf.Clamp(s.FontSize * s.Zoom / 100f, 1f, 400f);
        ApplyTextStyle(_text, s, baseSize, ParseColor(s.TextColor, Color.white));
    }

    private void ApplyTransStyle()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _transText == null)
            return;

        var transSize = s.TranslationFontSize > 0f
            ? Mathf.Clamp(s.TranslationFontSize * s.Zoom / 100f, 1f, 400f)
            : Mathf.Clamp(s.FontSize * s.Zoom / 100f * s.TranslationScale, 1f, 400f);
        ApplyTextStyle(_transText, s, transSize, ParseColor(s.TranslationColor, Color.white));
    }

    private static void ApplyTextStyle(TMP_Text text, LimbusCompanyLyricsSettings s, float fontSize, Color color)
    {
        if (text == null)
            return;

        text.fontSize = Mathf.Clamp(fontSize, 1f, 400f);
        text.characterSpacing = s.Spacing;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;

        if (text.font == null)
            return;

        var mat = text.fontMaterial;
        if (mat == null)
            return;

        mat.EnableKeyword("OUTLINE_ON");
        mat.SetColor("_OutlineColor", ParseColor(s.StrokeColor, Color.white));
        mat.SetFloat("_OutlineWidth", Mathf.Clamp(s.StrokeWidth / 12f, 0f, 1f));

        if (s.GlowEnabled)
        {
            var glow = ParseColor(s.GlowColor, Color.white);
            glow.a = Mathf.Clamp01(s.GlowAlpha / 100f);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", glow);
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", 0f);
            mat.SetFloat("_UnderlayDilate", Mathf.Clamp01(s.GlowSize / 40f));
            mat.SetFloat("_UnderlaySoftness", 0.55f);
        }
        else
        {
            mat.DisableKeyword("UNDERLAY_ON");
        }

        text.UpdateMeshPadding();
        text.havePropertiesChanged = true;
    }

    /// <summary>
    /// 把 U+F900–U+FAFF 的 CJK 兼容汉字（如 U+F9EC “溺”）转成标准汉字（溺），
    /// 避免字体缺字时在抖动/重绘中反复闪现。
    /// </summary>
    private static string NormalizeLyricText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string normalized;
        try
        {
            normalized = text.Normalize(NormalizationForm.FormKC);
        }
        catch (Exception)
        {
            normalized = text;
        }

        var builder = new StringBuilder(normalized.Length);
        var changed = !string.Equals(normalized, text, StringComparison.Ordinal);

        foreach (var c in normalized)
        {
            if (c == '\r')
            {
                changed = true;
                continue;
            }

            if (c == '\n' || c == '\t')
            {
                builder.Append(c);
                continue;
            }

            // 控制字符、替换符、变体选择符（常见于从音乐平台复制的歌词）直接去掉
            if (c < 0x20 || c == '�' || (c >= 0xFE00 && c <= 0xFE0F))
            {
                changed = true;
                continue;
            }

            builder.Append(c);
        }

        var result = builder.ToString();
        if (changed)
            LimbusCompanyLyrics.Log?.LogInfo($"歌词字符已归一化: \"{text}\" -> \"{result}\"");

        return result;
    }

    private static List<Color> ParseColorList(string text)
    {
        var colors = new List<Color>();
        if (string.IsNullOrEmpty(text))
            return colors;

        var tokens = text.Split(new[] { ' ', '\t', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            var parts = token.Replace('，', ',').Split(',');
            if (parts.Length >= 3 &&
                int.TryParse(parts[0].Trim(), out var r) &&
                int.TryParse(parts[1].Trim(), out var g) &&
                int.TryParse(parts[2].Trim(), out var b))
            {
                colors.Add(new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f));
            }
            else if (ColorUtility.TryParseHtmlString(token, out var c))
            {
                c.a = 1f;
                colors.Add(c);
            }
        }

        return colors;
    }

    private static Color SampleColorStops(List<Color> stops, float t)
    {
        if (stops.Count == 1)
            return stops[0];

        t = Mathf.Clamp01(t) * (stops.Count - 1);
        var index = Mathf.Min(Mathf.FloorToInt(t), stops.Count - 2);
        return Color.Lerp(stops[index], stops[index + 1], t - index);
    }

    private static Color ParseColor(string html, Color fallback)
    {
        if (!string.IsNullOrEmpty(html))
        {
            html = html.Trim().Replace('，', ',');

            // RGB triplet, e.g. "255,211,77".
            if (html.Contains(","))
            {
                var parts = html.Split(',');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0].Trim(), out var r) &&
                    int.TryParse(parts[1].Trim(), out var g) &&
                    int.TryParse(parts[2].Trim(), out var b))
                {
                    return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f);
                }
            }

            if (ColorUtility.TryParseHtmlString(html, out var c))
            {
                c.a = 1f;
                return c;
            }
        }

        return fallback;
    }

    // ------------------------------------------------------------------ player

    private void PollPlayer()
    {
        if (_player == null)
        {
            BuildUi();

            // 关卡内没有 JukeboxMusicPlayer；使用 F5 取代 BGM 时歌词由字幕文件驱动，
            // 不能在每帧把它清掉。
            if (!BgmReplacing)
                HideImmediate();

            if (Time.unscaledTime < _nextPlayerScan)
                return;

            _nextPlayerScan = Time.unscaledTime + 0.75f;
            var found = FindObjectOfType<JukeboxMusicPlayer>();
            if (found == null)
                return;

            _player = found;
            _player.OnSubtitleChanged += OnSubtitleChanged;
            _player.OnStop += OnStopped;
            LimbusCompanyLyrics.Log.LogInfo("LimbusCompanyLyrics 已连接 Jukebox 播放器。");
            return;
        }

        _nextPlayerScan = Time.unscaledTime + 0.75f;
    }

    private void PollConfig()
    {
        if (Time.unscaledTime < _nextConfigCheck)
            return;

        _nextConfigCheck = Time.unscaledTime + 1f;
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || !s.HasFileChanged())
            return;

        LimbusCompanyLyrics.Reload();
        ApplyStyle();
    }

    private void CheckOverlapDiagnostics()
    {
        if (Time.unscaledTime < _nextOverlapCheck)
            return;

        _nextOverlapCheck = Time.unscaledTime + 1f;

        // 诊断：屏幕上是否还有另一处文本正在显示同一句歌词（原版面板/其他 HUD）。
        if (Time.unscaledTime >= _nextTextScan)
        {
            _nextTextScan = Time.unscaledTime + 2f;
            _allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        }

        var current = _shownText;
        if (!string.IsNullOrEmpty(current) && _lastOtherText != current)
        {
            foreach (var t in _allTexts)
            {
                if (t == null || t == _text || t == _transText || !t.gameObject.scene.IsValid())
                    continue;

                if (t.gameObject.name == "LyricGhost")
                    continue;

                if (string.IsNullOrEmpty(t.text) ||
                    t.text.IndexOf(current, StringComparison.Ordinal) < 0)
                    continue;

                _lastOtherText = current;
                LimbusCompanyLyrics.Log?.LogWarning(
                    $"检测到另一处相同歌词文本 \"{t.text}\" 来自 {t.gameObject.name}");
                break;
            }
        }

        // 新句是否仍然压在某个正在渐隐的旧句残影上。
        if (_shownText != null)
        {
            foreach (var g in _ghosts)
            {
                if (g.Go == null || !g.Go.activeSelf || g.Alpha <= 0.05f)
                    continue;

                if (RectOverlaps(g.Rect.anchoredPosition, g.Half, g.Angle,
                        _mainCurrentPosition, _halfExtents, _lineAngle, 12f))
                {
                    LimbusCompanyLyrics.Log?.LogWarning(
                        $"新歌词与渐隐残影仍重叠 main={_mainCurrentPosition} ghost={g.Rect.anchoredPosition}");
                }
            }
        }

        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _transShown == null || _shownText == null || _transRect == null)
            return;

        if (RectOverlaps(_transRect.anchoredPosition, _transHalfExtents, _transAngle,
                _mainCurrentPosition, _halfExtents, _lineAngle, 12f))
        {
            LimbusCompanyLyrics.Log?.LogWarning(
                $"仍检测到歌词重叠 mode={s.AntiOverlapMode} main={_mainCurrentPosition} trans={_transRect.anchoredPosition}");
        }
    }

    private void PollPanelSuppression()
    {
        if (!LimbusCompanyLyrics.SuppressOriginal)
            return;

        // 每 0.5 秒重新收集所有字幕面板（含 Thorn HUD 模块实例化出来的那份），
        // 逐个清空并禁用，保证屏幕上只剩我们这一套歌词。
        if (Time.unscaledTime >= _nextPanelScan)
        {
            _nextPanelScan = Time.unscaledTime + 0.25f;
            _panels = Resources.FindObjectsOfTypeAll<SubtitlesPanel>();

            if (_panels.Length != _lastPanelCount)
            {
                _lastPanelCount = _panels.Length;
                LimbusCompanyLyrics.Log?.LogInfo($"原版字幕面板数量: {_panels.Length}");
            }
        }

        string found = null;
        foreach (var panel in _panels)
        {
            if (panel == null || !panel.gameObject.scene.IsValid())
                continue;

            if (panel.text != null && !string.IsNullOrEmpty(panel.text.text))
            {
                found = (found == null ? "" : found + " | ") +
                        panel.gameObject.name + ":" + panel.text.text;
                panel.text.text = string.Empty;
            }

            if (panel.enabled)
                panel.enabled = false;

            if (panel.panelGroup != null && panel.panelGroup.alpha > 0f)
                panel.panelGroup.alpha = 0f;
        }

        if (found != null && Time.unscaledTime >= _nextPanelLog)
        {
            _nextPanelLog = Time.unscaledTime + 1f;
            LimbusCompanyLyrics.Log?.LogWarning($"原版字幕面板仍在显示，已清空: {found}");
        }
    }

    private void OnSubtitleChanged(string contents)
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || !s.Enabled)
            return;

        if (string.IsNullOrEmpty(contents))
        {
            _pendingText = null;
            _targetAlpha = 0f;
            return;
        }

        if (s.Delay > 0)
        {
            _pendingText = contents;
            _pendingAt = Time.unscaledTime + s.Delay / 1000f;
        }
        else
        {
            BeginLine(contents);
        }
    }

    private void OnStopped()
    {
        _pendingText = null;
        _targetAlpha = 0f;
        _transTargetAlpha = 0f;
        _currentSong = null;
        _translationTrack = null;
        _translationIndex = -1;
        HideImmediate();
        _recent.Clear();
    }

    /// <summary>切歌时彻底清空上一首的歌词、残影与位置历史。</summary>
    private void ResetForSongSwitch()
    {
        HideImmediate();
        _recent.Clear();
        _translationTrack = null;
        _translationIndex = -1;
        _transTargetAlpha = 0f;
        _transShown = null;
        _transTyping = false;
        _hasLastPosition = false;
    }

    private void HideImmediate()
    {
        HideGhosts();

        if (_text != null && _shownText != null)
            _text.text = string.Empty;

        _shownText = null;
        _pendingText = null;
        _hasLastPosition = false;
        _alpha = 0f;
        _targetAlpha = 0f;
        if (_group != null)
            _group.alpha = 0f;

        if (_transText != null)
        {
            _transText.text = string.Empty;
            _transText.maxVisibleCharacters = 99999;
        }
        _transShown = null;
        _transTyping = false;
        _transAlpha = 0f;
        _transTargetAlpha = 0f;
        if (_transGroup != null)
            _transGroup.alpha = 0f;

        if (_text != null)
            _text.maxVisibleCharacters = 99999;
        _typing = false;
    }

    private GhostLine CreateGhost()
    {
        var go = new GameObject("LyricGhost", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(_canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1800f, 600f);

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.richText = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.text = string.Empty;

        go.SetActive(false);
        return new GhostLine { Go = go, Rect = rect, Text = text, Group = group };
    }

    /// <summary>把当前句复制成残影，让它按原来的位置和透明度继续渐隐。</summary>
    private void SpawnGhost()
    {
        if (_canvas == null || _text == null || _group == null || _rect == null)
            return;

        if (_shownText == null || _alpha <= 0.02f)
            return;

        GhostLine ghost = null;
        foreach (var g in _ghosts)
        {
            if (!g.Go.activeSelf)
            {
                ghost = g;
                break;
            }
        }

        if (ghost == null)
        {
            if (_ghosts.Count < MaxGhosts)
            {
                ghost = CreateGhost();
                _ghosts.Add(ghost);
            }
            else
            {
                ghost = _ghosts[0];
                foreach (var g in _ghosts)
                {
                    if (g.Alpha < ghost.Alpha)
                        ghost = g;
                }
            }
        }

        var s = LimbusCompanyLyrics.Settings;
        ghost.Go.SetActive(true);
        ghost.Rect.SetSiblingIndex(0);
        ghost.Text.font = _text.font;
        if (s != null)
            ApplyTextStyle(ghost.Text, s, _text.fontSize, _text.color);

        ghost.Text.text = _text.text;
        ghost.Text.maxVisibleCharacters = _text.maxVisibleCharacters;
        ghost.Text.ForceMeshUpdate(false, false);
        ghost.Rect.anchoredPosition = _rect.anchoredPosition;
        ghost.Rect.localEulerAngles = _rect.localEulerAngles;
        ghost.Rect.localScale = Vector3.one;
        ghost.Alpha = _group.alpha;
        ghost.Group.alpha = ghost.Alpha;
        ghost.Half = _halfExtents;
        ghost.Angle = _lineAngle;
        ghost.Elapsed = _elapsed;
        ghost.BasePosition = _basePosition;

        LimbusCompanyLyrics.Log?.LogInfo(
            $"残影 \"{_shownText}\" pos={ghost.Rect.anchoredPosition} alpha={ghost.Alpha:F2}");
    }

    private void UpdateGhosts(LimbusCompanyLyricsSettings s, float dt)
    {
        var speed = Mathf.Max(0.1f, s.FadeSpeed);
        foreach (var g in _ghosts)
        {
            if (g.Go == null || !g.Go.activeSelf)
                continue;

            g.Alpha = Mathf.MoveTowards(g.Alpha, 0f, dt * speed);
            g.Group.alpha = g.Alpha;
            if (g.Alpha <= 0.001f)
            {
                g.Text.text = string.Empty;
                g.Go.SetActive(false);
            }
        }
    }

    private void HideGhosts()
    {
        foreach (var g in _ghosts)
        {
            if (g.Go != null && g.Go.activeSelf)
                g.Go.SetActive(false);
            g.Alpha = 0f;
            if (g.Text != null)
                g.Text.text = string.Empty;
        }
    }

    private void BeginLine(string contents)
    {
        contents = NormalizeLyricText(contents);

        // 新句出现前，把仍然可见的上一句留在原位继续渐隐（不会被新句的位置覆盖）。
        SpawnGhost();

        _pendingText = null;
        _shownText = contents;
        _elapsed = 0f;
        _alpha = 0f;
        _targetAlpha = 1f;

        var settings = LimbusCompanyLyrics.Settings;

        // 先把上一句彻底隐藏，再写入新文字，避免逐字计数/旧网格残留一帧造成“双行”。
        _typing = settings != null && settings.TypewriterEnabled;
        if (_group != null)
            _group.alpha = 0f;
        _text.maxVisibleCharacters = _typing ? 0 : 99999;
        _text.text = string.Empty;
        _text.ForceMeshUpdate(false, false);

        _text.text = contents;
        ApplyFontFor(_text, contents);
        ApplyMainStyle();
        _text.ForceMeshUpdate(false, false);
        _halfExtents = new Vector2(Mathf.Max(0f, _text.preferredWidth) * 0.5f,
            Mathf.Max(0f, _text.preferredHeight) * 0.5f);

        _typeElapsed = 0f;
        _text.maxVisibleCharacters = _typing ? 0 : 99999;

        ApplyPlacement();

        if (_group != null)
            _group.alpha = 0f;

        if (_transShown != null)
            PlaceTranslation();

        LimbusCompanyLyrics.Log?.LogInfo(
            $"歌词 \"{contents}\" pos={_basePosition} mode={settings?.AntiOverlapMode}");
    }

    // ------------------------------------------------------------------ anim

    private void Animate()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _text == null)
            return;

        var dt = Time.unscaledDeltaTime;

        if (!s.Enabled)
        {
            HideImmediate();
            return;
        }

        UpdateGhosts(s, dt);

        if (_pendingText != null && Time.unscaledTime >= _pendingAt)
            BeginLine(_pendingText);

        if (!Mathf.Approximately(_alpha, _targetAlpha))
        {
            _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, dt * s.FadeSpeed);
            _group.alpha = _alpha;
        }

        if (_targetAlpha <= 0f && _alpha <= 0.001f && _shownText != null)
        {
            _text.text = string.Empty;
            _shownText = null;
        }

        if (_shownText == null)
            return;

        _elapsed += dt;

        var rise = Vector2.zero;
        if (!Mathf.Approximately(s.RiseSpeed, 0f))
            rise.y = s.RiseSpeed * _elapsed;

        var mainPosition = ClampToScreen(_basePosition + rise);
        _rect.anchoredPosition = mainPosition;
        _mainCurrentPosition = mainPosition;

        if (_typing && _shownText != null)
        {
            _typeElapsed += dt;
            var count = Mathf.FloorToInt(_typeElapsed * Mathf.Max(1f, s.TypewriterSpeed));
            if (count >= _shownText.Length)
            {
                _text.maxVisibleCharacters = 99999;
                _typing = false;
            }
            else
            {
                _text.maxVisibleCharacters = count;
            }
        }

        FollowMainWithTranslation();

        ApplyShake(s);
    }

    private void FollowMainWithTranslation()
    {
        if (_transShown == null || _transRect == null)
            return;

        const float hardGap = 12f;

        // 翻译固定在下半区，主歌词永远在上半区。
        var desired = ClampToBottomZone(_transBasePosition, _transHalfExtents, _transAngle);

        desired = ClampToScreen(desired, _transHalfExtents, _transAngle);

        if (RectOverlaps(desired, _transHalfExtents, _transAngle,
                _mainCurrentPosition, _halfExtents, _lineAngle, hardGap))
        {
            desired = MainFreeFallback(desired, hardGap);
        }

        _transRect.anchoredPosition = desired;
    }

    /// <summary>确定性地在主轴上下/左右找一处不与主歌词重叠的位置（不会每帧抖动）。</summary>
    private Vector2 MainFreeFallback(Vector2 preferred, float gap)
    {
        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;

        var main = RotatedHalf(_halfExtents, _lineAngle);
        var trans = RotatedHalf(_transHalfExtents, _transAngle);
        var gapY = main.y + trans.y + gap;
        var gapX = main.x + trans.x + gap;

        var candidates = new[]
        {
            new Vector2(_mainCurrentPosition.x, _mainCurrentPosition.y - gapY),
            new Vector2(_mainCurrentPosition.x, _mainCurrentPosition.y + gapY),
            new Vector2(_mainCurrentPosition.x - gapX, _mainCurrentPosition.y),
            new Vector2(_mainCurrentPosition.x + gapX, _mainCurrentPosition.y),
            new Vector2(preferred.x, height * 0.5f - trans.y - gap),
            new Vector2(preferred.x, -height * 0.5f + trans.y + gap)
        };

        foreach (var candidate in candidates)
        {
            var p = ClampToScreen(candidate, _transHalfExtents, _transAngle);
            if (!RectOverlaps(p, _transHalfExtents, _transAngle,
                    _mainCurrentPosition, _halfExtents, _lineAngle, gap))
                return p;
        }

        return ClampToScreen(candidates[candidates.Length - 1], _transHalfExtents, _transAngle);
    }

    /// <summary>本曲是否载入了真正可用的翻译字幕（决定分区模式是否启用）。</summary>
    private bool HasTranslationTrack()
    {
        var s = LimbusCompanyLyrics.Settings;
        return s != null && s.TranslationEnabled &&
               _translationTrack != null && _translationTrack.Lines.Count > 0;
    }

    private static float RandomRange(float min, float max) =>
        min + UnityEngine.Random.value * (max - min);

    /// <summary>
    /// 新句必须避开的区域：最近 4 秒内出现过的所有歌词位置 + 仍可见的残影 + 翻译行。
    /// 这样即使旧句已经完全淡出，相近时间段的新句也不会再落到同一个位置。
    /// </summary>
    private List<AvoidBox> CollectAvoidBoxes()
    {
        var boxes = new List<AvoidBox>();
        var now = Time.unscaledTime;
        _recent.RemoveAll(p => now - p.Time > 4f);

        foreach (var g in _ghosts)
        {
            if (g.Go != null && g.Go.activeSelf && g.Alpha > 0.02f)
            {
                boxes.Add(new AvoidBox
                {
                    Position = g.Rect.anchoredPosition,
                    Half = g.Half,
                    Angle = g.Angle
                });
            }
        }

        foreach (var recent in _recent)
        {
            boxes.Add(new AvoidBox
            {
                Position = recent.Position,
                Half = recent.Half,
                Angle = recent.Angle
            });
        }

        if (_transShown != null && _transRect != null)
        {
            boxes.Add(new AvoidBox
            {
                Position = _transRect.anchoredPosition,
                Half = _transHalfExtents,
                Angle = _transAngle
            });
        }

        return boxes;
    }

    private bool OverlapsAny(Vector2 position, List<AvoidBox> boxes)
    {
        foreach (var b in boxes)
        {
            if (RectOverlaps(position, _halfExtents, _lineAngle,
                    b.Position, b.Half, b.Angle, 12f))
                return true;
        }

        return false;
    }

    private static float SeparationTo(Vector2 position, Vector2 half, float angle, List<AvoidBox> boxes)
    {
        var a = RotatedHalf(half, angle);
        var best = float.MaxValue;
        foreach (var b in boxes)
        {
            var bb = RotatedHalf(b.Half, b.Angle);
            var dx = Mathf.Abs(position.x - b.Position.x) - (a.x + bb.x);
            var dy = Mathf.Abs(position.y - b.Position.y) - (a.y + bb.y);
            best = Mathf.Min(best, Mathf.Max(dx, dy));
        }

        return best;
    }

    /// <summary>主歌词取点：默认在 pos_x / pos_y 范围内全屏随机，用户可在 SETTINGS 里改范围。</summary>
    private Vector2 RandomMainPosition(LimbusCompanyLyricsSettings s, float width, float height)
    {
        // 固定位置：用户可把歌词钉在左上/上/右上/左/中/右/左下/下/右下。
        var anchor = LimbusCompanyLyricsSettings.NormalizeAnchor(s.PositionAnchor);
        if (anchor != "random")
        {
            var x = anchor.EndsWith("left") ? -width * 0.5f
                : anchor.EndsWith("right") ? width * 0.5f
                : 0f;
            var y = anchor.StartsWith("top") ? height * 0.5f
                : anchor.StartsWith("bottom") ? -height * 0.5f
                : 0f;

            var anchored = ClampToScreen(new Vector2(x, y));
            var boxes = CollectAvoidBoxes();
            if (boxes.Count == 0)
                return anchored;

            // 固定锚点也不与仍在渐隐的旧句重叠：沿远离屏幕边缘的方向逐级让位。
            var half = RotatedHalf(_halfExtents, _lineAngle);
            var stepX = half.x * 2f + 12f;
            var stepY = half.y * 2f + 12f;
            var dirX = anchor.EndsWith("left") ? 1f : anchor.EndsWith("right") ? -1f : 0f;
            var dirY = anchor.StartsWith("bottom") ? 1f : anchor.StartsWith("top") ? -1f : 1f;

            for (var i = 0; i < 8 && OverlapsAny(anchored, boxes); i++)
                anchored = ClampToScreen(anchored + new Vector2(dirX * stepX, dirY * stepY));

            return anchored;
        }

        Vector2 candidate;
        if (s.RandomPosition)
        {
            // 随机取点，但优先远离仍在渐隐的上一句（旧句残影）和翻译行。
            var boxes = CollectAvoidBoxes();
            candidate = RandomCandidate(width, height, s);
            if (boxes.Count > 0)
            {
                var bestSeparation = SeparationTo(candidate, _halfExtents, _lineAngle, boxes);
                for (var attempt = 0; attempt < 64 && bestSeparation < 20f; attempt++)
                {
                    var next = RandomCandidate(width, height, s);
                    var separation = SeparationTo(next, _halfExtents, _lineAngle, boxes);
                    if (separation > bestSeparation)
                    {
                        bestSeparation = separation;
                        candidate = next;
                    }
                }

                LimbusCompanyLyrics.Log?.LogInfo(
                    $"新句避让: pos={candidate} sep={bestSeparation:F1} 残影/翻译数={boxes.Count}");
            }
        }
        else
        {
            candidate = new Vector2(
                ((s.PosXMin + s.PosXMax) * 0.5f / 100f - 0.5f) * width,
                (0.5f - (s.PosYMin + s.PosYMax) / 200f) * height);
        }

        // 只有存在翻译字幕时才限制主歌词在上半屏，保证两行不重叠。
        if (HasTranslationTrack())
        {
            var half = RotatedHalf(_halfExtents, _lineAngle).y;
            candidate.y = Mathf.Max(candidate.y, half + 16f);
        }

        return ClampToScreen(candidate);
    }

    /// <summary>翻译歌词取点：固定在下半屏，同样可在 SETTINGS 里控制是否随机。</summary>
    private Vector2 RandomTranslationPosition(LimbusCompanyLyricsSettings s, float width, float height)
    {
        var rot = RotatedHalf(_transHalfExtents, _transAngle);
        var loX = -width * 0.5f + rot.x + 16f;
        var hiX = width * 0.5f - rot.x - 16f;
        var loY = -height * 0.5f + rot.y + 16f;
        var hiY = -rot.y - 16f;

        if (hiX < loX)
        {
            loX = -width * 0.5f;
            hiX = width * 0.5f;
        }

        if (hiY < loY)
        {
            loY = -height * 0.5f;
            hiY = 0f;
        }

        float x, y;
        if (s.RandomPosition)
        {
            x = RandomRange(loX, hiX);
            y = RandomRange(loY, hiY);
        }
        else
        {
            x = Mathf.Clamp(((s.PosXMin + s.PosXMax) * 0.5f / 100f - 0.5f) * width, loX, hiX);
            y = (loY + hiY) * 0.5f;
        }

        return ClampToScreen(new Vector2(x, y), _transHalfExtents, _transAngle);
    }

    private void ApplyPlacement()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _rect == null || _canvas == null)
            return;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;

        _lineAngle = s.RandomAngle ? UnityEngine.Random.Range(s.AngleMin, s.AngleMax) : 0f;

        // 主歌词默认全屏随机（位置范围由 SETTINGS → LAYOUT 的 pos_x / pos_y 决定）。
        var candidate = RandomMainPosition(s, width, height);

        _basePosition = candidate;
        _mainCurrentPosition = candidate;
        _lastBasePosition = candidate;
        _lastHalfExtents = _halfExtents;
        _hasLastPosition = true;
        AddRecent(candidate, _halfExtents, _lineAngle);

        var tiltX = 0f;
        var tiltY = 0f;
        if (s.PerspectiveEnabled)
        {
            tiltX = -s.PerspYStrength * 0.2f;
            tiltY = s.PerspXStrength * 0.2f;
        }

        _rect.anchoredPosition = _basePosition;
        _rect.localEulerAngles = new Vector3(tiltX, tiltY, _lineAngle);
        _rect.localScale = Vector3.one;
    }

    private static Vector2 RandomCandidate(float width, float height, LimbusCompanyLyricsSettings s)
    {
        var xPercent = UnityEngine.Random.Range(s.PosXMin, s.PosXMax) / 100f;
        var yPercent = UnityEngine.Random.Range(s.PosYMin, s.PosYMax) / 100f;
        return new Vector2((xPercent - 0.5f) * width, (0.5f - yPercent) * height);
    }

    private bool IsTooClose(Vector2 candidate, float minDistance)
    {
        if (Vector2.Distance(candidate, _lastBasePosition) < minDistance)
            return true;

        var dx = Mathf.Abs(candidate.x - _lastBasePosition.x);
        var dy = Mathf.Abs(candidate.y - _lastBasePosition.y);
        var ex = _halfExtents.x + _lastHalfExtents.x + 12f;
        var ey = _halfExtents.y + _lastHalfExtents.y + 12f;
        return dx < ex && dy < ey;
    }

    /// <summary>把歌词框限制在屏幕内（考虑文字尺寸与旋转）。</summary>
    private Vector2 ClampToScreen(Vector2 position) => ClampToScreen(position, _halfExtents, _lineAngle);

    private Vector2 ClampToScreen(Vector2 position, Vector2 halfExtents, float angle)
    {
        if (_canvas == null)
            return position;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;

        var rotated = RotatedHalf(halfExtents, angle);
        const float margin = 16f;
        var maxX = Mathf.Max(0f, width * 0.5f - rotated.x - margin);
        var maxY = Mathf.Max(0f, height * 0.5f - rotated.y - margin);

        return new Vector2(Mathf.Clamp(position.x, -maxX, maxX), Mathf.Clamp(position.y, -maxY, maxY));
    }

    private Vector2 ClampToBottomZone(Vector2 position, Vector2 halfExtents, float angle)
    {
        if (_canvas == null)
            return position;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;
        var half = RotatedHalf(halfExtents, angle).y;
        var min = -height * 0.5f + half + 16f;
        var max = -half - 16f;

        return max < min
            ? new Vector2(position.x, -height * 0.25f)
            : new Vector2(position.x, Mathf.Clamp(position.y, min, max));
    }

    private static Vector2 RotatedHalf(Vector2 halfExtents, float angle)
    {
        var radians = Mathf.Abs(angle) * Mathf.Deg2Rad;
        var cos = Mathf.Abs(Mathf.Cos(radians));
        var sin = Mathf.Abs(Mathf.Sin(radians));
        return new Vector2(cos * halfExtents.x + sin * halfExtents.y,
            sin * halfExtents.x + cos * halfExtents.y);
    }

    // ------------------------------------------------------------------ translation line

    private void PollTranslation()
    {
        if (_transText == null)
            return;

        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        // F5 关卡 BGM 取代：翻译歌词来自音频同目录的字幕文件。
        if (BgmReplacing)
        {
            UpdateBgmTranslation(s);
            FadeTranslation(s);
            return;
        }

        if (_player != null)
        {
            var song = _player.State.Song;
            if (!ReferenceEquals(song, _currentSong))
            {
                _currentSong = song;
                ResetForSongSwitch();
                LoadTranslation(song);
            }
        }

        var available = s.Enabled && s.TranslationEnabled && _player != null &&
                        _translationTrack != null && _translationTrack.Lines.Count > 0;

        if (!available)
        {
            _transTargetAlpha = 0f;
            FadeTranslation(s);
            return;
        }

        var index = _translationTrack.IndexAt(_player.Source.time, _translationIndex);
        if (index != _translationIndex)
        {
            _translationIndex = index;
            if (index < 0)
                _transTargetAlpha = 0f;
            else
                ShowTranslation(_translationTrack.Lines[index].Text);
        }

        FadeTranslation(s);
    }

    private void FadeTranslation(LimbusCompanyLyricsSettings s)
    {
        if (_transGroup == null)
            return;

        if (_transTyping && _transText != null && _transShown != null)
        {
            _transTypeElapsed += Time.unscaledDeltaTime;
            var count = Mathf.FloorToInt(_transTypeElapsed * Mathf.Max(1f, s.TypewriterSpeed));
            if (count >= _transShown.Length)
            {
                _transText.maxVisibleCharacters = 99999;
                _transTyping = false;
            }
            else
            {
                _transText.maxVisibleCharacters = count;
            }
        }

        if (!Mathf.Approximately(_transAlpha, _transTargetAlpha))
        {
            _transAlpha = Mathf.MoveTowards(_transAlpha, _transTargetAlpha,
                Time.unscaledDeltaTime * s.FadeSpeed);
            _transGroup.alpha = _transAlpha;
        }

        if (_transTargetAlpha <= 0f && _transAlpha <= 0.001f && _transShown != null)
        {
            _transText.text = string.Empty;
            _transShown = null;
        }
    }

    private void LoadTranslation(JukeboxSong song)
    {
        _translationTrack = null;
        _translationIndex = -1;
        _transTargetAlpha = 0f;

        if (song?.Metadata is not JukeboxCustomSongMetadata meta)
            return;

        var main = meta.Files?.Main;
        if (main == null)
            return;

        var path = FindTranslationSidecar(main);
        if (path == null)
            return;

        try
        {
            _translationTrack = SubtitleParser.FromFile(new FileInfo(path));
            LimbusCompanyLyrics.Log.LogInfo("LimbusCompany 载入翻译歌词: " + path);
        }
        catch (Exception e)
        {
            LimbusCompanyLyrics.Log.LogWarning($"LimbusCompany 读取翻译歌词失败 ({path}): {e.Message}");
        }
    }

    private static string FindTranslationSidecar(FileInfo main)
    {
        var directory = main.DirectoryName;
        if (string.IsNullOrEmpty(directory))
            return null;

        var name = Path.GetFileNameWithoutExtension(main.Name);
        var suffixes = new[]
        {
            ".zh", ".cn", ".chs", ".cht", ".ja", ".jp", ".trans", ".translation",
            ".中文", ".翻译", "_zh", "_cn", "_ja", "_trans", "_translation"
        };

        foreach (var suffix in suffixes)
        {
            var candidate = Path.Combine(directory, name + suffix + ".srt");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private void ShowTranslation(string contents)
    {
        contents = NormalizeLyricText(contents);
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _transText == null)
            return;

        _transShown = contents;
        _transTypeElapsed = 0f;
        _transTyping = s.TypewriterEnabled;
        _transAlpha = 0f;
        _transTargetAlpha = 1f;
        if (_transGroup != null)
            _transGroup.alpha = 0f;
        _transText.maxVisibleCharacters = _transTyping ? 0 : 99999;
        _transText.text = string.Empty;
        _transText.ForceMeshUpdate(false, false);

        _transText.text = contents;
        ApplyFontFor(_transText, contents);
        ApplyTransStyle();
        _transText.ForceMeshUpdate(false, false);
        _transHalfExtents = new Vector2(Mathf.Max(0f, _transText.preferredWidth) * 0.5f,
            Mathf.Max(0f, _transText.preferredHeight) * 0.5f);
        _transAngle = s.RandomAngle ? UnityEngine.Random.Range(s.AngleMin, s.AngleMax) : 0f;

        PlaceTranslation();

        LimbusCompanyLyrics.Log?.LogInfo(
            $"翻译 \"{contents}\" pos={_transBasePosition} mode={s?.AntiOverlapMode}");

        _transText.maxVisibleCharacters = _transTyping ? 0 : 99999;
    }

    /// <summary>翻译行单独取点，并避开主歌词框。</summary>
    private void PlaceTranslation()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null || _transRect == null || _canvas == null)
            return;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;

        // 翻译固定在下半屏，主歌词在上半屏（有翻译时），两行不会重叠。
        var candidate = RandomTranslationPosition(s, width, height);

        // 主歌词被固定到左下/右下等位置时，翻译若与其重叠就往上让开。
        if (_shownText != null &&
            RectOverlaps(candidate, _transHalfExtents, _transAngle,
                _mainCurrentPosition, _halfExtents, _lineAngle, 12f))
        {
            candidate.y = _mainCurrentPosition.y +
                          RotatedHalf(_halfExtents, _lineAngle).y +
                          RotatedHalf(_transHalfExtents, _transAngle).y + 24f;
            candidate = ClampToScreen(candidate, _transHalfExtents, _transAngle);
        }

        _transBasePosition = candidate;
        _transRelativeY = _shownText != null ? candidate.y - _mainCurrentPosition.y : 0f;
        _transRect.anchoredPosition = candidate;
        _transRect.localEulerAngles = new Vector3(0f, 0f, _transAngle);
        _transRect.localScale = Vector3.one;
        AddRecent(candidate, _transHalfExtents, _transAngle);
    }

    private void AddRecent(Vector2 position, Vector2 half, float angle)
    {
        _recent.Add(new RecentPlacement
        {
            Position = position,
            Half = half,
            Angle = angle,
            Time = Time.unscaledTime
        });
    }

    /// <summary>
    /// 防止相近时间的歌词交错：在时间窗内的历史位置、当前另一条歌词之外，
    /// 依次尝试上下/左右多个车道，最后退到屏幕边缘。
    /// </summary>
    private Vector2 ResolvePosition(Vector2 candidate, Vector2 half, float angle,
        bool hasExtra, Vector2 extraPos, Vector2 extraHalf, float extraAngle)
    {
        var s = LimbusCompanyLyrics.Settings;
        var window = s != null ? s.AvoidWindow : 6f;
        var preferredGap = (s != null ? s.AvoidGap : 30f) + 12f;
        const float hardGap = 12f;
        var now = Time.unscaledTime;

        _recent.RemoveAll(p => now - p.Time > window);

        // 硬约束：不能和当前另一条可见歌词重叠；2.5 秒内的上一条歌词也不能重叠。
        // 配置的 avoid_gap / avoid_window 只是软偏好，放不下时退化，但不会真的交叠。
        const float hardRecentWindow = 2.5f;
        Vector2? bestMainFree = null;
        var bestMainScore = float.MinValue;
        Vector2? bestAnyMainFree = null;
        var bestAnyScore = float.MinValue;

        float SeparationScore(Vector2 p)
        {
            if (_recent.Count == 0)
                return 999999f;

            var a = RotatedHalf(half, angle);
            var best = float.MinValue;
            foreach (var recent in _recent)
            {
                var b = RotatedHalf(recent.Half, recent.Angle);
                var dx = Mathf.Abs(p.x - recent.Position.x) - (a.x + b.x);
                var dy = Mathf.Abs(p.y - recent.Position.y) - (a.y + b.y);
                var separation = Mathf.Max(dx, dy);
                if (separation > best)
                    best = separation;
            }

            return best;
        }

        bool HardRecentOverlap(Vector2 p)
        {
            foreach (var recent in _recent)
            {
                if (now - recent.Time > hardRecentWindow)
                    continue;
                if (RectOverlaps(p, half, angle, recent.Position, recent.Half, recent.Angle, hardGap))
                    return true;
            }

            return false;
        }

        bool Accept(Vector2 p)
        {
            p = ClampToScreen(p, half, angle);
            if (hasExtra && RectOverlaps(p, half, angle, extraPos, extraHalf, extraAngle, hardGap))
                return false;

            var score = SeparationScore(p);
            if (!bestAnyMainFree.HasValue || score > bestAnyScore)
            {
                bestAnyMainFree = p;
                bestAnyScore = score;
            }

            if (HardRecentOverlap(p))
                return false;

            if (!bestMainFree.HasValue || score > bestMainScore)
            {
                bestMainFree = p;
                bestMainScore = score;
            }

            return !OverlapsRecent(p, half, angle, preferredGap);
        }

        if (Accept(candidate))
            return candidate;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var width = canvasRect.width > 0f ? canvasRect.width : Screen.width;
        var height = canvasRect.height > 0f ? canvasRect.height : Screen.height;
        var stepPercent = s != null ? Mathf.Clamp(s.AvoidStep, 2f, 50f) : 10f;
        var step = Mathf.Max(24f, height * stepPercent / 100f);

        for (var i = 1; i <= 12; i++)
        {
            foreach (var dir in VerticalDirections)
            {
                var p = ClampToScreen(new Vector2(candidate.x, candidate.y + dir * step * i), half, angle);
                if (Accept(p))
                    return p;
            }
        }

        var horizontalStep = Mathf.Max(48f, width * stepPercent / 100f);
        for (var i = 1; i <= 8; i++)
        {
            foreach (var dir in VerticalDirections)
            {
                var p = ClampToScreen(new Vector2(candidate.x + dir * horizontalStep * i, candidate.y), half, angle);
                if (Accept(p))
                    return p;
            }
        }

        if (s != null && s.RandomPosition)
        {
            for (var i = 0; i < 24; i++)
            {
                var p = ClampToScreen(RandomCandidate(width, height, s), half, angle);
                if (Accept(p))
                    return p;
            }
        }

        var rotated = RotatedHalf(half, angle);
        var edgeY = extraPos.y <= 0f
            ? height * 0.5f - rotated.y - hardGap
            : -height * 0.5f + rotated.y + hardGap;
        var edge = ClampToScreen(new Vector2(candidate.x, edgeY), half, angle);
        if (Accept(edge))
            return edge;

        if (bestMainFree.HasValue)
            return bestMainFree.Value;

        if (bestAnyMainFree.HasValue)
            return bestAnyMainFree.Value;

        return ClampToScreen(candidate, half, angle);
    }

    private static readonly float[] VerticalDirections = { -1f, 1f };

    private bool OverlapsRecent(Vector2 position, Vector2 half, float angle, float gap)
    {
        foreach (var recent in _recent)
        {
            if (RectOverlaps(position, half, angle, recent.Position, recent.Half, recent.Angle, gap))
                return true;
        }

        return false;
    }

    private static bool RectOverlaps(Vector2 posA, Vector2 halfA, float angleA,
        Vector2 posB, Vector2 halfB, float angleB, float gap)
    {
        var a = RotatedHalf(halfA, angleA);
        var b = RotatedHalf(halfB, angleB);
        var dx = Mathf.Abs(posA.x - posB.x);
        var dy = Mathf.Abs(posA.y - posB.y);
        return dx < a.x + b.x + gap && dy < a.y + b.y + gap;
    }

    private void ApplyShake(LimbusCompanyLyricsSettings s)
    {
        if (s.ShakeIntensity <= 0f || _alpha <= 0.01f)
            return;

        _text.ForceMeshUpdate(false, false);
        var info = _text.textInfo;
        if (info == null || info.characterCount == 0)
            return;

        var t = Time.unscaledTime * s.ShakeSpeed * 0.05f;
        var meshInfo = info.meshInfo;

        for (var i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible)
                continue;

            var vi = c.vertexIndex;
            var mi = c.materialReferenceIndex;

            var ox = (Mathf.PerlinNoise(i * 0.61f + 0.13f, t) - 0.5f) * 2f * s.ShakeIntensity;
            var oy = (Mathf.PerlinNoise(i * 0.53f + 17.2f, t) - 0.5f) * 2f * s.ShakeIntensity;
            var offset = new Vector3(ox, oy, 0f);

            var verts = meshInfo[mi].vertices;
            if (verts == null || vi + 3 >= verts.Length)
                continue;

            verts[vi + 0] += offset;
            verts[vi + 1] += offset;
            verts[vi + 2] += offset;
            verts[vi + 3] += offset;
        }

        for (var i = 0; i < meshInfo.Length; i++)
        {
            if (meshInfo[i].mesh == null)
                continue;

            meshInfo[i].mesh.vertices = meshInfo[i].vertices;
            _text.UpdateGeometry(meshInfo[i].mesh, i);
        }
    }
}

/// <summary>F5“显示”页中，用鼠标拖动曲绘行 / 滚轮缩放。</summary>
public sealed class NowPlayingDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public LimbusCompanyLyricsOverlay Owner;

    private Vector2 _startPointer;
    private float _startPercentX;
    private float _startPercentY;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Owner == null)
            return;

        var canvas = Owner.NowPlayingCanvasRect;
        if (canvas == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, eventData.position,
            eventData.pressEventCamera, out _startPointer);
        var anchor = Owner.GetNowPlayingAnchorPercent();
        _startPercentX = anchor.x;
        _startPercentY = anchor.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Owner == null)
            return;

        var canvas = Owner.NowPlayingCanvasRect;
        if (canvas == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, eventData.position,
                eventData.pressEventCamera, out var current))
            return;

        var scale = Owner.WindowCanvasScale;
        var windowWidth = Screen.width / scale;
        var windowHeight = Screen.height / scale;
        if (windowWidth <= 1f || windowHeight <= 1f)
        {
            var rect = canvas.rect;
            windowWidth = rect.width;
            windowHeight = rect.height;
        }

        if (windowWidth <= 0f || windowHeight <= 0f)
            return;

        var dx = (current.x - _startPointer.x) / windowWidth * 100f;
        var dy = (current.y - _startPointer.y) / windowHeight * 100f;
        Owner.SetNowPlayingAnchorPercent(_startPercentX + dx, _startPercentY - dy, false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Owner?.SaveNowPlayingSettings();
    }

    public void OnScroll(PointerEventData eventData)
    {
        Owner?.ScrollNowPlayingSize(eventData.scrollDelta.y);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jukebox.Components;
using Jukebox.UI.Subtitles;
using JukeboxCore.Logging;
using JukeboxCore.Songs.Models;
using JukeboxCore.Subtitles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 独立歌词层：订阅 JukeboxMusicPlayer.OnSubtitleChanged，用 TMP 按 LimbusCompany 演出风格渲染。
/// </summary>
public class LimbusCompanyLyricsOverlay : MonoBehaviour
{
    private Canvas _canvas;
    private GameObject _visRoot;
    private readonly Image[] _visBars = new Image[128];
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

    private void OnEnable()
    {
        LimbusCompanyLyrics.SettingsChanged += OnSettingsChanged;
    }

    private void OnDisable()
    {
        LimbusCompanyLyrics.SettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        ApplyStyle();
    }

    private void Update()
    {
        try
        {
            PollPlayer();
            PollConfig();
            PollPanelSuppression();
            Animate();
            PollTranslation();
            UpdateVisualizer();
            CheckOverlapDiagnostics();
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

    private void UpdateVisualizer()
    {
        if (_visRoot == null)
            return;

        var s = LimbusCompanyLyrics.Settings;
        var show = s != null && s.VisualizerEnabled && _player != null && _player.Source != null;
        if (!show)
        {
            if (_visRoot.activeSelf)
                _visRoot.SetActive(false);
            return;
        }

        if (!_visRoot.activeSelf)
            _visRoot.SetActive(true);

        _player.Source.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);

        var rootRect = _visRoot.GetComponent<RectTransform>();
        var size = rootRect.rect.size;
        var width = size.x > 0f ? size.x : Screen.width;
        var height = size.y > 0f ? size.y : Screen.height;

        var position = LimbusCompanyLyricsSettings.NormalizePosition(s.VisualizerPosition);
        var horizontal = position == "bottom" || position == "top";
        var maxExtent = (horizontal ? height : width) * Mathf.Clamp(s.VisualizerSize, 5f, 50f) / 100f;
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
            if (range > 0.001f)
            {
                // 颜色只随时间流动，与音量无关。
                // 用户可用 visualizer_hue_from / hue_to 指定渐变的色相范围（0-360）。
                var hueFrom = Mathf.Clamp(s.VisualizerHueFrom, 0f, 360f) / 360f;
                var hueTo = Mathf.Clamp(s.VisualizerHueTo, 0f, 360f) / 360f;

                var spread = count > 1 ? (float)band / (count - 1) : 0f;
                var phase = Mathf.Repeat(Time.unscaledTime * 0.08f + spread * range, 1f);
                // 在 [from, to] 之间来回渐变，避免色相绕圈时突然跳变。
                var wave = phase <= 0.5f ? phase * 2f : (1f - phase) * 2f;
                var hue = Mathf.Lerp(hueFrom, hueTo, wave);

                var custom = Color.HSVToRGB(hue, 1f, 1f);
                custom.a = 0.9f;
                bar.color = custom;
            }
            else
            {
                bar.color = new Color(0.776f, 0.635f, 0.29f, 0.55f);
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

    private static Color ParseColor(string html, Color fallback)
    {
        if (!string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out var c))
        {
            c.a = 1f;
            return c;
        }

        return fallback;
    }

    // ------------------------------------------------------------------ player

    private void PollPlayer()
    {
        if (_player == null)
        {
            BuildUi();
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

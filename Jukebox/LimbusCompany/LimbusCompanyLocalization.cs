using System;
using System.Globalization;
using Jukebox.UI.Windows;
using JukeboxCore.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jukebox.LimbusCompany;

/// <summary>
/// 只在 Jukebox 菜单里注入本 mod 自己的「语言 / 设置」界面（不翻译 CGME 原菜单）。
/// 默认 English。
/// </summary>
public class LimbusCompanyLocalization : MonoBehaviour
{
    private static readonly Color Gold = new(0.776f, 0.635f, 0.29f, 1f);
    private static readonly Color Bone = new(0.937f, 0.902f, 0.812f, 1f);
    private static readonly Color PanelBg = new(0.04f, 0.04f, 0.05f, 0.97f);

    private JukeboxMenuWindow _menu;
    private RectTransform _canvas;

    private GameObject _languageButton;
    private TextMeshProUGUI _languageText;
    private GameObject _settingsButton;
    private TextMeshProUGUI _settingsText;
    private GameObject _panel;
    private bool _panelOpen;
    private bool _rebuildPending;
    private int _page;

    private float _nextMenuScan;
    private bool _failed;

    private static LimbusCompanyLyricsSettings S => LimbusCompanyLyrics.Settings;

    private void Update()
    {
        if (_failed)
            return;

        try
        {
            if (_menu == null)
            {
                if (Time.unscaledTime < _nextMenuScan)
                    return;

                _nextMenuScan = Time.unscaledTime + 1f;
                EnsureMenu();
            }

            if (_canvas == null)
                return;

            EnsureButtons();
            UpdateVisibility();

            if (_rebuildPending)
            {
                _rebuildPending = false;
                if (_panelOpen)
                    RebuildPanel();
            }

            UpdateButtonLabels();
        }
        catch (Exception e)
        {
            _failed = true;
            LimbusCompanyLyrics.Log.LogError($"LimbusCompany UI 异常: {e}");
        }
    }

    private static bool IsChinese()
    {
        var s = LimbusCompanyLyrics.Settings;
        return s != null && s.Language == "zh";
    }

    private static string L(string en, string zh) => IsChinese() ? zh : en;

    private void EnsureMenu()
    {
        if (_menu != null)
            return;

        foreach (var window in Resources.FindObjectsOfTypeAll<JukeboxMenuWindow>())
        {
            if (window == null || !window.gameObject.scene.IsValid())
                continue;

            _menu = window;
            _canvas = window.transform.parent as RectTransform;
            if (_canvas == null)
            {
                var canvas = window.GetComponentInParent<Canvas>();
                _canvas = canvas != null ? canvas.transform as RectTransform : null;
            }

            return;
        }
    }

    // ------------------------------------------------------------------ injected buttons

    private GameObject CreateButton(string name, Vector2 position, Vector2 size, Action onClick, out TextMeshProUGUI text)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(_canvas, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = PanelBg;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(1f, -1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        text = labelGo.AddComponent<TextMeshProUGUI>();
        text.font = LimbusCompanyFonts.Resolve();
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.color = Bone;

        return go;
    }

    private void EnsureButtons()
    {
        if (_languageButton == null)
        {
            _languageButton = CreateButton("LimbusLanguageButton", new Vector2(-16f, -16f), new Vector2(200f, 34f),
                ToggleLanguage, out _languageText);
            _languageButton.transform.SetAsLastSibling();
        }

        if (_settingsButton == null)
        {
            _settingsButton = CreateButton("LimbusSettingsButton", new Vector2(-224f, -16f), new Vector2(150f, 34f),
                TogglePanel, out _settingsText);
            _settingsButton.transform.SetAsLastSibling();
        }
    }

    private void UpdateVisibility()
    {
        var menuOpen = _menu != null && _menu.IsOpened();

        if (_languageButton != null && _languageButton.activeSelf != menuOpen)
            _languageButton.SetActive(menuOpen);
        if (_settingsButton != null && _settingsButton.activeSelf != menuOpen)
            _settingsButton.SetActive(menuOpen);

        if (!menuOpen)
            _panelOpen = false;

        if (_panel != null && _panel.activeSelf != (_panelOpen && menuOpen))
            _panel.SetActive(_panelOpen && menuOpen);
    }

    private void UpdateButtonLabels()
    {
        if (_languageText != null)
        {
            var text = IsChinese() ? "LANGUAGE: 中文" : "LANGUAGE: ENGLISH";
            if (_languageText.text != text)
                _languageText.text = text;
        }

        if (_settingsText != null)
        {
            var text = L("SETTINGS", "设置");
            if (_settingsText.text != text)
                _settingsText.text = text;
        }
    }

    private void ToggleLanguage()
    {
        LimbusCompanyLyrics.SetLanguage(IsChinese() ? "en" : "zh");
        _rebuildPending = _panelOpen;
        UpdateButtonLabels();
    }

    // ------------------------------------------------------------------ feature settings panel

    private void TogglePanel()
    {
        _panelOpen = !_panelOpen;
        if (_panelOpen)
            RebuildPanel();
        else if (_panel != null)
            _panel.SetActive(false);
    }

    private void RebuildPanel()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
            Destroy(_panel);
        }

        _panel = BuildPanel();
        _panel.transform.SetAsLastSibling();
        _panel.SetActive(true);
    }

    private void SwitchPage(int page)
    {
        _page = page;
        RebuildPanel();
    }

    private GameObject BuildPanel()
    {
        var panel = new GameObject("LimbusSettingsPanel", typeof(RectTransform));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(_canvas, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(560f, 660f);

        var image = panel.AddComponent<Image>();
        image.color = PanelBg;
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreateLabel(rect, "Title", L("LYRICS SETTINGS", "歌词设置"), new Vector2(0f, 296f),
            new Vector2(500f, 40f), 24f, TextAlignmentOptions.Center);
        CreateLabel(rect, "Subtitle", "Jukebox LimbusCompany v" + Jukebox.JukeboxPluginInfo.Version,
            new Vector2(0f, 270f), new Vector2(500f, 20f), 13f, TextAlignmentOptions.Center);

        CreateButtonAt(rect, "TabLayout", L("LAYOUT", "位置"), new Vector2(-216f, 236f), new Vector2(100f, 28f),
            () => SwitchPage(0));
        CreateButtonAt(rect, "TabText", L("TEXT", "文字"), new Vector2(-108f, 236f), new Vector2(100f, 28f),
            () => SwitchPage(1));
        CreateButtonAt(rect, "TabMotion", L("MOTION", "动态"), new Vector2(0f, 236f), new Vector2(100f, 28f),
            () => SwitchPage(2));
        CreateButtonAt(rect, "TabTrans", L("TRANS", "翻译"), new Vector2(108f, 236f), new Vector2(100f, 28f),
            () => SwitchPage(3));
        CreateButtonAt(rect, "TabVisual", L("VISUAL", "频谱"), new Vector2(216f, 236f), new Vector2(100f, 28f),
            () => SwitchPage(4));

        var y = 188f;
        const float step = 34f;

        if (_page == 0)
        {
            AddInputRow(rect, ref y, step, L("X min (%)", "X 最小 (%)"), F(S.PosXMin), v => S.PosXMin = Mathf.Clamp(v, 0f, 100f));
            AddInputRow(rect, ref y, step, L("X max (%)", "X 最大 (%)"), F(S.PosXMax), v => S.PosXMax = Mathf.Clamp(v, 0f, 100f));
            AddInputRow(rect, ref y, step, L("Y min (%)", "Y 最小 (%)"), F(S.PosYMin), v => S.PosYMin = Mathf.Clamp(v, 0f, 100f));
            AddInputRow(rect, ref y, step, L("Y max (%)", "Y 最大 (%)"), F(S.PosYMax), v => S.PosYMax = Mathf.Clamp(v, 0f, 100f));
            AddInputRow(rect, ref y, step, L("Zoom (%)", "缩放 (%)"), F(S.Zoom), v => S.Zoom = Mathf.Clamp(v, 10f, 400f));
            AddInputRow(rect, ref y, step, L("Angle min", "最小角度"), F(S.AngleMin), v => S.AngleMin = Mathf.Clamp(v, -180f, 180f));
            AddInputRow(rect, ref y, step, L("Angle max", "最大角度"), F(S.AngleMax), v => S.AngleMax = Mathf.Clamp(v, -180f, 180f));
            AddInputRow(rect, ref y, step, L("Delay (ms)", "延迟 (毫秒)"), F(S.Delay), v => S.Delay = Mathf.RoundToInt(Mathf.Clamp(v, 0f, 10000f)));
            AddInputRow(rect, ref y, step, L("Anti-overlap window (s)", "防交错时间窗 (秒)"), F(S.AvoidWindow), v => S.AvoidWindow = Mathf.Clamp(v, 0f, 30f));
            AddInputRow(rect, ref y, step, L("Anti-overlap gap (px)", "防交错间隔 (像素)"), F(S.AvoidGap), v => S.AvoidGap = Mathf.Clamp(v, 0f, 300f));
            AddInputRow(rect, ref y, step, L("Lane step (% height)", "车道步长 (屏高%)"), F(S.AvoidStep), v => S.AvoidStep = Mathf.Clamp(v, 2f, 50f));
            AddValueRow(rect, ref y, step, L("Position anchor", "固定位置"), AnchorText(S.PositionAnchor),
                () => S.PositionAnchor = NextAnchor(S.PositionAnchor));
            AddToggleRow(rect, ref y, step, L("Random position", "随机位置"), () => S.RandomPosition, () => S.RandomPosition = !S.RandomPosition);
            AddToggleRow(rect, ref y, step, L("Random angle", "随机旋转"), () => S.RandomAngle, () => S.RandomAngle = !S.RandomAngle);
        }
        else if (_page == 1)
        {
            AddTextRow(rect, ref y, step, L("Text color", "文字颜色"), S.TextColor, v => S.TextColor = NormalizeColor(v, S.TextColor));
            AddInputRow(rect, ref y, step, L("Font size", "字号"), F(S.FontSize), v => S.FontSize = Mathf.Clamp(v, 1f, 400f));
            AddInputRow(rect, ref y, step, L("Spacing", "字距"), F(S.Spacing), v => S.Spacing = Mathf.Clamp(v, -50f, 100f));
            AddTextRow(rect, ref y, step, L("Stroke color", "描边颜色"), S.StrokeColor, v => S.StrokeColor = NormalizeColor(v, S.StrokeColor));
            AddInputRow(rect, ref y, step, L("Stroke width", "描边宽度"), F(S.StrokeWidth), v => S.StrokeWidth = Mathf.Clamp(v, 0f, 50f));
            AddTextRow(rect, ref y, step, L("Glow color", "辉光颜色"), S.GlowColor, v => S.GlowColor = NormalizeColor(v, S.GlowColor));
            AddInputRow(rect, ref y, step, L("Glow size", "辉光大小"), F(S.GlowSize), v => S.GlowSize = Mathf.Clamp(v, 0f, 100f));
            AddInputRow(rect, ref y, step, L("Glow alpha", "辉光透明度"), F(S.GlowAlpha), v => S.GlowAlpha = Mathf.Clamp(v, 0f, 100f));
            AddToggleRow(rect, ref y, step, L("Glow", "辉光"), () => S.GlowEnabled, () => S.GlowEnabled = !S.GlowEnabled);
        }
        else if (_page == 2)
        {
            AddInputRow(rect, ref y, step, L("Shake intensity", "抖动强度"), F(S.ShakeIntensity), v => S.ShakeIntensity = Mathf.Clamp(v, 0f, 50f));
            AddInputRow(rect, ref y, step, L("Shake speed", "抖动速度"), F(S.ShakeSpeed), v => S.ShakeSpeed = Mathf.Clamp(v, 0.1f, 1000f));
            AddInputRow(rect, ref y, step, L("Fade speed", "淡入速度"), F(S.FadeSpeed), v => S.FadeSpeed = Mathf.Clamp(v, 0.1f, 60f));
            AddInputRow(rect, ref y, step, L("Rise speed", "上飘速度"), F(S.RiseSpeed), v => S.RiseSpeed = Mathf.Clamp(v, -200f, 200f));
            AddToggleRow(rect, ref y, step, L("Typewriter", "逐字显示"), () => S.TypewriterEnabled, () => S.TypewriterEnabled = !S.TypewriterEnabled);
            AddInputRow(rect, ref y, step, L("Chars per second", "每秒字数"), F(S.TypewriterSpeed), v => S.TypewriterSpeed = Mathf.Clamp(v, 1f, 200f));
            AddToggleRow(rect, ref y, step, L("Perspective", "透视"), () => S.PerspectiveEnabled, () => S.PerspectiveEnabled = !S.PerspectiveEnabled);
            AddInputRow(rect, ref y, step, L("Persp X", "透视 X"), F(S.PerspXStrength), v => S.PerspXStrength = Mathf.Clamp(v, -90f, 90f));
            AddInputRow(rect, ref y, step, L("Persp Y", "透视 Y"), F(S.PerspYStrength), v => S.PerspYStrength = Mathf.Clamp(v, -90f, 90f));
            AddToggleRow(rect, ref y, step, L("Lyrics enabled", "启用歌词层"), () => S.Enabled, () => S.Enabled = !S.Enabled);
            AddToggleRow(rect, ref y, step, L("Replace original subtitles", "替换原版字幕"), () => S.ReplaceOriginal, () => S.ReplaceOriginal = !S.ReplaceOriginal);
        }
        else if (_page == 3)
        {
            AddToggleRow(rect, ref y, step, L("Show translation line", "显示翻译歌词"), () => S.TranslationEnabled, () => S.TranslationEnabled = !S.TranslationEnabled);
            AddInputRow(rect, ref y, step, L("Translation font size (0=auto)", "翻译字号 (0=自动)"), F(S.TranslationFontSize), v => S.TranslationFontSize = Mathf.Clamp(v, 0f, 400f));
            AddInputRow(rect, ref y, step, L("Translation scale (auto)", "翻译字号比例 (自动)"), F(S.TranslationScale), v => S.TranslationScale = Mathf.Clamp(v, 0.1f, 3f));
            AddTextRow(rect, ref y, step, L("Translation color", "翻译文字颜色"), S.TranslationColor, v => S.TranslationColor = NormalizeColor(v, S.TranslationColor));
            AddValueRow(rect, ref y, step, L("Anti-overlap mode", "防交错方式"), ModeText(S.AntiOverlapMode),
                () => S.AntiOverlapMode = NextMode(S.AntiOverlapMode));
            AddInputRow(rect, ref y, step, L("Custom offset X (px)", "自定义偏移 X (像素)"), F(S.CustomOffsetX), v => S.CustomOffsetX = Mathf.Clamp(v, -2000f, 2000f));
            AddInputRow(rect, ref y, step, L("Custom offset Y (px)", "自定义偏移 Y (像素)"), F(S.CustomOffsetY), v => S.CustomOffsetY = Mathf.Clamp(v, -2000f, 2000f));

            var info = CreateLabelObject(rect, "TransInfo",
                L("Add a sidecar SRT next to the track:\nTrack.zh.srt / Track.cn.srt / Track.ja.srt / Track.trans.srt\nModes: AUTO = lane avoidance, MIRROR = opposite point (x,y -> -x,-y), BELOW = under the main line, CUSTOM = main + offset.",
                  "在音频旁再放一个同名翻译字幕：\nTrack.zh.srt / Track.cn.srt / Track.ja.srt / Track.trans.srt\n方式：自动=车道避让；镜像=对称点 (x,y -> -x,-y)；下方=主歌词正下方；自定义=主歌词+偏移。"),
                new Vector2(0f, -208f), new Vector2(470f, 88f), 13f, TextAlignmentOptions.TopLeft);
            info.enableWordWrapping = true;
        }
        else
        {
            AddToggleRow(rect, ref y, step, L("Audio visualizer", "音频可视化"), () => S.VisualizerEnabled, () => S.VisualizerEnabled = !S.VisualizerEnabled);
            AddInputRow(rect, ref y, step, L("Wave size (x)", "波动大小 (倍)"), F(S.VisualizerAmplitude), v => S.VisualizerAmplitude = Mathf.Clamp(v, 0.1f, 5f));
            AddInputRow(rect, ref y, step, L("Max extent (% screen)", "最大幅度 (屏幕%)"), F(S.VisualizerSize), v => S.VisualizerSize = Mathf.Clamp(v, 5f, 50f));
            AddValueRow(rect, ref y, step, L("Position", "显示位置"), VisualizerPositionText(S.VisualizerPosition), () => S.VisualizerPosition = NextVisualizerPosition(S.VisualizerPosition));
            AddInputRow(rect, ref y, step, L("Bar density (8-128)", "频谱密度 (8-128)"), Mathf.RoundToInt(S.VisualizerBars).ToString(CultureInfo.InvariantCulture), v => S.VisualizerBars = Mathf.Clamp(Mathf.RoundToInt(v), 8, 128));
            AddInputRow(rect, ref y, step, L("Gradient range (0-1)", "渐变范围 (0-1)"), F(S.VisualizerGradientRange), v => S.VisualizerGradientRange = Mathf.Clamp(v, 0f, 1f));
            AddInputRow(rect, ref y, step, L("Hue from (0-360)", "色相起点 (0-360)"), F(S.VisualizerHueFrom), v => S.VisualizerHueFrom = Mathf.Clamp(v, 0f, 360f));
            AddInputRow(rect, ref y, step, L("Hue to (0-360)", "色相终点 (0-360)"), F(S.VisualizerHueTo), v => S.VisualizerHueTo = Mathf.Clamp(v, 0f, 360f));
            AddToggleRow(rect, ref y, step, L("X mirror", "X 轴镜像"), () => S.VisualizerMirrorX, () => S.VisualizerMirrorX = !S.VisualizerMirrorX);

            var info = CreateLabelObject(rect, "VisInfo",
                L("Bars react to the current Jukebox track.\nDefault position: BOTTOM. Max extent is capped at 50% of the screen height (or width for LEFT/RIGHT).",
                  "柱状频谱跟随当前 Jukebox 曲目。\n默认位置：底部。最大幅度限制为屏幕高度（左右时为宽度）的 50%。"),
                new Vector2(0f, -208f), new Vector2(470f, 80f), 13f, TextAlignmentOptions.TopLeft);
            info.enableWordWrapping = true;
        }

        CreateButtonAt(rect, "Reset", L("RESET", "重置"), new Vector2(-110f, -286f), new Vector2(180f, 36f), () =>
        {
            ResetDefaults();
            SaveAndRefresh();
        });
        CreateButtonAt(rect, "Close", L("CLOSE", "关闭"), new Vector2(110f, -286f), new Vector2(180f, 36f), () =>
        {
            _panelOpen = false;
            if (_panel != null)
                _panel.SetActive(false);
        });

        return panel;
    }

    private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private void AddInputRow(RectTransform parent, ref float y, float step, string label, string value, Action<float> apply)
    {
        CreateLabel(parent, "Row", label, new Vector2(-140f, y), new Vector2(270f, 28f), 15f,
            TextAlignmentOptions.Left);
        CreateInput(parent, "Input", value, new Vector2(160f, y), new Vector2(180f, 28f), text =>
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                apply(parsed);
                LimbusCompanyLyrics.SaveAllSettings();
            }
            else
            {
                _rebuildPending = _panelOpen;
            }
        });
        y -= step;
    }

    private void AddTextRow(RectTransform parent, ref float y, float step, string label, string value, Action<string> apply)
    {
        CreateLabel(parent, "Row", label, new Vector2(-140f, y), new Vector2(270f, 28f), 15f,
            TextAlignmentOptions.Left);
        CreateInput(parent, "Input", value, new Vector2(160f, y), new Vector2(180f, 28f), text =>
        {
            apply(text);
            LimbusCompanyLyrics.SaveAllSettings();
        });
        y -= step;
    }

    private void AddToggleRow(RectTransform parent, ref float y, float step, string label, Func<bool> getter, Action toggle)
    {
        CreateLabel(parent, "Row", label, new Vector2(-140f, y), new Vector2(270f, 28f), 15f,
            TextAlignmentOptions.Left);
        CreateButtonAt(parent, "Value", BoolText(getter()), new Vector2(160f, y), new Vector2(180f, 28f), () =>
        {
            toggle();
            SaveAndRefresh();
        });
        y -= step;
    }

    private void AddValueRow(RectTransform parent, ref float y, float step, string label, string value, Action next)
    {
        CreateLabel(parent, "Row", label, new Vector2(-140f, y), new Vector2(270f, 28f), 15f,
            TextAlignmentOptions.Left);
        CreateButtonAt(parent, "Value", value, new Vector2(160f, y), new Vector2(180f, 28f), () =>
        {
            next();
            SaveAndRefresh();
        });
        y -= step;
    }

    private static readonly string[] Modes = { "strict", "auto", "mirror", "below", "custom" };

    private static string NextMode(string mode)
    {
        var index = Array.IndexOf(Modes, LimbusCompanyLyricsSettings.NormalizeMode(mode));
        if (index < 0)
            index = 0;
        return Modes[(index + 1) % Modes.Length];
    }

    private static string ModeText(string mode)
    {
        switch (LimbusCompanyLyricsSettings.NormalizeMode(mode))
        {
            case "strict":
                return L("STRICT", "分区");
            case "mirror":
                return L("MIRROR", "镜像");
            case "below":
                return L("BELOW", "下方");
            case "custom":
                return L("CUSTOM", "自定义");
            default:
                return L("AUTO", "自动");
        }
    }

    private static readonly string[] Anchors =
    {
        "random", "top-left", "top", "top-right", "left", "center", "right",
        "bottom-left", "bottom", "bottom-right"
    };

    private static string NextAnchor(string anchor)
    {
        var index = Array.IndexOf(Anchors, LimbusCompanyLyricsSettings.NormalizeAnchor(anchor));
        if (index < 0)
            index = 0;
        return Anchors[(index + 1) % Anchors.Length];
    }

    private static string AnchorText(string anchor)
    {
        switch (LimbusCompanyLyricsSettings.NormalizeAnchor(anchor))
        {
            case "top-left":
                return L("TOP LEFT", "左上");
            case "top":
                return L("TOP", "上中");
            case "top-right":
                return L("TOP RIGHT", "右上");
            case "left":
                return L("LEFT", "左中");
            case "center":
                return L("CENTER", "居中");
            case "right":
                return L("RIGHT", "右中");
            case "bottom-left":
                return L("BOTTOM LEFT", "左下");
            case "bottom":
                return L("BOTTOM", "下中");
            case "bottom-right":
                return L("BOTTOM RIGHT", "右下");
            default:
                return L("RANDOM", "全屏随机");
        }
    }

    private static readonly string[] VisualizerPositions = { "bottom", "top", "left", "right" };

    private static string NextVisualizerPosition(string position)
    {
        var index = Array.IndexOf(VisualizerPositions, LimbusCompanyLyricsSettings.NormalizePosition(position));
        if (index < 0)
            index = 0;
        return VisualizerPositions[(index + 1) % VisualizerPositions.Length];
    }

    private static string VisualizerPositionText(string position)
    {
        switch (LimbusCompanyLyricsSettings.NormalizePosition(position))
        {
            case "top":
                return L("TOP", "顶部");
            case "left":
                return L("LEFT", "左侧");
            case "right":
                return L("RIGHT", "右侧");
            default:
                return L("BOTTOM", "底部");
        }
    }

    private void CreateButtonAt(RectTransform parent, string name, string text, Vector2 position, Vector2 size,
        Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.09f, 1f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(0.8f, -0.8f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = LimbusCompanyFonts.Resolve();
        label.fontSize = 15f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = Bone;
        label.text = text;
    }

    private void CreateInput(RectTransform parent, string name, string value, Vector2 position, Vector2 size,
        Action<string> onEndEdit)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.09f, 1f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(0.8f, -0.8f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = LimbusCompanyFonts.Resolve();
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.color = Bone;

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.textComponent = text;
        input.textViewport = textRect;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = value;
        input.onEndEdit.AddListener(v => onEndEdit(v));
    }

    private static string NormalizeColor(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        value = value.Trim();
        return value.StartsWith("#") ? value : "#" + value;
    }

    private static void CreateLabel(RectTransform parent, string name, string text, Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment)
    {
        CreateLabelObject(parent, name, text, position, size, fontSize, alignment);
    }

    private static TextMeshProUGUI CreateLabelObject(RectTransform parent, string name, string text, Vector2 position,
        Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = LimbusCompanyFonts.Resolve();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = Bone;
        label.text = text;
        return label;
    }

    private static string BoolText(bool value)
    {
        return IsChinese() ? (value ? "开" : "关") : (value ? "ON" : "OFF");
    }

    private static void ResetDefaults()
    {
        var s = LimbusCompanyLyrics.Settings;
        if (s == null)
            return;

        s.Enabled = true;
        s.ReplaceOriginal = true;
        s.GlowEnabled = true;
        s.RandomPosition = true;
        s.RandomAngle = true;
        s.PosXMin = 0f;
        s.PosXMax = 100f;
        s.PosYMin = 0f;
        s.PosYMax = 100f;
        s.PositionAnchor = "random";
        s.FontSize = 40f;
        s.GlowAlpha = 23f;
        s.ShakeIntensity = 1f;
        s.FadeSpeed = 6f;
        s.RiseSpeed = 0f;
        s.AngleMax = 10f;
        s.AngleMin = -10f;
        s.AntiOverlapMode = "strict";
        s.AvoidWindow = 6f;
        s.AvoidGap = 30f;
        s.AvoidStep = 10f;
        s.CustomOffsetX = 0f;
        s.CustomOffsetY = -140f;
        s.TypewriterEnabled = false;
        s.TypewriterSpeed = 12f;
        s.TranslationEnabled = true;
        s.TranslationFontSize = 0f;
        s.TranslationScale = 0.75f;
        s.TranslationColor = "#d8d8d8";
        s.VisualizerEnabled = false;
        s.VisualizerAmplitude = 1f;
        s.VisualizerSize = 50f;
        s.VisualizerPosition = "bottom";
        s.VisualizerBars = 48;
        s.VisualizerGradientRange = 1f;
        s.VisualizerHueFrom = 0f;
        s.VisualizerHueTo = 360f;
        s.VisualizerMirrorX = false;
    }

    private void SaveAndRefresh(bool rebuild = true)
    {
        LimbusCompanyLyrics.SaveAllSettings();
        if (rebuild)
            _rebuildPending = _panelOpen;
    }
}

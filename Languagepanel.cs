using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MainMenu = Il2Cpp.MainMenu;

namespace MultiLanguage
{
    // панель
    internal static class LanguagePanel
    {
        private const float MaxFlag = 72f;    // макс. размер флага
        private const int MaxFont = 18;

        private static Button _langButton;
        private static Image _langImage;
        private static int _hookedId;
        private static GameObject _panel;

        private static Font _gameFont;
        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        public static void Hook()
        {
            Button found = null;
            foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (b == null || b.name != "LanguageButton") continue;
                if (!b.gameObject.scene.IsValid()) continue;
                found = b;
                break;
            }
            if (found == null) return;

            if (_langButton != null && _hookedId == found.GetInstanceID()) return;

            _langButton = found;
            _hookedId = found.GetInstanceID();
            _langImage = found.GetComponent<Image>();
            _panel = null;
            _gameFont = null;
            _replFont = null;
            _replTried = false;

            // onClick
            found.onClick = new Button.ButtonClickedEvent();
            found.onClick.AddListener(new Action(Toggle));

            MelonLogger.Msg("LanguageButton hooked.");
        }

        public static void Tick()
        {
            if (_panel != null && Input.GetKeyDown(KeyCode.Escape)) Close();

            if (Time.unscaledTime >= _nextFix)
            {
                _nextFix = Time.unscaledTime + 1f;
                try { FixFonts(); }
                catch (Exception e)
                {
                    MelonLogger.Warning("FixFonts failed: " + e.Message);
                    _nextFix = Time.unscaledTime + 10f;
                }
            }
        }

        private static float _nextFix;
        private static Font _replFont;
        private static bool _replTried;
        private static int _replLang = -1;

        // шрифт
        private static void FixFonts()
        {
            int lang = PlayerPrefs.GetInt("Language");
            if (lang < Core.BuiltInCount || lang >= Core.Langs.Count) return;
            var def = Core.Langs[lang];

            if (_replLang != lang) { _replLang = lang; _replFont = null; _replTried = false; }
            if (_replFont == null)
            {
                if (_replTried) return;
                _replTried = true;
                _replFont = LoadFont(def.Code, def.Native);
                if (_replFont == null)
                {
                    MelonLogger.Warning("No replacement font for '" + def.Code + "': game text will have no glyphs.");
                    return;
                }
                MelonLogger.Msg("Replacement font for '" + def.Code + "': " + _replFont.name);
            }

            foreach (var t in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                if (t.font == _replFont) continue;
                string str = t.text;
                if (string.IsNullOrEmpty(str) || HasAll(t.font, str)) continue;
                t.font = _replFont;
            }
        }

        private static bool HasAll(Font f, string s)
        {
            if (f == null) return false;
            int n = Math.Min(s.Length, 60);
            for (int i = 0; i < n; i++)
            {
                char ch = s[i];
                if (ch <= ' ') continue;
                if (!f.HasCharacter(ch)) return false;
            }
            return true;
        }

        private static void Toggle()
        {
            try
            {
                if (_panel != null) { Close(); return; }
                Build();
            }
            catch (Exception e)
            {
                MelonLogger.Error("Language panel failed: " + e);
                Close();
            }
        }

        private static void Close()
        {
            if (_panel != null) UnityEngine.Object.Destroy(_panel);
            _panel = null;
        }

        private static void Select(int id)
        {
            bool ok = false;
            try
            {
                var menu = FindMainMenu();
                if (menu != null) menu.SetLanguage(id);
                else PlayerPrefs.SetInt("Language", id);
                PlayerPrefs.Save();

                // иконка
                if (menu != null && _langImage != null)
                {
                    var list = menu.OLHCOGFBIGC;
                    if (list != null && id < list.Count && list[id] != null && list[id].icon != null)
                        _langImage.sprite = list[id].icon;
                }
                ok = true;
            }
            catch (Exception e)
            {
                MelonLogger.Error("SetLanguage failed: " + e);
            }
            Close();

            // перезагрузка
            if (ok)
            {
                try { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                catch (Exception e) { MelonLogger.Error("Scene reload failed: " + e); }
            }
        }

        private static MainMenu FindMainMenu()
        {
            foreach (var m in Resources.FindObjectsOfTypeAll<MainMenu>())
                if (m != null && m.gameObject.scene.IsValid()) return m;
            return null;
        }

        private static void Build()
        {
            var canvas = _langButton.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRt = canvas.GetComponent<RectTransform>();
            float W = canvasRt.rect.width;
            float H = canvasRt.rect.height;

            var panelRt = Make("MultiLanguagePanel", canvasRt);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            _panel = panelRt.gameObject;
            _panel.transform.SetAsLastSibling();

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);
            var bgBtn = _panel.AddComponent<Button>();
            bgBtn.targetGraphic = bg;
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(new Action(Close));

            var langs = Core.Langs;
            int n = langs.Count;
            int current = PlayerPrefs.GetInt("Language");

            float availW = W * 0.70f, availH = H * 0.60f;
            int cols = 1;
            float best = 0f;
            for (int c = 1; c <= n; c++)
            {
                int rows = (n + c - 1) / c;
                float d = Mathf.Min(availW / c * 0.70f, availH / rows * 0.65f);
                if (d > best) { best = d; cols = c; }
            }
            float flag = Mathf.Min(best, MaxFlag);
            int rowCount = (n + cols - 1) / cols;
            float cellW = Mathf.Min(availW / cols, flag * 2.0f);
            float cellH = Mathf.Min(availH / rowCount, flag * 1.55f);

            for (int i = 0; i < n; i++)
            {
                var def = langs[i];
                int row = i / cols;
                int col = i % cols;
                int inRow = (row == rowCount - 1) ? n - row * cols : cols;   // последний ряд по центру

                float x = (col - (inRow - 1) * 0.5f) * cellW;
                float y = ((rowCount - 1) * 0.5f - row) * cellH;

                bool isCurrent = def.Id == current;

                // ячейка
                var cellRt = Make("Lang_" + def.Code, panelRt);
                Center(cellRt, new Vector2(cellW, cellH), new Vector2(x, y));
                var cellImg = cellRt.gameObject.AddComponent<Image>();
                cellImg.color = new Color(0f, 0f, 0f, 0f);

                // флаг
                var flagRt = Make("Flag", cellRt);
                flagRt.anchorMin = flagRt.anchorMax = flagRt.pivot = new Vector2(0.5f, 1f);
                flagRt.sizeDelta = new Vector2(flag, flag);
                flagRt.anchoredPosition = new Vector2(0f, -cellH * 0.03f);
                if (isCurrent) flagRt.localScale = new Vector3(1.1f, 1.1f, 1f);
                var flagImg = flagRt.gameObject.AddComponent<Image>();
                flagImg.sprite = def.Icon;
                flagImg.preserveAspect = true;
                flagImg.raycastTarget = false;

                var btn = cellRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = flagImg;
                var cb = btn.colors;
                cb.normalColor = isCurrent ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
                cb.highlightedColor = Color.white;
                cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                cb.selectedColor = cb.normalColor;
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.08f;
                btn.colors = cb;
                int id = def.Id;
                btn.onClick.AddListener(new Action(() => Select(id)));

                // подпись
                var labelRt = Make("Label", cellRt);
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 0f);
                labelRt.sizeDelta = new Vector2(-4f, Mathf.Max(20f, cellH - flag - 8f));
                labelRt.anchoredPosition = Vector2.zero;

                var txt = labelRt.gameObject.AddComponent<Text>();
                try { txt.font = PickFont(def); }
                catch (Exception e) { MelonLogger.Warning("Font for " + def.Code + " failed: " + e.Message); }
                txt.text = def.Native;
                txt.alignment = TextAnchor.UpperCenter;
                txt.color = isCurrent ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
                txt.fontSize = MaxFont;
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 10;
                txt.resizeTextMaxSize = MaxFont;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.raycastTarget = false;
            }
        }

        private static RectTransform Make(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return rt;
        }

        private static void Center(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // игровой шрифт, если в нём есть все буквы, иначе любой подходящий из загруженных
        private static Font PickFont(LangDef def)
        {
            if (_gameFont == null)
            {
                var any = FindFirstText(_langButton.transform.parent);
                if (any != null) _gameFont = any.font;
            }

            if (_gameFont != null)
            {
                bool all = true;
                foreach (char ch in def.Native)
                    if (ch != ' ' && !_gameFont.HasCharacter(ch)) { all = false; break; }
                if (all) return _gameFont;
            }

            return LoadFont(def.Code, def.Native) ?? _gameFont;
        }

        private static Text FindFirstText(Transform t)
        {
            if (t == null) return null;

            var own = t.GetComponent<Text>();
            if (own != null) return own;

            int n = t.childCount;
            for (int i = 0; i < n; i++)
            {
                var found = FindFirstText(t.GetChild(i));
                if (found != null) return found;
            }
            return null;
        }

        private static Font LoadFont(string code, string sample)
        {
            string key = code + "|" + sample;
            if (FontCache.TryGetValue(key, out var cached) && cached != null) return cached;

            Font font = string.IsNullOrEmpty(sample) ? null : FindLoadedFont(sample);
            if (font == null)
                MelonLogger.Warning($"No loaded font covers all characters of '{sample}' ({code}).");
            else
                FontCache[key] = font;
            return font;
        }

        // первый шрифт, где есть все символы строки
        private static Font FindLoadedFont(string sample)
        {
            try
            {
                foreach (var f in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (f == null) continue;
                    bool all = true;
                    foreach (char ch in sample)
                        if (ch != ' ' && !f.HasCharacter(ch)) { all = false; break; }
                    if (all)
                    {
                        MelonLogger.Msg("Using game font '" + f.name + "' for \"" + sample + "\"");
                        return f;
                    }
                }
            }
            catch (Exception e) { MelonLogger.Warning("Font scan failed: " + e.Message); }
            return null;
        }
    }
}

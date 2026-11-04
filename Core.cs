using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using MainMenu = Il2Cpp.MainMenu;

[assembly: MelonInfo(typeof(MultiLanguage.Core), "MultiLanguage", "1.0.0", "erezexx")]
[assembly: MelonGame("ZeoWorks", "Slendytubbies 3")]

namespace MultiLanguage
{
    internal class LangDef
    {
        public int Id;            // индекс в списке игры, он же PlayerPrefs["Language"]
        public string Code;       // по нему ищутся Flags/<код>.png и Lang/<код>.txt
        public string Native;     // родное название
        public bool BuiltIn;
        public Sprite Icon;
        public string[] Lines;    // строки файла
        public Dictionary<string, string[]> Sections;   // [группа] -> строки, если в файле есть заголовки
    }

    public class Core : MelonMod
    {
        internal const int BuiltInCount = 10;
        internal static readonly List<LangDef> Langs = new List<LangDef>();

        // не трогайте, этот порядок менять нельзя
        private static readonly string[][] BuiltIn =
        {
            new string[] { "en",   "English" },
            new string[] { "es",   "Español" },
            new string[] { "doge", "Doge Mode" },
            new string[] { "da",   "Dansk" },
            new string[] { "nl",   "Nederlands" },
            new string[] { "pt",   "Português" },
            new string[] { "mk",   "Македонски" },
            new string[] { "zh",   "中文" },
            new string[] { "fr",   "Français" },
            new string[] { "ro",   "Română" },
        };

        // В игре язык появится, только если есть Lang/<код>.txt
        private static readonly string[][] Custom =
        {
            new string[] { "ru", "Русский" },
            new string[] { "uk", "Українська" },
            new string[] { "de", "Deutsch" },
            new string[] { "it", "Italiano" },
            new string[] { "pl", "Polski" },
            new string[] { "tr", "Türkçe" },
            new string[] { "cs", "Čeština" },
            new string[] { "sv", "Svenska" },
            new string[] { "ja", "日本語" },
            new string[] { "ko", "한국어" },
            new string[] { "hu", "Magyar" },
            new string[] { "fi", "Suomi" },
            new string[] { "bg", "Български" },
            new string[] { "sr", "Српски" },
            new string[] { "hr", "Hrvatski" },
            new string[] { "el", "Ελληνικά" },
        };

        public override void OnInitializeMelon()
        {
            foreach (var b in BuiltIn)
                Langs.Add(new LangDef { Id = Langs.Count, Code = b[0], Native = b[1], BuiltIn = true });

            var present = new HashSet<string>(Assembly.GetExecutingAssembly().GetManifestResourceNames());
            foreach (var c in Custom)
            {
                string res = "Lang/" + c[0] + ".txt";
                if (!present.Contains(res)) continue;

                string text = FlagTools.ReadText(res) ?? "";
                var lines = text.Replace("\r", "").Split('\n');

                var def = new LangDef
                {
                    Id = Langs.Count,
                    Code = c[0],
                    Native = c[1],
                    Lines = lines
                };
                ParseSections(lines, def);

                Langs.Add(def);
                LoggerInstance.Msg($"Loaded {def.Code}: " + (def.Sections != null
                    ? def.Sections.Count + " sections"
                    : lines.Length + " lines"));
            }

            LoggerInstance.Msg($"Languages: {Langs.Count} total, {Langs.Count - BuiltInCount} custom.");
            PatchTranslator();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            try { LanguagePanel.Hook(); }
            catch (Exception e) { LoggerInstance.Error(e.ToString()); }
        }

        public override void OnUpdate()
        {
            LanguagePanel.Tick();
        }

        private static void ParseSections(string[] lines, LangDef def)
        {
            Dictionary<string, List<string>> sec = null;
            List<string> cur = null;

            foreach (var raw in lines)
            {
                var line = raw.TrimStart('\uFEFF');
                if (line.Length > 2 && line[0] == '[' && line.TrimEnd().EndsWith("]"))
                {
                    if (sec == null) sec = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                    string name = line.Trim().Trim('[', ']');
                    cur = new List<string>();
                    sec[name] = cur;
                    continue;
                }
                if (cur != null) cur.Add(line);
            }

            if (sec == null) return;
            def.Sections = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var kv in sec) def.Sections[kv.Key] = kv.Value.ToArray();
        }

        private void PatchTranslator()
        {
            try
            {
                var type = typeof(MainMenu).Assembly.GetType("Il2Cpp.KDLPMCJBDEB");
                var method = type == null
                    ? null
                    : AccessTools.Method(type, "NNNPLCHBBGH", new[] { typeof(int), typeof(string) });

                if (method == null)
                {
                    LoggerInstance.Error("Translator method KDLPMCJBDEB.NNNPLCHBBGH not found. " +
                                         "The game was probably updated; custom languages will stay in English.");
                    return;
                }

                HarmonyInstance.Patch(method, prefix: new HarmonyMethod(typeof(Core), nameof(TranslatorPrefix)));
                LoggerInstance.Msg("Translator patched.");
            }
            catch (Exception e)
            {
                LoggerInstance.Error("PatchTranslator failed: " + e);
            }
        }

        private static readonly HashSet<string> Missed = new HashSet<string>();

        private static void LogMiss(string code, int idx, string key)
        {
            if (Missed.Count >= 80) return;
            if (Missed.Add(code + "|" + key))
                MelonLogger.Warning($"[{code}] no translation for #{idx}: \"{key}\"");
        }

        public static bool TranslatorPrefix(int __0, string __1, ref string __result)
        {
            int lang = PlayerPrefs.GetInt("Language");

            if (lang < BuiltInCount) return true;          // встроенные идут как в игре

            __result = string.Empty;
            if (lang >= Langs.Count) return false;

            var def = Langs[lang];
            string[] lines = def.Lines;

            if (def.Sections != null)
            {
                if (__1 == null || !def.Sections.TryGetValue(__1, out lines))
                {
                    LogMiss(def.Code, __0, "[" + __1 + "] section missing");
                    return false;
                }
            }

            if (lines != null && __0 >= 1 && __0 <= lines.Length)
            {
                string line = lines[__0 - 1].TrimStart('\uFEFF').TrimEnd('\r');
                int colon = line.IndexOf(':');
                if (colon >= 0) __result = line.Substring(colon + 1).Replace("#", string.Empty);
            }
            else
            {
                LogMiss(def.Code, __0, __1);
            }

            return false;
        }

        internal static void ApplyToMenu(MainMenu menu)
        {
            var list = menu.OLHCOGFBIGC;
            if (list == null) return;

            // иконки встроенных берём из игры
            for (int i = 0; i < BuiltInCount && i < list.Count; i++)
            {
                var item = list[i];
                if (item != null && item.icon != null) Langs[i].Icon = item.icon;
            }

            if (list.Count > BuiltInCount) return;

            for (int i = BuiltInCount; i < Langs.Count; i++)
            {
                var def = Langs[i];
                if (def.Icon == null) def.Icon = FlagTools.LoadRound(def.Code);
                list.Add(new MainMenu.language { name = def.Native, icon = def.Icon });
            }
        }
    }

    [HarmonyPatch(typeof(MainMenu), "Awake")]
    internal static class MainMenuAwakePatch
    {
        [HarmonyPrefix]
        private static void Prefix(MainMenu __instance)
        {
            try { Core.ApplyToMenu(__instance); }
            catch (Exception e) { MelonLogger.Error("ApplyToMenu failed: " + e); }
        }
    }
}

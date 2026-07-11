using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Four-language (한국어/English/日本語/中文) switch for every on-screen
    /// string. Call sites pass all variants inline via Loc.T(kr, en, ja, zh)
    /// — no key tables to maintain. Components whose text is not rebuilt
    /// every frame subscribe to Changed (or poll Version) and refresh on
    /// toggle. K key cycles the language. Narration clips resolve to
    /// Resources/Narration/{folder}: "" (kr), "en/", "ja/", "zh/".
    /// </summary>
    public static class Loc
    {
        public enum Lang { Korean = 0, English = 1, Japanese = 2, Chinese = 3 }

        public static Lang Language { get; private set; }

        /// <summary>Bumped on every language change — cheap dirty-check for
        /// components that poll instead of subscribing.</summary>
        public static int Version { get; private set; }

        public static event System.Action Changed;

        /// <summary>True for every non-Korean language — used where a single
        /// "international" variant serves EN/JA/ZH (e.g. formula annotations).</summary>
        public static bool NonKorean => Language != Lang.Korean;

        public static string T(string kr, string en, string ja, string zh)
        {
            switch (Language)
            {
                case Lang.English: return en;
                case Lang.Japanese: return ja;
                case Lang.Chinese: return zh;
                default: return kr;
            }
        }

        /// <summary>Subfolder under Resources/Narration for the active language.</summary>
        public static string NarrationFolder
        {
            get
            {
                switch (Language)
                {
                    case Lang.English: return "en/";
                    case Lang.Japanese: return "ja/";
                    case Lang.Chinese: return "zh/";
                    default: return "";
                }
            }
        }

        public static string DisplayName
        {
            get
            {
                switch (Language)
                {
                    case Lang.English: return "English";
                    case Lang.Japanese: return "日本語";
                    case Lang.Chinese: return "中文";
                    default: return "한국어";
                }
            }
        }

        public static void SetLanguage(Lang lang)
        {
            if (Language == lang) return;
            Language = lang;
            Version++;
            Changed?.Invoke();
        }

        public static void Cycle() => SetLanguage((Lang)(((int)Language + 1) % 4));
    }
}

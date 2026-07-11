using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Minimal two-language (한국어/English) switch for every on-screen string.
    /// Call sites pass both languages inline via Loc.T(kr, en) — no key tables
    /// to maintain. Components whose text is not rebuilt every frame subscribe
    /// to Changed (or track Version) and refresh on toggle. K key toggles.
    /// Narration clips resolve to Resources/Narration/ (kr) or
    /// Resources/Narration/en/ (en).
    /// </summary>
    public static class Loc
    {
        public static bool English { get; private set; }

        /// <summary>Bumped on every language change — cheap dirty-check for
        /// components that poll instead of subscribing.</summary>
        public static int Version { get; private set; }

        public static event System.Action Changed;

        public static string T(string kr, string en) => English ? en : kr;

        public static void SetEnglish(bool english)
        {
            if (English == english) return;
            English = english;
            Version++;
            Changed?.Invoke();
        }

        public static void Toggle() => SetEnglish(!English);
    }
}

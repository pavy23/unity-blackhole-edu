using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Persistent language selector: a small button in the top-right corner
    /// showing the current language; clicking it drops down the four
    /// options. Always available (K still cycles), hidden while a cinematic
    /// owns that corner with its skip/stop button.
    /// </summary>
    public static class LanguageSelect
    {
        static Button mainButton;
        static Text mainLabel;
        static RectTransform dropdown;

        static readonly (Loc.Lang lang, string label)[] Options =
        {
            (Loc.Lang.Korean,   "한국어"),
            (Loc.Lang.English,  "English"),
            (Loc.Lang.Japanese, "日本語"),
            (Loc.Lang.Chinese,  "中文"),
        };

        public static void CreateWidget()
        {
            if (mainButton != null) return;
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
            mainButton = BlackHoleUI.MakeButton(canvas.transform, "Language Button", "",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(150f, 40f),
                ToggleDropdown);
            mainLabel = mainButton.GetComponentInChildren<Text>();
            UpdateLabel();
            Loc.Changed -= UpdateLabel;   // statics can survive play sessions
            Loc.Changed += UpdateLabel;
        }

        /// <summary>Cinematics reuse this corner for their skip/stop button.</summary>
        public static void SetVisible(bool on)
        {
            if (mainButton != null) mainButton.gameObject.SetActive(on);
            if (!on) CloseDropdown();
        }

        static void UpdateLabel()
        {
            if (mainLabel != null) mainLabel.text = Loc.DisplayName + "  ▾";
        }

        static void ToggleDropdown()
        {
            if (dropdown != null && dropdown.gameObject.activeSelf) { CloseDropdown(); return; }
            if (dropdown == null) BuildDropdown();
            dropdown.gameObject.SetActive(true);
        }

        static void CloseDropdown()
        {
            if (dropdown != null) dropdown.gameObject.SetActive(false);
        }

        static void BuildDropdown()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
            dropdown = BlackHoleUI.MakePanel(canvas.transform, "Language Dropdown",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -72f), new Vector2(150f, 4f + Options.Length * 46f),
                accentLine: false);
            for (int i = 0; i < Options.Length; i++)
            {
                var lang = Options[i].lang;
                BlackHoleUI.MakeButton(dropdown, "Lang " + Options[i].label, Options[i].label,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f - i * 46f), new Vector2(138f, 42f),
                    () =>
                    {
                        Loc.SetLanguage(lang);
                        var controls = Object.FindAnyObjectByType<DesktopControls>();
                        if (controls != null) controls.RefreshLanguage();
                        CloseDropdown();
                    });
            }
        }
    }
}

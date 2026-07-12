using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// First-launch language picker: shown once before the intro cinematic,
    /// centered over the live scene. Picking a language sets Loc, refreshes
    /// every visible overlay, and hands control back to the caller (which
    /// then starts the intro). K still cycles languages any time later.
    /// </summary>
    public static class LanguageSelect
    {
        public static bool Open { get; private set; }

        static readonly (Loc.Lang lang, string label)[] Options =
        {
            (Loc.Lang.Korean,   "한국어"),
            (Loc.Lang.English,  "English"),
            (Loc.Lang.Japanese, "日本語"),
            (Loc.Lang.Chinese,  "中文"),
        };

        public static void Show(System.Action onDone)
        {
            if (Open) return;
            Open = true;

            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
            var panel = BlackHoleUI.MakePanel(canvas.transform, "Language Select",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(440f, 372f));

            var title = BlackHoleUI.MakeText(panel, "Title", 21, BlackHoleUI.TitleGold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(400f, 28f), FontStyle.Bold);
            title.text = "언어를 선택하세요";

            var sub = BlackHoleUI.MakeText(panel, "Subtitle", 15, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(400f, 22f));
            sub.text = "Choose your language · 言語を選択 · 请选择语言";

            for (int i = 0; i < Options.Length; i++)
            {
                var lang = Options[i].lang;
                BlackHoleUI.MakeButton(panel, "Lang " + Options[i].label, Options[i].label,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f - i * 62f), new Vector2(300f, 50f),
                    () =>
                    {
                        Loc.SetLanguage(lang);
                        var controls = Object.FindAnyObjectByType<DesktopControls>();
                        if (controls != null) controls.RefreshLanguage();
                        Object.Destroy(panel.gameObject);
                        Open = false;
                        onDone?.Invoke();
                    });
            }
        }
    }
}

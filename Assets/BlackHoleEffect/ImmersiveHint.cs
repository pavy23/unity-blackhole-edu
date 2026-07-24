using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// A faint, persistent "Esc — exit immersive view" reminder shown while an
    /// exhibit is in immersive (UI-hidden) mode. Immersive hides the toolbar
    /// that would otherwise toggle it, so this is the one affordance left for
    /// getting back — bright for a few seconds on entry, then settling to a low
    /// alpha so it never fully disappears. Shared by every exhibit's controls.
    /// </summary>
    public class ImmersiveHint : MonoBehaviour
    {
        static ImmersiveHint instance;
        Text label;
        float shownAt;
        bool on;
        int locVersion = -1;

        System.Action exitAction;
        Image buttonBg;

        /// <summary>Show the reminder. Pass the exit action so the hint is
        /// also a button — touch and mouse visitors (web, mobile) have no Esc
        /// key, and immersive hides the toolbar that would toggle it back.</summary>
        public static void Show(System.Action exit = null)
        {
            if (instance == null)
            {
                var go = new GameObject("Immersive Hint");
                instance = go.AddComponent<ImmersiveHint>();
                instance.Build();
            }
            instance.exitAction = exit;
            instance.on = true;
            instance.shownAt = Time.unscaledTime;
            if (instance.buttonBg != null) instance.buttonBg.gameObject.SetActive(true);
            instance.Refresh();
        }

        public static void Hide()
        {
            if (instance == null || instance.buttonBg == null) return;
            instance.on = false;
            instance.buttonBg.gameObject.SetActive(false);
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
            var btn = BlackHoleUI.MakeButton(canvas.transform, "Immersive Hint Button", "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(400f, 36f),
                () => { if (exitAction != null) exitAction(); });
            buttonBg = btn.GetComponent<Image>();
            label = btn.GetComponentInChildren<Text>();
            label.fontSize = 16;
            label.fontStyle = FontStyle.Normal;
            label.color = BlackHoleUI.TextSecondary;
            Refresh();
        }

        void Refresh()
        {
            if (label != null)
                label.text = Loc.T("몰입 보기 나가기 (Esc)", "Exit immersive view (Esc)",
                                   "没入ビューを終了 (Esc)", "退出沉浸视图 (Esc)");
        }

        void Update()
        {
            if (locVersion != Loc.Version) { locVersion = Loc.Version; Refresh(); }
            if (!on || label == null) return;
            float t = Time.unscaledTime - shownAt;
            // Bright for 3 s on entry, then settle faint but visible — it must
            // stay findable, it is the only way back without a keyboard.
            float a = t < 3f ? 0.85f : Mathf.Lerp(0.85f, 0.30f, Mathf.Clamp01((t - 3f) / 1.5f));
            var c = label.color; c.a = a; label.color = c;
            if (buttonBg != null) { var b = buttonBg.color; b.a = a * 0.75f; buttonBg.color = b; }
        }
    }
}

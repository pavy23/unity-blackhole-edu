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

        public static void Show()
        {
            if (instance == null)
            {
                var go = new GameObject("Immersive Hint");
                instance = go.AddComponent<ImmersiveHint>();
                instance.Build();
            }
            instance.on = true;
            instance.shownAt = Time.unscaledTime;
            if (instance.label != null) instance.label.gameObject.SetActive(true);
            instance.Refresh();
        }

        public static void Hide()
        {
            if (instance == null || instance.label == null) return;
            instance.on = false;
            instance.label.gameObject.SetActive(false);
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
            label = BlackHoleUI.MakeText(canvas.transform, "Immersive Hint Label", 16,
                BlackHoleUI.TextSecondary, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -22f), new Vector2(640f, 28f));
            Refresh();
        }

        void Refresh()
        {
            if (label != null)
                label.text = Loc.T("Esc — 몰입 보기 나가기", "Esc — exit immersive view",
                                   "Esc — 没入ビューを終了", "Esc — 退出沉浸视图");
        }

        void Update()
        {
            if (locVersion != Loc.Version) { locVersion = Loc.Version; Refresh(); }
            if (!on || label == null) return;
            float t = Time.unscaledTime - shownAt;
            // Bright for 3 s on entry, then settle to a faint but visible reminder.
            float a = t < 3f ? 0.85f : Mathf.Lerp(0.85f, 0.26f, Mathf.Clamp01((t - 3f) / 1.5f));
            var c = label.color; c.a = a; label.color = c;
        }
    }
}

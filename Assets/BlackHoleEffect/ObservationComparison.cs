using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// "Simulation vs. observation": the EHT image of M87* either as a card
    /// (top-right) or as a translucent overlay centered on the simulation for
    /// direct visual comparison. O key cycles Off → Card → Overlay.
    /// Image credit: EHT Collaboration (CC BY 4.0).
    /// </summary>
    [ExecuteAlways]
    public class ObservationComparison : MonoBehaviour
    {
        public enum DisplayMode { Off, Card, Overlay }

        public DisplayMode mode = DisplayMode.Off;
        public Texture2D observationImage;
        [TextArea] public string caption = "실제 관측: M87* — EHT (2019)\n© EHT Collaboration (CC BY 4.0)";

        /// <summary>Back-compat boolean view of mode (tour/immersive use this).</summary>
        public bool show
        {
            get => mode != DisplayMode.Off;
            set { mode = value ? DisplayMode.Card : DisplayMode.Off; }
        }

        RectTransform panel;
        Image panelBg;
        RawImage image;
        Text title, label;

        void OnEnable() => Build();
        void OnDisable() => Teardown();

        public void CycleMode()
        {
            mode = (DisplayMode)(((int)mode + 1) % 3);
            Refresh();
        }

        void Build()
        {
            Teardown();
            var canvas = BlackHoleUI.EnsureCanvas(GetComponentInParent<Camera>());

            panel = BlackHoleUI.MakePanel(canvas.transform, "Observation Card",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(310f, 386f));
            panelBg = panel.GetComponent<Image>();

            title = BlackHoleUI.MakeText(panel, "Title", 20, BlackHoleUI.TitleGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(270f, 26f), FontStyle.Bold);

            var imgGo = new GameObject("EHT Image") { hideFlags = HideFlags.DontSave };
            imgGo.transform.SetParent(panel, false);
            var rt = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -48f);
            rt.sizeDelta = new Vector2(270f, 270f);
            image = imgGo.AddComponent<RawImage>();
            image.raycastTarget = false;

            label = BlackHoleUI.MakeText(panel, "Caption", 15, BlackHoleUI.TextSecondary, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(280f, 44f));

            Refresh();
        }

        void Teardown()
        {
            if (panel != null) DestroyImmediate(panel.gameObject);
            panel = null; panelBg = null; image = null; title = null; label = null;
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (panel == null) return;
            bool visible = mode != DisplayMode.Off && observationImage != null;
            panel.gameObject.SetActive(visible);
            if (!visible) return;

            title.text = Loc.T("시뮬레이션 vs 실제 관측", "Simulation vs Real Observation");
            image.texture = observationImage;
            // The serialized caption is mostly a photo credit; localize the
            // default one, pass custom captions through unchanged.
            label.text = caption.StartsWith("실제 관측")
                ? Loc.T(caption, "Real observation: M87* — EHT (2019)\n© EHT Collaboration (CC BY 4.0)")
                : caption;
            var imgRt = image.rectTransform;

            if (mode == DisplayMode.Card)
            {
                panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
                panel.pivot = new Vector2(1f, 1f);
                panel.anchoredPosition = new Vector2(-28f, -28f);
                panel.sizeDelta = new Vector2(310f, 386f);
                panelBg.color = BlackHoleUI.PanelBg;
                title.gameObject.SetActive(true);
                imgRt.anchoredPosition = new Vector2(0f, -48f);
                imgRt.sizeDelta = new Vector2(270f, 270f);
                image.color = Color.white;
                label.rectTransform.anchoredPosition = new Vector2(0f, 56f);
            }
            else // Overlay: translucent image centered on the black hole
            {
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(600f, 640f);
                panelBg.color = new Color(0f, 0f, 0f, 0f); // frameless
                title.gameObject.SetActive(false);
                imgRt.anchoredPosition = new Vector2(0f, -20f);
                imgRt.sizeDelta = new Vector2(560f, 560f);
                image.color = new Color(1f, 1f, 1f, 0.42f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            }
        }
    }
}

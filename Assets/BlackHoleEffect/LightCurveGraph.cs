using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Real-time light curve (V key): plots the disk luminosity over the last
    /// ~32 seconds. When the tidal probe (T key) is torn apart near the hole,
    /// the disk genuinely flares — a tidal disruption event — and the graph
    /// shows the spike-and-decay astronomers use to discover black holes.
    /// </summary>
    public class LightCurveGraph : MonoBehaviour
    {
        public bool show;
        public BlackHoleController controller;
        public SpaghettificationDemo spaghetti;

        const int W = 256, H = 90;
        const float SampleHz = 8f;

        RectTransform panel;
        RawImage graphImg;
        Text title, readout;
        Texture2D tex;
        Color32[] pixels;
        readonly float[] samples = new float[W];
        int head;
        float sampleTimer, redrawTimer;

        float flare;
        bool armed = true, touching;
        float baseBrightness;

        void OnDisable()
        {
            RestoreBrightness();
            if (panel != null) DestroyImmediate(panel.gameObject);
            if (tex != null) DestroyImmediate(tex);
            panel = null; tex = null; graphImg = null; readout = null; title = null;
        }

        void Update()
        {
            UpdateFlare();
            Sample();

            if (panel == null && show) Build();
            if (panel == null) return;
            panel.gameObject.SetActive(show);
            if (!show) return;

            redrawTimer -= Time.deltaTime;
            if (redrawTimer <= 0f) { redrawTimer = 0.2f; Redraw(); }
        }

        void UpdateFlare()
        {
            // Trigger once per fall as the probe shreds near the hole.
            if (spaghetti != null && spaghetti.active)
            {
                float r = spaghetti.CurrentDistanceRs;
                if (armed && r < 1.8f) { flare = 1f; armed = false; }
                if (r > 6f) armed = true; // re-arm when the loop restarts
            }
            flare = Mathf.Max(0f, flare - flare * 0.45f * Time.deltaTime); // ~e^-t/2.2

            if (controller == null) return;
            if (flare > 0.01f)
            {
                if (!touching) { baseBrightness = controller.diskBrightness; touching = true; }
                controller.diskBrightness = baseBrightness * (1f + 1.7f * flare);
                controller.Apply();
            }
            else RestoreBrightness();
        }

        void RestoreBrightness()
        {
            if (!touching || controller == null) return;
            controller.diskBrightness = baseBrightness;
            controller.Apply();
            touching = false;
        }

        void Sample()
        {
            sampleTimer -= Time.deltaTime;
            if (sampleTimer > 0f) return;
            sampleTimer = 1f / SampleHz;
            // Relative luminosity: 1 at rest, up to ~2.7 during a flare.
            samples[head] = 1f + 1.7f * flare;
            head = (head + 1) % W;
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
            panel = BlackHoleUI.MakePanel(canvas.transform, "Light Curve",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 96f), new Vector2(330f, 190f));

            title = BlackHoleUI.MakeText(panel, "Title", 19, BlackHoleUI.TitleGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -13f), new Vector2(290f, 26f), FontStyle.Bold);

            var imgGo = new GameObject("Graph") { hideFlags = HideFlags.DontSave };
            imgGo.transform.SetParent(panel, false);
            var rt = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -44f);
            rt.sizeDelta = new Vector2(294f, 100f);
            graphImg = imgGo.AddComponent<RawImage>();
            graphImg.raycastTarget = false;

            tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.filterMode = FilterMode.Bilinear;
            pixels = new Color32[W * H];
            graphImg.texture = tex;

            readout = BlackHoleUI.MakeText(panel, "Readout", 15, BlackHoleUI.TextSecondary, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 10f), new Vector2(290f, 22f));
        }

        void Redraw()
        {
            var bg = new Color32(6, 9, 16, 255);
            var grid = new Color32(40, 46, 60, 255);
            var lineC = new Color32(255, 196, 110, 255);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
            // Baseline (L = 1) and a mid gridline.
            int yBase = ToY(1f);
            for (int x = 0; x < W; x++) { pixels[yBase * W + x] = grid; pixels[ToY(2f) * W + x] = grid; }

            int prevY = -1;
            for (int x = 0; x < W; x++)
            {
                float v = samples[(head + x) % W];
                int y = ToY(v);
                if (prevY < 0) prevY = y;
                int y0 = Mathf.Min(y, prevY), y1 = Mathf.Max(y, prevY);
                for (int yy = y0; yy <= y1; yy++) pixels[yy * W + x] = lineC;
                prevY = y;
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);

            title.text = Loc.T("광도 곡선 — 조석 파괴 이벤트", "Light Curve — Tidal Disruption Event",
                               "光度曲線 — 潮汐破壊イベント", "光变曲线 — 潮汐瓦解事件");
            string lum = (1f + 1.7f * flare).ToString("0.00");
            readout.text = Loc.T(
                "상대 광도 L = " + lum + (flare > 0.01f ? "   <color=#FFC46E>★ 플레어!</color>" : "   (T 키로 별을 떨어뜨려 보세요)"),
                "Relative luminosity L = " + lum + (flare > 0.01f ? "   <color=#FFC46E>★ Flare!</color>" : "   (press T to drop a star)"),
                "相対光度 L = " + lum + (flare > 0.01f ? "   <color=#FFC46E>★ フレア！</color>" : "   (Tキーで星を落とせます)"),
                "相对光度 L = " + lum + (flare > 0.01f ? "   <color=#FFC46E>★ 耀发！</color>" : "   (按T键让恒星坠落)"));
        }

        static int ToY(float v)
        {
            float norm = Mathf.InverseLerp(0.6f, 3.0f, v);
            return Mathf.Clamp((int)(norm * (H - 1)), 0, H - 1);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Small performance readout (F1): smoothed FPS / frame time, raymarch
    /// step count and quality profile. Useful when tuning for Quest.
    /// </summary>
    public class PerformanceHud : MonoBehaviour
    {
        public bool show;
        public BlackHoleController controller;

        RectTransform panel;
        Text text;
        float smoothedDt = 1f / 60f;
        float refreshTimer;

        void OnDisable()
        {
            if (panel != null) DestroyImmediate(panel.gameObject);
            panel = null; text = null;
        }

        void Update()
        {
            smoothedDt = Mathf.Lerp(smoothedDt, Time.unscaledDeltaTime, 0.06f);

            if (panel == null && show)
            {
                var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
                panel = BlackHoleUI.MakePanel(canvas.transform, "Perf HUD",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(330f, 44f),
                    accentLine: false);
                text = BlackHoleUI.MakeText(panel, "Text", 16, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 36f));
            }
            if (panel == null) return;
            panel.gameObject.SetActive(show);
            if (!show) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.25f;

            float fps = 1f / Mathf.Max(smoothedDt, 0.0001f);
            string quality = controller != null ? controller.quality.ToString() : "-";
            int steps = controller != null ? controller.raymarchSteps : 0;
            text.text = fps.ToString("0") + " FPS  ·  " + (smoothedDt * 1000f).ToString("0.0") + " ms  ·  "
                      + steps + " steps  ·  " + quality;
        }
    }
}

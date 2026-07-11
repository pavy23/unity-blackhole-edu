using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Gravitational lens "magnifier" (F3): switches the accretion disk off so
    /// only the pure lens remains, and puts an extended bright source (a
    /// background galaxy) behind it. Students see the galaxy smeared into arcs
    /// and rings — gravity as a natural telescope.
    /// </summary>
    public class GravitationalLensDemo : MonoBehaviour
    {
        public BlackHoleController controller;
        public EinsteinRingDemo einstein;

        public bool Active { get; private set; }

        float savedBrightness, savedStarSize, savedStarBrightness;
        Text caption;
        RectTransform captionPanel;

        public void Toggle()
        {
            if (Active) End();
            else Begin();
        }

        void Begin()
        {
            if (controller == null || einstein == null) return;
            Active = true;
            savedBrightness = controller.diskBrightness;
            savedStarSize = einstein.starSize;
            savedStarBrightness = einstein.starBrightness;

            controller.diskBrightness = 0f;   // disk off — pure lens
            controller.Apply();
            einstein.starSize = 0.0045f;      // extended source ≈ galaxy
            einstein.starBrightness = 1.8f;
            einstein.autoSweep = true;
            einstein.sweepSpeed = 2f;
            einstein.active = true;

            Caption(Loc.T(
                "중력 렌즈 돋보기 — 원반을 끄면 순수한 '렌즈'만 남습니다.\n" +
                "블랙홀 뒤의 은하가 호(弧)와 링으로 늘어나며 확대됩니다. (A/D로 은하 이동, F3으로 종료)",
                "Gravitational lens magnifier — with the disk off, only the pure 'lens' remains.\n" +
                "The galaxy behind the hole is stretched into arcs and rings. (A/D to move it, F3 to exit)"));
        }

        void End()
        {
            Active = false;
            if (controller != null)
            {
                controller.diskBrightness = savedBrightness;
                controller.Apply();
            }
            if (einstein != null)
            {
                einstein.active = false;
                einstein.starSize = savedStarSize;
                einstein.starBrightness = savedStarBrightness;
            }
            if (captionPanel != null) captionPanel.gameObject.SetActive(false);
        }

        void Caption(string text)
        {
            if (caption == null)
            {
                var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
                captionPanel = BlackHoleUI.MakePanel(canvas.transform, "Lens Caption",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(880f, 100f));
                caption = BlackHoleUI.MakeText(captionPanel, "Text", 21, BlackHoleUI.TextPrimary, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 84f));
            }
            captionPanel.gameObject.SetActive(true);
            caption.text = text;
        }
    }
}

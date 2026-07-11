using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Educational readout card (top-left): real physical quantities for a
    /// chosen black hole mass plus a gravitational time-dilation comparison
    /// with two animated clocks. Rendered with the shared BlackHoleUI theme;
    /// CanvasScaler keeps it correct at any aspect ratio.
    /// </summary>
    [ExecuteAlways]
    public class BlackHolePhysicsPanel : MonoBehaviour
    {
        public enum MassPreset { Stellar10, SagittariusA, M87 }

        public bool show = true;

        [Header("Black Hole")]
        [Tooltip("Mass in solar masses. 10 = stellar, 4.3e6 = Sgr A*, 6.5e9 = M87*")]
        public double massSolarMasses = 4.3e6;
        public string massLabel = "궁수자리 A* (우리은하 중심)";
        public string massLabelEn = "Sagittarius A* (Milky Way center)";

        [Header("Time Dilation Probe")]
        [Range(1.02f, 20f)] public float probeDistanceRs = 1.5f;
        [Tooltip("When on, the observer clock uses the actual camera distance from the hole (static-observer dilation √(1−Rs/r)) instead of an idealized far observer.")]
        public bool observerFollowsCamera = true;

        [Header("Optional Links")]
        [Tooltip("When set, mass presets also change the visual: scale, disk temperature (T ∝ M^-1/4) and flow speed.")]
        public BlackHoleController controller;
        [Tooltip("Hidden at elementary difficulty.")]
        public bool showDilationRow = true;

        const double RsKmPerSolarMass = 2.953;

        RectTransform panel;
        Text title, body, clockHeader, farLabel, probeLabel;
        RectTransform farHand, probeHand;
        float farAngle, probeAngle;

        void OnEnable() => Build();
        void OnDisable() => Teardown();

        void Build()
        {
            Teardown();
            var canvas = BlackHoleUI.EnsureCanvas(GetComponentInParent<Camera>());

            panel = BlackHoleUI.MakePanel(canvas.transform, "Physics Panel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(400f, 348f));

            title = BlackHoleUI.MakeText(panel, "Title", 22, BlackHoleUI.TitleGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(360f, 30f), FontStyle.Bold);

            body = BlackHoleUI.MakeText(panel, "Body", 19, BlackHoleUI.TextPrimary, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -50f), new Vector2(360f, 150f));

            // --- time-dilation comparison: two clocks, each clearly labelled.
            // All strings are assigned in RefreshText (runs every Update) so a
            // language toggle applies immediately.
            clockHeader = BlackHoleUI.MakeText(panel, "Clock Header", 14, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(200f, 122f), new Vector2(360f, 18f));

            farHand = BuildClock("Observer Clock", new Vector2(110f, 78f));
            probeHand = BuildClock("Probe Clock", new Vector2(285f, 78f));

            farLabel = BlackHoleUI.MakeText(panel, "Observer Label", 15, BlackHoleUI.TextPrimary, TextAnchor.UpperCenter,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(110f, 46f), new Vector2(170f, 40f));

            probeLabel = BlackHoleUI.MakeText(panel, "Probe Label", 15, BlackHoleUI.TextPrimary, TextAnchor.UpperCenter,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(285f, 46f), new Vector2(190f, 40f));

            RefreshText();
        }

        RectTransform BuildClock(string name, Vector2 pos)
        {
            var face = BlackHoleUI.MakeImage(panel, name, BlackHoleUI.CircleSprite,
                new Color(1f, 1f, 1f, 0.55f),
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), pos, new Vector2(52f, 52f));

            var hand = BlackHoleUI.MakeImage(face.transform, "Hand", null, BlackHoleUI.Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3.5f, 20f));
            return hand.rectTransform;
        }

        void Teardown()
        {
            if (panel != null) DestroyImmediate(panel.gameObject);
            panel = null; title = null; body = null; farHand = null; probeHand = null;
            clockHeader = null; farLabel = null; probeLabel = null;
        }

        void Update()
        {
            RefreshText();
            if (!show || !Application.isPlaying) return;

            // Both clocks tick at their own gravitational rate — the observer
            // clock genuinely follows the camera's current distance.
            farAngle -= 120f * Time.deltaTime * Dilation(ObserverDistanceRs());
            probeAngle -= 120f * Time.deltaTime * Dilation(probeDistanceRs);
            if (farHand != null) farHand.localRotation = Quaternion.Euler(0f, 0f, farAngle);
            if (probeHand != null) probeHand.localRotation = Quaternion.Euler(0f, 0f, probeAngle);
        }

        /// <summary>Camera distance from the hole in Rs (∞ when not linked, so
        /// the observer falls back to the idealized far observer).</summary>
        float ObserverDistanceRs()
        {
            if (!observerFollowsCamera || controller == null) return float.PositiveInfinity;
            var cam = Camera.main;
            if (cam == null) return float.PositiveInfinity;
            float rs = Mathf.Max(controller.transform.lossyScale.x, 1e-4f);
            return (cam.transform.position - controller.transform.position).magnitude / rs;
        }

        /// <summary>Static-observer time dilation √(1 − Rs/r); 1 at infinity.</summary>
        static float Dilation(float rRs)
        {
            if (float.IsInfinity(rRs)) return 1f;
            return Mathf.Sqrt(Mathf.Max(1f - 1f / Mathf.Max(rRs, 1.0001f), 0.0001f));
        }

        /// <summary>Recomputes the readout; public so editor tooling can force
        /// a refresh before an on-demand render.</summary>
        public void RefreshText()
        {
            if (panel == null) return;
            panel.gameObject.SetActive(show);
            if (!show) return;

            double rsKm = RsKmPerSolarMass * massSolarMasses;
            double shadowKm = rsKm * 2.6 * 2.0;
            double earthDiams = shadowKm / 12742.0;
            // "1 hour on MY clock = X minutes on the probe": the ratio of the
            // two static-observer rates, using the camera's real distance.
            float obsRs = ObserverDistanceRs();
            float gObs = Dilation(obsRs);
            float gProbe = Dilation(probeDistanceRs);
            double probeMinutes = 60.0 * gProbe / gObs;

            // Inner-disk temperature scales as M^-1/4 (thin-disk theory);
            // anchored to ~12 million K for a 10-solar-mass hole.
            double diskTempK = 1.2e7 / System.Math.Pow(massSolarMasses / 10.0, 0.25);

            // Kerr row (only when the hole is actually spinning).
            float a = controller != null ? controller.spin : 0f;
            string spinRowKr = "", spinRowEn = "";
            if (a > 0.001f)
            {
                string h = BlackHoleController.HorizonRadiusM(a).ToString("0.00");
                string isco = BlackHoleController.IscoRadiusM(a).ToString("0.00");
                spinRowKr = "\n스핀        a = " + a.ToString("0.###") + " M · 지평선 " + h + "M · ISCO " + isco + "M";
                spinRowEn = "\nSpin          a = " + a.ToString("0.###") + " M · horizon " + h + "M · ISCO " + isco + "M";
            }

            title.text = Loc.T(massLabel, massLabelEn);
            body.text = Loc.T(
                "질량        " + FormatMass(massSolarMasses) + "\n" +
                "지평선 Rs   " + FormatKm(rsKm) + "\n" +
                "그림자 지름 " + FormatKm(shadowKm)
                    + (earthDiams >= 1.0 ? "  (지구 " + earthDiams.ToString("0.#") + "개)" : "") + "\n" +
                "원반 온도   " + FormatTempK(diskTempK) + "   ·   ISCO 속도  광속의 50%"
                + spinRowKr
                + (showDilationRow
                    ? "\n<color=#9AA3B5>내 시계 1시간 = 탐사선 " + probeMinutes.ToString("0.0") + "분</color>"
                    : ""),
                "Mass          " + FormatMassEn(massSolarMasses) + "\n" +
                "Horizon Rs   " + FormatKmEn(rsKm) + "\n" +
                "Shadow dia.  " + FormatKmEn(shadowKm)
                    + (earthDiams >= 1.0 ? "  (" + earthDiams.ToString("0.#") + " Earths)" : "") + "\n" +
                "Disk temp    " + FormatTempKEn(diskTempK) + "   ·   ISCO speed  0.5 c"
                + spinRowEn
                + (showDilationRow
                    ? "\n<color=#9AA3B5>1 hour on my clock = " + probeMinutes.ToString("0.0") + " min on the probe</color>"
                    : ""));

            if (clockHeader != null)
                clockHeader.text = Loc.T("— 같은 시간, 서로 다른 시계 —", "— same time, different clocks —");
            if (farLabel != null)
            {
                if (float.IsInfinity(obsRs))
                    farLabel.text = Loc.T("관찰자\n<size=12><color=#9AA3B5>멀리서 지켜보는 나</color></size>",
                                          "Observer\n<size=12><color=#9AA3B5>us, watching from afar</color></size>");
                else
                    farLabel.text = Loc.T("관찰자 (나)", "Observer (me)")
                        + "\n<size=12><color=#9AA3B5>r = " + obsRs.ToString("0.#")
                        + " Rs · ×" + gObs.ToString("0.000") + "</color></size>";
            }
            if (probeLabel != null)
                probeLabel.text = Loc.T(
                    "탐사선\n<size=12><color=#9AA3B5>블랙홀 곁 r = " + probeDistanceRs.ToString("0.0") + " Rs</color></size>",
                    "Probe\n<size=12><color=#9AA3B5>beside the hole, r = " + probeDistanceRs.ToString("0.0") + " Rs</color></size>");
        }

        /// <summary>Runtime API for hotkeys / UI buttons. Besides the numbers,
        /// mass presets change the visual physically: smaller holes have
        /// hotter, faster disks (T ∝ M^-1/4, orbital period ∝ M).</summary>
        public void SetMassPreset(MassPreset preset)
        {
            float scale = 0.5f, temp = 1.06f, flow = 0.9f;
            switch (preset)
            {
                case MassPreset.Stellar10:
                    massSolarMasses = 10; massLabel = "항성급 블랙홀 (초신성 잔해)";
                    massLabelEn = "Stellar-mass black hole (supernova remnant)";
                    scale = 0.28f; temp = 1.5f; flow = 2.2f; break;
                case MassPreset.SagittariusA:
                    massSolarMasses = 4.3e6; massLabel = "궁수자리 A* (우리은하 중심)";
                    massLabelEn = "Sagittarius A* (Milky Way center)";
                    scale = 0.5f; temp = 1.06f; flow = 0.9f; break;
                case MassPreset.M87:
                    massSolarMasses = 6.5e9; massLabel = "M87* (EHT 최초 관측 대상)";
                    massLabelEn = "M87* (first EHT image target)";
                    scale = 0.78f; temp = 0.86f; flow = 0.5f; break;
            }
            if (controller != null)
            {
                controller.transform.localScale = Vector3.one * scale;
                controller.diskTemperature = temp;
                controller.flowSpeed = flow;
                controller.Apply();
            }
            RefreshText();
        }

        static string FormatMass(double m)
        {
            if (m >= 1e8) return (m / 1e8).ToString("0.#") + "억 태양질량";
            if (m >= 1e4) return (m / 1e4).ToString("0.#") + "만 태양질량";
            return m.ToString("0.#") + " 태양질량";
        }

        static string FormatMassEn(double m)
        {
            if (m >= 1e9) return (m / 1e9).ToString("0.#") + " billion M☉";
            if (m >= 1e6) return (m / 1e6).ToString("0.#") + " million M☉";
            return m.ToString("0.#") + " M☉";
        }

        static string FormatTempK(double k)
        {
            if (k >= 1e8) return (k / 1e8).ToString("0.#") + "억 K";
            if (k >= 1e4) return (k / 1e4).ToString("0.#") + "만 K";
            return k.ToString("0") + " K";
        }

        static string FormatTempKEn(double k)
        {
            if (k >= 1e6) return (k / 1e6).ToString("0.#") + " million K";
            return k.ToString("#,##0") + " K";
        }

        static string FormatKm(double km)
        {
            if (km >= 1e8) return (km / 1.496e8).ToString("0.##") + " AU";
            if (km >= 1e4) return (km / 1e4).ToString("0.#") + "만 km";
            return km.ToString("0.#") + " km";
        }

        static string FormatKmEn(double km)
        {
            if (km >= 1e8) return (km / 1.496e8).ToString("0.##") + " AU";
            if (km >= 1e6) return (km / 1e6).ToString("0.##") + " million km";
            if (km >= 1e4) return (km / 1e3).ToString("0.#") + " thousand km";
            return km.ToString("0.#") + " km";
        }
    }
}

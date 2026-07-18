using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackHoleEffect; // Loc, BlackHoleUI

namespace MilkyWay
{
    /// <summary>
    /// The solar-system showcase's click-only control surface. Clicking a planet
    /// still zooms to it (a scene interaction); this bar carries the rest —
    /// planet tour, the scale-truth reveal, sound, and the scene hops — so the
    /// exhibit needs no keyboard and runs in a browser. Hides while a tour or
    /// the scale-truth demo is playing.
    /// </summary>
    [DisallowMultipleComponent]
    public class SolarToolbar : MonoBehaviour
    {
        public SolarSystemControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        readonly List<GameObject> rows = new();
        int locVersion = -1;
        bool shown = true;

        const float BtnW = 156f, BtnH = 40f, Gap = 8f, RowPitch = 46f;

        void Start()
        {
            if (controls == null) controls = GetComponent<SolarSystemControls>();
            if (controls == null) return;
            Build();
        }

        void Update()
        {
            if (locVersion != Loc.Version)
            {
                locVersion = Loc.Version;
                foreach (var (label, text) in localized)
                    if (label != null) label.text = text();
            }
            bool wantShown = !controls.Busy;
            if (wantShown != shown)
            {
                shown = wantShown;
                foreach (var r in rows) if (r != null) r.SetActive(shown);
            }
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);

            var actions = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("행성 투어", "Planet tour", "惑星ツアー", "行星导览"), controls.ToggleTour),
                (() => Loc.T("진짜 크기", "True scale", "本当の縮尺", "真实比例"), controls.ToggleScaleTruth),
                (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
            };
            var scenes = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("🏠 타이틀", "🏠 Title", "🏠 タイトル", "🏠 标题"), () => SolarSystemControls.LoadScene("TitleScreen")),
                (() => Loc.T("은하 전시", "Milky Way", "銀河展示", "银河展区"), () => SolarSystemControls.LoadScene("MilkyWayShowcase")),
            };

            BuildRow(canvas.transform, "Solar Toolbar Actions", actions, 20f + RowPitch);
            BuildRow(canvas.transform, "Solar Toolbar Scenes", scenes, 20f);
        }

        void BuildRow(Transform parent, string name,
            (System.Func<string> text, UnityEngine.Events.UnityAction act)[] items, float y)
        {
            float total = items.Length * BtnW + (items.Length - 1) * Gap;
            float x = -total * 0.5f + BtnW * 0.5f;
            foreach (var (text, act) in items)
            {
                var btn = BlackHoleUI.MakeButton(parent, name + " / " + text(), text(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, y),
                    new Vector2(BtnW, BtnH), act);
                var label = btn.GetComponentInChildren<Text>();
                if (label != null) { label.fontSize = 15; localized.Add((label, text)); }
                rows.Add(btn.gameObject);
                x += BtnW + Gap;
            }
        }
    }
}

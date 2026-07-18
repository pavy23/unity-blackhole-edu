using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackHoleEffect; // Loc, BlackHoleUI, ExhibitBar, SceneNavigator

namespace MilkyWay
{
    /// <summary>
    /// The solar-system exhibit's click-only control bar. Clicking a planet
    /// still zooms to it; this grouped bar carries the guided experiences and
    /// the sound toggle, and the corner cluster moves to the other exhibits.
    /// Both hide while a tour or the scale-truth demo plays.
    /// </summary>
    [DisallowMultipleComponent]
    public class SolarToolbar : MonoBehaviour
    {
        public SolarSystemControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        GameObject bar;
        SceneNavigator nav;
        int locVersion = -1;
        bool shown = true;

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
                if (bar != null) bar.SetActive(shown);
                if (nav != null) nav.SetVisible(shown);
            }
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);

            var groups = new[]
            {
                new ExhibitBar.Group {
                    label = () => Loc.T("체험", "Experience", "体験", "体验"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("행성 투어", "Planet tour", "惑星ツアー", "行星导览"), controls.ToggleTour),
                        (() => Loc.T("진짜 크기", "True scale", "本当の縮尺", "真实比例"), controls.ToggleScaleTruth),
                        (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
                    }},
            };

            var (panel, loc) = ExhibitBar.Build(canvas.transform, groups);
            bar = panel;
            localized.AddRange(loc);

            nav = new GameObject("Scene Navigator").AddComponent<SceneNavigator>();
            nav.Init(new[] {
                new SceneNavigator.Dest { scene = "MilkyWayShowcase",
                    name = () => Loc.T("우리은하", "Milky Way", "天の川銀河", "银河系"), image = "TitleCards/card_galaxy" },
                new SceneNavigator.Dest { scene = "BlackHoleShowcase",
                    name = () => Loc.T("블랙홀", "Black Hole", "ブラックホール", "黑洞"), image = "TitleCards/card_blackhole" },
            });
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackHoleEffect; // Loc, BlackHoleUI, ExhibitBar, SceneNavigator

namespace MilkyWay
{
    /// <summary>
    /// The Milky Way exhibit's click-only control bar. Its nine experiences are
    /// grouped by kind — cinematic journeys, guided tours &amp; labs, the core —
    /// so the long list reads as a few clear themes. Moving to another exhibit
    /// is the corner thumbnail cluster. Both hide while an experience plays.
    /// </summary>
    [DisallowMultipleComponent]
    public class MilkyWayToolbar : MonoBehaviour
    {
        public MilkyWayControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        GameObject bar;
        SceneNavigator nav;
        int locVersion = -1;
        bool shown = true;

        void Start()
        {
            if (controls == null) controls = GetComponent<MilkyWayControls>();
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
                    label = () => Loc.T("여행", "Journeys", "旅", "旅程"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("줌 여행", "Zoom journey", "ズームの旅", "缩放之旅"), controls.PlayJourney),
                        (() => Loc.T("밤하늘", "Night sky", "夜空", "夜空"), controls.PlayNightSky),
                        (() => Loc.T("안드로메다", "Andromeda", "アンドロメダ", "仙女座"), controls.PlayAndromeda),
                        (() => Loc.T("우주 줌아웃", "Cosmic zoom-out", "宇宙ズームアウト", "宇宙缩放"), controls.PlayCosmicZoom),
                    }},
                new ExhibitBar.Group {
                    label = () => Loc.T("투어·실험", "Tours & labs", "ツアー・実験", "导览·实验"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("은하 투어", "Galaxy tour", "銀河ツアー", "星系导览"), controls.ToggleTour),
                        (() => Loc.T("태양계 투어", "Solar tour", "太陽系ツアー", "太阳系之旅"), controls.ToggleSolarTour),
                        (() => Loc.T("회전 곡선", "Rotation curve", "回転曲線", "旋转曲线"), controls.ToggleRotationLab),
                        (() => Loc.T("은하 동물원", "Galaxy zoo", "銀河動物園", "星系动物园"), controls.ToggleZoo),
                    }},
                new ExhibitBar.Group {
                    label = () => Loc.T("은하 중심", "The core", "銀河中心", "银河中心"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("궁수자리 A*", "Sagittarius A*", "いて座A*", "人马座A*"), controls.PlaySgrA),
                        (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
                    }},
            };

            var (panel, loc) = ExhibitBar.Build(canvas.transform, groups);
            bar = panel;
            localized.AddRange(loc);

            nav = new GameObject("Scene Navigator").AddComponent<SceneNavigator>();
            nav.Init(new[] {
                new SceneNavigator.Dest { scene = "SolarSystemShowcase",
                    name = () => Loc.T("태양계", "Solar System", "太陽系", "太阳系"), image = "TitleCards/card_solar" },
                new SceneNavigator.Dest { scene = "NebulaShowcase",
                    name = () => Loc.T("성운과 성단", "Nebulae & Clusters", "星雲と星団", "星云与星团"), image = "TitleCards/card_nebula" },
                new SceneNavigator.Dest { scene = "BlackHoleShowcase",
                    name = () => Loc.T("블랙홀", "Black Hole", "ブラックホール", "黑洞"), image = "TitleCards/card_blackhole" },
            });
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// The black-hole exhibit's desktop control surface, click-only. The actions
    /// are grouped by kind in a single contained bar (see <see cref="ExhibitBar"/>);
    /// moving to another exhibit is a separate thumbnail cluster in the corner
    /// (see <see cref="SceneNavigator"/>). Both hide while a cinematic or the
    /// guided tour owns the screen. Buttons call straight into
    /// <see cref="DesktopControls"/> so the four language variants stay there.
    /// </summary>
    [DisallowMultipleComponent]
    public class DesktopToolbar : MonoBehaviour
    {
        public DesktopControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        GameObject bar;
        SceneNavigator nav;
        int locVersion = -1;
        bool shown = true;

        void Start()
        {
            if (controls == null) controls = GetComponent<DesktopControls>();
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

            bool wantShown = !(controls.CinematicBusy || controls.Immersive);
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
                        (() => Loc.T("가이드 투어", "Guided tour", "ガイドツアー", "导览"), controls.ToggleTour),
                        (() => Loc.T("블랙홀 탄생", "Birth", "誕生", "黑洞诞生"), controls.PlayIntro),
                        (() => Loc.T("낙하 체험", "Fall in", "落下体験", "坠入体验"), controls.BeginFallIn),
                        (() => Loc.T("블랙홀 병합", "Merger", "合体", "黑洞合并"), controls.BeginMerger),
                    }},
                new ExhibitBar.Group {
                    label = () => Loc.T("블랙홀", "Black hole", "ブラックホール", "黑洞"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("원반 색상", "Disk colors", "円盤の色", "吸积盘颜色"), controls.CycleColor),
                        (() => Loc.T("질량", "Mass", "質量", "质量"), controls.CycleMass),
                        (() => Loc.T("스핀", "Spin", "スピン", "自旋"), controls.CycleSpin),
                        (() => Loc.T("관측사진", "EHT photo", "観測写真", "观测照片"), controls.CycleComparison),
                        (() => Loc.T("설명 난이도", "Level", "難易度", "难度"), controls.CycleDifficulty),
                    }},
                new ExhibitBar.Group {
                    label = () => Loc.T("현상", "Phenomena", "現象", "现象"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("아인슈타인 링", "Einstein ring", "アインシュタイン環", "爱因斯坦环"), controls.ToggleEinstein),
                        (() => Loc.T("스파게티화", "Spaghettify", "スパゲッティ化", "面条化"), controls.ToggleSpaghetti),
                        (() => Loc.T("제트", "Jets", "ジェット", "喷流"), controls.ToggleJets),
                        (() => Loc.T("렌즈", "Lens", "レンズ", "透镜"), controls.ToggleLens),
                        (() => Loc.T("광도곡선", "Light curve", "光度曲線", "光变曲线"), controls.ToggleLightCurve),
                        (() => Loc.T("광자 발사", "Fire photons", "光子発射", "发射光子"), controls.FirePhotons),
                    }},
                new ExhibitBar.Group {
                    label = () => Loc.T("보기", "View", "表示", "视图"),
                    items = new (System.Func<string>, UnityEngine.Events.UnityAction)[] {
                        (() => Loc.T("이름표", "Labels", "名札", "标签"), controls.ToggleLabels),
                        (() => Loc.T("물리 패널", "Data panel", "データ", "数据面板"), controls.TogglePanel),
                        (() => Loc.T("수식", "Formulas", "数式", "公式"), controls.ToggleTheory),
                        (() => Loc.T("몰입 보기", "Immersive", "没入", "沉浸"), () => controls.SetImmersive(!controls.Immersive)),
                        (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
                        (() => Loc.T("시점 리셋", "Reset view", "視点リセット", "重置视角"), controls.ResetCamera),
                    }},
            };

            var (panel, loc) = ExhibitBar.Build(canvas.transform, groups);
            bar = panel;
            localized.AddRange(loc);

            nav = new GameObject("Scene Navigator").AddComponent<SceneNavigator>();
            nav.Init(new[] {
                new SceneNavigator.Dest { scene = "MilkyWayShowcase",
                    name = () => Loc.T("우리은하", "Milky Way", "天の川銀河", "银河系"), image = "TitleCards/card_galaxy" },
                new SceneNavigator.Dest { scene = "SolarSystemShowcase",
                    name = () => Loc.T("태양계", "Solar System", "太陽系", "太阳系"), image = "TitleCards/card_solar" },
                new SceneNavigator.Dest { scene = "NebulaShowcase",
                    name = () => Loc.T("성운과 성단", "Nebulae & Clusters", "星雲と星団", "星云与星团"), image = "TitleCards/card_nebula" },
            });
        }
    }
}

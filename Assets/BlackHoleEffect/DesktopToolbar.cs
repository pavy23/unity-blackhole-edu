using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// The desktop control surface, click-only. Every feature that used to be a
    /// keyboard shortcut is a button here, grouped into rows along the bottom of
    /// the screen — the same pattern the MR hand-menu uses, but screen-space.
    /// This is what lets the exhibit run in a browser, where keyboard shortcuts
    /// collide with the browser's own (F5 reloads, F11 fullscreens, …).
    ///
    /// The buttons call straight into <see cref="DesktopControls"/>' public
    /// methods, so the cycles, toasts and four language variants stay in one
    /// place. The whole bar hides while a narrated cinematic or the guided tour
    /// owns the screen, and while the immersive view is on.
    /// </summary>
    [DisallowMultipleComponent]
    public class DesktopToolbar : MonoBehaviour
    {
        public DesktopControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        readonly List<GameObject> rows = new();
        int locVersion = -1;
        bool shown = true;

        const float BtnW = 138f, BtnH = 40f, Gap = 8f, RowPitch = 46f;

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

            // Give the screen to the tours and cinematics (they narrate and put
            // their own stop button up), and to the immersive view.
            bool wantShown = !(controls.CinematicBusy || controls.Immersive);
            if (wantShown != shown)
            {
                shown = wantShown;
                foreach (var r in rows) if (r != null) r.SetActive(shown);
            }
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);

            var experiences = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("가이드 투어", "Guided tour", "ガイドツアー", "导览"), controls.ToggleTour),
                (() => Loc.T("블랙홀 탄생", "Birth", "誕生", "黑洞诞生"), controls.PlayIntro),
                (() => Loc.T("낙하 체험", "Fall in", "落下体験", "坠入体验"), controls.BeginFallIn),
                (() => Loc.T("블랙홀 병합", "Merger", "合体", "黑洞合并"), controls.BeginMerger),
            };
            var blackHole = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("원반 색상", "Disk colors", "円盤の色", "吸积盘颜色"), controls.CycleColor),
                (() => Loc.T("질량", "Mass", "質量", "质量"), controls.CycleMass),
                (() => Loc.T("스핀", "Spin", "スピン", "自旋"), controls.CycleSpin),
                (() => Loc.T("관측사진", "EHT photo", "観測写真", "观测照片"), controls.CycleComparison),
                (() => Loc.T("설명 난이도", "Level", "難易度", "难度"), controls.CycleDifficulty),
            };
            var phenomena = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("아인슈타인 링", "Einstein ring", "アインシュタイン環", "爱因斯坦环"), controls.ToggleEinstein),
                (() => Loc.T("스파게티화", "Spaghettify", "スパゲッティ化", "面条化"), controls.ToggleSpaghetti),
                (() => Loc.T("제트", "Jets", "ジェット", "喷流"), controls.ToggleJets),
                (() => Loc.T("렌즈", "Lens", "レンズ", "透镜"), controls.ToggleLens),
                (() => Loc.T("광도곡선", "Light curve", "光度曲線", "光变曲线"), controls.ToggleLightCurve),
                (() => Loc.T("광자 발사", "Fire photons", "光子発射", "发射光子"), controls.FirePhotons),
            };
            var display = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("이름표", "Labels", "名札", "标签"), controls.ToggleLabels),
                (() => Loc.T("물리 패널", "Data panel", "データ", "数据面板"), controls.TogglePanel),
                (() => Loc.T("수식", "Formulas", "数式", "公式"), controls.ToggleTheory),
                (() => Loc.T("몰입 보기", "Immersive", "没入", "沉浸"), () => controls.SetImmersive(!controls.Immersive)),
                (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
                (() => Loc.T("시점 리셋", "Reset view", "視点リセット", "重置视角"), controls.ResetCamera),
                (() => Loc.T("🏠 타이틀", "🏠 Title", "🏠 タイトル", "🏠 标题"), () => DesktopControls.LoadScene("TitleScreen")),
                (() => Loc.T("은하 전시", "Milky Way", "銀河展示", "银河展区"), () => DesktopControls.LoadScene("MilkyWayShowcase")),
            };

            BuildRow(canvas.transform, "Toolbar Experiences", experiences, 20f + RowPitch * 3f);
            BuildRow(canvas.transform, "Toolbar BlackHole", blackHole, 20f + RowPitch * 2f);
            BuildRow(canvas.transform, "Toolbar Phenomena", phenomena, 20f + RowPitch);
            BuildRow(canvas.transform, "Toolbar Display", display, 20f);
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

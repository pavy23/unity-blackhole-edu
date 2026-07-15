using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// The MR stand-in for the keyboard: a button menu along the bottom of the
    /// world-space frame, reachable with a hand ray. Every button calls straight
    /// into <see cref="DesktopControls"/> — the cycles, the toasts and the four
    /// language variants live there, and duplicating them here would guarantee
    /// the two drift apart.
    /// </summary>
    [DisallowMultipleComponent]
    public class MRControls : MonoBehaviour
    {
        public DesktopControls controls;
        public Transform hole;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        int locVersion = -1;

        void Start()
        {
            if (controls == null) controls = GetComponent<DesktopControls>();
            if (controls == null) return;
            Build();
        }

        void Update()
        {
            if (locVersion == Loc.Version) return;
            locVersion = Loc.Version;
            foreach (var (label, text) in localized)
                if (label != null) label.text = text();
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(GetComponentInChildren<Camera>() ?? Camera.main);

            // Two rows hugging the bottom edge of the frame, mirroring how the
            // desktop help bar groups the features.
            var row1 = new (System.Func<string> text, UnityEngine.Events.UnityAction act)[]
            {
                (() => Loc.T("가이드 투어", "Guided tour", "ガイドツアー", "导览"), ToggleTour),
                (() => Loc.T("블랙홀 병합", "Merger", "ブラックホール合体", "黑洞合并"), BeginMerger),
                (() => Loc.T("원반 색상", "Disk colors", "円盤の色", "吸积盘颜色"), () => controls.CycleColor()),
                (() => Loc.T("질량", "Mass", "質量", "质量"), () => controls.CycleMass()),
                (() => Loc.T("스핀", "Spin", "スピン", "自旋"), CycleSpin),
                (() => Loc.T("관측사진", "EHT photo", "観測写真", "观测照片"), () => controls.CycleComparison()),
                (() => Loc.T("설명 난이도", "Level", "難易度", "难度"), () => controls.CycleDifficulty()),
            };

            var row2 = new (System.Func<string> text, UnityEngine.Events.UnityAction act)[]
            {
                (() => Loc.T("아인슈타인 링", "Einstein ring", "アインシュタイン環", "爱因斯坦环"), () => controls.ToggleEinstein()),
                (() => Loc.T("스파게티화", "Spaghettify", "スパゲッティ化", "面条化"), () => controls.ToggleSpaghetti()),
                (() => Loc.T("제트", "Jets", "ジェット", "喷流"), () => controls.ToggleJets()),
                (() => Loc.T("렌즈", "Lens", "レンズ", "透镜"), () => controls.ToggleLens()),
                (() => Loc.T("광도곡선", "Light curve", "光度曲線", "光变曲线"), () => controls.ToggleLightCurve()),
                (() => Loc.T("수식", "Formulas", "数式", "公式"), ToggleTheory),
            };

            BuildRow(canvas.transform, "MR Menu Row 1", row1, 92f);
            BuildRow(canvas.transform, "MR Menu Row 2", row2, 26f);
        }

        void BuildRow(Transform parent, string name,
            (System.Func<string> text, UnityEngine.Events.UnityAction act)[] items, float y)
        {
            const float w = 224f, h = 58f, gap = 10f;
            float total = items.Length * w + (items.Length - 1) * gap;
            float x = -total * 0.5f + w * 0.5f;

            foreach (var (text, act) in items)
            {
                var btn = BlackHoleUI.MakeButton(parent, name + " / " + text(), text(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, y), new Vector2(w, h), act);
                var label = btn.GetComponentInChildren<Text>();
                if (label != null) localized.Add((label, text));
                x += w + gap;
            }
        }

        void ToggleTour()
        {
            var tour = controls.tour;
            if (tour == null) return;
            if (tour.Running) tour.StopTour();
            else if (!controls.CinematicBusy) tour.StartTour();
        }

        void ToggleTheory()
        {
            if (controls.Theory != null) controls.Theory.Toggle();
        }

        void BeginMerger()
        {
            if (controls.Binary != null && !controls.CinematicBusy) controls.Binary.Begin();
        }

        void CycleSpin()
        {
            // The merger owns the spin while it runs (it ramps to the Kerr remnant).
            if (controls.Binary != null && controls.Binary.Running) return;
            controls.CycleSpin();
        }
    }
}

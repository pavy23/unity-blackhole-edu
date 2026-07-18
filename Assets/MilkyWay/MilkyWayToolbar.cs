using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackHoleEffect; // Loc, BlackHoleUI

namespace MilkyWay
{
    /// <summary>
    /// The Milky Way showcase's click-only control surface: the nine experiences
    /// and the scene hops as buttons along the bottom of the screen. Replaces
    /// the keyboard shortcuts so the exhibit runs in a browser. Hides while any
    /// experience is playing (they narrate and show their own stop button).
    /// </summary>
    [DisallowMultipleComponent]
    public class MilkyWayToolbar : MonoBehaviour
    {
        public MilkyWayControls controls;

        readonly List<(Text label, System.Func<string> text)> localized = new();
        readonly List<GameObject> rows = new();
        int locVersion = -1;
        bool shown = true;

        const float BtnW = 150f, BtnH = 40f, Gap = 8f, RowPitch = 46f;

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
                foreach (var r in rows) if (r != null) r.SetActive(shown);
            }
        }

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(Camera.main);

            var experiencesA = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("줌 여행", "Zoom journey", "ズームの旅", "缩放之旅"), controls.PlayJourney),
                (() => Loc.T("밤하늘", "Night sky", "夜空", "夜空"), controls.PlayNightSky),
                (() => Loc.T("안드로메다", "Andromeda", "アンドロメダ", "仙女座"), controls.PlayAndromeda),
                (() => Loc.T("은하 투어", "Galaxy tour", "銀河ツアー", "星系导览"), controls.ToggleTour),
            };
            var experiencesB = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("우주 줌아웃", "Cosmic zoom-out", "宇宙ズームアウト", "宇宙缩放"), controls.PlayCosmicZoom),
                (() => Loc.T("태양계 투어", "Solar tour", "太陽系ツアー", "太阳系之旅"), controls.ToggleSolarTour),
                (() => Loc.T("회전 곡선", "Rotation curve", "回転曲線", "旋转曲线"), controls.ToggleRotationLab),
                (() => Loc.T("은하 동물원", "Galaxy zoo", "銀河動物園", "星系动物园"), controls.ToggleZoo),
                (() => Loc.T("궁수자리 A*", "Sagittarius A*", "いて座A*", "人马座A*"), controls.PlaySgrA),
            };
            var scenes = new (System.Func<string>, UnityEngine.Events.UnityAction)[]
            {
                (() => Loc.T("소리", "Sound", "音", "声音"), controls.ToggleMute),
                (() => Loc.T("🏠 타이틀", "🏠 Title", "🏠 タイトル", "🏠 标题"), () => MilkyWayControls.LoadScene("TitleScreen")),
                (() => Loc.T("태양계 전시", "Solar system", "太陽系展示", "太阳系展区"), () => MilkyWayControls.LoadScene("SolarSystemShowcase")),
            };

            BuildRow(canvas.transform, "MW Toolbar A", experiencesA, 20f + RowPitch * 2f);
            BuildRow(canvas.transform, "MW Toolbar B", experiencesB, 20f + RowPitch);
            BuildRow(canvas.transform, "MW Toolbar Scenes", scenes, 20f);
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

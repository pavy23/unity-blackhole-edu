using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Guided tour: steps through the physics of the scene one concept at a
    /// time — highlighting the matching label, triggering the matching demo,
    /// and showing narration text at the bottom of the view.
    /// G = start/stop, N/→ = next, B/← = previous (wired in DesktopControls).
    /// </summary>
    public class GuidedTour : MonoBehaviour
    {
        public BlackHoleAnnotations annotations;
        public BlackHolePhysicsPanel panel;
        public EinsteinRingDemo einsteinDemo;
        public PhotonLauncher launcher;
        public SpaghettificationDemo spaghetti;
        public RelativisticJets jets;
        public ObservationComparison comparison;

        public bool Running { get; private set; }

        /// <summary>Current step index — the theory panel mirrors it.</summary>
        public int CurrentStep => step;

        bool prevPanelShow, prevComparisonShow;
        int step;
        RectTransform card;
        UnityEngine.UI.Text cardTitle, cardBody, cardFooter;

        struct Step
        {
            public string title, body;
            public string titleEn, bodyEn;
            public int focus;                 // annotation index to highlight, -1 = all
            public System.Action<GuidedTour> enter;
        }

        // The narration clips (Resources/Narration/tour_N and Narration/en/
        // tour_N) are generated from these lines — spoken versions of the card
        // bodies without the on-screen key hints. Public so tooling can
        // regenerate the audio.
        public static readonly string[] NarrationLines =
        {
            "블랙홀 여행에 오신 것을 환영합니다. 지금 보고 있는 모든 것은 아인슈타인의 일반상대성이론으로 계산된 것입니다. 빛이 실제로 휘어지는 경로를 따라가며, 하나씩 살펴봅시다.",
            "가운데 검은 원은 빛조차 탈출할 수 없는 영역입니다. 실제 그림자는 사건의 지평선보다 2.6배 크게 보입니다. 중력이 주변의 빛까지 삼키기 때문입니다.",
            "그림자 가장자리의 밝은 테두리는, 빛이 블랙홀 주위를 몇 바퀴나 돌다가 간신히 빠져나온 것입니다. 이곳에서 빛은 슈바르츠실트 반지름의 1.5배 되는 곳에서 원 궤도를 돕니다.",
            "빛나는 원반은 블랙홀로 빨려 들어가는 뜨거운 가스입니다. 원반 위아래로 보이는 고리는, 블랙홀 뒤쪽 원반의 빛이 휘어져 보이는 중력 렌즈 효과입니다.",
            "원반의 한쪽이 더 밝은 이유는, 가스가 광속의 절반으로 회전하고 있어서, 다가오는 쪽의 빛이 상대론적으로 증폭되기 때문입니다.",
            "왼쪽 위의 두 시계를 보세요. 관찰자는 블랙홀에서 멀리 떨어진 안전한 곳의 우리이고, 탐사선은 블랙홀 바로 옆까지 내려간 무인 탐사선입니다. 블랙홀 가까이에서는 시간이 느리게 흘러서, 밖에서 한 시간이 지나는 동안 탐사선의 시계는 24분밖에 가지 않습니다.",
            "블랙홀 뒤의 별 하나가 좌우로 움직이고 있습니다. 별이 정확히 뒤에 올 때, 빛이 사방으로 휘어져 완전한 고리가 됩니다. 이것이 아인슈타인 링입니다.",
            "광자들을 발사했습니다. 블랙홀에 가까이 지나갈수록 궤적이 크게 휘고, 임계 거리 안쪽의 광자는 붉은색으로 표시되며 포획됩니다. 빛의 궤도는 언제나 블랙홀 중심을 지나는 하나의 평면 안에 있습니다.",
            "떨어지는 작은 별을 보세요. 머리와 발에 걸리는 중력의 차이, 즉 조석력 때문에 길게 늘어나며 국수처럼 찢어집니다. 과학자들은 이것을 스파게티화라고 부릅니다.",
            "일부 블랙홀은 삼킨 물질의 일부를 자기장으로 감아 올려, 양극으로 뿜어냅니다. 이 제트는 거의 광속으로 수천 광년을 날아갑니다.",
            "이 모든 현상이 단 하나의 방정식, 측지선 방정식에서 나옵니다. 이제 자유롭게 탐험해 보세요.",
        };

        public static readonly string[] NarrationLinesEn =
        {
            "Welcome to the black hole tour. Everything you see here is computed from Einstein's general theory of relativity. Let's follow the paths that light actually bends along, one step at a time.",
            "The dark circle in the center is the region from which not even light can escape. The shadow looks 2.6 times larger than the event horizon itself, because gravity swallows the light passing nearby as well.",
            "The bright rim at the edge of the shadow is light that circled the black hole several times before barely escaping. Here, light orbits in a circle at one and a half times the Schwarzschild radius.",
            "The glowing disk is hot gas spiraling into the black hole. The rings above and below it are gravitational lensing — light from the far side of the disk, bent over and under the hole.",
            "One side of the disk looks brighter because the gas rotates at half the speed of light, and the light from the approaching side is relativistically amplified.",
            "Look at the two clocks in the upper left. The observer is us, watching from a safe distance. The probe is an unmanned spacecraft sent right next to the black hole. Time flows more slowly near the black hole — while one hour passes outside, the probe's clock ticks only twenty-four minutes.",
            "A single star is moving back and forth behind the black hole. When it lines up exactly behind, its light bends around in every direction and becomes a complete ring. This is an Einstein ring.",
            "Photons away! The closer a photon passes to the black hole, the more its path bends. Photons inside the critical distance are shown in red and captured. A light ray's orbit always stays within a single plane through the center of the black hole.",
            "Watch the little falling star. The difference in gravity between its near and far side — the tidal force — stretches it out like a noodle. Scientists call this spaghettification.",
            "Some black holes wind part of the matter they swallow into magnetic fields and blast it out from their poles. These jets fly for thousands of light-years at nearly the speed of light.",
            "All of these phenomena come from a single equation — the geodesic equation. Now, explore freely.",
        };

        static readonly Step[] Steps =
        {
            new Step { title = "블랙홀 여행에 오신 것을 환영합니다", focus = -1,
                body = "지금 보고 있는 모든 것은 아인슈타인의 일반상대성이론으로 계산된 것입니다.\n빛이 실제로 휘어지는 경로를 따라가며 하나씩 살펴봅시다.",
                titleEn = "Welcome to the Black Hole Tour",
                bodyEn = "Everything you see is computed from Einstein's general relativity.\nLet's follow the paths light actually bends along, one step at a time." },
            new Step { title = "1. 사건의 지평선 그림자", focus = 0,
                body = "가운데 검은 원은 빛조차 탈출할 수 없는 영역입니다.\n실제 그림자는 사건의 지평선보다 2.6배 크게 보입니다 — 중력이 주변 빛까지 삼키기 때문입니다.",
                titleEn = "1. The Event Horizon Shadow",
                bodyEn = "The dark circle is the region not even light can escape.\nThe shadow looks 2.6× larger than the horizon itself — gravity swallows nearby light too." },
            new Step { title = "2. 광자 고리", focus = 1,
                body = "그림자 가장자리의 밝은 테두리는 빛이 블랙홀 주위를 몇 바퀴나 돌다가\n빠져나온 것입니다. 이곳에서 빛은 반지름 1.5 Rs의 원 궤도를 돕니다.",
                titleEn = "2. The Photon Ring",
                bodyEn = "The bright rim is light that circled the hole several times before\nescaping. Here light orbits in a circle of radius 1.5 Rs." },
            new Step { title = "3. 강착원반과 ISCO", focus = 2,
                body = "빛나는 원반은 빨려 들어가는 가스입니다. 원반 위아래로 보이는 고리는\n블랙홀 뒤쪽 원반의 빛이 휘어져 보이는 것 — 중력 렌즈 효과입니다.\n안쪽 가장자리(ISCO) 안에서는 안정된 궤도가 존재할 수 없습니다.",
                titleEn = "3. Accretion Disk & ISCO",
                bodyEn = "The glowing disk is infalling gas. The rings above and below it are the\nfar side of the disk, gravitationally lensed over and under the hole.\nInside the inner edge (ISCO) no stable orbit can exist." },
            new Step { title = "4. 도플러 비밍", focus = 3,
                body = "원반의 한쪽이 더 밝은 이유: 가스가 광속의 절반으로 회전하기 때문에\n다가오는 쪽의 빛이 상대론적으로 증폭됩니다. (숫자 1~3으로 원반 색도 바꿔보세요)",
                titleEn = "4. Doppler Beaming",
                bodyEn = "One side is brighter because the gas rotates at half the speed of light,\nso light from the approaching side is relativistically boosted. (Try colors 1–3)",
                enter = t => { } },
            new Step { title = "5. 중력 시간 지연", focus = -1,
                body = "왼쪽 위의 두 시계 — <color=#FFC46E>관찰자</color>는 멀리 안전한 곳의 우리,\n<color=#FFC46E>탐사선</color>은 블랙홀 바로 옆(r = 1.2 Rs)까지 내려간 무인 탐사선입니다.\n밖에서 1시간이 지나는 동안 탐사선의 시계는 24분만 갑니다.",
                titleEn = "5. Gravitational Time Dilation",
                bodyEn = "Top-left clocks — the <color=#FFC46E>observer</color> is us, far away and safe;\nthe <color=#FFC46E>probe</color> is a craft sent down to r = 1.2 Rs beside the hole.\nWhile 1 hour passes outside, the probe's clock ticks only 24 minutes.",
                enter = t => { if (t.panel != null) { t.panel.show = true; t.panel.probeDistanceRs = 1.2f; t.panel.RefreshText(); } } },
            new Step { title = "6. 아인슈타인 링", focus = -1,
                body = "블랙홀 뒤의 별 하나가 좌우로 움직이고 있습니다.\n별이 정확히 뒤에 올 때 빛이 사방으로 휘어 완전한 고리가 됩니다. (A/D로 직접 움직여보세요)",
                titleEn = "6. The Einstein Ring",
                bodyEn = "A star is sweeping back and forth behind the hole. When it lines up\nexactly, its light bends into a complete ring. (Move it yourself with A/D)",
                enter = t => { if (t.einsteinDemo != null) { t.einsteinDemo.active = true; t.einsteinDemo.autoSweep = true; } } },
            new Step { title = "7. 빛의 궤적", focus = -1,
                body = "블랙홀에 가까이 지나갈수록 궤적이 크게 휘고, 임계 거리(2.6 Rs) 안쪽의\n광자(빨간색)는 포획됩니다. 빛의 궤도는 언제나 블랙홀 중심을 지나는 평면 안에\n있습니다 — 옆에서는 평면이 얇아 안 보이므로 궤적이 화면을 따라 돕니다. (Space 재발사)",
                titleEn = "7. Light Trajectories",
                bodyEn = "The closer a photon passes, the more it bends; inside the critical distance\n(2.6 Rs) photons (red) are captured. Light orbits always lie in a plane through\nthe hole — edge-on it vanishes, so the plane turns to face you. (Space to refire)",
                enter = t => { if (t.launcher != null) t.launcher.FireSweep(); } },
            new Step { title = "8. 스파게티화", focus = -1,
                body = "떨어지는 작은 별을 보세요. 머리와 발에 걸리는 중력 차이(조석력) 때문에\n길게 늘어나며 국수처럼 찢어집니다. 항성급 블랙홀일수록 이 효과는 극단적입니다.",
                titleEn = "8. Spaghettification",
                bodyEn = "Watch the little falling star. The gravity difference between its near and\nfar side (tidal force) stretches it like a noodle. Stellar-mass holes are extreme.",
                enter = t => { if (t.spaghetti != null) t.spaghetti.active = true; } },
            new Step { title = "9. 상대론적 제트", focus = -1,
                body = "일부 블랙홀은 삼킨 물질의 일부를 자기장으로 감아 양극으로 뿜어냅니다.\n이 제트는 거의 광속으로 수천 광년을 날아갑니다 — M87*의 제트가 유명합니다.",
                titleEn = "9. Relativistic Jets",
                bodyEn = "Some holes wind swallowed matter into magnetic fields and eject it from\nthe poles at near light speed for thousands of light-years — M87* is famous.",
                enter = t => { if (t.jets != null) t.jets.active = true; } },
            new Step { title = "여행 끝!", focus = -1,
                body = "이 모든 현상이 단 하나의 방정식(측지선 방정식)에서 나옵니다.\nG를 눌러 투어를 마치고 자유롭게 탐험해보세요.",
                titleEn = "Tour Complete!",
                bodyEn = "All of this comes from a single equation — the geodesic equation.\nPress G to end the tour and explore freely.",
                enter = t => { if (t.annotations != null) { t.annotations.showLabels = true; t.annotations.focusIndex = -1; } } },
        };

        public void StartTour()
        {
            Running = true;
            step = 0;
            // Start from a completely clean stage: every overlay disappears,
            // then each step brings in only what it is talking about.
            if (panel != null) { prevPanelShow = panel.show; }
            if (comparison != null) { prevComparisonShow = comparison.show; }
            EnsureNarration();
            ApplyStep();
        }

        public void StopTour()
        {
            Running = false;
            NarrationManager.Instance.Stop();
            if (card != null) card.gameObject.SetActive(false);
            ResetDemos();
            if (annotations != null) { annotations.focusIndex = -1; annotations.showLabels = true; }
            if (panel != null) { panel.show = prevPanelShow; panel.RefreshText(); }
            if (comparison != null) { comparison.show = prevComparisonShow; comparison.Refresh(); }
        }

        public void Next() { if (Running && step < Steps.Length - 1) { step++; ApplyStep(); } }
        public void Prev() { if (Running && step > 0) { step--; ApplyStep(); } }

        void ApplyStep()
        {
            ResetDemos();
            var s = Steps[step];
            // Clean stage each step; the step's own content appears alone.
            if (panel != null) { panel.show = false; panel.RefreshText(); }
            if (comparison != null) { comparison.show = false; comparison.Refresh(); }
            if (annotations != null)
            {
                annotations.showLabels = s.focus >= 0;
                annotations.focusIndex = s.focus;
            }
            s.enter?.Invoke(this);
            if (panel != null) panel.RefreshText();
            if (Application.isPlaying) NarrationManager.Instance.Play("tour_" + step);
            EnsureNarration();
            card.gameObject.SetActive(true);
            cardTitle.text = Loc.T(s.title, s.titleEn);
            cardBody.text = Loc.T(s.body, s.bodyEn);
            cardFooter.text = Loc.T("N 다음    B 이전    G 종료    X 수식",
                                    "N Next    B Prev    G End    X Math")
                            + "                                  " + (step + 1) + " / " + Steps.Length;
        }

        /// <summary>Re-applies the current step after a language toggle so the
        /// card text and narration voice switch immediately.</summary>
        public void OnLanguageChanged()
        {
            if (Running) ApplyStep();
        }

        void ResetDemos()
        {
            if (einsteinDemo != null) einsteinDemo.active = false;
            if (spaghetti != null) spaghetti.active = false;
            if (jets != null) jets.active = false;
        }

        void EnsureNarration()
        {
            if (card != null) return;
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());

            card = BlackHoleUI.MakePanel(canvas.transform, "Tour Card",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(920f, 190f));

            cardTitle = BlackHoleUI.MakeText(card, "Title", 26, BlackHoleUI.TitleGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(860f, 34f), FontStyle.Bold);

            cardBody = BlackHoleUI.MakeText(card, "Body", 20, BlackHoleUI.TextPrimary, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(860f, 90f));

            cardFooter = BlackHoleUI.MakeText(card, "Footer", 15, BlackHoleUI.TextSecondary, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 12f), new Vector2(860f, 22f));
        }
    }
}

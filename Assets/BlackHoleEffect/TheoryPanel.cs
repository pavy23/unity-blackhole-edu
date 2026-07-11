using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// "이론 배경" card (X key, auto-shown at 고등 difficulty): the actual
    /// governing equation behind whatever is currently on screen, with a
    /// two-line plain-language reading of it. Context-sensitive — follows the
    /// guided tour step when the tour runs, otherwise picks the active demo.
    /// Play mode only.
    /// </summary>
    public class TheoryPanel : MonoBehaviour
    {
        public GuidedTour tour;
        public EinsteinRingDemo einstein;
        public SpaghettificationDemo spaghetti;
        public RelativisticJets jets;
        public PhotonLauncher launcher;
        public BlackHoleController controller;
        public BinaryMergerCinematic binary;

        public bool Visible { get; private set; }

        RectTransform panel;
        Text title, formula, body, hint;
        int currentKey = -1;

        struct Card
        {
            public string title, formula, body;
            public string titleEn, formulaEn, bodyEn; // formulaEn null → formula is language-neutral
        }

        // Index 0..10 mirror the tour steps; 11 = free-exploration default.
        static readonly Card[] Cards =
        {
            new Card { title = "측지선 방정식 — 이 화면의 전부",
                formula = "d²x/dλ² = −(3/2) h² x / r⁵    (GM = c = 1, Rs = 2)",
                body = "일반상대성이론의 빛 궤도 방정식을 매 픽셀 수치적분합니다.\nh = |x × v| 는 보존되는 각운동량 — 화면의 모든 휘어짐이 이 식의 출력입니다.",
                titleEn = "The Geodesic Equation — everything on screen",
                bodyEn = "GR's light-path equation, numerically integrated per pixel.\nh = |x × v| is the conserved angular momentum — every bend you see is its output." },
            new Card { title = "그림자의 크기",
                formula = "b_c = (3√3 ⁄ 2) Rs ≈ 2.6 Rs",
                body = "임계 충돌계수 b_c 안쪽으로 조준된 빛은 전부 포획됩니다.\n그림자가 지평선보다 2.6배 커 보이는 이유 — 적분에서 저절로 나오는 값입니다.",
                titleEn = "Size of the Shadow",
                bodyEn = "Any light aimed inside the critical impact parameter b_c is captured.\nThat is why the shadow looks 2.6× larger than the horizon — it falls out of the integration." },
            new Card { title = "광자 고리 — 빛의 원 궤도",
                formula = "r_ph = 1.5 Rs    (유효 퍼텐셜의 불안정 원궤도)",
                body = "빛이 블랙홀을 몇 바퀴 돌 수 있는 유일한 반지름.\n불안정하므로 결국 새어 나와 그림자 가장자리의 밝은 테두리가 됩니다.",
                titleEn = "Photon Ring — light on a circular orbit",
                formulaEn = "r_ph = 1.5 Rs    (unstable circular orbit of the effective potential)",
                bodyEn = "The one radius where light can circle the hole.\nBeing unstable, it eventually leaks out — the bright rim at the shadow's edge." },
            new Card { title = "강착원반의 온도 구조",
                formula = "r_ISCO = 3 Rs ,    T(r) ∝ r^(−3/4)",
                body = "샤쿠라–수냐예프 박원반: 안쪽일수록 뜨겁고 밝습니다.\nISCO(최내안정궤도) 안쪽에는 안정된 원궤도가 존재할 수 없습니다.",
                titleEn = "Temperature Structure of the Disk",
                bodyEn = "Shakura–Sunyaev thin disk: hotter and brighter toward the center.\nInside the ISCO (innermost stable circular orbit) no stable orbit exists." },
            new Card { title = "도플러 비밍 — 왜 한쪽이 밝은가",
                formula = "I_obs = (δ·g)³ I_em ,    δ = 1 ⁄ (1 − β cosθ)",
                body = "가스가 ISCO 근처에서 광속의 50%로 돕니다(β=0.5).\n광자수 보존(I/ν³ 불변)에서 지수 3이 나옵니다 — 다가오는 쪽이 δ³배 밝아집니다.",
                titleEn = "Doppler Beaming — why one side is brighter",
                bodyEn = "Gas near the ISCO orbits at 50% of light speed (β = 0.5).\nPhoton-number conservation (I/ν³ invariant) gives the power of 3 — the approaching side glows δ³ brighter." },
            new Card { title = "중력 시간 지연",
                formula = "dτ = dt · √(1 − Rs ⁄ r)",
                body = "r = 1.2 Rs 에서 √(1 − 1/1.2) ≈ 0.41.\n밖에서 1시간이 흐르는 동안 탐사선의 시계는 약 24분만 갑니다 — 정확한 계산값.",
                titleEn = "Gravitational Time Dilation",
                bodyEn = "At r = 1.2 Rs, √(1 − 1/1.2) ≈ 0.41.\nWhile one hour passes outside, the probe's clock ticks only ~24 minutes — the exact value." },
            new Card { title = "아인슈타인 링",
                formula = "θ_E = √( 4GM·D_LS ⁄ (c² D_L D_S) )",
                body = "광원–렌즈–관찰자가 일직선이면 상이 완전한 고리가 됩니다.\n이 데모는 공식 대신 측지선 적분이 직접 링을 만들어냅니다.",
                titleEn = "The Einstein Ring",
                bodyEn = "When source, lens and observer align, the image becomes a full ring.\nHere the geodesic integration itself produces the ring — no formula needed." },
            new Card { title = "빛의 궤적 — 비네 방정식",
                formula = "u″ + u = 3u²    (u = 1/r)",
                body = "뉴턴 궤도와의 차이가 우변의 3u² 항입니다.\nb < 2.6 Rs 면 포획(빨강), 크면 휘어져 탈출(파랑) — Space로 직접 실험해보세요.",
                titleEn = "Light Trajectories — the Binet Equation",
                bodyEn = "The 3u² term on the right is the difference from Newtonian orbits.\nb < 2.6 Rs → captured (red); larger → bent but escaping (blue). Try Space yourself." },
            new Card { title = "조석력 — 스파게티화",
                formula = "Δa ≈ 2GM·L ⁄ r³",
                body = "머리와 발(길이 L)에 걸리는 중력의 차이가 r³에 반비례해 폭증합니다.\n작은 블랙홀일수록 지평선 밖에서 이미 찢깁니다 — 4번(항성급)으로 바꿔 상상해보세요.",
                titleEn = "Tidal Force — Spaghettification",
                bodyEn = "The gravity difference across a body of length L explodes as 1/r³.\nSmaller holes tear you apart well outside the horizon — try preset 4 (stellar) and imagine." },
            new Card { title = "상대론적 제트",
                formula = "P_jet ∝ B² a² M²    (블랜드포드–즈나젝)",
                body = "회전하는 블랙홀의 에너지를 자기장이 뽑아 양극으로 분출합니다.\n(회전 효과라 이 씬에서는 개념 연출 — 렌징처럼 적분된 결과는 아닙니다.)",
                titleEn = "Relativistic Jets",
                formulaEn = "P_jet ∝ B² a² M²    (Blandford–Znajek)",
                bodyEn = "Magnetic fields extract a spinning hole's rotational energy and blast it from the poles.\n(A spin effect — visualized conceptually here, unlike the fully-integrated lensing.)" },
            new Card { title = "모든 것의 근원",
                formula = "G_μν = (8πG ⁄ c⁴) T_μν",
                body = "아인슈타인 장방정식 — 좌변은 시공간의 곡률, 우변은 물질과 에너지.\n\"물질은 시공간을 휘게 하고, 휘어진 시공간은 물질의 길을 정한다.\"",
                titleEn = "The Source of Everything",
                bodyEn = "Einstein's field equations — curvature of spacetime on the left, matter and energy on the right.\n\"Matter tells spacetime how to curve; spacetime tells matter how to move.\"" },
            new Card { title = "지금 보고 있는 것",
                formula = "d²x/dλ² = −(3/2) h² x / r⁵ ,    I_obs = (δ·g)³ I_em",
                body = "상의 기하(그림자·고리·렌즈 상)는 측지선 적분, 밝기와 색은 상대론적\n편이의 결과입니다. 투어(G)를 켜면 단계마다 해당 수식이 여기 표시됩니다.",
                titleEn = "What You Are Looking At",
                bodyEn = "The image geometry (shadow, rings, lensed arcs) comes from geodesic integration;\nbrightness and color from relativistic shifts. Start the tour (G) for per-step equations." },
            new Card { title = "중력파 병합 — 시공간의 소리",
                formula = "f_GW = 2 f_orb ,    ℳ = (m₁m₂)^(3/5) ⁄ (m₁+m₂)^(1/5)",
                body = "중력파 진동수는 공전의 2배로 올라가는 '처프'입니다(지금 소리가 실제 궤도와 동기화). 파형을 결정하는 것이 처프 질량 ℳ. 병합 후 질량의 ~5%가 파동으로 방출됩니다. (이중 렌징은 중첩 근사)",
                titleEn = "GW Merger — the sound of spacetime",
                bodyEn = "The wave frequency is twice the orbital frequency — the rising 'chirp' (the sound you hear tracks the actual orbit). The chirp mass ℳ shapes the waveform; ~5% of the total mass is radiated away. (Dual lensing is a superposition approximation.)" },
            new Card { title = "커(회전) 블랙홀 — 끌려 도는 시공간",
                formula = "r₊ = M + √(M² − a²) ,    ISCO : 6M → 1.2M",
                body = "회전이 시공간 자체를 끌고 돕니다(프레임 드래깅). 같이 도는 쪽의 빛은 더 쉽게 살아남아 그림자가 D자로 찌그러지고, ISCO가 안쪽으로 파고들어 원반이 지평선 코앞까지 붙습니다. (커–실트 좌표 실계산)",
                titleEn = "Kerr (Spinning) Black Hole — dragged spacetime",
                bodyEn = "Rotation drags spacetime itself around (frame dragging). Co-rotating light survives more easily, so the shadow squashes into a D-shape, and the ISCO plunges inward — the disk hugs the horizon. (Real Kerr-Schild integration)" },
        };

        public void Toggle() => SetVisible(!Visible);

        public void SetVisible(bool on)
        {
            Visible = on;
            if (!on) { if (panel != null) panel.gameObject.SetActive(false); return; }
            currentKey = -1; // force refresh
            Refresh();
        }

        void Update()
        {
            if (Visible) Refresh();
        }

        void Refresh()
        {
            // Language is part of the dirty-key so a toggle refreshes the card.
            int key = ContextKey() + Loc.Version * 100;
            if (key == currentKey) return;
            currentKey = key;
            Ensure();
            panel.gameObject.SetActive(true);
            var c = Cards[ContextKey()];
            title.text = Loc.T("이론 배경 — " + c.title, "Theory — " + c.titleEn);
            formula.text = Loc.English && c.formulaEn != null ? c.formulaEn : c.formula;
            body.text = Loc.T(c.body, c.bodyEn);
            if (hint != null) hint.text = Loc.T("X 닫기 · F2 난이도", "X close · F2 level");
        }

        int ContextKey()
        {
            if (binary != null && binary.Running) return 12;          // merger card
            if (tour != null && tour.Running) return Mathf.Clamp(tour.CurrentStep, 0, 10);
            if (einstein != null && einstein.active) return 6;
            if (spaghetti != null && spaghetti.active) return 8;
            if (jets != null && jets.active) return 9;
            if (launcher != null && launcher.HasTrails) return 7;
            if (controller != null && controller.spin > 0.001f) return 13; // Kerr card
            return 11;
        }

        void Ensure()
        {
            if (panel != null) return;
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());

            panel = BlackHoleUI.MakePanel(canvas.transform, "Theory Panel",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(470f, 200f));

            title = BlackHoleUI.MakeText(panel, "Title", 18, BlackHoleUI.TitleGold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(430f, 26f), FontStyle.Bold);

            formula = BlackHoleUI.MakeText(panel, "Formula", 21, BlackHoleUI.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(430f, 50f), FontStyle.Bold);

            body = BlackHoleUI.MakeText(panel, "Body", 15, BlackHoleUI.TextPrimary, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -102f), new Vector2(430f, 70f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap; // long lines stay inside the card

            hint = BlackHoleUI.MakeText(panel, "Hint", 12, BlackHoleUI.TextSecondary, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 8f), new Vector2(200f, 16f));
            hint.text = Loc.T("X 닫기 · F2 난이도", "X close · F2 level");
        }
    }
}

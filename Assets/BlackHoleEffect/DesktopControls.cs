using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlackHoleEffect
{
    /// <summary>
    /// Keyboard + mouse control for the desktop showcase (play mode).
    ///
    ///   우클릭 드래그  카메라 궤도 회전      휠           줌
    ///   1 / 2 / 3     색 프리셋             4 / 5 / 6    질량 프리셋
    ///   Space         광자 발사             C            궤적 지우기
    ///   E             아인슈타인 링         A / D        데모 별 이동
    ///   L             라벨                  I            물리 패널
    ///   O             관측사진 비교         H            도움말
    ///   R             카메라 리셋 (자동 궤도 재개)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DesktopControls : MonoBehaviour
    {
        public Transform target;
        public BlackHoleController controller;
        public BlackHolePhysicsPanel panel;
        public BlackHoleAnnotations annotations;
        public PhotonLauncher launcher;
        public EinsteinRingDemo einsteinDemo;
        public ObservationComparison comparison;
        public CinematicOrbit autoOrbit;
        public SpaghettificationDemo spaghetti;
        public RelativisticJets jets;
        public GuidedTour tour;
        public BlackHoleAudio audioScape;
        public PerformanceHud hud;
        public LightCurveGraph lightCurve;
        public IntroSequence intro;
        public FallInMode fallIn;
        public GravitationalLensDemo lensDemo;

        [Tooltip("Set by cinematic modes (fall-in) to take over the camera.")]
        public bool suspendCamera;

        TheoryPanel theory;
        BinaryMergerCinematic binary;

        void CycleDifficulty()
        {
            if (annotations == null) return;
            annotations.difficulty = (BlackHoleAnnotations.Difficulty)
                (((int)annotations.difficulty + 1) % 3);
            if (panel != null)
            {
                panel.showDilationRow = annotations.difficulty != BlackHoleAnnotations.Difficulty.Elementary;
                panel.RefreshText();
            }
            // 고등(정량) 난이도 = 수식 패널 자동 표시; 그 외에는 숨김 (X로 언제든 토글).
            bool advanced = annotations.difficulty == BlackHoleAnnotations.Difficulty.High;
            if (theory != null) theory.SetVisible(advanced);
            string name = annotations.difficulty switch
            {
                BlackHoleAnnotations.Difficulty.Elementary => Loc.T(
                    "초등 — 쉬운 설명", "Elementary — simple wording",
                    "初級 — やさしい説明", "初级 — 简单说明"),
                BlackHoleAnnotations.Difficulty.High => Loc.T(
                    "고등 — 정량적 설명 (수식 패널 ON)", "Advanced — quantitative (theory panel ON)",
                    "上級 — 定量的な説明（数式パネルON）", "高级 — 定量说明（公式面板开）"),
                _ => Loc.T("중등 — 표준 설명", "Standard — default wording",
                           "標準 — 標準的な説明", "中级 — 标准说明"),
            };
            ShowToast(Loc.T("난이도: ", "Level: ", "難易度: ", "难度: ") + name);
        }

        void ToggleLanguage()
        {
            Loc.Cycle();
            UpdateHelpText();
            if (tour != null) tour.OnLanguageChanged();
            if (panel != null) panel.RefreshText();
            ShowToast(Loc.T("언어: ", "Language: ", "言語: ", "语言: ") + Loc.DisplayName);
        }

        static readonly float[] SpinPresets = { 0f, 0.5f, 0.9f, 0.998f };

        void CycleSpin()
        {
            if (controller == null) return;
            // Advance to the next preset above the current value (wraps to 0).
            int next = 0;
            for (int i = 0; i < SpinPresets.Length; i++)
                if (Mathf.Abs(controller.spin - SpinPresets[i]) < 0.01f) { next = (i + 1) % SpinPresets.Length; break; }
            controller.SetSpin(SpinPresets[next]);

            float a = controller.spin;
            if (a < 0.001f)
            {
                ShowToast(Loc.T("스핀 a = 0 — 슈바르츠실트 (비회전)", "Spin a = 0 — Schwarzschild (non-rotating)",
                                "スピン a = 0 — シュヴァルツシルト（非回転）", "自旋 a = 0 — 史瓦西（不旋转）"));
            }
            else
            {
                string aS = a.ToString("0.###");
                string h = BlackHoleController.HorizonRadiusM(a).ToString("0.00");
                string isco = BlackHoleController.IscoRadiusM(a).ToString("0.00");
                ShowToast(Loc.T(
                    "스핀 a = " + aS + " M — 지평선 r₊ = " + h + "M · ISCO " + isco + "M (원반이 안쪽으로!)",
                    "Spin a = " + aS + " M — horizon r₊ = " + h + "M · ISCO " + isco + "M (disk creeps inward!)",
                    "スピン a = " + aS + " M — 地平面 r₊ = " + h + "M · ISCO " + isco + "M（円盤が内側へ！）",
                    "自旋 a = " + aS + " M — 视界 r₊ = " + h + "M · ISCO " + isco + "M（吸积盘向内！）"));
            }
            if (panel != null) panel.RefreshText();
        }

        [Header("Camera")]
        public float orbitSensitivity = 0.25f;
        [Tooltip("Zoom factor per scroll notch (multiplicative).")]
        public float zoomFactor = 0.86f;
        public Vector2 pitchLimits = new Vector2(-2f, 70f);
        public Vector2 distanceLimits = new Vector2(3.2f, 40f);

        float yaw, pitch, distance;
        Vector3 initialPos;
        Quaternion initialRot;
        UnityEngine.UI.Text help;
        GameObject helpBar;
        bool showHelp = true;
        bool immersive;

        /// <summary>Full immersion: hides every overlay and label at once (U key).</summary>
        public void SetImmersive(bool on)
        {
            immersive = on;
            showHelp = !on;
            if (panel != null) { panel.show = !on; panel.RefreshText(); }
            if (comparison != null && on) { comparison.show = false; comparison.Refresh(); }
            if (annotations != null) annotations.showLabels = !on;
            if (theory != null && on) theory.SetVisible(false);
        }

        void Start()
        {
            initialPos = transform.position;
            initialRot = transform.rotation;
            SyncFromTransform();
            BuildHelp();

            // Theory (수식) panel lives on the camera, wired from our own refs
            // so the saved scene needs no changes.
            theory = GetComponent<TheoryPanel>();
            if (theory == null) theory = gameObject.AddComponent<TheoryPanel>();
            theory.tour = tour;
            theory.einstein = einsteinDemo;
            theory.spaghetti = spaghetti;
            theory.jets = jets;
            theory.launcher = launcher;
            theory.controller = controller;

            binary = GetComponent<BinaryMergerCinematic>();
            if (binary == null) binary = gameObject.AddComponent<BinaryMergerCinematic>();
            binary.controller = controller;
            binary.controls = this;
            theory.binary = binary;
        }

        void SyncFromTransform()
        {
            if (target == null) return;
            Vector3 offset = transform.position - target.position;
            distance = offset.magnitude;
            yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(offset.y / Mathf.Max(distance, 0.001f), -1f, 1f)) * Mathf.Rad2Deg;
        }

        void Update()
        {
            ReadHotkeys();
            ReadMouse();
            if (helpBar != null) helpBar.SetActive(showHelp && (tour == null || !tour.Running));
        }

        void ReadMouse()
        {
            if (target == null || suspendCamera) return;
            float dx = 0f, dy = 0f, scroll = 0f;
            bool dragging = false, zoomIn = false, zoomOut = false;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                dragging = mouse.rightButton.isPressed;
                var d = mouse.delta.ReadValue();
                dx = d.x; dy = d.y;
                scroll = mouse.scroll.ReadValue().y;
                // Windows reports ±120 per notch, some devices ±1. Normalize.
                if (Mathf.Abs(scroll) > 10f) scroll /= 120f;
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                zoomIn = kb.wKey.isPressed;
                zoomOut = kb.sKey.isPressed;
            }
#else
            dragging = Input.GetMouseButton(1);
            dx = Input.GetAxis("Mouse X") * 12f;
            dy = Input.GetAxis("Mouse Y") * 12f;
            scroll = Input.mouseScrollDelta.y;
            zoomIn = Input.GetKey(KeyCode.W);
            zoomOut = Input.GetKey(KeyCode.S);
#endif
            bool zooming = !Mathf.Approximately(scroll, 0f) || zoomIn || zoomOut;
            if (!dragging && !zooming) return;

            // The cinematic orbit is never paused: each input frame re-syncs
            // from the transform (which the orbit advanced last LateUpdate)
            // and layers the user's deltas on top — the view keeps drifting
            // even while dragging.
            SyncFromTransform();

            if (dragging)
            {
                yaw += dx * orbitSensitivity;
                pitch = Mathf.Clamp(pitch + dy * orbitSensitivity, pitchLimits.x, pitchLimits.y);
            }

            // Multiplicative zoom: each notch scales distance by zoomFactor,
            // so it feels equally fast whether near or far. W/S = smooth zoom.
            if (!Mathf.Approximately(scroll, 0f))
                distance *= Mathf.Pow(zoomFactor, scroll);
            if (zoomIn) distance *= 1f - 1.4f * Time.deltaTime;
            if (zoomOut) distance *= 1f + 1.4f * Time.deltaTime;
            // The near limit scales with the hole (2.4 Rs) instead of being a
            // fixed world distance — otherwise small mass presets keep the
            // camera tens of Rs away and the observer-clock dilation never
            // visibly drops. At 2.4 Rs the clock reads ×0.76.
            float minDist = target != null ? Mathf.Max(2.4f * target.lossyScale.x, 0.35f) : distanceLimits.x;
            distance = Mathf.Clamp(distance, minDist, distanceLimits.y);

            float pr = pitch * Mathf.Deg2Rad, yr = yaw * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(yr) * Mathf.Cos(pr), Mathf.Sin(pr), Mathf.Cos(yr) * Mathf.Cos(pr));
            transform.position = target.position + dir * distance;
            transform.LookAt(target.position + Vector3.up * 0.1f);
        }

        void ReadHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) controller?.SetPreset(BlackHoleController.DiskPreset.Gargantua);
            if (kb.digit2Key.wasPressedThisFrame) controller?.SetPreset(BlackHoleController.DiskPreset.RedGiant);
            if (kb.digit3Key.wasPressedThisFrame) controller?.SetPreset(BlackHoleController.DiskPreset.BlueQuasar);
            if (kb.digit4Key.wasPressedThisFrame) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.Stellar10);
            if (kb.digit5Key.wasPressedThisFrame) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.SagittariusA);
            if (kb.digit6Key.wasPressedThisFrame) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.M87);
            if (kb.cKey.wasPressedThisFrame) launcher?.ClearTrails();
            if (kb.eKey.wasPressedThisFrame && einsteinDemo != null) einsteinDemo.active = !einsteinDemo.active;
            if (kb.lKey.wasPressedThisFrame && annotations != null) annotations.showLabels = !annotations.showLabels;
            if (kb.iKey.wasPressedThisFrame && panel != null) { panel.show = !panel.show; panel.RefreshText(); }
            if (kb.oKey.wasPressedThisFrame && comparison != null) comparison.CycleMode();
            if (kb.f1Key.wasPressedThisFrame && hud != null) hud.show = !hud.show;
            if (kb.f2Key.wasPressedThisFrame) CycleDifficulty();
            if (kb.f3Key.wasPressedThisFrame && lensDemo != null) lensDemo.Toggle();
            if (kb.f4Key.wasPressedThisFrame && (binary == null || !binary.Running)) CycleSpin();
            if (kb.f7Key.wasPressedThisFrame && binary != null) binary.Begin();
            if (kb.f5Key.wasPressedThisFrame && intro != null) intro.Play();
            if (kb.f6Key.wasPressedThisFrame && fallIn != null) fallIn.Begin();
            if (kb.vKey.wasPressedThisFrame && lightCurve != null) lightCurve.show = !lightCurve.show;
            if (kb.f12Key.wasPressedThisFrame) Snapshot();
            if (kb.hKey.wasPressedThisFrame) showHelp = !showHelp;
            if (kb.uKey.wasPressedThisFrame) SetImmersive(!immersive);
            if (kb.rKey.wasPressedThisFrame) ResetCamera();
            if (kb.tKey.wasPressedThisFrame && spaghetti != null) spaghetti.active = !spaghetti.active;
            if (kb.jKey.wasPressedThisFrame && jets != null) jets.active = !jets.active;
            if (kb.mKey.wasPressedThisFrame && audioScape != null) audioScape.muted = !audioScape.muted;
            if (kb.xKey.wasPressedThisFrame && theory != null) theory.Toggle();
            if (kb.kKey.wasPressedThisFrame) ToggleLanguage();
            if (tour != null)
            {
                if (kb.gKey.wasPressedThisFrame) { if (tour.Running) tour.StopTour(); else tour.StartTour(); }
                if (kb.nKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) tour.Next();
                if (kb.bKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) tour.Prev();
            }
            if (einsteinDemo != null && einsteinDemo.active)
            {
                if (kb.aKey.isPressed) einsteinDemo.Nudge(-12f * Time.deltaTime);
                if (kb.dKey.isPressed) einsteinDemo.Nudge(12f * Time.deltaTime);
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) controller?.SetPreset(BlackHoleController.DiskPreset.Gargantua);
            if (Input.GetKeyDown(KeyCode.Alpha2)) controller?.SetPreset(BlackHoleController.DiskPreset.RedGiant);
            if (Input.GetKeyDown(KeyCode.Alpha3)) controller?.SetPreset(BlackHoleController.DiskPreset.BlueQuasar);
            if (Input.GetKeyDown(KeyCode.Alpha4)) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.Stellar10);
            if (Input.GetKeyDown(KeyCode.Alpha5)) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.SagittariusA);
            if (Input.GetKeyDown(KeyCode.Alpha6)) panel?.SetMassPreset(BlackHolePhysicsPanel.MassPreset.M87);
            if (Input.GetKeyDown(KeyCode.C)) launcher?.ClearTrails();
            if (Input.GetKeyDown(KeyCode.E) && einsteinDemo != null) einsteinDemo.active = !einsteinDemo.active;
            if (Input.GetKeyDown(KeyCode.L) && annotations != null) annotations.showLabels = !annotations.showLabels;
            if (Input.GetKeyDown(KeyCode.I) && panel != null) { panel.show = !panel.show; panel.RefreshText(); }
            if (Input.GetKeyDown(KeyCode.O) && comparison != null) comparison.CycleMode();
            if (Input.GetKeyDown(KeyCode.F1) && hud != null) hud.show = !hud.show;
            if (Input.GetKeyDown(KeyCode.F2)) CycleDifficulty();
            if (Input.GetKeyDown(KeyCode.F3) && lensDemo != null) lensDemo.Toggle();
            if (Input.GetKeyDown(KeyCode.F4) && (binary == null || !binary.Running)) CycleSpin();
            if (Input.GetKeyDown(KeyCode.F7) && binary != null) binary.Begin();
            if (Input.GetKeyDown(KeyCode.F5) && intro != null) intro.Play();
            if (Input.GetKeyDown(KeyCode.F6) && fallIn != null) fallIn.Begin();
            if (Input.GetKeyDown(KeyCode.V) && lightCurve != null) lightCurve.show = !lightCurve.show;
            if (Input.GetKeyDown(KeyCode.F12)) Snapshot();
            if (Input.GetKeyDown(KeyCode.H)) showHelp = !showHelp;
            if (Input.GetKeyDown(KeyCode.U)) SetImmersive(!immersive);
            if (Input.GetKeyDown(KeyCode.R)) ResetCamera();
            if (Input.GetKeyDown(KeyCode.T) && spaghetti != null) spaghetti.active = !spaghetti.active;
            if (Input.GetKeyDown(KeyCode.J) && jets != null) jets.active = !jets.active;
            if (Input.GetKeyDown(KeyCode.M) && audioScape != null) audioScape.muted = !audioScape.muted;
            if (Input.GetKeyDown(KeyCode.X) && theory != null) theory.Toggle();
            if (Input.GetKeyDown(KeyCode.K)) ToggleLanguage();
            if (tour != null)
            {
                if (Input.GetKeyDown(KeyCode.G)) { if (tour.Running) tour.StopTour(); else tour.StartTour(); }
                if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.RightArrow)) tour.Next();
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.LeftArrow)) tour.Prev();
            }
            if (einsteinDemo != null && einsteinDemo.active)
            {
                if (Input.GetKey(KeyCode.A)) einsteinDemo.Nudge(-12f * Time.deltaTime);
                if (Input.GetKey(KeyCode.D)) einsteinDemo.Nudge(12f * Time.deltaTime);
            }
#endif
        }

        UnityEngine.UI.Text toast;
        Coroutine toastRoutine;

        void Snapshot()
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Snapshots"));
            System.IO.Directory.CreateDirectory(dir);
            string file = System.IO.Path.Combine(dir, "blackhole_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(file);
            ShowToast(Loc.T("스냅샷 저장됨 — ", "Snapshot saved — ", "スナップショット保存 — ", "截图已保存 — ")
                      + "Snapshots/" + System.IO.Path.GetFileName(file));
        }

        void ShowToast(string message)
        {
            if (toast == null)
            {
                var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
                var bar = BlackHoleUI.MakePanel(canvas.transform, "Toast",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(620f, 46f),
                    accentLine: false);
                toast = BlackHoleUI.MakeText(bar, "Text", 17, BlackHoleUI.TitleGold, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 38f));
            }
            toast.text = message;
            toast.transform.parent.gameObject.SetActive(true);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToast());
        }

        System.Collections.IEnumerator HideToast()
        {
            yield return new WaitForSeconds(2.5f);
            if (toast != null) toast.transform.parent.gameObject.SetActive(false);
        }

        void ResetCamera()
        {
            transform.position = initialPos;
            transform.rotation = initialRot;
            if (autoOrbit != null) autoOrbit.enabled = true;
        }

        void BuildHelp()
        {
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
            // Three explicit rows + wrap so no language ever spills past the bar.
            var bar = BlackHoleUI.MakePanel(canvas.transform, "Help Bar",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1600f, 88f),
                accentLine: false);
            helpBar = bar.gameObject;

            help = BlackHoleUI.MakeText(bar, "Help Text", 15, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 80f));
            help.horizontalOverflow = HorizontalWrapMode.Wrap;
            UpdateHelpText();
        }

        static string Key(string k) => "<color=#FFC46E>" + k + "</color> ";

        void UpdateHelpText()
        {
            if (help == null) return;
            help.text = Loc.T(
                Key("우클릭 드래그") + "회전  " + Key("휠·W/S") + "줌  " + Key("R") + "리셋  " + Key("1·2·3") + "색상  "
                    + Key("4·5·6") + "질량  " + Key("Space") + "광자  " + Key("C") + "지우기\n"
                + Key("E") + "아인슈타인 링(A/D)  " + Key("T") + "스파게티화  " + Key("J") + "제트  " + Key("G") + "투어(N/B)  "
                    + Key("X") + "수식  " + Key("V") + "광도곡선  " + Key("L") + "라벨  " + Key("I") + "패널  " + Key("O") + "관측사진\n"
                + Key("U") + "몰입  " + Key("M") + "소리  " + Key("K") + "언어  " + Key("F2") + "난이도  " + Key("F3") + "렌즈  "
                    + Key("F4") + "스핀  " + Key("F5") + "인트로  " + Key("F6") + "낙하  " + Key("F7") + "병합  "
                    + Key("F12") + "스냅샷  " + Key("F1") + "성능  " + Key("H") + "도움말",

                Key("RMB drag") + "orbit  " + Key("Wheel·W/S") + "zoom  " + Key("R") + "reset  " + Key("1·2·3") + "colors  "
                    + Key("4·5·6") + "mass  " + Key("Space") + "photons  " + Key("C") + "clear\n"
                + Key("E") + "Einstein ring(A/D)  " + Key("T") + "spaghettify  " + Key("J") + "jets  " + Key("G") + "tour(N/B)  "
                    + Key("X") + "math  " + Key("V") + "light curve  " + Key("L") + "labels  " + Key("I") + "panel  " + Key("O") + "EHT photo\n"
                + Key("U") + "immersive  " + Key("M") + "sound  " + Key("K") + "language  " + Key("F2") + "level  " + Key("F3") + "lens  "
                    + Key("F4") + "spin  " + Key("F5") + "intro  " + Key("F6") + "fall-in  " + Key("F7") + "merger  "
                    + Key("F12") + "snapshot  " + Key("F1") + "perf  " + Key("H") + "help",

                Key("右ドラッグ") + "回転  " + Key("ホイール·W/S") + "ズーム  " + Key("R") + "リセット  " + Key("1·2·3") + "色  "
                    + Key("4·5·6") + "質量  " + Key("Space") + "光子  " + Key("C") + "消去\n"
                + Key("E") + "アインシュタインリング(A/D)  " + Key("T") + "スパゲッティ化  " + Key("J") + "ジェット  " + Key("G") + "ツアー(N/B)  "
                    + Key("X") + "数式  " + Key("V") + "光度曲線  " + Key("L") + "ラベル  " + Key("I") + "パネル  " + Key("O") + "観測写真\n"
                + Key("U") + "没入  " + Key("M") + "音  " + Key("K") + "言語  " + Key("F2") + "難易度  " + Key("F3") + "レンズ  "
                    + Key("F4") + "スピン  " + Key("F5") + "イントロ  " + Key("F6") + "落下  " + Key("F7") + "合体  "
                    + Key("F12") + "撮影  " + Key("F1") + "性能  " + Key("H") + "ヘルプ",

                Key("右键拖动") + "旋转  " + Key("滚轮·W/S") + "缩放  " + Key("R") + "重置  " + Key("1·2·3") + "颜色  "
                    + Key("4·5·6") + "质量  " + Key("Space") + "光子  " + Key("C") + "清除\n"
                + Key("E") + "爱因斯坦环(A/D)  " + Key("T") + "面条化  " + Key("J") + "喷流  " + Key("G") + "导览(N/B)  "
                    + Key("X") + "公式  " + Key("V") + "光变曲线  " + Key("L") + "标签  " + Key("I") + "面板  " + Key("O") + "观测照片\n"
                + Key("U") + "沉浸  " + Key("M") + "声音  " + Key("K") + "语言  " + Key("F2") + "难度  " + Key("F3") + "透镜  "
                    + Key("F4") + "自旋  " + Key("F5") + "序章  " + Key("F6") + "坠落  " + Key("F7") + "并合  "
                    + Key("F12") + "截图  " + Key("F1") + "性能  " + Key("H") + "帮助");
        }
    }
}

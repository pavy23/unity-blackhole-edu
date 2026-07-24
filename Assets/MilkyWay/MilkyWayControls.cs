using UnityEngine;
using UnityEngine.UI;
using BlackHoleEffect;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MilkyWay
{
    /// <summary>
    /// Keyboard + mouse control for the Milky Way showcase.
    ///   F1  zoom journey (Sun → whole galaxy)
    ///   우클릭 드래그 회전 · 휠 줌(로그) · K 언어 · H 도움말
    /// The orbit-layering trick is the black-hole exhibit's: re-sync from the
    /// transform each input frame so the ambient drift and the user's drag
    /// compose instead of fighting.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MilkyWayControls : MonoBehaviour
    {
        public MilkyWayController controller;
        public CinematicOrbit orbit;
        public ZoomJourney journey;
        public NightSkyConnection nightSky;
        public AndromedaCollision andromeda;
        public MilkyWayTour tour;
        public CosmicZoomOut cosmicZoom;
        public SolarSystemTour solarTour;
        public RotationCurveLab rotationLab;
        public GalaxyZoo zoo;
        public SgrACrossover sgrA;
        public MilkyWayAudio audioScape;

        bool AnyPlaying =>
            (journey != null && journey.IsPlaying) ||
            (nightSky != null && nightSky.IsPlaying) ||
            (andromeda != null && andromeda.IsPlaying) ||
            (tour != null && tour.Running) ||
            (cosmicZoom != null && cosmicZoom.IsPlaying) ||
            (solarTour != null && solarTour.Running) ||
            (rotationLab != null && rotationLab.IsPlaying) ||
            (zoo != null && zoo.Running) ||
            (sgrA != null && sgrA.IsPlaying);

        float distance, yaw, pitch;
        bool immersive;
        GameObject helpBar;
        Text help;
        bool showHelp = true;
        int helpLocVersion = -1;

        void Start()
        {
            if (Application.isPlaying) LanguageSelect.CreateWidget();
            // The K-key language handler used to refresh a running tour's card;
            // now that language changes come from the LanguageSelect widget,
            // relay them the same way.
            Loc.Changed -= OnLocChanged;
            Loc.Changed += OnLocChanged;
        }

        void OnDestroy() { Loc.Changed -= OnLocChanged; }

        void OnLocChanged()
        {
            if (tour != null) tour.OnLanguageChanged();
            if (solarTour != null) solarTour.OnLanguageChanged();
            if (zoo != null) zoo.OnLanguageChanged();
        }

        void Update()
        {
            // Esc leaves immersive view (the toolbar that would toggle it is hidden).
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && immersive) SetImmersive(false);
#else
            if (Input.GetKeyDown(KeyCode.Escape) && immersive) SetImmersive(false);
#endif
            ReadTourNav();
            if (!AnyPlaying)
                ReadMouse();
        }

        // ---- toolbar entry points (click-only UI; guards centralized here) ---
        public bool Busy => AnyPlaying;
        public bool Immersive => immersive;
        public void SetImmersive(bool on)
        {
            immersive = on;
            LanguageSelect.SetVisible(!on);
            if (on) ImmersiveHint.Show(); else ImmersiveHint.Hide();
        }
        public void PlayJourney() { if (journey != null && !AnyPlaying) journey.Begin(); }
        public void PlayNightSky() { if (nightSky != null && !AnyPlaying) nightSky.Begin(); }
        public void PlayAndromeda() { if (andromeda != null && !AnyPlaying) andromeda.Begin(); }
        public void ToggleTour() { if (tour == null) return; if (tour.Running) tour.StopTour(); else if (!AnyPlaying) tour.StartTour(); }
        public void PlayCosmicZoom() { if (cosmicZoom != null && !AnyPlaying) cosmicZoom.Begin(); }
        public void ToggleSolarTour() { if (solarTour == null) return; if (solarTour.Running) solarTour.StopTour(); else if (!AnyPlaying) solarTour.StartTour(); }
        public void ToggleRotationLab() { if (rotationLab == null) return; if (rotationLab.IsPlaying) rotationLab.Abort(); else if (!AnyPlaying) rotationLab.Begin(); }
        public void ToggleZoo() { if (zoo == null) return; if (zoo.Running) zoo.StopZoo(); else if (!AnyPlaying) zoo.StartZoo(); }
        public void PlaySgrA() { if (sgrA != null && !AnyPlaying) sgrA.Begin(); }
        public void ToggleMute() { if (audioScape != null) audioScape.muted = !audioScape.muted; }
        public static void LoadScene(string s) => UnityEngine.SceneManagement.SceneManager.LoadScene(s);

        /// <summary>The only key left is the arrows, to step whichever tour is
        /// running — everything else is a toolbar button (see MilkyWayToolbar)
        /// so WebGL never collides with browser shortcuts.</summary>
        void ReadTourNav()
        {
            bool next = false, prev = false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            next = kb.rightArrowKey.wasPressedThisFrame;
            prev = kb.leftArrowKey.wasPressedThisFrame;
#else
            next = Input.GetKeyDown(KeyCode.RightArrow);
            prev = Input.GetKeyDown(KeyCode.LeftArrow);
#endif
            if (!next && !prev) return;
            if (tour != null && tour.Running) { if (next) tour.Next(); else tour.Prev(); }
            else if (solarTour != null && solarTour.Running) { if (next) solarTour.Next(); else solarTour.Prev(); }
            else if (zoo != null && zoo.Running) { if (next) zoo.Next(); else zoo.Prev(); }
        }

        void SyncFromTransform()
        {
            Vector3 offset = transform.position; // galaxy sits at the origin
            distance = offset.magnitude;
            yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(offset.y / Mathf.Max(distance, 0.001f), -1f, 1f)) * Mathf.Rad2Deg;
        }

        void ReadMouse()
        {
            float dx = 0f, dy = 0f, scroll = 0f;
            bool dragging = false;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                dragging = mouse.rightButton.isPressed;
                var d = mouse.delta.ReadValue();
                dx = d.x; dy = d.y;
                scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 10f) scroll /= 120f; // Windows notches
            }
#else
            dragging = Input.GetMouseButton(1);
            dx = Input.GetAxis("Mouse X") * 12f;
            dy = Input.GetAxis("Mouse Y") * 12f;
            scroll = Input.mouseScrollDelta.y;
#endif
            // Mobile browsers: one-finger drag orbits, two-finger pinch zooms.
            if (TouchOrbit.Dragging)
            {
                dragging = true;
                dx += TouchOrbit.DragDelta.x; dy += TouchOrbit.DragDelta.y;
            }
            scroll += TouchOrbit.PinchNotches;
            bool zooming = !Mathf.Approximately(scroll, 0f);
            if (!dragging && !zooming) return;

            SyncFromTransform();
            if (dragging)
            {
                yaw += dx * 0.25f;
                pitch = Mathf.Clamp(pitch + dy * 0.25f, -80f, 80f);
            }
            if (zooming) distance *= Mathf.Pow(0.86f, scroll); // log zoom
            distance = Mathf.Clamp(distance, 2f, 110f);

            float pr = pitch * Mathf.Deg2Rad, yr = yaw * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(yr) * Mathf.Cos(pr), Mathf.Sin(pr), Mathf.Cos(yr) * Mathf.Cos(pr));
            transform.position = dir * distance;
            transform.LookAt(Vector3.zero);
        }

        void BuildHelp()
        {
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
            var bar = BlackHoleUI.MakePanel(canvas.transform, "MW Help Bar",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1420f, 62f),
                accentLine: false);
            helpBar = bar.gameObject;
            help = BlackHoleUI.MakeText(bar, "Text", 15, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1380f, 54f));
            UpdateHelpText();
        }

        static string Key(string k) => "<color=#FFC46E>" + k + "</color> ";

        void UpdateHelpText()
        {
            if (help == null) return;
            help.text = Loc.T(
                Key("F1") + "줌 여행   " + Key("F2") + "밤하늘   " + Key("F3") + "안드로메다   " + Key("F4") + "은하 투어   " + Key("F5") + "우주 줌아웃   " + Key("F6") + "태양계 투어   " + Key("F7") + "회전 곡선   " + Key("F8") + "은하 동물원   " + Key("F9") + "궁수자리 A*\n"
                + Key("F11") + "태양계 전시   " + Key("F10") + "처음으로   " + Key("우클릭") + "회전   " + Key("휠") + "줌   " + Key("N/B") + "투어 이동   " + Key("M") + "소리   " + Key("K") + "언어   " + Key("H") + "도움말",
                Key("F1") + "zoom journey   " + Key("F2") + "night sky   " + Key("F3") + "Andromeda   " + Key("F4") + "galaxy tour   " + Key("F5") + "cosmic zoom-out   " + Key("F6") + "solar system   " + Key("F7") + "rotation curve   " + Key("F8") + "galaxy zoo   " + Key("F9") + "Sagittarius A*\n"
                + Key("F11") + "solar system exhibit   " + Key("F10") + "title   " + Key("R-drag") + "orbit   " + Key("wheel") + "zoom   " + Key("N/B") + "tour steps   " + Key("M") + "sound   " + Key("K") + "language   " + Key("H") + "help",
                Key("F1") + "ズームの旅   " + Key("F2") + "夜空   " + Key("F3") + "アンドロメダ   " + Key("F4") + "銀河ツアー   " + Key("F5") + "宇宙ズームアウト   " + Key("F6") + "太陽系ツアー   " + Key("F7") + "回転曲線   " + Key("F8") + "銀河動物園   " + Key("F9") + "いて座A*\n"
                + Key("F11") + "太陽系展示   " + Key("F10") + "最初へ   " + Key("右ドラッグ") + "回転   " + Key("ホイール") + "ズーム   " + Key("N/B") + "ツアー移動   " + Key("M") + "音   " + Key("K") + "言語   " + Key("H") + "ヘルプ",
                Key("F1") + "缩放之旅   " + Key("F2") + "夜空   " + Key("F3") + "仙女座   " + Key("F4") + "星系导览   " + Key("F5") + "宇宙缩放   " + Key("F6") + "太阳系之旅   " + Key("F7") + "旋转曲线   " + Key("F8") + "星系动物园   " + Key("F9") + "人马座A*\n"
                + Key("F11") + "太阳系展区   " + Key("F10") + "回标题   " + Key("右键拖动") + "旋转   " + Key("滚轮") + "缩放   " + Key("N/B") + "导览步进   " + Key("M") + "声音   " + Key("K") + "语言   " + Key("H") + "帮助");
        }
    }
}

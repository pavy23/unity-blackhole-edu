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

        float distance, yaw, pitch;
        GameObject helpBar;
        Text help;
        bool showHelp = true;
        int helpLocVersion = -1;

        void Start()
        {
            if (Application.isPlaying) LanguageSelect.CreateWidget();
            BuildHelp();
        }

        void Update()
        {
            ReadHotkeys();
            if (journey == null || !journey.IsPlaying)
                ReadMouse();
            if (helpBar != null)
            {
                helpBar.SetActive(showHelp && (journey == null || !journey.IsPlaying));
                if (helpLocVersion != Loc.Version) { helpLocVersion = Loc.Version; UpdateHelpText(); }
            }
        }

        void ReadHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame && journey != null && !journey.IsPlaying) journey.Begin();
            if (kb.kKey.wasPressedThisFrame) Loc.Cycle();
            if (kb.hKey.wasPressedThisFrame) showHelp = !showHelp;
#else
            if (Input.GetKeyDown(KeyCode.F1) && journey != null && !journey.IsPlaying) journey.Begin();
            if (Input.GetKeyDown(KeyCode.K)) Loc.Cycle();
            if (Input.GetKeyDown(KeyCode.H)) showHelp = !showHelp;
#endif
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
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1080f, 40f),
                accentLine: false);
            helpBar = bar.gameObject;
            help = BlackHoleUI.MakeText(bar, "Text", 15, BlackHoleUI.TextSecondary, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 32f));
            UpdateHelpText();
        }

        static string Key(string k) => "<color=#FFC46E>" + k + "</color> ";

        void UpdateHelpText()
        {
            if (help == null) return;
            help.text = Loc.T(
                Key("F1") + "줌 여행 (태양에서 은하까지)   " + Key("우클릭") + "회전   " + Key("휠") + "줌   " + Key("K") + "언어   " + Key("H") + "도움말",
                Key("F1") + "zoom journey (Sun to galaxy)   " + Key("R-drag") + "orbit   " + Key("wheel") + "zoom   " + Key("K") + "language   " + Key("H") + "help",
                Key("F1") + "ズームの旅（太陽から銀河へ）   " + Key("右ドラッグ") + "回転   " + Key("ホイール") + "ズーム   " + Key("K") + "言語   " + Key("H") + "ヘルプ",
                Key("F1") + "缩放之旅（从太阳到银河）   " + Key("右键拖动") + "旋转   " + Key("滚轮") + "缩放   " + Key("K") + "语言   " + Key("H") + "帮助");
        }
    }
}

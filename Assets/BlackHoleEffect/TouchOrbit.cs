using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using ISTouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace BlackHoleEffect
{
    /// <summary>
    /// Touch equivalents of the exhibit-wide camera gestures, for the WebGL
    /// build running in mobile browsers: a one-finger drag orbits (the mouse's
    /// right-drag), a two-finger pinch zooms (the wheel), and a short still
    /// tap clicks (the left button). Controls scripts poll the static
    /// properties from their input frame; the state is computed once per frame
    /// no matter how many callers ask. A touch that begins over UI belongs to
    /// the UI for its whole life and never moves the camera.
    /// </summary>
    public static class TouchOrbit
    {
        // Drag deltas are normalized to a 1440px-wide reference screen so a
        // full-width swipe turns the camera the same amount on every device
        // (and lands in the range the exhibits' mouse sensitivities expect).
        const float RefWidth = 1440f;
        // A pinch spanning the full screen width equals this many wheel
        // notches (the exhibits zoom ×0.86 per notch).
        const float PinchNotchesPerWidth = 15f;
        const float TapMaxSeconds = 0.35f;
        const float TapMaxTravelFrac = 0.02f;   // of screen width

        static int frame = -1;
        static bool dragging;
        static Vector2 dragDelta;
        static float pinchNotches;
        static bool tapped;
        static Vector2 tapPosition;

        public static bool Dragging { get { Poll(); return dragging; } }
        public static Vector2 DragDelta { get { Poll(); return dragDelta; } }
        /// <summary>Wheel-notch equivalents this frame (+ = zoom in).</summary>
        public static float PinchNotches { get { Poll(); return pinchNotches; } }
        public static bool Tapped { get { Poll(); return tapped; } }
        public static Vector2 TapPosition { get { Poll(); return tapPosition; } }

        struct TouchStart { public Vector2 pos; public float time; public bool overUI; public float travel; }
        static readonly Dictionary<int, TouchStart> starts = new Dictionary<int, TouchStart>();
        static float prevPinchDist = -1f;

        static void Poll()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            dragging = false; dragDelta = Vector2.zero; pinchNotches = 0f; tapped = false;

#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts == null) { prevPinchDist = -1f; starts.Clear(); return; }

            int active = 0;
            Vector2 pos0 = default, pos1 = default, delta0 = default;

            foreach (var t in ts.touches)
            {
                var phase = t.phase.ReadValue();
                if (phase == ISTouchPhase.None) continue;
                int id = t.touchId.ReadValue();
                Vector2 pos = t.position.ReadValue();
                Vector2 delta = t.delta.ReadValue();

                if (phase == ISTouchPhase.Began)
                    starts[id] = new TouchStart { pos = pos, time = Time.unscaledTime, overUI = IsOverUI(id) };

                if (phase == ISTouchPhase.Ended || phase == ISTouchPhase.Canceled)
                {
                    if (phase == ISTouchPhase.Ended && starts.TryGetValue(id, out var s) && !s.overUI &&
                        Time.unscaledTime - s.time <= TapMaxSeconds &&
                        s.travel <= TapMaxTravelFrac * Screen.width)
                    {
                        tapped = true; tapPosition = pos;
                    }
                    starts.Remove(id);
                    continue;
                }

                // Began / Moved / Stationary → the touch is live this frame.
                bool overUI = false;
                if (starts.TryGetValue(id, out var st))
                {
                    st.travel += delta.magnitude;
                    starts[id] = st;
                    overUI = st.overUI;
                }
                if (overUI) continue;

                if (active == 0) { pos0 = pos; delta0 = delta; }
                else if (active == 1) pos1 = pos;
                active++;
            }

            if (active == 1)
            {
                prevPinchDist = -1f;
                dragging = true;
                dragDelta = delta0 * (RefWidth / Mathf.Max(Screen.width, 1));
            }
            else if (active >= 2)
            {
                float dist = Vector2.Distance(pos0, pos1);
                if (prevPinchDist > 0f)
                    pinchNotches = (dist - prevPinchDist) / Mathf.Max(Screen.width, 1) * PinchNotchesPerWidth;
                prevPinchDist = dist;
            }
            else prevPinchDist = -1f;
#else
            if (Input.touchCount == 0) { prevPinchDist = -1f; starts.Clear(); return; }
            int active = 0;
            Vector2 pos0 = default, pos1 = default, delta0 = default;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                int id = t.fingerId;
                if (t.phase == TouchPhase.Began)
                    starts[id] = new TouchStart { pos = t.position, time = Time.unscaledTime, overUI = IsOverUI(id) };
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (t.phase == TouchPhase.Ended && starts.TryGetValue(id, out var s) && !s.overUI &&
                        Time.unscaledTime - s.time <= TapMaxSeconds &&
                        s.travel <= TapMaxTravelFrac * Screen.width)
                    {
                        tapped = true; tapPosition = t.position;
                    }
                    starts.Remove(id);
                    continue;
                }
                bool overUI = false;
                if (starts.TryGetValue(id, out var st))
                {
                    st.travel += t.deltaPosition.magnitude;
                    starts[id] = st;
                    overUI = st.overUI;
                }
                if (overUI) continue;
                if (active == 0) { pos0 = t.position; delta0 = t.deltaPosition; }
                else if (active == 1) pos1 = t.position;
                active++;
            }
            if (active == 1)
            {
                prevPinchDist = -1f;
                dragging = true;
                dragDelta = delta0 * (RefWidth / Mathf.Max(Screen.width, 1));
            }
            else if (active >= 2)
            {
                float dist = Vector2.Distance(pos0, pos1);
                if (prevPinchDist > 0f)
                    pinchNotches = (dist - prevPinchDist) / Mathf.Max(Screen.width, 1) * PinchNotchesPerWidth;
                prevPinchDist = dist;
            }
            else prevPinchDist = -1f;
#endif
        }

        static bool IsOverUI(int touchId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return es.IsPointerOverGameObject(touchId);
        }
    }
}

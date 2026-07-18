using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BlackHoleEffect; // Loc

namespace MilkyWay
{
    /// <summary>
    /// The MR miniature of the galaxy: a ~1 m Milky Way floating in the room.
    /// Owns everything that makes the miniature feel like an exhibit piece —
    /// the slow ambient spin (paused while a hand holds it), the feature name
    /// tags anchored in galaxy-local kpc coordinates, the pulsing "we are
    /// here" sun ring, and a highlight ring the guided tour points with.
    /// </summary>
    public class MilkyWayMRStage : MonoBehaviour
    {
        public MilkyWayController controller;
        public MRBodyLabels labels;

        [Tooltip("Ambient yaw while nobody is holding the galaxy. Slow enough " +
                 "to read as majestic, fast enough to sell the 3D structure.")]
        public float ambientSpinDegPerSec = 0.8f;
        public bool spin = true;

        XRGrabInteractable grab;
        Vector3 homePos; Quaternion homeRot; Vector3 homeScale;

        LineRenderer sunRing, highlight;
        Material ringMat;
        Color highlightColor;

        // Feature anchors in galaxy-local kpc (1 unit = 1 kpc), matching the
        // shader's layout: disk R=16, Sun at (8.2, 0, 0), bar 27° off +x.
        static readonly Vector3 SunLocal = new Vector3(8.2f, 0.02f, 0f);

        void Start()
        {
            grab = GetComponent<XRGrabInteractable>();
            homePos = transform.position;
            homeRot = transform.rotation;
            homeScale = transform.localScale;

            ringMat = new Material(Shader.Find("Sprites/Default"));
            sunRing = MakeRing("You Are Here (MR)", SunLocal, 0.9f, 64);
            BuildLabels();
        }

        void BuildLabels()
        {
            if (labels == null) return;
            labels.Init(transform);
            // Spread the tags: from the exhibit's oblique view every label
            // near the axis stacks over the bulge glare, so only Sgr A* may
            // claim the centre — everything else lives out on the disk.
            AddLabel(new Vector3(0f, 2.2f, 0f),
                () => Loc.T("궁수자리 A*\n(은하 중심 블랙홀)", "Sagittarius A*\n(central black hole)",
                            "いて座A*\n(銀河中心ブラックホール)", "人马座A*\n(银河中心黑洞)"));
            AddLabel(SunLocal + new Vector3(0f, 1.1f, 0f),
                () => Loc.T("태양 — 우리는 여기", "The Sun — we are here",
                            "太陽 — 私たちはここ", "太阳——我们在这里"));
            AddLabel(new Vector3(3.4f, 0.5f, 1.7f),
                () => Loc.T("막대", "The bar", "棒", "棒"));
            AddLabel(new Vector3(-10f, 0.5f, 4f),
                () => Loc.T("나선팔", "Spiral arm", "渦状腕", "旋臂"));
            AddLabel(new Vector3(11f, 4.5f, -3f),
                () => Loc.T("헤일로", "Halo", "ハロー", "银晕"));
        }

        void AddLabel(Vector3 local, System.Func<string> text)
        {
            var anchor = new GameObject("Anchor — label").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = local;
            anchor.localScale = Vector3.zero; // radius term contributes nothing
            labels.Add(anchor, text, 0f, 0.01f);
        }

        public bool Held => grab != null && grab.isSelected;

        void Update()
        {
            if (spin && !Held)
                transform.Rotate(0f, ambientSpinDegPerSec * Time.deltaTime, 0f, Space.Self);

            float s = transform.lossyScale.x;
            if (sunRing != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.6f);
                var c = new Color(1.7f, 1.25f, 0.55f, pulse);
                sunRing.startColor = c; sunRing.endColor = c;
                sunRing.widthMultiplier = 3.5f * s; // LineRenderer width ignores transform scale
            }
            if (highlight != null)
            {
                float pulse = 0.6f + 0.3f * Mathf.Sin(Time.time * 3.1f);
                var c = highlightColor; c.a = pulse;
                highlight.startColor = c; highlight.endColor = c;
                highlight.widthMultiplier = 7f * s;
            }
        }

        /// <summary>Put the miniature back where the exhibit expects it —
        /// visitors walk away with it in their hands.</summary>
        public void ResetPose()
        {
            if (Held) return;
            transform.SetPositionAndRotation(homePos, homeRot);
            transform.localScale = homeScale;
        }

        public void SetLabelsVisible(bool on)
        {
            if (labels != null) labels.SetVisible(on);
            if (sunRing != null) sunRing.gameObject.SetActive(on);
        }

        public bool LabelsVisible => labels != null && labels.Visible;

        // ---------------- tour highlight ---------------------------------

        /// <summary>Ring the feature being narrated. Local kpc coordinates —
        /// the ring is parented to the galaxy, so grabbing, spinning and
        /// scaling all carry it along.</summary>
        public void SetHighlight(Vector3 localCenter, float localRadius, Color color)
        {
            ClearHighlight();
            highlightColor = color;
            // Float the ring a little above the disk plane — flush with it,
            // the ring sinks into the bulge/arm glare.
            localCenter.y += 0.3f;
            highlight = MakeRing("Tour Highlight (MR)", localCenter, localRadius, 96);
        }

        public void ClearHighlight()
        {
            if (highlight != null) Destroy(highlight.gameObject);
            highlight = null;
        }

        LineRenderer MakeRing(string name, Vector3 localCenter, float localRadius, int segments)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localCenter;
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = segments;
            line.loop = true;
            line.useWorldSpace = false;
            line.material = ringMat;
            line.startWidth = line.endWidth = 0.03f; // scaled via widthMultiplier
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * localRadius);
            }
            return line;
        }

        void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
        }
    }
}

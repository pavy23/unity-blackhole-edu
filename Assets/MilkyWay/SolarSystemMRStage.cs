using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BlackHoleEffect; // Loc, BlackHoleUI, NarrationManager

namespace MilkyWay
{
    /// <summary>
    /// The MR orrery: the detailed solar system spawned as a room-scale
    /// exhibit piece (Neptune's orbit ≈ 1.2 m), floating at chest height.
    /// Mirrors the desktop <see cref="SolarSystemStage"/> ownership pattern —
    /// spawn once, keep forever — and adds what MR needs: name tags, a
    /// camera-facing highlight ring that follows an orbiting body, and the
    /// scale-truth blend re-authored for a fixed viewer: instead of the
    /// camera pulling back, the WHOLE RIG shrinks so Neptune's true orbit
    /// lands exactly where its friendly-map orbit was.
    /// </summary>
    public class SolarSystemMRStage : MonoBehaviour
    {
        [Tooltip("Rig-local Neptune orbit is 0.0604; scale 20 puts it at 1.21 m.")]
        public float rigScale = 20f;
        [Tooltip("Ambient orbit/spin speed (desktop stage uses 0.25).")]
        public float ambientMotionScale = 0.25f;
        public MRBodyLabels labels;

        public SolarSystemRig Rig { get; private set; }
        public bool TruthRunning { get; private set; }

        /// <summary>Fired when the scale-truth sequence finishes or aborts —
        /// the menu uses it to bring its rows back.</summary>
        public event System.Action TruthEnded;

        // True-scale mapping: Neptune's true orbit is √30.1 ≈ 5.49× its map
        // orbit. Shrinking the rig by that factor while realism blends in
        // keeps Neptune's ring exactly where it was — the planets shrink into
        // grains around it, which is the whole lesson.
        const float TruthShrink = 0.18225f;

        static readonly string[] BodyKeys =
            { "Sun", "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };

        XRGrabInteractable grab;
        LineRenderer highlight;
        Material ringMat;
        Transform highlightTarget;
        Coroutine truthRoutine;
        RectTransform captionPanel;
        Text caption;

        void Awake()
        {
            Rig = SolarSystemRig.Spawn(transform.position, transform);
            Rig.transform.localScale = Vector3.one * rigScale;
            Rig.motionScale = ambientMotionScale;
            grab = GetComponent<XRGrabInteractable>();
            ringMat = new Material(Shader.Find("Sprites/Default"));
        }

        void Start()
        {
            BuildLabels();
        }

        void BuildLabels()
        {
            if (labels == null) return;
            labels.Init(Rig.transform);
            foreach (var key in BodyKeys)
            {
                var visual = Rig.GetBodyVisual(key);
                if (visual == null) continue;
                string k = key; // capture per body, not the loop variable
                labels.Add(visual, () => BodyName(k), 1.7f, 0.012f);
            }
        }

        static string BodyName(string key) => key switch
        {
            "Sun"     => Loc.T("태양", "Sun", "太陽", "太阳"),
            "Mercury" => Loc.T("수성", "Mercury", "水星", "水星"),
            "Venus"   => Loc.T("금성", "Venus", "金星", "金星"),
            "Earth"   => Loc.T("지구", "Earth", "地球", "地球"),
            "Mars"    => Loc.T("화성", "Mars", "火星", "火星"),
            "Jupiter" => Loc.T("목성", "Jupiter", "木星", "木星"),
            "Saturn"  => Loc.T("토성", "Saturn", "土星", "土星"),
            "Uranus"  => Loc.T("천왕성", "Uranus", "天王星", "天王星"),
            "Neptune" => Loc.T("해왕성", "Neptune", "海王星", "海王星"),
            _ => key,
        };

        public bool Held => grab != null && grab.isSelected;

        public void SetLabelsVisible(bool on)
        {
            if (labels != null) labels.SetVisible(on);
        }

        public bool LabelsVisible => labels != null && labels.Visible;

        public void SetMotionScale(float s)
        {
            if (Rig != null) Rig.motionScale = s;
        }

        // ---------------- tour highlight ----------------------------------

        /// <summary>Ring a body. The ring follows the body around its orbit
        /// and faces the viewer (a flat ring collapses to a line edge-on at
        /// chest height).</summary>
        public void SetHighlight(string bodyKey)
        {
            highlightTarget = Rig != null ? Rig.GetBodyVisual(bodyKey) : null;
            if (highlightTarget == null) { ClearHighlight(); return; }
            if (highlight == null)
            {
                var go = new GameObject("Tour Highlight (MR)");
                highlight = go.AddComponent<LineRenderer>();
                highlight.positionCount = 72;
                highlight.loop = true;
                highlight.useWorldSpace = true;
                highlight.material = ringMat;
                highlight.startWidth = highlight.endWidth = 1f;
                highlight.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            highlight.gameObject.SetActive(true);
        }

        public void ClearHighlight()
        {
            highlightTarget = null;
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (highlight == null || !highlight.gameObject.activeSelf) return;
            if (highlightTarget == null) { ClearHighlight(); return; }
            var cam = Camera.main;
            if (cam == null) return;

            float scaleRatio = Rig != null ? Rig.transform.lossyScale.x / Mathf.Max(rigScale, 1e-6f) : 1f;
            float radius = Mathf.Max(highlightTarget.lossyScale.x * 2.6f, 0.05f * scaleRatio);
            Vector3 center = highlightTarget.position;
            Vector3 toCam = (cam.transform.position - center).normalized;
            Vector3 upRef = Mathf.Abs(toCam.y) > 0.98f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(upRef, toCam).normalized;
            Vector3 up = Vector3.Cross(toCam, right);
            int n = highlight.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                highlight.SetPosition(i, center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius);
            }
            float pulse = 0.6f + 0.3f * Mathf.Sin(Time.time * 3.1f);
            var c = new Color(0.55f, 1.35f, 1.7f, pulse);
            highlight.startColor = c; highlight.endColor = c;
            highlight.widthMultiplier = 0.006f * Mathf.Max(scaleRatio, 0.2f);
        }

        // ---------------- scale truth, MR edition --------------------------

        public void ToggleTruth()
        {
            if (TruthRunning) AbortTruth();
            else truthRoutine = StartCoroutine(RunTruth());
        }

        public void AbortTruth()
        {
            if (!TruthRunning) return;
            if (truthRoutine != null) StopCoroutine(truthRoutine);
            NarrationManager.Instance.Stop();
            FinishTruth();
        }

        void FinishTruth()
        {
            if (Rig != null)
            {
                Rig.SetRealism(0f);
                Rig.transform.localScale = Vector3.one * rigScale;
            }
            SetLabelsVisible(true);
            HideCaption();
            TruthRunning = false;
            TruthEnded?.Invoke();
        }

        IEnumerator RunTruth()
        {
            TruthRunning = true;
            SetLabelsVisible(false); // tags would float over vanished grains
            ClearHighlight();
            float baseScale = rigScale;

            // Beat 0: confess the friendly map.
            float len0 = Narrate(0);
            yield return new WaitForSeconds(Mathf.Max(4f, len0 + 0.4f));

            // Blend out: map → truth. The rig shrinks in step so Neptune's
            // orbit holds its place in the room while everything inside it
            // collapses toward the sun-point.
            float len1 = Narrate(1);
            const float blend = 10f;
            for (float t = 0f; t < blend; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / blend);
                if (Rig != null)
                {
                    Rig.SetRealism(u);
                    Rig.transform.localScale = Vector3.one * (baseScale * Mathf.Lerp(1f, TruthShrink, u));
                }
                yield return null;
            }
            if (Rig != null)
            {
                Rig.SetRealism(1f);
                Rig.transform.localScale = Vector3.one * (baseScale * TruthShrink);
            }
            yield return new WaitForSeconds(Mathf.Max(4f, len1 - blend + 2f));

            // The emptiness beat, then the blend home.
            float len2 = Narrate(2);
            yield return new WaitForSeconds(Mathf.Max(6f, len2 - 4f));
            for (float t = 0f; t < blend * 0.5f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / (blend * 0.5f));
                if (Rig != null)
                {
                    Rig.SetRealism(1f - u);
                    Rig.transform.localScale = Vector3.one * (baseScale * Mathf.Lerp(TruthShrink, 1f, u));
                }
                yield return null;
            }
            FinishTruth();
        }

        float Narrate(int i)
        {
            float len = NarrationManager.Instance.Play("ss_scale_" + i);
            Caption(Loc.T(ScaleTruth.NarrationLines[i], ScaleTruth.NarrationLinesEn[i],
                          ScaleTruth.NarrationLinesJa[i], ScaleTruth.NarrationLinesZh[i]));
            return len;
        }

        void Caption(string text)
        {
            if (captionPanel == null)
            {
                var canvas = BlackHoleUI.EnsureCanvas(Camera.main);
                captionPanel = BlackHoleUI.MakePanel(canvas.transform, "Scale Truth Caption (MR)",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1100f, 150f));
                caption = BlackHoleUI.MakeText(captionPanel, "Text", 21, BlackHoleUI.TextPrimary,
                    TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(28f, 0f), new Vector2(1044f, 150f));
                caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            captionPanel.gameObject.SetActive(true);
            caption.text = text;
        }

        void HideCaption()
        {
            if (captionPanel != null) captionPanel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
        }
    }
}

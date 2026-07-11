using System.Collections.Generic;
using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Tidal-force ("spaghettification") demo: a small glowing star falls
    /// toward the hole on a diagonal in the camera-facing plane, stretching
    /// along the radial direction and squeezing sideways as the tidal
    /// gradient grows. Once the stretch becomes violent the star sheds two
    /// glowing tidal streams (inward and outward — like a real tidal
    /// disruption event), reddens and dims from gravitational redshift, and
    /// fades out just above the horizon before restarting.
    /// </summary>
    [ExecuteAlways]
    public class SpaghettificationDemo : MonoBehaviour
    {
        public Transform blackHole;
        public bool active;
        public float startDistanceRs = 10f;
        public float fallDuration = 9f;
        [Tooltip("Cap on the tidal stretch factor so the probe stays readable.")]
        public float maxStretch = 9f;

        /// <summary>Current probe distance in Rs (large when inactive) — used
        /// by the light-curve graph to trigger the tidal disruption flare.</summary>
        public float CurrentDistanceRs { get; private set; } = 999f;

        const int StreamPoints = 14;

        GameObject probe;
        Material probeMat;
        LineRenderer streamIn, streamOut;
        Material streamMat;
        float t;

        static readonly int StarColorId = Shader.PropertyToID("_StarColor");

        void OnDisable()
        {
            if (probe != null) DestroyImmediate(probe);
            if (streamIn != null) DestroyImmediate(streamIn.gameObject);
            if (streamOut != null) DestroyImmediate(streamOut.gameObject);
            if (probeMat != null) DestroyImmediate(probeMat);
            if (streamMat != null) DestroyImmediate(streamMat);
            probe = null; probeMat = null; streamMat = null;
            streamIn = null; streamOut = null;
        }

        void Update()
        {
            if (blackHole == null) { enabled = false; return; }
            if (!active)
            {
                CurrentDistanceRs = 999f;
                if (probe != null) probe.SetActive(false);
                if (streamIn != null) streamIn.gameObject.SetActive(false);
                if (streamOut != null) streamOut.gameObject.SetActive(false);
                return;
            }
            EnsureProbe();
            probe.SetActive(true);

            if (Application.isPlaying)
            {
                t += Time.deltaTime / fallDuration;
                if (t >= 1f) t = 0f;
            }
            else t = 0.82f; // representative mid-stretch pose for the editor

            var cam = Camera.main;
            if (cam == null) return;

            float rs = blackHole.lossyScale.x;
            Vector3 center = blackHole.position;
            Vector3 toCam = (cam.transform.position - center).normalized;
            Vector3 upRef = Mathf.Abs(toCam.y) > 0.98f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(upRef, -toCam).normalized;
            Vector3 up = Vector3.Cross(-toCam, right);

            // Accelerating fall from upper-right toward the hole.
            float s = Mathf.Pow(t, 2.2f);
            float rRs = Mathf.Lerp(startDistanceRs, 1.02f, s);
            CurrentDistanceRs = rRs;
            Vector3 radial = (right * 0.72f + up * 0.62f).normalized;
            Vector3 pos = center + radial * (rRs * rs) + toCam * (0.9f * rs);
            probe.transform.position = pos;

            // Tidal stretch ~ (r0/r)^1.4 along the radial direction.
            float stretch = Mathf.Min(Mathf.Pow(startDistanceRs / rRs, 1.4f), maxStretch);
            float squeeze = 1f / Mathf.Sqrt(stretch);
            float baseSize = 0.55f * rs;
            probe.transform.localScale = new Vector3(baseSize * squeeze, baseSize * stretch * 0.5f, baseSize * squeeze);
            probe.transform.rotation = Quaternion.FromToRotation(Vector3.up, radial);

            // Dim and redden as it approaches the horizon (gravitational redshift).
            float fade = Mathf.Clamp01((rRs - 1.02f) / 0.9f);
            if (probeMat != null)
                probeMat.SetColor(StarColorId,
                    Color.Lerp(new Color(1.4f, 0.16f, 0.05f), new Color(2.4f, 1.35f, 0.55f), fade)
                    * Mathf.Lerp(0.12f, 1f, fade));

            // Tidal streams appear once the star is being torn apart.
            UpdateStreams(center, radial, rRs, rs, toCam, stretch, fade);
        }

        void UpdateStreams(Vector3 center, Vector3 radial, float rRs, float rs,
                           Vector3 toCam, float stretch, float fade)
        {
            if (streamIn == null) return;
            float shed = Mathf.InverseLerp(2.2f, 5.5f, stretch); // 0 → intact, 1 → fully shedding
            bool show = shed > 0.01f;
            streamIn.gameObject.SetActive(show);
            streamOut.gameObject.SetActive(show);
            if (!show) return;

            // Inner stream: matter racing ahead toward the horizon.
            float innerLen = Mathf.Lerp(0.3f, rRs - 1.05f, shed);
            FillStream(streamIn, center, radial, rRs, -innerLen, rs, toCam, shed);

            // Outer stream: the tail flung outward, shorter.
            float outerLen = Mathf.Lerp(0.2f, 2.6f, shed) * 0.8f;
            FillStream(streamOut, center, radial, rRs, outerLen, rs, toCam, shed * 0.7f);

            float w = 0.10f * rs * Mathf.Lerp(0.4f, 1f, shed);
            streamIn.widthMultiplier = w;
            streamOut.widthMultiplier = w * 0.8f;

            // Streams redden and dim with the star.
            Color tint = Color.Lerp(new Color(1.6f, 0.25f, 0.08f), new Color(2.4f, 1.2f, 0.4f), fade);
            streamMat.SetColor("_Tint", tint * Mathf.Lerp(0.25f, 1f, fade));
        }

        void FillStream(LineRenderer line, Vector3 center, Vector3 radial, float rRs,
                        float lengthRs, float rs, Vector3 toCam, float wiggleAmp)
        {
            line.positionCount = StreamPoints;
            Vector3 side = Vector3.Cross(radial, toCam).normalized;
            for (int i = 0; i < StreamPoints; i++)
            {
                float k = i / (float)(StreamPoints - 1);
                float r = rRs + lengthRs * k;
                // Slight sideways drift + animated ripple so the stream feels fluid.
                float wiggle = Mathf.Sin(k * 9f + Time.time * 5f) * 0.05f * wiggleAmp * k;
                float drift = 0.10f * wiggleAmp * k * k * Mathf.Sign(lengthRs);
                Vector3 p = center + radial * (r * rs) + side * ((wiggle + drift) * rs) + toCam * (0.9f * rs);
                line.SetPosition(i, p);
            }
        }

        void EnsureProbe()
        {
            if (probe != null) return;

            probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            probe.name = "Tidal Star";
            probe.hideFlags = HideFlags.HideAndDontSave;
            probe.transform.SetParent(transform, false);
            DestroyImmediate(probe.GetComponent<Collider>());

            // A real little star, not a flat capsule: granulation + corona.
            probeMat = new Material(Shader.Find("BlackHole/StarSurface")) { hideFlags = HideFlags.HideAndDontSave };
            probeMat.SetColor(StarColorId, new Color(2.4f, 1.35f, 0.55f));
            probeMat.SetFloat("_Granulation", 0.5f);
            probeMat.SetFloat("_GranScale", 9f);
            probeMat.SetFloat("_SpotStrength", 0.2f);
            probeMat.SetFloat("_CoronaBoost", 0.9f);
            probeMat.SetFloat("_CoronaExtent", 1.7f);
            probe.GetComponent<MeshRenderer>().sharedMaterial = probeMat;

            streamMat = new Material(Shader.Find("BlackHole/PhotonTrail")) { hideFlags = HideFlags.HideAndDontSave };
            streamMat.SetColor("_Tint", new Color(2.4f, 1.2f, 0.4f, 1f));
            streamMat.SetFloat("_PulseSpeed", 3.5f);
            streamMat.SetFloat("_PulseAmount", 0.35f);
            streamMat.SetFloat("_HeadBoost", 0f);
            streamMat.SetFloat("_TailFade", 0.05f);

            streamIn = MakeStream("Tidal Stream In");
            streamOut = MakeStream("Tidal Stream Out");
        }

        LineRenderer MakeStream(string name)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(probe.transform.parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.material = streamMat;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.useWorldSpace = true;
            line.startColor = line.endColor = Color.white;
            go.SetActive(false);
            return line;
        }
    }
}

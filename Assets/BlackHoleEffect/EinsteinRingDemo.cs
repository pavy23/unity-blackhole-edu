using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Einstein ring demonstration: places a single bright star far behind the
    /// black hole (relative to the camera) and sweeps it sideways. As the star
    /// passes directly behind the hole its image is lensed from a point into
    /// two arcs and finally a complete ring — computed by the same geodesics
    /// that render everything else.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class EinsteinRingDemo : MonoBehaviour
    {
        public bool active;
        [Tooltip("Horizontal offset of the star from directly-behind, in degrees.")]
        [Range(-25f, 25f)] public float offsetDegrees = 8f;
        public bool autoSweep = true;
        public float sweepSpeed = 3f;     // degrees per second
        [Range(-25f, 25f)] public float sweepRange = 10f;
        public float starBrightness = 6f;
        [Tooltip("Angular size — small = star, large = extended galaxy source.")]
        public float starSize = 0.0007f;

        static readonly int OnId = Shader.PropertyToID("_DemoStarOn");
        static readonly int DirId = Shader.PropertyToID("_DemoStarDir");
        static readonly int BrightId = Shader.PropertyToID("_DemoStarBrightness");
        static readonly int SizeId = Shader.PropertyToID("_DemoStarSize");

        Renderer cachedRenderer;

        void OnDisable() => Push(false);
        void Update()
        {
            if (active && autoSweep && Application.isPlaying)
                offsetDegrees = Mathf.PingPong(Time.time * sweepSpeed, sweepRange * 2f) - sweepRange;
            Push(active);
        }

        /// <summary>Manual nudge from input controls; disables the auto sweep.</summary>
        public void Nudge(float degrees)
        {
            autoSweep = false;
            offsetDegrees = Mathf.Clamp(offsetDegrees + degrees, -25f, 25f);
        }

        void Push(bool on)
        {
            if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();
            var mat = cachedRenderer != null ? cachedRenderer.sharedMaterial : null;
            if (mat == null) return;

            mat.SetFloat(OnId, on ? 1f : 0f);
            if (!on) return;

            var cam = Camera.main;
            if (cam == null) return;

            // Star direction: directly away from the camera through the hole,
            // rotated sideways by offsetDegrees (plus a tiny elevation so the
            // ring is not perfectly degenerate with the disk plane).
            Vector3 center = transform.position;
            Vector3 behind = (center - cam.transform.position).normalized;
            Vector3 dirWS = Quaternion.AngleAxis(offsetDegrees, Vector3.up)
                          * Quaternion.AngleAxis(1.5f, Vector3.right) * behind;
            Vector3 dirOS = transform.InverseTransformDirection(dirWS).normalized;

            mat.SetVector(DirId, dirOS);
            mat.SetFloat(BrightId, starBrightness);
            mat.SetFloat(SizeId, starSize);
        }
    }
}

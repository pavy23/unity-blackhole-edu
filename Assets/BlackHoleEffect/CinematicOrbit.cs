using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Very slow cinematic drift around a target — enough motion to make the
    /// gravitational lensing readable in an educational presentation without
    /// distracting from it. Play mode only.
    /// </summary>
    public class CinematicOrbit : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Degrees per second around the vertical axis.")]
        public float orbitSpeed = 0.8f;
        [Tooltip("Slow vertical bobbing amplitude in metres.")]
        public float bobAmplitude = 0.15f;
        public float bobPeriod = 34f;

        float baseHeight;
        float enableTime;

        // Re-baseline on every enable so resuming after manual camera control
        // continues smoothly from wherever the user left the camera (no snap
        // back to the original height, bob phase restarts at zero offset).
        void OnEnable()
        {
            baseHeight = transform.position.y;
            enableTime = Time.time;
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);
            var p = transform.position;
            p.y = baseHeight + Mathf.Sin((Time.time - enableTime) * (2f * Mathf.PI / bobPeriod)) * bobAmplitude;
            transform.position = p;
            transform.LookAt(target.position);
        }
    }
}

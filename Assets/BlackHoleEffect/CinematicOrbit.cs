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

        float enableTime;
        float lastBob;

        void OnEnable()
        {
            enableTime = Time.time;
            lastBob = 0f;
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);

            // Bob is applied as an INCREMENT, never as an absolute height —
            // an absolute assignment would overwrite the user's pitch input
            // every frame (right-drag up/down felt dead because of that).
            float bob = Mathf.Sin((Time.time - enableTime) * (2f * Mathf.PI / bobPeriod)) * bobAmplitude;
            var p = transform.position;
            p.y += bob - lastBob;
            lastBob = bob;
            transform.position = p;
            transform.LookAt(target.position);
        }
    }
}

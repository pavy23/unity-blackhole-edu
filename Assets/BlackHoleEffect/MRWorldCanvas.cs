using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Hangs the shared UI canvas in the room for MR: centred on the black hole
    /// so the empty middle of the 1920x1080 layout frames the real object and
    /// the panels sit around it, kept upright and turned toward the viewer.
    ///
    /// The canvas only copies the target's position — never parents to it. The
    /// hole is two-hand scalable, and a parented canvas would scale the text
    /// along with it.
    /// </summary>
    [DisallowMultipleComponent]
    public class MRWorldCanvas : MonoBehaviour
    {
        [Tooltip("Left empty, the hole in the scene is found on first placement.")]
        public Transform target;
        public Camera viewer;

        [Tooltip("Distance in front of the viewer when there is no target.")]
        public float fallbackDistance = 2f;

        [Tooltip("Degrees per second the frame turns to face the viewer. Snapping " +
                 "it every frame makes the panels feel glued to the face.")]
        public float turnSpeed = 90f;

        void LateUpdate() => Place(Time.deltaTime * turnSpeed);

        /// <summary>Snap into place without easing (first frame).</summary>
        public void PlaceNow() => Place(360f);

        void Place(float maxTurnDegrees)
        {
            var cam = viewer != null ? viewer : Camera.main;
            if (cam == null) return;

            if (target == null)
            {
                var hole = FindAnyObjectByType<BlackHoleController>();
                if (hole != null) target = hole.transform;
            }

            transform.position = target != null
                ? target.position
                : cam.transform.position + cam.transform.forward * fallbackDistance;

            // Yaw only: a frame that pitches with the viewer's gaze reads as a
            // heads-up display stuck to the helmet, not as an object in the room.
            var away = transform.position - cam.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 1e-4f) return;

            var want = Quaternion.LookRotation(away.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, maxTurnDegrees);
        }
    }
}

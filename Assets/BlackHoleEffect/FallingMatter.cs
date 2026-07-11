using System.Collections;
using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// MR throwable: grab a glowing star-ball, toss it, and gravity pulls it
    /// into an orbit that decays into the hole — the disk flares as it feeds.
    /// The ball respawns at its shelf position a moment later.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingMatter : MonoBehaviour
    {
        public Transform hole;
        public MatterFlare flare;
        [Tooltip("Effective GM in m^3/s^2 — tuned for room-scale orbits.")]
        public float gm = 0.35f;

        Rigidbody rb;
        Vector3 spawnPos;
        Quaternion spawnRot;
        Vector3 baseScale;
        bool consumed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnPos = transform.position;
            spawnRot = transform.rotation;
            baseScale = transform.localScale;
        }

        void FixedUpdate()
        {
            if (consumed || hole == null || rb.isKinematic) return;

            Vector3 d = hole.position - transform.position;
            float r = Mathf.Max(d.magnitude, 0.05f);
            rb.AddForce(d / r * (gm / (r * r)), ForceMode.Acceleration);

            if (r < 1.6f * hole.lossyScale.x)
            {
                consumed = true;
                if (flare != null) flare.Trigger();
                StartCoroutine(ConsumeAndRespawn());
            }
        }

        IEnumerator ConsumeAndRespawn()
        {
            // Spiral shrink into the hole.
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                transform.localScale = baseScale * (1f - t / 0.5f);
                yield return null;
            }
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.localScale = Vector3.zero;

            yield return new WaitForSeconds(2.5f);

            transform.SetPositionAndRotation(spawnPos, spawnRot);
            transform.localScale = baseScale;
            consumed = false;
        }
    }
}

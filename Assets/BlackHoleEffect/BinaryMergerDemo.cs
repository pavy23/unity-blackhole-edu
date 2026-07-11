using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace BlackHoleEffect
{
    /// <summary>
    /// Binary black hole inspiral and merger (MR): a second, smaller hole
    /// orbits the first with shrinking separation and rising frequency —
    /// exactly the chirp LIGO hears. Controller haptics pulse harder as the
    /// merger approaches ("feel the gravitational waves"), then the survivors
    /// merge with a flash and expanding wave shells. Loops while enabled.
    /// </summary>
    public class BinaryMergerDemo : MonoBehaviour
    {
        public Transform holeA;
        public MatterFlare flare;
        public bool autoRun = true;
        [Tooltip("Seconds between merger loops.")]
        public float loopPeriod = 55f;
        public float inspiralSeconds = 18f;

        GameObject holeB;
        Vector3 centerPos;
        Vector3 aBaseScale;

        void Start()
        {
            if (Application.isPlaying && autoRun && holeA != null)
                StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            aBaseScale = holeA.localScale;
            centerPos = holeA.position;
            yield return new WaitForSeconds(8f); // let the user settle in first
            while (autoRun)
            {
                yield return StartCoroutine(RunMerger());
                yield return new WaitForSeconds(Mathf.Max(loopPeriod - inspiralSeconds, 5f));
            }
        }

        IEnumerator RunMerger()
        {
            float rsA = holeA.lossyScale.x;

            // Companion: same renderer/material, stripped of interaction bits.
            holeB = Instantiate(holeA.gameObject, centerPos, holeA.rotation);
            holeB.name = "Companion Hole";
            holeB.hideFlags = HideFlags.HideAndDontSave;
            foreach (var comp in holeB.GetComponents<Component>())
            {
                if (comp is Transform || comp is MeshFilter || comp is MeshRenderer) continue;
                if (comp is BlackHoleController) continue; // keeps material props pushed
                Destroy(comp);
            }
            holeB.transform.localScale = aBaseScale * 0.62f;

            float phase = 0f;
            for (float t = 0f; t < inspiralSeconds; t += Time.deltaTime)
            {
                float k = t / inspiralSeconds;
                float sep = Mathf.Lerp(9f * rsA, 0.4f * rsA, Mathf.Pow(k, 1.7f));
                float omega = 1.2f / Mathf.Pow(Mathf.Max(sep / rsA, 0.4f), 1.5f); // Kepler chirp
                phase += omega * Time.deltaTime * 4f;

                Vector3 offset = new Vector3(Mathf.Cos(phase), 0f, Mathf.Sin(phase)) * sep;
                holeA.position = centerPos - offset * 0.38f;
                holeB.transform.position = centerPos + offset * 0.62f;

                // Gravitational-wave haptics: stronger and faster near merger.
                float amp = Mathf.Clamp01(0.08f + Mathf.Pow(k, 2.5f));
                if (Mathf.Sin(phase * 2f) > 0.6f) SendHaptics(amp, 0.04f);
                yield return null;
            }

            // Merger: flash + expanding wave shells + one bigger hole.
            SendHaptics(1f, 0.3f);
            if (flare != null) flare.Trigger();
            Destroy(holeB);
            holeA.position = centerPos;
            StartCoroutine(WaveShells());

            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                holeA.localScale = Vector3.Lerp(aBaseScale, aBaseScale * 1.22f, t / 1.5f);
                yield return null;
            }

            // Settle back to the standard size for the next loop.
            yield return new WaitForSeconds(6f);
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                holeA.localScale = Vector3.Lerp(aBaseScale * 1.22f, aBaseScale, t / 2f);
                yield return null;
            }
            holeA.localScale = aBaseScale;
        }

        IEnumerator WaveShells()
        {
            for (int s = 0; s < 3; s++)
            {
                var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shell.name = "GW Shell";
                shell.hideFlags = HideFlags.HideAndDontSave;
                Destroy(shell.GetComponent<Collider>());
                shell.transform.position = centerPos;
                var mat = new Material(Shader.Find("BlackHole/JetParticle")) { hideFlags = HideFlags.HideAndDontSave };
                shell.GetComponent<MeshRenderer>().sharedMaterial = mat;
                StartCoroutine(ExpandShell(shell, mat));
                yield return new WaitForSeconds(0.35f);
            }
        }

        IEnumerator ExpandShell(GameObject shell, Material mat)
        {
            float rsA = holeA.lossyScale.x;
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                float k = t / 2.2f;
                shell.transform.localScale = Vector3.one * Mathf.Lerp(0.5f * rsA, 30f * rsA, k);
                mat.SetColor("_Tint", new Color(1.6f, 1.9f, 2.6f, 0.5f * Mathf.Pow(1f - k, 2f)));
                yield return null;
            }
            Destroy(shell);
            Destroy(mat);
        }

        void SendHaptics(float amplitude, float duration)
        {
            SendTo(XRNode.LeftHand, amplitude, duration);
            SendTo(XRNode.RightHand, amplitude, duration);
        }

        static void SendTo(XRNode node, float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}

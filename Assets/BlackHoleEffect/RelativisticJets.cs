using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Relativistic polar jets, two layers per pole:
    ///   * Core — a narrow, fast, white-hot stretched beam.
    ///   * Sheath — a wider, slower cone of wisps twisted into a helix by
    ///     orbital velocity + noise, like magnetic field lines winding up.
    /// Brightness fades with distance (color-over-lifetime), everything is
    /// additive so the bloom ties it into the disk. Built entirely in code.
    /// </summary>
    [ExecuteAlways]
    public class RelativisticJets : MonoBehaviour
    {
        public bool active;
        [Tooltip("Jet length in Rs.")]
        public float lengthRs = 16f;

        ParticleSystem[] systems;
        Material jetMat;
        float builtScale = -1f;
        static Texture2D softTex;

        static Texture2D SoftParticleTexture()
        {
            if (softTex != null) return softTex;
            const int dim = 64;
            softTex = new Texture2D(dim, dim, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float c = (dim - 1) * 0.5f;
            for (int y = 0; y < dim; y++)
            for (int x = 0; x < dim; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
                softTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            softTex.Apply();
            return softTex;
        }

        void OnDisable() => Teardown();

        void Teardown()
        {
            if (systems != null)
                foreach (var s in systems)
                    if (s != null) DestroyImmediate(s.gameObject);
            if (jetMat != null) DestroyImmediate(jetMat);
            systems = null; jetMat = null; builtScale = -1f;
        }

        void Update()
        {
            float rs = transform.lossyScale.x;
            // Rebuild when the hole is rescaled (mass presets change scale).
            if (active && (systems == null || !Mathf.Approximately(builtScale, rs))) Build();
            if (systems == null) return;

            var cam = Camera.main;
            Vector3 bias = Vector3.zero;
            if (cam != null)
                bias = (cam.transform.position - transform.position).normalized * (0.9f * rs);

            for (int i = 0; i < systems.Length; i++)
            {
                var s = systems[i];
                if (s == null) continue;
                float sign = (i < 2) ? 1f : -1f;
                s.transform.position = transform.position + bias;
                s.transform.rotation = Quaternion.LookRotation(sign * transform.up, transform.forward);
                var emission = s.emission;
                emission.enabled = active;
                if (active && !s.isPlaying) s.Play();
            }
        }

        void Build()
        {
            Teardown();
            // HideAndDontSave systems survive domain reloads while our field
            // references reset — sweep orphans by name before rebuilding.
            foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
                if (ps != null && (ps.name.StartsWith("Jet N ") || ps.name.StartsWith("Jet S ")))
                    DestroyImmediate(ps.gameObject);

            float rs = transform.lossyScale.x;
            builtScale = rs;

            jetMat = new Material(Shader.Find("BlackHole/JetParticle")) { hideFlags = HideFlags.HideAndDontSave };
            jetMat.SetTexture("_BaseMap", SoftParticleTexture());
            jetMat.SetColor("_Tint", new Color(2.2f, 2.9f, 5.2f, 1f));

            systems = new ParticleSystem[4];
            systems[0] = BuildLayer(+1f, true, rs);
            systems[1] = BuildLayer(+1f, false, rs);
            systems[2] = BuildLayer(-1f, true, rs);
            systems[3] = BuildLayer(-1f, false, rs);
        }

        ParticleSystem BuildLayer(float sign, bool core, float rs)
        {
            var go = new GameObject((sign > 0 ? "Jet N " : "Jet S ") + (core ? "Core" : "Sheath"))
            { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform.parent, false);
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.LookRotation(sign * transform.up, transform.forward);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = core ? 700 : 500;

            float length = lengthRs * rs;
            if (core)
            {
                main.startLifetime = 2.0f;
                main.startSpeed = length / 2.0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f * rs, 0.15f * rs);
                main.startColor = new Color(0.9f, 0.95f, 1f, 0.85f);
                main.maxParticles = 1200;
            }
            else
            {
                main.startLifetime = 2.8f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(length / 4.2f, length / 3.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.28f * rs, 0.55f * rs);
                main.startColor = new Color(0.5f, 0.55f, 1f, 0.38f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.maxParticles = 800;
            }

            var emission = ps.emission;
            emission.rateOverTime = core ? 320f : 160f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = core ? 1.6f : 6.5f;
            shape.radius = (core ? 0.045f : 0.13f) * rs;

            // Fade in fast, glow, fade out toward the tip.
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            if (core)
                grad.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                            new GradientColorKey(new Color(0.62f, 0.78f, 1f), 0.45f),
                            new GradientColorKey(new Color(0.4f, 0.5f, 1f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.06f),
                            new GradientAlphaKey(0.4f, 0.55f), new GradientAlphaKey(0f, 1f) });
            else
                grad.SetKeys(
                    new[] { new GradientColorKey(new Color(0.75f, 0.8f, 1f), 0f),
                            new GradientColorKey(new Color(0.5f, 0.45f, 1f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.15f),
                            new GradientAlphaKey(0.12f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, core ? 0.7f : 0.6f, 1f, core ? 1.4f : 2.6f));

            if (!core)
            {
                // Helical wind-up: orbital velocity around the beam axis plus
                // soft noise gives the sheath its twisting synchrotron look.
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.orbitalZ = new ParticleSystem.MinMaxCurve(1.6f);

                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = new ParticleSystem.MinMaxCurve(0.35f * rs);
                noise.frequency = 0.4f;
                noise.scrollSpeed = 0.35f;
                noise.damping = true;

                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = jetMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (core)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 7f;
            }
            return ps;
        }

        /// <summary>Editor helper: simulate a few seconds so screenshots show the beams.</summary>
        public void PrewarmEditor()
        {
            if (systems == null && active) Build();
            if (systems == null) return;
            foreach (var s in systems)
                if (s != null) { s.Simulate(3.5f, true, true); s.Play(); }
        }
    }
}

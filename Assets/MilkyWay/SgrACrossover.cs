using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlackHoleEffect; // Loc, BlackHoleUI, NarrationManager, CinematicOrbit

namespace MilkyWay
{
    /// <summary>
    /// The Sagittarius A* crossover (F9): the bridge between the two exhibits.
    /// A log dolly dives from the galaxy overview into the bulge core — three
    /// decades of scale, stars crowding, exposure riding down against the
    /// blinding centre — while the narration sets up the hidden four-million-
    /// solar-mass point. Visible light ends at the dust, the screen fades to
    /// black, and the visitor lands in the BLACK HOLE EXHIBIT itself
    /// (BlackHoleShowcase), where F9 leads back. Two scenes, one universe.
    ///
    /// Scene-transition discipline: the ride mutates controller exposure,
    /// which writes into the SHARED .mat assets — everything is restored to
    /// its saved values BEFORE LoadScene, or the drift outlives play mode
    /// (the StarfieldSkybox lesson).
    /// </summary>
    public class SgrACrossover : MonoBehaviour
    {
        public MilkyWayController controller;
        public CinematicOrbit orbit;

        [Tooltip("Seconds for the dive from overview to the bulge core.")]
        public float diveDuration = 20f;

        public bool IsPlaying { get; private set; }

        const float DStart = 34f, DEnd = 0.05f;
        const string TargetScene = "BlackHoleShowcase";

        Coroutine routine;
        Vector3 savedPos;
        Quaternion savedRot;
        float savedNear, savedFar;
        float savedBrightness, savedStarBrightness;
        Image fade;
        Text caption;
        RectTransform captionPanel;
        Button stopButton;

        // The S-star swarm: the innermost stars on tight, fast Kepler-ish
        // ellipses around the invisible point. Beat 1's whole story ("stars
        // whip around something unseen") happens deep in the dust where the
        // frame was otherwise a featureless brown wall — these ARE the shot.
        GameObject swarm;
        Transform[] sstars;
        float[] sA, sE, sTh, sK;
        Quaternion[] sPlane;
        Material sstarMat;
        Texture2D sstarTex;

        // Tightened 2026-07: the crossover's length was narration-bound (the
        // dive itself is 20 s) — same teaching beats, half the words.
        public static readonly string[] NarrationLines =
        {
            "이제 은하의 심장으로 들어갑니다. 중심에 가까울수록, 별들은 태양 주변보다 수백만 배나 빽빽합니다.",
            "천문학자들은 이 별들이 보이지 않는 한 점을 초속 수천 킬로미터로 도는 것을 지켜봤습니다. 그 점에, 태양 4백만 배의 질량이 숨어 있습니다.",
            "가시광은 먼지에 막혀 여기까지 — 전파 망원경들이 마침내 그 그림자를 찍었습니다. 궁수자리 A 스타. 이제, 그곳으로 갑니다.",
        };

        public static readonly string[] NarrationLinesEn =
        {
            "Now we dive into the heart of the galaxy — near the centre, stars crowd millions of times denser than around the Sun.",
            "Astronomers watched these stars whip around one unseen point at thousands of kilometres per second. Four million solar masses hide there.",
            "Dust stops visible light here — but radio telescopes finally photographed the shadow itself. Sagittarius A star. Now, let's go there.",
        };

        public static readonly string[] NarrationLinesJa =
        {
            "いよいよ銀河の心臓部へ。中心に近づくほど、星は太陽のまわりの数百万倍も密集しています。",
            "天文学者たちは、星々が見えない一点を秒速数千キロで回るのを見つめてきました。そこに、太陽の400万倍の質量が隠れています。",
            "可視光はここまで — 電波望遠鏡がついにその影を撮影しました。いて座Aスター。さあ、そこへ行きましょう。",
        };

        public static readonly string[] NarrationLinesZh =
        {
            "现在，我们潜入银河的心脏——越靠近中心，恒星密度是太阳周围的数百万倍。",
            "天文学家看着这些恒星以每秒数千公里绕着一个看不见的点疾驰。那里，藏着四百万倍太阳的质量。",
            "可见光止步于此——射电望远镜终于拍下了那道阴影。人马座A星。现在，我们就去那里。",
        };

        public void Begin()
        {
            if (!Application.isPlaying || IsPlaying || controller == null) return;
            routine = StartCoroutine(Run());
        }

        public void Abort()
        {
            if (!IsPlaying) return;
            if (routine != null) StopCoroutine(routine);
            NarrationManager.Instance.Stop();
            RestoreAll();
            IsPlaying = false;
        }

        void RestoreAll()
        {
            DestroySwarm();
            var cam = GetComponent<Camera>();
            if (cam != null) { cam.nearClipPlane = savedNear; cam.farClipPlane = savedFar; }
            controller.brightness = savedBrightness;
            controller.starBrightness = savedStarBrightness;
            controller.Apply();
            transform.position = savedPos;
            transform.rotation = savedRot;
            if (fade != null) fade.gameObject.SetActive(false);
            HideCaption();
            ShowStop(false);
            if (orbit != null) orbit.enabled = true;
        }

        // ---------------- the S-star swarm ----------------

        void SpawnSwarm()
        {
            if (swarm != null) return;
            swarm = new GameObject("S-Stars");
            const int n = 9;
            sstars = new Transform[n]; sA = new float[n]; sE = new float[n];
            sTh = new float[n]; sK = new float[n]; sPlane = new Quaternion[n];

            sstarTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 28f, dy = (y - 31.5f) / 28f;
                    float g = Mathf.Exp(-(dx * dx + dy * dy) * 3.2f);
                    sstarTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(g)));
                }
            sstarTex.Apply();
            sstarMat = new Material(Shader.Find("Sprites/Default"))
                { mainTexture = sstarTex, renderQueue = 3100 };

            var rng = new System.Random(41);
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "S" + i;
                go.transform.SetParent(swarm.transform, false);
                go.transform.localScale = Vector3.one * (0.010f + 0.008f * (float)rng.NextDouble());
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = sstarMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                sA[i] = 0.09f + 0.30f * (float)rng.NextDouble();
                sE[i] = 0.35f + 0.45f * (float)rng.NextDouble();
                sTh[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
                sK[i] = 0.9f + 0.5f * (float)rng.NextDouble();
                sPlane[i] = Quaternion.Euler(360f * (float)rng.NextDouble(),
                    360f * (float)rng.NextDouble(), 360f * (float)rng.NextDouble());
                sstars[i] = go.transform;
            }
            SetSwarmAlpha(0f);
        }

        void SetSwarmAlpha(float a)
        {
            if (sstarMat != null)
                sstarMat.color = new Color(1f, 0.93f, 0.78f, Mathf.Clamp01(a));
        }

        void UpdateSwarm(float dt)
        {
            if (swarm == null) return;
            for (int i = 0; i < sstars.Length; i++)
            {
                // Sweep rate ~ r^-1.5: the pericentre whip IS the physics.
                float r = sA[i] * (1f - sE[i] * sE[i]) / (1f + sE[i] * Mathf.Cos(sTh[i]));
                sTh[i] += dt * sK[i] * Mathf.Pow(Mathf.Max(r, 0.02f), -1.5f) * 0.06f;
                r = sA[i] * (1f - sE[i] * sE[i]) / (1f + sE[i] * Mathf.Cos(sTh[i]));
                sstars[i].position = sPlane[i] *
                    new Vector3(Mathf.Cos(sTh[i]) * r, 0f, Mathf.Sin(sTh[i]) * r);
                // Sprites/Default culls off, so any camera-ish facing works.
                sstars[i].rotation = Quaternion.LookRotation(sstars[i].position - transform.position);
            }
        }

        void DestroySwarm()
        {
            if (swarm != null) Destroy(swarm);
            if (sstarMat != null) Destroy(sstarMat);
            if (sstarTex != null) Destroy(sstarTex);
            swarm = null; sstarMat = null; sstarTex = null;
        }

        // A beat may only fire once its predecessor's voice has finished —
        // thresholds alone cut the longer (ja, ko) lines mid-sentence.
        float narrEnd;
        bool NarrationDone => Time.time >= narrEnd;
        float Narrate(int i)
        {
            float len = NarrationManager.Instance.Play("mw_sgr_" + i);
            narrEnd = Time.time + len + 0.4f;
            return len;
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            var cam = GetComponent<Camera>();
            savedPos = transform.position;
            savedRot = transform.rotation;
            savedNear = cam != null ? cam.nearClipPlane : 0.02f;
            savedFar = cam != null ? cam.farClipPlane : 600f;
            savedBrightness = controller.brightness;
            savedStarBrightness = controller.starBrightness;
            if (orbit != null) orbit.enabled = false;
            ShowStop(true);
            NarrationManager.Instance.Preload("mw_sgr_0", "mw_sgr_1", "mw_sgr_2");
            SpawnSwarm();

            // Dive along whatever azimuth the visitor is at — the centre is
            // the destination, the direction is theirs.
            Vector3 dir = transform.position.sqrMagnitude > 1e-4f
                ? transform.position.normalized
                : new Vector3(0f, 0.47f, -0.88f);

            // Stage 0: glide onto the departure ray.
            float len0 = Narrate(0);
            Caption(Loc.T(NarrationLines[0], NarrationLinesEn[0], NarrationLinesJa[0], NarrationLinesZh[0]));
            Vector3 fromPos = transform.position;
            Quaternion fromRot = transform.rotation;
            for (float t = 0f, dur = 2.0f; t < dur; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                transform.position = Vector3.Lerp(fromPos, dir * DStart, u);
                transform.rotation = Quaternion.Slerp(fromRot, Quaternion.LookRotation(-dir, Vector3.up), u);
                yield return null;
            }

            // The dive: three decades of log dolly into the bulge core, the
            // exposure riding DOWN so the crowding core stays readable
            // instead of blowing out.
            float lnA = Mathf.Log(DStart), lnB = Mathf.Log(DEnd);
            int stage = 0;
            for (float t = 0f; t < diveDuration; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / diveDuration);
                float d = Mathf.Exp(Mathf.Lerp(lnA, lnB, u));
                transform.position = dir * d;
                transform.LookAt(Vector3.zero);
                if (cam != null) cam.nearClipPlane = Mathf.Max(d * 0.002f, 0.0006f);

                // The old ride dimmed the core to 0.9/0.7 "so it stays
                // readable" — the actual result was an empty brown wall while
                // the narration said "millions of times denser". Keep the
                // exposure up; the crowding is the point.
                controller.brightness = Mathf.Lerp(savedBrightness, 1.05f, Mathf.SmoothStep(0f, 1f, u));
                controller.starBrightness = Mathf.Lerp(savedStarBrightness, 0.95f, u);
                controller.Apply();

                UpdateSwarm(Time.deltaTime);
                SetSwarmAlpha(Mathf.InverseLerp(0.55f, 0.75f, u));

                if (stage == 0 && u > 0.52f && NarrationDone)
                {
                    stage = 1; Narrate(1);
                    Caption(Loc.T(NarrationLines[1], NarrationLinesEn[1], NarrationLinesJa[1], NarrationLinesZh[1]));
                }
                yield return null;
            }

            // Final beat at the core: the dust wall, the radio image, the exit.
            while (!NarrationDone)
            {
                transform.RotateAround(Vector3.zero, Vector3.up, 1.2f * Time.deltaTime);
                UpdateSwarm(Time.deltaTime);
                yield return null;
            }
            float len2 = Narrate(2);
            Caption(Loc.T(NarrationLines[2], NarrationLinesEn[2], NarrationLinesJa[2], NarrationLinesZh[2]));
            for (float t = 0f, dur = Mathf.Max(6f, len2 - 1.5f); t < dur; t += Time.deltaTime)
            {
                transform.RotateAround(Vector3.zero, Vector3.up, 1.2f * Time.deltaTime);
                UpdateSwarm(Time.deltaTime);
                yield return null;
            }

            // Fade to black, put every shared asset back, and cross over.
            EnsureFade();
            fade.gameObject.SetActive(true);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 1.4f));
                UpdateSwarm(Time.deltaTime);
                yield return null;
            }
            fade.color = Color.black;
            DestroySwarm();

            controller.brightness = savedBrightness;
            controller.starBrightness = savedStarBrightness;
            controller.Apply();
            IsPlaying = false;
            SceneManager.LoadScene(TargetScene);
        }

        void Update()
        {
            if (!IsPlaying) return;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Abort();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) Abort();
#endif
        }

        // ---------------- UI (the shared factory) ---------------------------

        void EnsureFade()
        {
            if (fade != null) return;
            fade = BlackHoleUI.MakeFullViewOverlay(GetComponent<Camera>(), "SgrA Fade");
            fade.color = new Color(0f, 0f, 0f, 0f);
        }

        void ShowStop(bool on)
        {
            if (stopButton == null)
            {
                if (!on) return;
                stopButton = BlackHoleUI.MakeCinematicButton(GetComponent<Camera>(), "SgrA Stop", Abort);
            }
            stopButton.gameObject.SetActive(on);
            if (on)
                stopButton.GetComponentInChildren<Text>().text =
                    Loc.T("중단 ■", "Stop ■", "中止 ■", "停止 ■");
        }

        void Caption(string text)
        {
            if (caption == null)
            {
                var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
                captionPanel = BlackHoleUI.MakePanel(canvas.transform, "SgrA Caption",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(940f, 104f));
                caption = BlackHoleUI.MakeText(captionPanel, "Text", 19, BlackHoleUI.TextPrimary, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 92f));
                caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            captionPanel.gameObject.SetActive(true);
            caption.text = text;
        }

        void HideCaption()
        {
            if (captionPanel != null) captionPanel.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (IsPlaying) Abort();
        }
    }
}

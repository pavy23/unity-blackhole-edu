using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using System.IO;
#endif

namespace BlackHoleEffect
{
    /// <summary>
    /// Auto-plays a promo reel and records it with Unity Recorder:
    ///   immersive establishing shot (5 s) → binary merger (F4, full) →
    ///   first-person fall-in (F3, full) → Kerr spin → Einstein ring → restore.
    /// Trigger it from the "Tools/Black Hole/Record Demo Reel" menu with the
    /// BlackHoleShowcase scene open. Editor-only; the recorder path is stripped
    /// from player builds. Output: &lt;project&gt;/Recordings/BlackHoleReel_*.mp4
    /// (1920x1080 @ 60 fps, frame-rate capped so heavy frames still render clean).
    /// </summary>
    public class DemoReelDirector : MonoBehaviour
    {
        const string FlagKey = "BH_RecordDemoReel";

#if UNITY_EDITOR
        RecorderController recorder;

        [MenuItem("Tools/Black Hole/Record Demo Reel")]
        static void RecordReel()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[DemoReel] Already in play mode — exit play first, then run this.");
                return;
            }
            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (active.name != null && active.name.IndexOf("Showcase", System.StringComparison.OrdinalIgnoreCase) < 0)
                Debug.LogWarning("[DemoReel] Active scene is '" + active.name +
                    "', not the desktop showcase. If the reel aborts, open BlackHoleShowcase first.");
            EditorPrefs.SetBool(FlagKey, true);
            Debug.Log("[DemoReel] Entering play mode to record…");
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (!EditorPrefs.GetBool(FlagKey, false)) return;
            EditorPrefs.SetBool(FlagKey, false);
            var go = new GameObject("~DemoReelDirector");
            go.AddComponent<DemoReelDirector>();
        }
#endif

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // Wait for the runtime bootstrap to wire the controllers up.
            DesktopControls controls = null;
            BlackHoleController controller = null;
            FallInMode fallIn = null;
            BinaryMergerCinematic binary = null;
            EinsteinRingDemo einstein = null;
            for (float t = 0f; t < 12f; t += Time.deltaTime)
            {
                controls = FindFirstObjectByType<DesktopControls>();
                binary = FindFirstObjectByType<BinaryMergerCinematic>();
                if (controls != null)
                {
                    controller = controls.controller;
                    fallIn = controls.fallIn;
                    einstein = controls.einsteinDemo;
                }
                if (controls && controller && fallIn && binary) break;
                yield return null;
            }
            if (!controls || !controller || !fallIn || !binary)
            {
                Debug.LogError("[DemoReel] Required components not found — is BlackHoleShowcase the open scene? Aborting.");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
                yield break;
            }

#if UNITY_EDITOR
            StartRecorder();
#endif
            float savedSpin = controller.spin;

            // 1) Immersive establishing shot — the ambient orbit shows the disk.
            controls.SetImmersive(true);
            yield return Wait(5f);

            // 2) Binary merger (F4), played to completion.
            binary.Begin();
            yield return Until(() => binary.Running, 3f);
            yield return While(() => binary.Running, 90f);
            controls.SetImmersive(true);
            yield return Wait(1.5f);

            // 3) First-person fall-in (F3), played to completion.
            fallIn.Begin();
            yield return Until(() => fallIn.IsFalling, 3f);
            yield return While(() => fallIn.IsFalling, 90f);
            controls.SetImmersive(true);
            yield return Wait(1.5f);

            // 4) Kerr spin — ramp up, hold while the orbit shows the D-shadow.
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                controller.SetSpin(Mathf.Lerp(0f, 0.9f, t / 2.5f));
                yield return null;
            }
            controller.SetSpin(0.9f);
            yield return Wait(10f);

            // 5) Einstein ring — sweep the background source so the ring opens.
            einstein = einstein != null ? einstein : FindFirstObjectByType<EinsteinRingDemo>();
            if (einstein != null)
            {
                controller.SetSpin(0f);
                einstein.active = true;
                for (float t = 0f; t < 9f; t += Time.deltaTime)
                {
                    einstein.Nudge(9f * Time.deltaTime);
                    yield return null;
                }
                einstein.active = false;
            }

            // 6) Restore + clean tail.
            controller.SetSpin(savedSpin);
            controls.SetImmersive(false);
            yield return Wait(2f);

#if UNITY_EDITOR
            StopRecorder();
            Debug.Log("[DemoReel] Done. Recording saved under <project>/Recordings/. Exiting play mode.");
            EditorApplication.isPlaying = false;
#endif
        }

        static IEnumerator Wait(float s)
        {
            for (float t = 0f; t < s; t += Time.deltaTime) yield return null;
        }
        static IEnumerator Until(System.Func<bool> c, float timeout)
        {
            for (float t = 0f; !c() && t < timeout; t += Time.deltaTime) yield return null;
        }
        static IEnumerator While(System.Func<bool> c, float timeout)
        {
            for (float t = 0f; c() && t < timeout; t += Time.deltaTime) yield return null;
        }

#if UNITY_EDITOR
        void StartRecorder()
        {
            var cs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            cs.name = "BH Reel Controller Settings";

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "BH Reel";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movie.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            movie.CaptureAudio = false;

            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings");
            Directory.CreateDirectory(folder);
            // NOTE: build the wildcard path by concatenation, NOT Path.Combine —
            // Mono's Path.Combine rejects the '<' '>' in "<Take>" as illegal path
            // characters. Recorder substitutes the wildcard itself at write time.
            movie.OutputFile = folder.Replace("\\", "/") + "/BlackHoleReel_<Take>";

            cs.AddRecorderSettings(movie);
            cs.SetRecordModeToManual();
            cs.FrameRate = 60f;
            cs.CapFrameRate = true;

            recorder = new RecorderController(cs);
            recorder.PrepareRecording();
            recorder.StartRecording();
            Debug.Log("[DemoReel] Recorder started (1920x1080 @ 60 fps, capped, MP4) → " + folder);
        }

        void StopRecorder()
        {
            if (recorder != null && recorder.IsRecording())
                recorder.StopRecording();
        }

        void OnDisable()
        {
            // If play mode is aborted mid-reel, finalize the partial file.
            StopRecorder();
        }
#endif
    }
}

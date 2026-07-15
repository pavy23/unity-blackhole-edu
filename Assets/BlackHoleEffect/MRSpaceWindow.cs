using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Trades the room for open space while a cinematic runs, then hands it back.
    ///
    /// Why this has to exist: passthrough is composited by the headset, not
    /// rendered into our frame, so the shader cannot sample it. In MR the hole
    /// therefore has nothing to lens — escaped rays just turn transparent and the
    /// room shows through. That is fine while the accretion disk is what you are
    /// looking at, but the merger is gas-free (GW150914 had no disk), and a
    /// gas-free hole with nothing behind it is a plain black disc: no Einstein
    /// ring, no lensing, none of what makes the desktop version legible.
    ///
    /// So for the length of the merger the room fades out and a real starfield
    /// takes its place, the shader leaves MR mode, and the desktop cinematic —
    /// bare holes lensing stars — plays exactly as authored. Then the room fades
    /// back.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MRSpaceWindow : MonoBehaviour
    {
        public BlackHoleController controller;
        public Camera viewer;

        [Tooltip("Starfield material shown in place of the room. Same asset the " +
                 "desktop showcase uses as its skybox.")]
        public Material starfield;

        [Tooltip("The material's passthrough setting, reapplied in edit mode. The " +
                 "hole shader writes to the shared material asset, so a play " +
                 "session killed mid-cinematic would otherwise leave _MRMode=0 " +
                 "baked into the .mat and passthrough dead for good.")]
        public float passthroughMRMode = 1f;

        public bool IsOpen { get; private set; }

        static readonly int MRModeId = Shader.PropertyToID("_MRMode");

        CameraClearFlags savedClear;
        Color savedBackground;
        Material savedSkybox;
        bool savedPost;
        float savedMRMode;
        Image fade;

        void OnEnable()
        {
            // Self-heal: opening the scene, or leaving play mode, repairs whatever
            // an interrupted cinematic left behind.
            if (!Application.isPlaying) RestoreMaterial();
        }

        void OnDisable()
        {
            if (Application.isPlaying && IsOpen) CloseNow();
            RestoreMaterial();
        }

        void RestoreMaterial()
        {
            var mat = HoleMaterial();
            if (mat != null && mat.HasProperty(MRModeId)) mat.SetFloat(MRModeId, passthroughMRMode);
        }

        Material HoleMaterial()
        {
            if (controller == null) return null;
            var r = controller.GetComponent<Renderer>();
            return r != null ? r.sharedMaterial : null;
        }

        Camera Viewer => viewer != null ? viewer : Camera.main;

        public IEnumerator Open(float fadeSeconds)
        {
            if (IsOpen || !Application.isPlaying) yield break;
            yield return FadeTo(1f, fadeSeconds * 0.5f);
            ApplySpace();
            IsOpen = true;
            yield return FadeTo(0f, fadeSeconds * 0.5f);
        }

        public IEnumerator Close(float fadeSeconds)
        {
            if (!IsOpen || !Application.isPlaying) yield break;
            yield return FadeTo(1f, fadeSeconds * 0.5f);
            RestoreRoom();
            IsOpen = false;
            yield return FadeTo(0f, fadeSeconds * 0.5f);
        }

        /// <summary>Abort path: give the room back this frame, no fade.</summary>
        public void CloseNow()
        {
            if (!IsOpen) return;
            RestoreRoom();
            IsOpen = false;
            if (fade != null) fade.color = Color.clear;
        }

        void ApplySpace()
        {
            var cam = Viewer;
            if (cam != null)
            {
                savedClear = cam.clearFlags;
                savedBackground = cam.backgroundColor;
                cam.clearFlags = CameraClearFlags.Skybox;
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null)
                {
                    savedPost = data.renderPostProcessing;
                    // The compositor is no longer reading our alpha, so bloom is
                    // safe again — and the GW rings need it to glow.
                    data.renderPostProcessing = true;
                }
            }

            savedSkybox = RenderSettings.skybox;
            if (starfield != null) RenderSettings.skybox = starfield;

            var mat = HoleMaterial();
            if (mat != null && mat.HasProperty(MRModeId))
            {
                savedMRMode = mat.GetFloat(MRModeId);
                mat.SetFloat(MRModeId, 0f); // escaped rays sample the starfield again — lensing returns
            }
        }

        void RestoreRoom()
        {
            var cam = Viewer;
            if (cam != null)
            {
                cam.clearFlags = savedClear;
                cam.backgroundColor = savedBackground;
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) data.renderPostProcessing = savedPost;
            }
            RenderSettings.skybox = savedSkybox;

            var mat = HoleMaterial();
            if (mat != null && mat.HasProperty(MRModeId)) mat.SetFloat(MRModeId, savedMRMode);
        }

        IEnumerator FadeTo(float alpha, float seconds)
        {
            // Sorts above the merger flash: the curtain's job is to hide the swap
            // itself, including anything else drawing over the view.
            if (fade == null) fade = BlackHoleUI.MakeFullViewOverlay(Viewer, "MR Space Fade", 200);
            if (fade == null) yield break;
            var from = fade.color;
            var to = new Color(0f, 0f, 0f, alpha);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                fade.color = Color.Lerp(from, to, t / Mathf.Max(seconds, 0.0001f));
                yield return null;
            }
            fade.color = to;
        }

    }
}

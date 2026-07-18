using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlackHoleEffect.Editor
{
    /// <summary>
    /// Builds the exhibit's title screen: language + experience picker over a
    /// live galaxy backdrop (the Milky Way showcase's own volume and star
    /// field, with a slow cinematic orbit). Registered as build index 0 —
    /// this is the scene a visitor boots into.
    /// </summary>
    public static class TitleSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/TitleScreen.unity";

        [MenuItem("Tools/Cosmos/Create Title Scene")]
        public static void Build()
        {
            var volumeShader = Shader.Find("MilkyWay/GalaxyVolume");
            if (!volumeShader)
            {
                Debug.LogError("Galaxy shaders not compiled yet.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "TitleScreen";

            RenderSettings.skybox =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/MilkyWay/Materials/DeepSpaceSkybox.mat");
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false;

            // --- backdrop: the galaxy itself, reusing the showcase materials
            // (values match the showcase defaults, so the shared assets stay
            // exactly as that scene expects them).
            var volumeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MilkyWay/Materials/GalaxyVolume.mat");
            var starMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MilkyWay/Materials/GalaxyStars.mat");

            var root = new GameObject("Milky Way (backdrop)");

            var volumeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volumeGO.name = "Galaxy Volume";
            volumeGO.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(volumeGO.GetComponent<Collider>());
            var volumeRenderer = volumeGO.GetComponent<MeshRenderer>();
            volumeRenderer.sharedMaterial = volumeMat;
            volumeRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var starsGO = new GameObject("Galaxy Stars");
            starsGO.transform.SetParent(root.transform, false);
            starsGO.AddComponent<MilkyWay.GalaxyStarField>().material = starMat;

            var controller = root.AddComponent<MilkyWay.MilkyWayController>();
            controller.volumeMaterial = volumeMat;
            controller.starMaterial = starMat;
            controller.volumeRenderer = volumeRenderer;
            controller.Apply();

            // --- camera: the classic three-quarter view, drifting.
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGO.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 17f, -32f);
            cam.transform.LookAt(Vector3.zero);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            camGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGO.AddComponent<AudioListener>();

            var orbit = camGO.AddComponent<CinematicOrbit>();
            orbit.target = root.transform;
            orbit.orbitSpeed = 0.35f;
            orbit.bobAmplitude = 0.5f;
            orbit.bobPeriod = 46f;

            camGO.AddComponent<MilkyWay.MilkyWayAudio>();
            camGO.AddComponent<TitleScreen>();

            // --- post: the galaxy showcase's profile, shared.
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/MilkyWay/Settings/MilkyWayVolume.asset");
            if (profile != null)
            {
                var post = new GameObject("Post Processing").AddComponent<Volume>();
                post.isGlobal = true;
                post.priority = 10;
                post.sharedProfile = profile;
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build index 0: the title screen is the boot scene.
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes.Where(s => s.path != ScenePath));
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("Title scene created at build index 0: " + ScenePath);
        }
    }
}

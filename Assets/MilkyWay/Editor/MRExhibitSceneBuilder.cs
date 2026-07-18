using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace MilkyWay.Editor
{
    /// <summary>
    /// Builds the MR (passthrough) editions of the galaxy and solar-system
    /// exhibits: the Milky Way as a ~1.1 m grabbable miniature, the solar
    /// system as a room-scale orrery. Same menu-driven pattern as every other
    /// scene here — the scenes are build artifacts, never hand-edited. Both
    /// are registered in the build settings so the MR menus can hop between
    /// the three exhibits by scene name.
    /// </summary>
    public static class MRExhibitSceneBuilder
    {
        const string Root = "Assets/MilkyWay";

        // Visible disk radius is 16 kpc (object units); 0.034 puts the disk
        // at ~1.09 m across, floating at chest height within arm's reach.
        const float GalaxyScale = 0.034f;

        [MenuItem("Tools/Milky Way/Create MR Scene (Passthrough)")]
        public static void BuildMilkyWayMR()
        {
            var volumeShader = Shader.Find("MilkyWay/GalaxyVolume");
            var starShader = Shader.Find("MilkyWay/GalaxyStars");
            if (!volumeShader || !starShader)
            {
                Debug.LogError("Milky Way shaders not compiled yet.");
                return;
            }

            var scene = NewMRScene("MilkyWayMR");
            var cam = BuildXRRig();

            // MR-specific materials: the desktop .mats stay untouched, so MR
            // tuning (and any values a killed play session bakes in) never
            // leaks into the desktop showcase.
            var volumeMat = SaveMaterial("GalaxyVolumeMR", volumeShader);
            var starMat = SaveMaterial("GalaxyStarsMR", starShader);
            // The star shader fades stars within _NearFade WORLD units of the
            // camera (a fly-through guard). At miniature scale that would be
            // a quarter of the galaxy — shrink it with the exhibit.
            starMat.SetFloat("_NearFade", 0.08f * GalaxyScale);
            // At 1/30 scale most stars land under the shader's 1.5-pixel
            // floor and get energy-conserved into dimness; larger points keep
            // the starfield alive at arm's length.
            starMat.SetFloat("_SizeScale", 2.4f);

            var root = new GameObject("Milky Way (MR)");
            // Below eye level and tipped toward the viewer: at eye height the
            // disk is edge-on — a bright bar, no spiral. Tilting the plane
            // shows the face the way a table hologram would.
            root.transform.position = new Vector3(0f, 1.05f, 1.55f);
            root.transform.rotation = Quaternion.Euler(-28f, 0f, 0f);
            root.transform.localScale = Vector3.one * GalaxyScale;

            var volumeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volumeGO.name = "Galaxy Volume";
            volumeGO.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(volumeGO.GetComponent<Collider>());
            var volumeRenderer = volumeGO.GetComponent<MeshRenderer>();
            volumeRenderer.sharedMaterial = volumeMat;
            volumeRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var starsGO = new GameObject("Galaxy Stars");
            starsGO.transform.SetParent(root.transform, false);
            var starField = starsGO.AddComponent<GalaxyStarField>();
            starField.material = starMat;

            var controller = root.AddComponent<MilkyWayController>();
            controller.volumeMaterial = volumeMat;
            controller.starMaterial = starMat;
            controller.volumeRenderer = volumeRenderer;
            controller.Apply(); // AddComponent already ran OnEnable before wiring — the BH lesson

            // Hand grab: move with one hand, scale with two.
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 17f; // local kpc → ~0.58 m grab bubble
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackRotation = true;
            grab.trackScale = true;
            var transformer = root.AddComponent<XRGeneralGrabTransformer>();
            transformer.allowTwoHandedScaling = true;

            var labelsGO = new GameObject("MR Labels");
            var labels = labelsGO.AddComponent<MRBodyLabels>();

            var stage = root.AddComponent<MilkyWayMRStage>();
            stage.controller = controller;
            stage.labels = labels;

            var audio = cam.gameObject.AddComponent<MilkyWayAudio>();

            var tour = cam.gameObject.AddComponent<MilkyWayMRTour>();
            tour.controller = controller;
            tour.stage = stage;

            var controls = cam.gameObject.AddComponent<MilkyWayMRControls>();
            controls.stage = stage;
            controls.tour = tour;
            tour.controls = controls;

            SaveAndRegister(scene, Root + "/Scenes/MilkyWayMR.unity");
            Selection.activeGameObject = root;
        }

        [MenuItem("Tools/Solar System/Create MR Scene (Passthrough)")]
        public static void BuildSolarSystemMR()
        {
            if (!Shader.Find("MilkyWay/PlanetSurface") || !Shader.Find("MilkyWay/OrbitLine"))
            {
                Debug.LogError("Solar system shaders not compiled yet.");
                return;
            }

            var scene = NewMRScene("SolarSystemMR");
            var cam = BuildXRRig();

            // The rig spawns at runtime (SolarSystemStage pattern) under this
            // anchor; grabbing the anchor moves the whole orrery, and the rig
            // re-bakes _SunPos + line widths when its root moves or rescales.
            var anchor = new GameObject("Solar System Anchor (MR)");
            anchor.transform.position = new Vector3(0f, 1.1f, 1.5f);

            var sphere = anchor.AddComponent<SphereCollider>();
            sphere.radius = 0.16f; // grab bubble around the sun
            var rb = anchor.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            var grab = anchor.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackRotation = true;
            grab.trackScale = true;
            var transformer = anchor.AddComponent<XRGeneralGrabTransformer>();
            transformer.allowTwoHandedScaling = true;

            var labelsGO = new GameObject("MR Labels");
            var labels = labelsGO.AddComponent<MRBodyLabels>();
            // 0.014 shouted: the four inner planets orbit within ~25 cm of the
            // sun and their tags collided into a jumble. Smaller text plus a
            // tighter offset keeps the family legible from a step back.
            labels.baseCharSize = 0.0095f;

            var stage = anchor.AddComponent<SolarSystemMRStage>();
            stage.labels = labels;

            var audio = cam.gameObject.AddComponent<MilkyWayAudio>();

            var tour = cam.gameObject.AddComponent<SolarSystemMRTour>();
            tour.stage = stage;

            var controls = cam.gameObject.AddComponent<SolarSystemMRControls>();
            controls.stage = stage;
            controls.tour = tour;
            tour.controls = controls;

            SaveAndRegister(scene, Root + "/Scenes/SolarSystemMR.unity");
            Selection.activeGameObject = anchor;
        }

        // ------------------------------------------------------------------
        //  shared scaffolding (the BlackHoleMR recipe)
        // ------------------------------------------------------------------

        static UnityEngine.SceneManagement.Scene NewMRScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
            RenderSettings.fog = false;
            return scene;
        }

        static Camera BuildXRRig()
        {
            Camera cam;
            var rigPrefab = FindHandsRigPrefab();
            if (rigPrefab != null)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.transform.position = Vector3.zero;
                cam = rig.GetComponentInChildren<Camera>();
            }
            else
            {
                Debug.LogWarning("Hands rig prefab not found; creating plain camera. Import XRI 'Hands Interaction Demo' sample for full interaction.");
                var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGO.AddComponent<Camera>();
            }

            // Passthrough: clear to transparent black, no post (post can stomp
            // the alpha channel the compositor uses for passthrough).
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;

            if (Object.FindFirstObjectByType<ARSession>() == null)
                new GameObject("AR Session", typeof(ARSession));
            if (cam.GetComponent<ARCameraManager>() == null)
                cam.gameObject.AddComponent<ARCameraManager>();

            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            if (cam.GetComponent<AudioSource>() == null) cam.gameObject.AddComponent<AudioSource>();
            return cam;
        }

        /// <summary>Finds the XRI hands rig whatever sample version is
        /// imported — a hardcoded path silently degrades to a bare camera
        /// after any package upgrade.</summary>
        static GameObject FindHandsRigPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("\"XR Origin Hands\" t:GameObject"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Samples/XR Interaction Toolkit/") && path.EndsWith(".prefab"))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        static void SaveAndRegister(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Any(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log("MR exhibit scene created: " + path);
        }

        static Material SaveMaterial(string name, Shader shader)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) { existing.shader = shader; return existing; }
            var mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}

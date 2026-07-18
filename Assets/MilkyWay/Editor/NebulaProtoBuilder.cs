using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MilkyWay.Editor
{
    /// <summary>
    /// Phase-0 prototype scene for the nebulae &amp; clusters exhibit: a single
    /// volumetric nebula (cycling emission / reflection / planetary) beside a
    /// globular star cluster, on a slow orbit. Purely to judge the look and the
    /// raymarch cost before committing to the full exhibit — not shipped.
    /// </summary>
    public static class NebulaProtoBuilder
    {
        const string Root = "Assets/MilkyWay";

        [MenuItem("Tools/Nebula/Create Prototype Scene")]
        public static void Build()
        {
            var nebShader = Shader.Find("MilkyWay/NebulaVolume");
            var starShader = Shader.Find("MilkyWay/GalaxyStars");
            if (!nebShader || !starShader) { Debug.LogError("Nebula/star shaders not compiled yet."); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "NebulaProto";

            var skyShader = Shader.Find("MilkyWay/DeepSpaceSkybox");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_StarDensity", 0.3f);
                sky.SetFloat("_GalaxyCount", 0f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false;

            // --- nebula ---------------------------------------------------------
            var nebMat = SaveMaterial("NebulaVolumeProto", nebShader);
            var nebGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nebGO.name = "Nebula";
            Object.DestroyImmediate(nebGO.GetComponent<Collider>());
            var nr = nebGO.GetComponent<MeshRenderer>();
            nr.sharedMaterial = nebMat;
            nr.shadowCastingMode = ShadowCastingMode.Off;
            nebGO.transform.position = new Vector3(-9f, 0f, 0f);

            // --- globular cluster ----------------------------------------------
            var starMat = SaveMaterial("ClusterStarsProto", starShader);
            starMat.SetFloat("_StarBrightness", 1.4f);
            starMat.SetFloat("_SizeScale", 1.6f);
            starMat.SetFloat("_NearFade", 0.05f);
            var cluGO = new GameObject("Globular Cluster",
                typeof(MeshFilter), typeof(MeshRenderer), typeof(ClusterField));
            cluGO.transform.position = new Vector3(11f, 0f, 0f);
            var clu = cluGO.GetComponent<ClusterField>();
            clu.kind = ClusterField.Kind.Globular;
            clu.material = starMat;
            clu.radius = 6f;
            clu.starCount = 4000;

            // --- camera ---------------------------------------------------------
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGO.AddComponent<Camera>();
            cam.transform.position = new Vector3(-9f, 6f, -24f);
            cam.transform.LookAt(nebGO.transform.position);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            camGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGO.AddComponent<AudioListener>();

            var orbit = camGO.AddComponent<BlackHoleEffect.CinematicOrbit>();
            orbit.target = nebGO.transform;
            orbit.orbitSpeed = 0.5f;
            orbit.bobAmplitude = 0.4f;
            orbit.bobPeriod = 40f;

            var proto = camGO.AddComponent<NebulaProto>();
            proto.nebulaMaterial = nebMat;

            EnsureFolder(Root, "Scenes");
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/NebulaProto.unity");
            Selection.activeGameObject = nebGO;
            Debug.Log("Nebula prototype scene created: " + Root + "/Scenes/NebulaProto.unity");
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

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}

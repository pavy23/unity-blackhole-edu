using UnityEditor;
using UnityEngine;

namespace MilkyWay
{
    /// <summary>
    /// WebGL build for the hosted site. Unlike Build Profiles (which share the
    /// global scene list), this builds the five desktop scenes only: the MR
    /// scenes are inert in a browser but would drag the XR sample assets
    /// (hand recordings, demo textures, ~14 MB) into the data file.
    /// Also runnable headless:
    ///   Unity -batchmode -quit -executeMethod MilkyWay.WebGLSiteBuild.Build
    /// </summary>
    public static class WebGLSiteBuild
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/TitleScreen.unity",
            "Assets/BlackHoleEffect/Scenes/BlackHoleShowcase.unity",
            "Assets/MilkyWay/Scenes/MilkyWayShowcase.unity",
            "Assets/MilkyWay/Scenes/SolarSystemShowcase.unity",
            "Assets/MilkyWay/Scenes/NebulaShowcase.unity",
        };

        [MenuItem("Tools/Cosmos/Build WebGL (Site, desktop scenes)")]
        public static void Build()
        {
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                target = BuildTarget.WebGL,
                locationPathName = "Builds/WebGL",
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[WebGLSiteBuild] {s.result} — {s.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalTime.TotalMinutes:F1} min");
        }

        /// <summary>The full exhibit (all scenes, MR included — inert without
        /// a headset) for the Windows player. Same slimmed content as the web
        /// build: narration at Vorbis 35%, no splash, no Sentis.</summary>
        [MenuItem("Tools/Cosmos/Build Windows (full exhibit)")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, sc => sc.path),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/Windows/CosmosEdu.exe",
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[WindowsBuild] {s.result} — {s.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalTime.TotalMinutes:F1} min");
        }
    }
}

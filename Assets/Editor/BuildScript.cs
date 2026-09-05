using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>
    /// Headless player build for CI / sanity checks.
    ///   Unity -batchmode -quit -projectPath . -executeMethod JellyRush.EditorTools.BuildScript.BuildMac
    /// (Android/iOS builds need the matching platform module installed.)
    /// </summary>
    public static class BuildScript
    {
        static readonly string[] Scenes = { "Assets/Scenes/Prototype.unity" };

        public static void BuildMac()
        {
            Run(BuildTarget.StandaloneOSX, "Build/mac/JellyRush.app");
        }

        public static void BuildAndroid()
        {
            Run(BuildTarget.Android, "Build/android/JellyRush.apk");
        }

        static void Run(BuildTarget target, string output)
        {
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"[BuildScript] {target} result={summary.result} " +
                      $"errors={summary.totalErrors} size={summary.totalSize} time={summary.totalTime}");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}

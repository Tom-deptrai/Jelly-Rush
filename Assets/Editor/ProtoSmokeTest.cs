using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>
    /// Headless acceptance harness (round 6): opens the prototype scene, enters Play
    /// Mode with deterministic 60 fps timing, turns the Auto Test bot ON, and runs
    /// until the bot reaches GameState.Completed (PASS) or Fails / times out.
    ///
    ///   Unity -batchmode -projectPath . -executeMethod JellyRush.EditorTools.ProtoSmokeTest.Run
    ///
    /// Exit 0 = a full level-01 run Start -> Finish -> Completed.
    /// Exit 1 = Failed / red error.  Exit 2 = timed out.
    /// </summary>
    public static class ProtoSmokeTest
    {
        static readonly List<string> _errors = new();
        static string _shotPath;
        static bool _done, _botOn, _captured;
        static float _nextLog;

        const float TimeoutSeconds = 130f;

        public static void Run()
        {
            _shotPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), "prototype_playmode.png");

            Application.logMessageReceived += OnLog;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            // Deterministic 60 fps: every frame advances exactly 1/60 s regardless of
            // how fast the batch actually runs.
            Time.captureDeltaTime = 1f / 60f;

            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
            EditorApplication.update += Poll;
            EditorApplication.EnterPlaymode();
        }

        static readonly string[] IgnoreStack = { "QuickSearch", "SearchDatabase", "SearchInit", "SearchService" };

        static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string s = stack ?? "";
            foreach (var f in IgnoreStack) if (s.Contains(f)) return;
            _errors.Add($"{type}: {condition}\n{stack}");
        }

        static void Poll()
        {
            if (_done || !EditorApplication.isPlaying) return;

            var gm = JellyRush.Core.GameManager.Instance;
            if (gm == null) return;
            var bot = Object.FindAnyObjectByType<JellyRush.Debugging.AutoPlayBot>();

            float gt = Time.timeSinceLevelLoad;

            if (!_botOn && gt > 0.3f && bot != null) { bot.Active = true; _botOn = true; }
            if (!_botOn && gt > 0.3f && bot == null) _errors.Add("AutoPlayBot not found");

            if (!_captured && gt > 2.5f) { Capture(); _captured = true; }

            if (gt >= _nextLog)
            {
                _nextLog += 5f;
                Debug.Log($"[ProtoSmokeTest] t={gt:F0}s state={gm.State} dist={gm.DistanceMeters:F0} " +
                          $"coins={gm.Coins} route={(bot != null ? bot.RouteIndex : -1)}/" +
                          $"{(bot != null ? bot.RouteLength : -1)}");
            }

            if (gm.State == JellyRush.Core.GameState.Completed)
                Finish(0, $"PASS - level complete in {gt:F1}s game time, coins={gm.Coins}", bot, gm);
            else if (gm.State == JellyRush.Core.GameState.Failed)
                Finish(1, "FAIL - bot could not complete the level", bot, gm);
            else if (gt >= TimeoutSeconds)
                Finish(2, $"TIMEOUT at {gt:F0}s game time", bot, gm);
        }

        static void Finish(int code, string msg, JellyRush.Debugging.AutoPlayBot bot, JellyRush.Core.GameManager gm)
        {
            _done = true;
            EditorApplication.update -= Poll;
            Time.captureDeltaTime = 0f;

            if (_errors.Count > 0 && code == 0) code = 1;

            Capture();
            Debug.Log($"[ProtoSmokeTest] {msg}");
            Debug.Log($"[ProtoSmokeTest] errors={_errors.Count} " +
                      $"route={(bot != null ? bot.RouteIndex : -1)}/{(bot != null ? bot.RouteLength : -1)} " +
                      $"dist={(gm != null ? gm.DistanceMeters : 0f):F0} coins={(gm != null ? gm.Coins : 0)} exit={code}");
            foreach (var e in _errors) Debug.LogWarning("[ProtoSmokeTest] " + e);

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }

        static void Capture()
        {
            var cam = Camera.main;
            if (cam == null) return;
            int w = 810, h = 1440;
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(_shotPath, tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.active = null;
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>
    /// Headless acceptance harness (round 6): opens the prototype scene, enters Play
    /// Mode with deterministic 60 fps stepping, turns the Auto Test bot ON, and runs
    /// until the bot either reaches GameState.Completed (PASS) or Fails / times out.
    ///
    ///   Unity -batchmode -projectPath . -executeMethod JellyRush.EditorTools.ProtoSmokeTest.Run
    ///
    /// Exit 0 = a full level-01 run Start -> Finish -> Completed.
    /// Exit 1 = Failed / red error.  Exit 2 = timed out.
    /// </summary>
    public static class ProtoSmokeTest
    {
        static int _frames;
        static readonly List<string> _errors = new();
        static string _shotPath;
        static bool _done;

        const int TimeoutFrames = 90 * 60;   // 90 s of level time

        public static void Run()
        {
            _shotPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), "prototype_playmode.png");

            Application.logMessageReceived += OnLog;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
            EditorApplication.isPaused = true;
        }

        static readonly string[] IgnoreStack = { "QuickSearch", "SearchDatabase", "SearchInit", "SearchService" };

        static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string s = stack ?? "";
            foreach (var f in IgnoreStack) if (s.Contains(f)) return;
            _errors.Add($"{type}: {condition}\n{stack}");
        }

        static bool _inTick;

        static void Tick()
        {
            if (_inTick || _done || !EditorApplication.isPlaying) return;
            _inTick = true;
            try { TickBody(); } finally { _inTick = false; }
        }

        static void TickBody()
        {
            Time.captureDeltaTime = 1f / 60f;
            if (!EditorApplication.isPaused) EditorApplication.isPaused = true;
            _frames++;

            if (_frames == 8) EnableBot();

            EditorApplication.Step();

            var gm = JellyRush.Core.GameManager.Instance;
            var bot = Object.FindAnyObjectByType<JellyRush.Debugging.AutoPlayBot>();

            if (_frames % 600 == 0 && gm != null)
                Debug.Log($"[ProtoSmokeTest] t={_frames / 60f:F0}s state={gm.State} " +
                          $"dist={gm.DistanceMeters:F0} coins={gm.Coins} " +
                          $"route={(bot != null ? bot.RouteIndex : -1)}/{(bot != null ? bot.RouteLength : -1)}");

            if (_frames == 120) Capture();

            if (gm != null && gm.State == JellyRush.Core.GameState.Completed)
                Finish(0, $"PASS - level complete in {_frames / 60f:F1}s, coins={gm.Coins}", bot, gm);
            else if (gm != null && gm.State == JellyRush.Core.GameState.Failed)
                Finish(1, "FAIL - bot could not complete the level", bot, gm);
            else if (_frames >= TimeoutFrames)
                Finish(2, $"TIMEOUT after {_frames / 60f:F0}s", bot, gm);
        }

        static void Finish(int code, string msg, JellyRush.Debugging.AutoPlayBot bot, JellyRush.Core.GameManager gm)
        {
            _done = true;
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = 0f;

            if (_errors.Count > 0 && code == 0) code = 1;

            Capture();
            Debug.Log($"[ProtoSmokeTest] {msg}");
            Debug.Log($"[ProtoSmokeTest] errors={_errors.Count} " +
                      $"route={(bot != null ? bot.RouteIndex : -1)}/{(bot != null ? bot.RouteLength : -1)} " +
                      $"dist={(gm != null ? gm.DistanceMeters : 0f):F0} exit={code}");
            foreach (var e in _errors) Debug.LogWarning("[ProtoSmokeTest] " + e);

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }

        static void EnableBot()
        {
            var bot = Object.FindAnyObjectByType<JellyRush.Debugging.AutoPlayBot>();
            if (bot == null) { _errors.Add("AutoPlayBot not found in scene"); return; }
            bot.Active = true;
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

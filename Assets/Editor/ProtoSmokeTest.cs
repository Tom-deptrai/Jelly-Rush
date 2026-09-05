using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>
    /// Batch-mode smoke test: opens the prototype scene, enters Play Mode for a few
    /// seconds, injects a couple of synthetic jumps, screenshots the Game view and
    /// exits non-zero if anything logged an error/exception.
    /// Run:  Unity -batchmode -projectPath . -executeMethod JellyRush.EditorTools.ProtoSmokeTest.Run
    /// </summary>
    public static class ProtoSmokeTest
    {
        static int _frames;
        static readonly List<string> _errors = new();
        static string _shotPath;

        static float _prevY;
        static float _maxYStep;         // biggest single-frame Y move (teleport detector)
        static float _maxY;
        static int _beatsSeen;

        public static void Run()
        {
            _shotPath = System.IO.Path.Combine(Directory(), "prototype_playmode.png");

            Application.logMessageReceived += OnLog;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
            // Advance the player loop one frame per editor tick so headless timing
            // is deterministic (batch mode otherwise runs Update only sporadically).
            EditorApplication.isPaused = true;
        }

        static string Directory()
        {
            var p = Application.dataPath;              // <proj>/Assets
            return System.IO.Path.GetDirectoryName(p);
        }

        static readonly string[] IgnoreStackContains =
        {
            "QuickSearch", "SearchDatabase", "SearchInit", "SearchService"
        };

        static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            string s = stack ?? string.Empty;
            foreach (var frag in IgnoreStackContains)
                if (s.Contains(frag)) return;   // editor-internal noise, not our game

            _errors.Add($"{type}: {condition}\n{stack}");
        }

        static bool _inTick;

        static void Tick()
        {
            if (_inTick) return;                 // Step() can re-pump editor updates
            if (!EditorApplication.isPlaying) return;
            _inTick = true;
            try { TickBody(); } finally { _inTick = false; }
        }

        static void TickBody()
        {

            // Deterministic 60 fps stepping so headless play matches on-device timing.
            Time.captureDeltaTime = 1f / 60f;
            if (!EditorApplication.isPaused) EditorApplication.isPaused = true;

            _frames++;

            // one normal tap (settles), then a fast tap burst (~20/s) with swipes
            // fired mid-burst while airborne, then a second fast burst.
            if (_frames == 40) SendGesture(JellyRush.InputSystem.GestureType.Tap);
            if (_frames >= 90 && _frames <= 150 && _frames % 3 == 0)
                SendGesture(JellyRush.InputSystem.GestureType.Tap);
            if (_frames == 108) SendGesture(JellyRush.InputSystem.GestureType.SwipeLeft);   // airborne
            if (_frames == 126) SendGesture(JellyRush.InputSystem.GestureType.SwipeRight);  // airborne
            if (_frames >= 200 && _frames <= 250 && _frames % 3 == 0)
                SendGesture(JellyRush.InputSystem.GestureType.Tap);

            // advance exactly one engine frame with the input we just injected
            EditorApplication.Step();

            TrackPlayer();

            if (_frames == 60)
                CaptureCameraToFile();

            if (_frames >= 340)
            {
                EditorApplication.update -= Tick;
                Time.captureDeltaTime = 0f;

                // Only hard-fail on red errors / exceptions and on a Y teleport.
                float launchStep = 16.5f / 60f;  // ~ v0 * dt, the largest legit per-frame move
                if (_maxYStep > launchStep * 2.2f)
                    _errors.Add($"Y teleport suspected: max single-frame Y move = {_maxYStep:F3}");

                var st = JellyRush.Core.GameManager.Instance != null
                    ? JellyRush.Core.GameManager.Instance.State.ToString() : "?";
                bool ok = _errors.Count == 0;
                Debug.Log($"[ProtoSmokeTest] frames={_frames} errors={_errors.Count} ok={ok} " +
                          $"state={st} maxY={_maxY:F2} maxYStep={_maxYStep:F3} bestCombo={_beatsSeen}");
                foreach (var e in _errors) Debug.LogWarning("[ProtoSmokeTest] " + e);
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        static void TrackPlayer()
        {
            var pc = Object.FindAnyObjectByType<JellyRush.Player.PlayerController>();
            if (pc == null) return;

            float y = pc.transform.position.y;
            _maxY = Mathf.Max(_maxY, y);
            if (_frames == 55 || _frames == 120 || _frames == 240)
                Debug.Log($"[ProtoSmokeTest] f{_frames} y={y:F3} airborne={pc.IsAirborne}");
            if (_frames > 3 && Time.deltaTime <= 0.05f)
                _maxYStep = Mathf.Max(_maxYStep, Mathf.Abs(y - _prevY));
            _prevY = y;

            if (JellyRush.Core.GameManager.Instance != null)
                _beatsSeen = Mathf.Max(_beatsSeen, JellyRush.Core.GameManager.Instance.BestCombo);
        }

        static void CaptureCameraToFile()
        {
            var cam = Camera.main;
            if (cam == null) { _errors.Add("Camera.main missing for capture"); return; }
            int w = 810, h = 1440; // portrait 9:16
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
            Debug.Log("[ProtoSmokeTest] wrote " + _shotPath);
        }

        static void SendGesture(JellyRush.InputSystem.GestureType g)
        {
            var input = Object.FindAnyObjectByType<JellyRush.InputSystem.SwipeTapInput>();
            if (input == null) { _errors.Add("SwipeTapInput not found in scene"); return; }
            input.Simulate(g);
        }
    }
}

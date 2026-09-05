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

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            _frames++;

            if (_frames == 60) SendGesture(JellyRush.InputSystem.GestureType.Tap);
            if (_frames == 95) SendGesture(JellyRush.InputSystem.GestureType.SwipeLeft);
            if (_frames == 130) SendGesture(JellyRush.InputSystem.GestureType.SwipeRight);
            if (_frames == 160) SendGesture(JellyRush.InputSystem.GestureType.Tap);

            if (_frames == 200)
                CaptureCameraToFile();

            if (_frames >= 260)
            {
                EditorApplication.update -= Tick;
                bool ok = _errors.Count == 0;
                Debug.Log($"[ProtoSmokeTest] frames={_frames} errors={_errors.Count} ok={ok}");
                foreach (var e in _errors) Debug.LogWarning("[ProtoSmokeTest] " + e);
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(ok ? 0 : 1);
            }
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

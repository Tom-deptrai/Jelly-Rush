using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>Game-camera visual acceptance measurement and deterministic capture.</summary>
    public static class CharacterVisualAudit
    {
        [MenuItem("JellyRush/Start Full Auto Test", true)]
        static bool CanStartAutoTest() => EditorApplication.isPlaying;

        [MenuItem("JellyRush/Start Full Auto Test")]
        public static void StartAutoTest()
        {
            var bot = Object.FindAnyObjectByType<JellyRush.Debugging.AutoPlayBot>();
            Require(bot != null, "AutoPlayBot not found");
            bot.Active = true;
            Debug.Log("[CharacterVisualAudit] Full Auto Test started from editor menu.");
        }

        [MenuItem("JellyRush/Capture Character Visual Audit", true)]
        static bool CanCapture() => EditorApplication.isPlaying && Camera.main != null;

        [MenuItem("JellyRush/Capture Character Visual Audit")]
        public static void Capture()
        {
            var cam = Camera.main;
            var player = GameObject.Find("PlayerUnit");
            Require(cam != null && player != null, "Play Mode PlayerUnit/Main Camera not found");

            var carrier = FindDeep(player.transform, "CarrierRoot");
            var jelly = FindDeep(player.transform, "JellyRoot");
            Require(carrier != null && jelly != null, "CarrierRoot/JellyRoot not found");

            Bounds cb = BoundsOf(carrier, jelly);
            Bounds jb = BoundsOf(jelly);
            Bounds duo = cb;
            duo.Encapsulate(jb);
            float viewportHeight = ViewportHeight(cam, duo);

            string root = Directory.GetParent(Application.dataPath).FullName;
            string png = Path.Combine(root, "character_visual_audit.png");
            string txt = Path.Combine(root, "character_visual_audit.txt");
            Render(cam, png, 810, 1440);

            string report =
                $"Carrier bounds size: {cb.size}\n" +
                $"Jelly bounds size: {jb.size}\n" +
                $"Carrier:Jelly height ratio: {cb.size.y / Mathf.Max(jb.size.y, 0.0001f):F3}\n" +
                $"Combined bounds size: {duo.size}\n" +
                $"Combined viewport height: {viewportHeight * 100f:F2}%\n" +
                $"Camera FOV: {cam.fieldOfView:F2}\n" +
                $"Player collider: radius={player.GetComponent<SphereCollider>().radius:F2}, center={player.GetComponent<SphereCollider>().center}\n";
            File.WriteAllText(txt, report);
            Debug.Log("[CharacterVisualAudit]\n" + report + "Capture: " + png);
        }

        static float ViewportHeight(Camera cam, Bounds b)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (var p in Corners(b))
            {
                float y = cam.WorldToViewportPoint(p).y;
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
            }
            return max - min;
        }

        static Vector3[] Corners(Bounds b)
        {
            var p = new Vector3[8];
            int i = 0;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                p[i++] = b.center + Vector3.Scale(b.extents, new Vector3(x, y, z));
            return p;
        }

        static Bounds BoundsOf(Transform root, Transform exclude = null)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => exclude == null || !r.transform.IsChildOf(exclude))
                .ToArray();
            Require(renderers.Length > 0, root.name + " has no renderers");
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }

        static void Render(Camera cam, string path, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(rt);
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void Require(bool value, string message)
        {
            if (!value) throw new System.InvalidOperationException("[CharacterVisualAudit] " + message);
        }
    }
}

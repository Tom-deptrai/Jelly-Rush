using JellyRush.Spawnables;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Spawning
{
    /// <summary>
    /// Turns a <see cref="SpawnableKind"/> into a GameObject. If the active
    /// <see cref="WorldThemeData"/> supplies a prefab for that function it is
    /// instantiated (so each world can look completely different); otherwise a
    /// tinted primitive PLACEHOLDER is built using the theme palette. Gameplay code
    /// never knows which happened.
    /// </summary>
    public class SpawnableFactory
    {
        readonly Transform _parent;
        readonly WorldThemeData _theme;

        readonly Material _matPlatform;
        readonly Material _matMoving;
        readonly Material _matCoin;
        readonly Material _matObstacle;
        readonly Material _matBar;
        readonly Material _matGate;
        readonly Material _matBounce;
        readonly Material _matFinish;

        public SpawnableFactory(Transform parent, WorldThemeData theme)
        {
            _parent = parent;
            _theme = theme;

            Color platform = theme != null ? theme.platformColor : new Color(0.95f, 0.86f, 0.62f);
            Color accent   = theme != null ? theme.accentColor   : new Color(0.98f, 0.70f, 0.35f);
            Color hazard   = theme != null ? theme.hazardColor   : new Color(0.90f, 0.25f, 0.28f);
            Color coin     = theme != null ? theme.coinColor     : new Color(1.00f, 0.85f, 0.15f);

            _matPlatform = Mat(platform);
            _matMoving   = Mat(accent);
            _matCoin     = Mat(coin, emissive: true);
            _matObstacle = Mat(hazard);
            _matBar      = Mat(hazard * 0.85f + accent * 0.15f);
            _matGate     = Mat(accent * 0.6f + hazard * 0.4f);
            _matBounce   = Mat(new Color(0.30f, 0.90f, 0.55f), emissive: true);
            _matFinish   = Mat(new Color(0.20f, 0.95f, 0.45f), emissive: true);
        }

        static Material Mat(Color c, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { color = c };
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.6f);
            }
            return m;
        }

        GameObject Prim(PrimitiveType type, string name, Material mat, Vector3 scale, bool trigger)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Collider>().isTrigger = trigger;
            return go;
        }

        public GameObject Create(SpawnableKind kind)
        {
            var prefab = _theme != null ? _theme.PrefabFor(kind) : null;
            GameObject root = prefab != null ? Object.Instantiate(prefab) : BuildPlaceholder(kind);

            var tag = root.GetComponent<SpawnableTag>() ?? root.AddComponent<SpawnableTag>();
            tag.SetKind(kind);
            root.transform.SetParent(_parent, false);
            return root;
        }

        GameObject BuildPlaceholder(SpawnableKind kind)
        {
            switch (kind)
            {
                case SpawnableKind.Platform:
                    return Prim(PrimitiveType.Cube, "Platform_PLACEHOLDER", _matPlatform,
                               new Vector3(1.9f, 0.4f, 2.6f), false);

                case SpawnableKind.MovingPlatform:
                {
                    var g = Prim(PrimitiveType.Cube, "MovingPlatform_PLACEHOLDER", _matMoving,
                                 new Vector3(1.7f, 0.4f, 2.4f), false);
                    g.AddComponent<MovingPlatform>();
                    return g;
                }

                case SpawnableKind.Coin:
                {
                    var g = Prim(PrimitiveType.Cylinder, "Coin_PLACEHOLDER", _matCoin,
                                 new Vector3(0.55f, 0.06f, 0.55f), true);
                    g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    g.AddComponent<CoinSpin>();
                    return g;
                }

                case SpawnableKind.Obstacle:
                    return Prim(PrimitiveType.Cube, "Obstacle_PLACEHOLDER", _matObstacle,
                               new Vector3(1.5f, 1.5f, 0.5f), true);

                case SpawnableKind.RotatingBar:
                {
                    var g = new GameObject("RotatingBar_PLACEHOLDER");
                    var bar = Prim(PrimitiveType.Cube, "Bar", _matBar, new Vector3(4.6f, 0.4f, 0.4f), true);
                    bar.transform.SetParent(g.transform, false);
                    g.AddComponent<RotatingBar>().Init(120f);
                    return g;
                }

                case SpawnableKind.ClosingGate:
                {
                    var g = new GameObject("ClosingGate_PLACEHOLDER");
                    var lp = Prim(PrimitiveType.Cube, "LeftPanel", _matGate, new Vector3(2.0f, 2.2f, 0.35f), true);
                    var rp = Prim(PrimitiveType.Cube, "RightPanel", _matGate, new Vector3(2.0f, 2.2f, 0.35f), true);
                    lp.transform.SetParent(g.transform, false);
                    rp.transform.SetParent(g.transform, false);
                    lp.transform.localPosition = new Vector3(-2.6f, 1.1f, 0f);
                    rp.transform.localPosition = new Vector3(2.6f, 1.1f, 0f);
                    g.AddComponent<ClosingGate>().Init(lp.transform, rp.transform, 1, 3.6f);
                    return g;
                }

                case SpawnableKind.BouncePad:
                {
                    var g = Prim(PrimitiveType.Cube, "BouncePad_PLACEHOLDER", _matBounce,
                                 new Vector3(1.6f, 0.3f, 1.6f), true);
                    g.AddComponent<BouncePad>();
                    return g;
                }

                case SpawnableKind.FinishPlatform:
                {
                    // Big bright landing pad + a tall banner wall so it reads from afar.
                    var g = new GameObject("FinishPlatform_PLACEHOLDER");
                    var pad = Prim(PrimitiveType.Cube, "Pad", _matFinish, new Vector3(3.4f, 0.5f, 13f), false);
                    pad.transform.SetParent(g.transform, false);
                    pad.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                    var wall = Prim(PrimitiveType.Cube, "Banner", _matFinish, new Vector3(7f, 4.5f, 0.4f), false);
                    wall.transform.SetParent(g.transform, false);
                    wall.transform.localPosition = new Vector3(0f, 2f, 6.5f);
                    Object.Destroy(wall.GetComponent<Collider>());
                    return g;
                }

                default:
                    return Prim(PrimitiveType.Cube, "Unknown", _matPlatform, Vector3.one, false);
            }
        }
    }
}

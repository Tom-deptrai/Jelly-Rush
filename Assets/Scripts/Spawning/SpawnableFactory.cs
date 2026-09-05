using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Spawning
{
    /// <summary>
    /// Builds PLACEHOLDER primitives for every gameplay element. One place to swap
    /// for real prefabs later - gameplay code only ever asks for a SpawnableKind.
    /// </summary>
    public class SpawnableFactory
    {
        readonly Transform _parent;
        readonly Material _matPlatform;
        readonly Material _matMoving;
        readonly Material _matCoin;
        readonly Material _matObstacle;
        readonly Material _matBar;
        readonly Material _matGate;
        readonly Material _matBounce;

        public SpawnableFactory(Transform parent)
        {
            _parent = parent;
            _matPlatform = Mat(new Color(0.95f, 0.86f, 0.62f));
            _matMoving   = Mat(new Color(0.98f, 0.70f, 0.35f));
            _matCoin     = Mat(new Color(1.00f, 0.85f, 0.15f), emissive: true);
            _matObstacle = Mat(new Color(0.90f, 0.25f, 0.28f));
            _matBar      = Mat(new Color(0.75f, 0.20f, 0.55f));
            _matGate     = Mat(new Color(0.55f, 0.35f, 0.85f));
            _matBounce   = Mat(new Color(0.30f, 0.90f, 0.55f), emissive: true);
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
            var col = go.GetComponent<Collider>();
            col.isTrigger = trigger;
            return go;
        }

        public GameObject Create(SpawnableKind kind)
        {
            GameObject root;
            switch (kind)
            {
                case SpawnableKind.Platform:
                    root = Prim(PrimitiveType.Cube, "Platform", _matPlatform, new Vector3(1.7f, 0.4f, 2.6f), false);
                    break;

                case SpawnableKind.MovingPlatform:
                    root = Prim(PrimitiveType.Cube, "MovingPlatform", _matMoving, new Vector3(1.6f, 0.4f, 2.4f), false);
                    root.AddComponent<MovingPlatform>();
                    break;

                case SpawnableKind.Coin:
                    root = Prim(PrimitiveType.Cylinder, "Coin", _matCoin, new Vector3(0.55f, 0.06f, 0.55f), true);
                    root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    root.AddComponent<CoinSpin>();
                    break;

                case SpawnableKind.Obstacle:
                    root = Prim(PrimitiveType.Cube, "Obstacle", _matObstacle, new Vector3(1.5f, 1.6f, 0.5f), true);
                    break;

                case SpawnableKind.RotatingBar:
                    root = new GameObject("RotatingBar");
                    var bar = Prim(PrimitiveType.Cube, "Bar", _matBar, new Vector3(4.6f, 0.4f, 0.4f), true);
                    bar.transform.SetParent(root.transform, false);
                    root.AddComponent<RotatingBar>().Init(120f);
                    break;

                case SpawnableKind.ClosingGate:
                    root = new GameObject("ClosingGate");
                    var frame = Prim(PrimitiveType.Cube, "Frame", _matGate, new Vector3(0.3f, 2.4f, 0.3f), false);
                    frame.transform.SetParent(root.transform, false);
                    frame.transform.localScale = new Vector3(7f, 0.25f, 0.3f);
                    frame.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                    var lp = Prim(PrimitiveType.Cube, "LeftPanel", _matGate, new Vector3(2.0f, 2.2f, 0.35f), true);
                    var rp = Prim(PrimitiveType.Cube, "RightPanel", _matGate, new Vector3(2.0f, 2.2f, 0.35f), true);
                    lp.transform.SetParent(root.transform, false);
                    rp.transform.SetParent(root.transform, false);
                    lp.transform.localPosition = new Vector3(-2.6f, 1.1f, 0f);
                    rp.transform.localPosition = new Vector3(2.6f, 1.1f, 0f);
                    root.AddComponent<ClosingGate>().Init(lp.transform, rp.transform, 1, 2.4f);
                    break;

                case SpawnableKind.BouncePad:
                    root = Prim(PrimitiveType.Cube, "BouncePad", _matBounce, new Vector3(1.5f, 0.25f, 1.5f), true);
                    root.AddComponent<BouncePad>();
                    break;

                default:
                    root = Prim(PrimitiveType.Cube, "Unknown", _matPlatform, Vector3.one, false);
                    break;
            }

            root.AddComponent<SpawnableTag>().SetKind(kind);
            root.transform.SetParent(_parent, false);
            return root;
        }
    }
}

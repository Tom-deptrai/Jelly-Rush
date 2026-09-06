using UnityEngine;

namespace JellyRush.Feedback
{
    /// <summary>Allocation-free renderer burst pool with no optional package dependency.</summary>
    public sealed class VfxManager : MonoBehaviour
    {
        const int PoolSize = 28;
        readonly Transform[] _items = new Transform[PoolSize];
        readonly Renderer[] _renderers = new Renderer[PoolSize];
        readonly MaterialPropertyBlock[] _properties = new MaterialPropertyBlock[PoolSize];
        readonly Vector3[] _velocity = new Vector3[PoolSize];
        readonly float[] _life = new float[PoolSize];
        readonly float[] _totalLife = new float[PoolSize];
        int _next;
        Material _material;
        GameplayFeedbackHub _hub;

        public void Configure(GameplayFeedbackHub hub)
        {
            _hub = hub;
            BuildPool();
            hub.OnJumpBeat += Jump; hub.OnLand += Land; hub.OnCoinCollected += Coin;
            hub.OnHeadHit += HeadHit; hub.OnHit += Hit; hub.OnLevelComplete += Finish;
            hub.OnComboMilestone += Combo;
        }

        void BuildPool()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            if (shader != null) _material = new Material(shader) { name = "GameFeelV1_BurstMat" };
            for (int i = 0; i < PoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "VfxBurst_" + i;
                go.transform.SetParent(transform, false);
                Destroy(go.GetComponent<Collider>());
                var renderer = go.GetComponent<Renderer>();
                if (_material != null) renderer.sharedMaterial = _material;
                go.SetActive(false);
                _items[i] = go.transform;
                _renderers[i] = renderer;
                _properties[i] = new MaterialPropertyBlock();
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var camera = Camera.main;
            for (int i = 0; i < PoolSize; i++)
            {
                if (_life[i] <= 0f) continue;
                _life[i] -= dt;
                if (_life[i] <= 0f) { _items[i].gameObject.SetActive(false); continue; }
                _velocity[i] += Vector3.down * (1.3f * dt);
                _items[i].position += _velocity[i] * dt;
                if (camera != null) _items[i].rotation = camera.transform.rotation;
                float t = Mathf.Clamp01(_life[i] / _totalLife[i]);
                _items[i].localScale *= Mathf.Lerp(0.90f, 1.02f, t);
            }
        }

        void Burst(Vector3 position, Color color, int count, float size, float speed, float lifetime, bool ring = false)
        {
            count = Mathf.Clamp(count, 1, PoolSize);
            for (int n = 0; n < count; n++)
            {
                int i = _next++ % PoolSize;
                float angle = (n / (float)count) * Mathf.PI * 2f;
                Vector3 planar = new Vector3(Mathf.Cos(angle), ring ? 0.12f : Mathf.Sin(angle * 1.7f) * 0.45f,
                                             Mathf.Sin(angle));
                _items[i].position = position;
                _items[i].localScale = Vector3.one * size;
                _velocity[i] = planar.normalized * speed + Vector3.up * (ring ? 0.25f : 0.55f * speed);
                _life[i] = _totalLife[i] = lifetime;
                _properties[i].SetColor("_Color", color);
                _renderers[i].SetPropertyBlock(_properties[i]);
                _items[i].gameObject.SetActive(true);
            }
        }

        void Jump(FeedbackSignal s) => Burst(s.position + Vector3.up * 0.05f, new Color(0.55f, 0.9f, 1f), 6, 0.10f, 0.65f, 0.28f);
        void Land(FeedbackSignal s) => Burst(s.position + Vector3.up * 0.04f, new Color(1f, 0.88f, 0.45f), 9, 0.12f, 0.85f, 0.34f, true);
        void Coin(FeedbackSignal s) => Burst(s.position, new Color(1f, 0.86f, 0.12f), 10, 0.11f, 1.35f, 0.38f);
        void HeadHit(FeedbackSignal s) => Burst(s.position, new Color(0.55f, 0.92f, 1f), 8, 0.10f, 1.15f, 0.30f);
        void Hit(FeedbackSignal s) => Burst(s.position, new Color(1f, 0.25f, 0.16f), 13, 0.14f, 1.55f, 0.42f);
        void Finish(FeedbackSignal s) => Burst(s.position + Vector3.up, new Color(0.35f, 1f, 0.55f), 24, 0.16f, 2f, 0.75f);
        void Combo(FeedbackSignal s) => Burst(s.position + Vector3.up * 1.5f, new Color(0.65f, 0.9f, 1f), 8, 0.08f, 0.8f, 0.3f);

        void OnDestroy()
        {
            if (_hub != null)
            {
                _hub.OnJumpBeat -= Jump; _hub.OnLand -= Land; _hub.OnCoinCollected -= Coin;
                _hub.OnHeadHit -= HeadHit; _hub.OnHit -= Hit; _hub.OnLevelComplete -= Finish;
                _hub.OnComboMilestone -= Combo;
            }
            if (_material != null) Destroy(_material);
        }
    }
}

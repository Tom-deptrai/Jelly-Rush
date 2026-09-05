using UnityEngine;

namespace JellyRush.Spawnables
{
    /// <summary>Spins a coin so it reads clearly against the background.</summary>
    public class CoinSpin : MonoBehaviour
    {
        [SerializeField] float _speed = 140f;
        void Update() => transform.Rotate(0f, _speed * Time.deltaTime, 0f, Space.World);
    }

    /// <summary>
    /// PLACEHOLDER moving platform: slides side to side across lanes in local space
    /// while the world carries it toward the camera.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        float _amplitude = 2.1f;
        float _speed = 1.2f;
        float _phase;
        Vector3 _origin;

        public void Init(float amplitude, float speed, float phase)
        {
            _amplitude = amplitude;
            _speed = speed;
            _phase = phase;
            _origin = transform.localPosition;
        }

        void OnEnable() => _origin = transform.localPosition;

        void Update()
        {
            var p = _origin;
            p.x = _origin.x + Mathf.Sin(Time.time * _speed + _phase) * _amplitude;
            transform.localPosition = p;
        }
    }

    /// <summary>PLACEHOLDER rotating bar sweeping around the center lane.</summary>
    public class RotatingBar : MonoBehaviour
    {
        float _speed = 90f;
        public void Init(float speed) => _speed = speed;
        void Update() => transform.Rotate(0f, _speed * Time.deltaTime, 0f, Space.Self);
    }

    /// <summary>
    /// PLACEHOLDER closing gate: two panels that slide inward and back on a cycle,
    /// leaving one safe lane. Fully closed briefly blocks everything.
    /// </summary>
    public class ClosingGate : MonoBehaviour
    {
        [SerializeField] Transform _leftPanel;
        [SerializeField] Transform _rightPanel;
        int _safeLane = 1;
        float _cycle = 3.6f;
        float _openX = 2.6f;
        float _closedX = 0.55f;

        public void Init(Transform left, Transform right, int safeLane, float cycleSeconds)
        {
            _leftPanel = left;
            _rightPanel = right;
            _safeLane = safeLane;
            _cycle = cycleSeconds;
        }

        float Closed01 => 0.5f - 0.5f * Mathf.Cos(Time.time * (Mathf.PI * 2f / _cycle));

        void Update()
        {
            float x = Mathf.Lerp(_openX, _closedX, Closed01);
            if (_leftPanel != null) _leftPanel.localPosition = new Vector3(-x, _leftPanel.localPosition.y, 0f);
            if (_rightPanel != null) _rightPanel.localPosition = new Vector3(x, _rightPanel.localPosition.y, 0f);
        }

        public bool BlocksPlayerNow(int lane)
        {
            if (Closed01 > 0.9f) return true;       // slammed shut
            return lane != _safeLane;               // only the safe lane is clear
        }
    }

    /// <summary>PLACEHOLDER bounce pad: gentle pulsing so the player notices it.</summary>
    public class BouncePad : MonoBehaviour
    {
        Vector3 _base;
        void OnEnable() => _base = transform.localScale;
        void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
            transform.localScale = new Vector3(_base.x, _base.y * s, _base.z);
        }
    }
}

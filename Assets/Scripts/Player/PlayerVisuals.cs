using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// PLACEHOLDER visuals for the player unit. The hierarchy is built so real art
    /// can drop in later without touching gameplay code:
    ///
    ///   PlayerRoot            (PlayerController, kinematic Rigidbody, trigger collider)
    ///     LeanPivot           (this component - tilt on lane change)
    ///       Carrier           (placeholder capsule) -> replace with the small animal carrier
    ///         JellyAnchor
    ///           Jelly         (placeholder rounded cube) -> replace with the blue Jelly mascot
    ///             Face        (placeholder quad so the mascot's "face" stays camera-visible)
    ///
    /// Reaction hooks (OnJump / OnLand / OnLaneChange / squash) are stubbed with
    /// simple scale + colour pops so the feel can be evaluated now.
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] Transform _leanPivot;
        [SerializeField] Transform _jelly;
        [SerializeField] Renderer _jellyRenderer;

        Vector3 _jellyBaseScale;
        float _squash;         // 0..1 transient
        float _targetLean;
        float _lean;
        Color _baseColor = new Color(0.30f, 0.60f, 0.95f);
        float _excite;         // rises with rapid jumps, decays over time
        float _hitFlash;
        float _reactionLean;

        public void Bind(Transform leanPivot, Transform jelly, Renderer jellyRenderer)
        {
            _leanPivot = leanPivot;
            _jelly = jelly;
            _jellyRenderer = jellyRenderer;
            _jellyBaseScale = jelly.localScale;
            if (_jellyRenderer != null) _jellyRenderer.material.color = _baseColor;
        }

        public void OnJump(float heightScale)
        {
            _squash = 1f;
            _excite = Mathf.Min(1f, _excite + 0.35f);
        }

        public void OnLand()
        {
            _squash = 0.6f;
        }

        /// <summary>Hook for the future victory pose / two-character celebration. Stub for now.</summary>
        public void OnLevelComplete()
        {
            _excite = 1f;
            _squash = 1f;
            _reactionLean = -5f;
        }

        public void OnHeadHit()
        {
            _squash = 0.85f;
            _hitFlash = 0.7f;
            _reactionLean = 4f;
        }

        public void OnHit()
        {
            _squash = 1f;
            _hitFlash = 1f;
            _reactionLean = -8f;
        }

        public void OnFail()
        {
            _hitFlash = 0.85f;
            _reactionLean = 11f;
        }

        public void OnComboMilestone(int combo)
        {
            _excite = 1f;
            _squash = Mathf.Clamp01(0.45f + combo * 0.025f);
        }

        public void OnLaneChange(int dir)
        {
            _excite = Mathf.Min(1f, _excite + 0.2f);
        }

        public void SetLean(float degrees) => _targetLean = degrees;

        void Update()
        {
            float dt = Time.deltaTime;
            _excite = Mathf.Max(0f, _excite - dt * 0.5f);
            _squash = Mathf.Max(0f, _squash - dt * 3.5f);
            _hitFlash = Mathf.Max(0f, _hitFlash - dt * 5f);
            _reactionLean = Mathf.Lerp(_reactionLean, 0f, 1f - Mathf.Exp(-7f * dt));

            _lean = Mathf.Lerp(_lean, _targetLean + _reactionLean, 1f - Mathf.Exp(-12f * dt));
            if (_leanPivot != null)
                _leanPivot.localRotation = Quaternion.Euler(0f, 0f, _lean);

            if (_jelly != null)
            {
                // squash-stretch + "rounder when excited" per GAMEPLAY_SPEC section 4
                float round = 1f + _excite * 0.12f;
                float sx = 1f + _squash * 0.25f + (_excite * 0.05f);
                float sy = 1f - _squash * 0.22f;
                _jelly.localScale = Vector3.Scale(_jellyBaseScale, new Vector3(sx * round, sy * round, sx * round));
            }

            if (_jellyRenderer != null)
            {
                Color excited = Color.Lerp(_baseColor, new Color(0.55f, 0.8f, 1f), _excite);
                _jellyRenderer.material.color = Color.Lerp(excited, new Color(1f, 0.38f, 0.3f), _hitFlash);
            }
        }
    }
}

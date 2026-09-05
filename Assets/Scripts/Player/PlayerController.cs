using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// The player "unit" = Jelly + carrier treated as one thing (GAMEPLAY_SPEC 3).
    ///
    /// Jump model is velocity + gravity (not a fixed-length arc), so every valid
    /// Tap can start a NEW jump beat immediately - even mid-air - by re-launching
    /// from the current height. No queue, no minimum interval between taps: the
    /// faster the player taps, the more continuous the jump chain
    /// (GAMEPLAY_SPEC 1 & 4). Y stays continuous (only vertical velocity changes),
    /// so there is no teleport and no positional snap.
    ///
    /// Lane sliding is a smooth SmoothDamp toward the target lane X and is fully
    /// independent of the jump, so a swipe changes lane AND fires a jump beat at
    /// once, on the ground or in the air. Rendering / squash-stretch reactions
    /// live in <see cref="PlayerVisuals"/>.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        PrototypeConfig _cfg;
        LaneSystem _lanes;
        GameManager _game;
        SwipeTapInput _input;
        PlayerVisuals _visuals;

        int _currentLane = LaneSystem.Center;
        float _laneX;               // current interpolated X
        float _laneXVel;            // SmoothDamp velocity
        int _targetLane = LaneSystem.Center;

        float _y;                   // current height above the lane floor
        float _vy;                  // vertical velocity
        bool _airborne;

        public int CurrentLane => _currentLane;
        public bool IsAirborne => _airborne;

        /// <summary>Gravity from the "reference" jump: apex = jumpHeight at jumpDuration/2. g = 8h / T^2.</summary>
        float Gravity
        {
            get
            {
                float t = Mathf.Max(0.05f, _cfg.jumpDuration);
                return 8f * Mathf.Max(0.01f, _cfg.jumpHeight) / (t * t);
            }
        }

        /// <summary>Take-off velocity for a reference-height hop. v0 = g * T / 2.</summary>
        float LaunchVelocity => Gravity * Mathf.Max(0.05f, _cfg.jumpDuration) * 0.5f;

        public void Configure(PrototypeConfig cfg, LaneSystem lanes, GameManager game,
                              SwipeTapInput input, PlayerVisuals visuals)
        {
            _cfg = cfg;
            _lanes = lanes;
            _game = game;
            _input = input;
            _visuals = visuals;

            _lanes.Configure(cfg.laneSpacing);
            _laneX = _lanes.LaneToX(_currentLane);
            _y = _cfg.groundY;
            _vy = 0f;
            ApplyPosition();

            _input.Gesture += OnGesture;
        }

        void OnDestroy()
        {
            if (_input != null) _input.Gesture -= OnGesture;
        }

        void OnGesture(GestureType g)
        {
            if (_game.State == GameState.Failed || _game.State == GameState.Paused) return;
            _game.BeginRunning();

            switch (g)
            {
                case GestureType.SwipeLeft:
                    _targetLane = _lanes.Step(_targetLane, -1);
                    _visuals?.OnLaneChange(-1);
                    Beat(1f);                 // lane change also fires a jump beat now
                    break;
                case GestureType.SwipeRight:
                    _targetLane = _lanes.Step(_targetLane, +1);
                    _visuals?.OnLaneChange(+1);
                    Beat(1f);
                    break;
                case GestureType.Tap:
                    Beat(1f);
                    break;
            }
        }

        /// <summary>
        /// Start a new jump beat right now. Works whether grounded or airborne - it
        /// (re)sets the upward velocity from the current height, so Y stays
        /// continuous (no teleport). This is the whole "rapid tap = continuous
        /// chain". A normal mid-air beat is scaled down as the pair nears
        /// <see cref="PrototypeConfig.airChainCeiling"/> so a fast chain plateaus
        /// instead of flying skyward; bounce pads (heightScale &gt; 1) ignore that.
        /// </summary>
        void Beat(float heightScale)
        {
            float full = LaunchVelocity * Mathf.Sqrt(Mathf.Max(0.01f, heightScale));

            if (_airborne && heightScale <= 1f && _cfg.airChainCeiling > 0f)
            {
                float headroom = Mathf.Max(0f, _cfg.airChainCeiling - _y);
                float capped = Mathf.Sqrt(2f * Gravity * headroom);
                _vy = Mathf.Min(full, capped);
            }
            else
            {
                _vy = full;
            }

            _airborne = true;
            _game.RegisterJump();
            _visuals?.OnJump(heightScale);
        }

        /// <summary>External trigger (bounce pad) - a stronger beat.</summary>
        public void ForceBounce() => Beat(_cfg.bouncePadMultiplier);

        void Update()
        {
            if (_cfg == null) return;
            if (_game.State == GameState.Paused) return;

            float dt = Time.deltaTime;

            // --- lane slide (smooth, independent of jump) -------------------
            _currentLane = _targetLane;
            float targetX = _lanes.LaneToX(_targetLane);
            _laneX = Mathf.SmoothDamp(_laneX, targetX, ref _laneXVel,
                                     Mathf.Max(0.02f, _cfg.laneChangeDuration));

            // --- vertical integration (velocity + gravity) -----------------
            if (_airborne)
            {
                _vy -= Gravity * dt;
                _y += _vy * dt;

                if (_y <= _cfg.groundY && _vy <= 0f)
                {
                    _y = _cfg.groundY;
                    _vy = 0f;
                    _airborne = false;
                    _visuals?.OnLand();
                }
            }
            else
            {
                _y = _cfg.groundY;
            }

            ApplyPosition();

            // --- visual lean toward the lane we are sliding to --------------
            float lean = Mathf.Clamp(_laneXVel / _cfg.laneSpacing, -1f, 1f);
            _visuals?.SetLean(-lean * _cfg.laneLeanAngle);
        }

        void ApplyPosition()
        {
            transform.position = new Vector3(_laneX, _y, _cfg.playerZ);
        }
    }
}

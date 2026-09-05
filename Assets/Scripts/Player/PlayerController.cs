using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// The player "unit" = Jelly + carrier treated as one thing (GAMEPLAY_SPEC 3).
    /// Handles: jump arc (modest height), lane sliding (smooth, never teleport),
    /// visual lean, and full acceptance of rapid input. Tapping while airborne
    /// queues the next hop so no tap is ever dropped and the motion stays fluid.
    /// Rendering / squash-stretch reactions live in <see cref="PlayerVisuals"/>.
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

        bool _airborne;
        float _jumpT;               // 0..jumpDuration
        float _jumpHeightScale = 1f;
        int _queuedJumps;
        const int MaxQueuedJumps = 2;

        public int CurrentLane => _currentLane;
        public bool IsAirborne => _airborne;

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
            ApplyPosition(_cfg.groundY);

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
                    RequestJump();
                    _visuals?.OnLaneChange(-1);
                    break;
                case GestureType.SwipeRight:
                    _targetLane = _lanes.Step(_targetLane, +1);
                    RequestJump();
                    _visuals?.OnLaneChange(+1);
                    break;
                case GestureType.Tap:
                    RequestJump();
                    break;
            }
        }

        void RequestJump()
        {
            if (!_airborne)
            {
                StartJump(1f);
            }
            else if (_queuedJumps < MaxQueuedJumps)
            {
                _queuedJumps++;               // rapid tap: honoured on landing
            }
        }

        void StartJump(float heightScale)
        {
            _airborne = true;
            _jumpT = 0f;
            _jumpHeightScale = heightScale;
            _game.RegisterJump();
            _visuals?.OnJump(heightScale);
        }

        /// <summary>External trigger (bounce pad).</summary>
        public void ForceBounce()
        {
            StartJump(_cfg.bouncePadMultiplier);
            _queuedJumps = 0;
        }

        void Update()
        {
            if (_cfg == null) return;
            if (_game.State == GameState.Paused) return;

            float dt = Time.deltaTime;

            // --- lane slide (smooth) -----------------------------------------
            _currentLane = _targetLane;
            float targetX = _lanes.LaneToX(_targetLane);
            _laneX = Mathf.SmoothDamp(_laneX, targetX, ref _laneXVel,
                                     Mathf.Max(0.02f, _cfg.laneChangeDuration));

            // --- jump arc ---------------------------------------------------
            float y = _cfg.groundY;
            if (_airborne)
            {
                _jumpT += dt;
                float dur = Mathf.Max(0.05f, _cfg.jumpDuration);
                float n = _jumpT / dur;
                if (n >= 1f)
                {
                    _airborne = false;
                    y = _cfg.groundY;
                    if (_queuedJumps > 0)
                    {
                        _queuedJumps--;
                        StartJump(1f);
                        y = _cfg.groundY;
                    }
                    else
                    {
                        _visuals?.OnLand();
                    }
                }
                else
                {
                    y = _cfg.groundY + Mathf.Sin(n * Mathf.PI) * _cfg.jumpHeight * _jumpHeightScale;
                }
            }

            ApplyPosition(y);

            // --- visual lean toward the lane we are sliding to --------------
            float lean = Mathf.Clamp((_laneXVel) / _cfg.laneSpacing, -1f, 1f);
            _visuals?.SetLean(-lean * _cfg.laneLeanAngle);
        }

        void ApplyPosition(float y)
        {
            transform.position = new Vector3(_laneX, y, _cfg.playerZ);
        }
    }
}

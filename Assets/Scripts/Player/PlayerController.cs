using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// The player "unit" = Jelly + carrier treated as one thing (GAMEPLAY_SPEC 3).
    ///
    /// Jump = velocity + gravity. Every valid Tap fires a NEW beat immediately
    /// (grounded or mid-air) by re-setting vertical velocity from the current
    /// height, so Y stays continuous - no teleport, no queue. Swipe L/R changes
    /// lane AND fires a beat.
    ///
    /// Round 2 rules:
    ///  - At most <see cref="PrototypeConfig.maxAirJumpBeats"/> (4) consecutive
    ///    beats between two successful landings. Each Tap / lane-swipe beat spends
    ///    one; when they run out, further Tap/Swipe still changes lane but no
    ///    longer produces a jump. Landing on a platform refills them.
    ///  - There is no ground. A downward raycast finds the platform under the
    ///    pair; landing on its top (while falling) refills beats. Falling past
    ///    <see cref="PrototypeConfig.failY"/> is Game Over.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        PrototypeConfig _cfg;
        LaneSystem _lanes;
        GameManager _game;
        SwipeTapInput _input;
        PlayerVisuals _visuals;

        int _currentLane = LaneSystem.Center;
        float _laneX;
        float _laneXVel;
        int _targetLane = LaneSystem.Center;

        float _y;
        float _vy;
        bool _airborne;
        int _beatsLeft;

        public int CurrentLane => _currentLane;
        public bool IsAirborne => _airborne;
        public int BeatsLeft => _beatsLeft;

        float Gravity
        {
            get
            {
                float t = Mathf.Max(0.05f, _cfg.jumpDuration);
                return 8f * Mathf.Max(0.01f, _cfg.jumpHeight) / (t * t);
            }
        }

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
            _y = 0f;
            _vy = 0f;
            _airborne = false;
            _beatsLeft = Mathf.Max(1, cfg.maxAirJumpBeats);
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
                    TryBeat(1f, gated: true);          // lane still changes even if no beat left
                    break;
                case GestureType.SwipeRight:
                    _targetLane = _lanes.Step(_targetLane, +1);
                    _visuals?.OnLaneChange(+1);
                    TryBeat(1f, gated: true);
                    break;
                case GestureType.Tap:
                    TryBeat(1f, gated: true);
                    break;
            }
        }

        /// <summary>
        /// Fire a jump beat now. Returns false (no beat) when gated and the 4-beat
        /// budget is spent. Y stays continuous; only vertical velocity changes.
        /// </summary>
        bool TryBeat(float heightScale, bool gated)
        {
            if (gated)
            {
                if (_beatsLeft <= 0) return false;
                _beatsLeft--;
            }

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
            return true;
        }

        /// <summary>External launch (bounce pad): ungated, ignores the beat budget and the ceiling.</summary>
        public void ForceBounce() => TryBeat(_cfg.bouncePadMultiplier, gated: false);

        void Update()
        {
            if (_cfg == null) return;
            var state = _game.State;
            if (state == GameState.Paused || state == GameState.Failed) return;

            float dt = Time.deltaTime;

            // --- lane slide (smooth, independent of jump) -------------------
            _currentLane = _targetLane;
            float targetX = _lanes.LaneToX(_targetLane);
            _laneX = Mathf.SmoothDamp(_laneX, targetX, ref _laneXVel,
                                     Mathf.Max(0.02f, _cfg.laneChangeDuration));

            bool overPlatform = TryGetPlatformBelow(out float surfaceY);

            if (_airborne)
            {
                _vy -= Gravity * dt;
                _y += _vy * dt;

                if (_vy <= 0f && overPlatform && _y <= surfaceY + _cfg.landSnap)
                {
                    Land(surfaceY);
                }
                else if (_y < _cfg.failY)
                {
                    _game.Fail("fell into the gap");
                }
            }
            else
            {
                if (overPlatform)
                    _y = surfaceY;                 // riding the platform as it scrolls past
                else
                    _airborne = true;              // walked off the edge / platform gone -> fall
            }

            ApplyPosition();

            float lean = Mathf.Clamp(_laneXVel / _cfg.laneSpacing, -1f, 1f);
            _visuals?.SetLean(-lean * _cfg.laneLeanAngle);
        }

        void Land(float surfaceY)
        {
            _y = surfaceY;
            _vy = 0f;
            _airborne = false;
            _beatsLeft = Mathf.Max(1, _cfg.maxAirJumpBeats);   // refill the 4-beat budget
            _visuals?.OnLand();
        }

        /// <summary>
        /// Raycast straight down for a landable platform under the pair's current
        /// lane X. Only solid Platform / MovingPlatform colliders count.
        /// </summary>
        bool TryGetPlatformBelow(out float surfaceY)
        {
            surfaceY = 0f;
            Vector3 origin = new Vector3(_laneX, _y + 0.6f, _cfg.playerZ);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                var tag = hit.collider.GetComponentInParent<SpawnableTag>();
                if (tag != null &&
                    (tag.Kind == SpawnableKind.Platform || tag.Kind == SpawnableKind.MovingPlatform))
                {
                    surfaceY = hit.point.y;
                    return true;
                }
            }
            return false;
        }

        void ApplyPosition()
        {
            transform.position = new Vector3(_laneX, _y, _cfg.playerZ);
        }
    }
}

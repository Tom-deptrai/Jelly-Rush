using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// The player "unit" = Jelly + carrier as one thing (GAMEPLAY_SPEC 3).
    ///
    /// Round 3 - NO fixed ground. _y is an ABSOLUTE world height. When the pair
    /// lands on a platform that platform's Y becomes the current support height;
    /// the next jump starts from exactly there (no snap back to 0). If nothing is
    /// under the pair it keeps falling; once it drops
    /// <see cref="PrototypeConfig.failDropBelowSupport"/> below the last platform
    /// it stood on, Game Over.
    ///
    /// Jump = velocity + gravity. Every Tap fires a new beat immediately (grounded
    /// or mid-air). Tap and lane-swipe each spend one of
    /// <see cref="PrototypeConfig.maxAirJumpBeats"/> (4) beats between landings;
    /// when spent, Tap does nothing and Swipe still changes lane but no longer
    /// jumps. The chain plateaus at airChainCeiling measured ABOVE the platform the
    /// chain started from, so a full 4-beat chain can just reach the High tier but
    /// never flies away.
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

        float _y;                 // absolute world Y of the pair's feet
        float _vy;
        bool _airborne;
        float _supportY;          // Y of the platform currently / last supporting
        float _chainBaseY;        // support height the current jump chain launched from
        int _beatsLeft;

        public int CurrentLane => _currentLane;
        public int TargetLane => _targetLane;
        public bool IsAirborne => _airborne;
        public bool Grounded => !_airborne;
        public int BeatsLeft => _beatsLeft;
        public float SupportY => _supportY;
        public float Y => _y;
        public float VerticalVelocity => _vy;

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
            _supportY = cfg.startHeight;
            _y = cfg.startHeight + cfg.playerFootClearance;
            _chainBaseY = cfg.startHeight;
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
            var s = _game.State;
            if (s == GameState.Failed || s == GameState.Paused || s == GameState.Completed) return;
            _game.BeginRunning();

            switch (g)
            {
                case GestureType.SwipeLeft:
                    _targetLane = _lanes.Step(_targetLane, -1);
                    _visuals?.OnLaneChange(-1);
                    TryBeat(1f, gated: true);
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

        bool TryBeat(float heightScale, bool gated)
        {
            if (gated)
            {
                if (_beatsLeft <= 0) return false;
                _beatsLeft--;
            }

            if (!_airborne) _chainBaseY = _supportY;   // a fresh chain starts from this platform

            float full = LaunchVelocity * Mathf.Sqrt(Mathf.Max(0.01f, heightScale));

            if (_airborne && heightScale <= 1f && _cfg.airChainCeiling > 0f)
            {
                float ceilingY = _chainBaseY + _cfg.airChainCeiling;
                float headroom = Mathf.Max(0f, ceilingY - _y);
                _vy = Mathf.Min(full, Mathf.Sqrt(2f * Gravity * headroom));
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
            if (state == GameState.Paused || state == GameState.Failed || state == GameState.Completed)
                return;

            float dt = Time.deltaTime;

            _currentLane = _targetLane;
            float targetX = _lanes.LaneToX(_targetLane);
            _laneX = Mathf.SmoothDamp(_laneX, targetX, ref _laneXVel,
                                     Mathf.Max(0.02f, _cfg.laneChangeDuration));

            bool over = TryGetPlatformBelow(out float surfaceY, out bool isFinish);

            if (_airborne)
            {
                _vy -= Gravity * dt;
                _y += _vy * dt;

                if (_vy <= 0f && over && _y <= surfaceY + _cfg.landSnap)
                {
                    Land(surfaceY, isFinish);
                }
                else if (_y < _supportY - _cfg.failDropBelowSupport || _y < _cfg.failHeightAbsolute)
                {
                    _game.Fail("missed the platform");
                }
            }
            else
            {
                if (over && Mathf.Abs(surfaceY - _supportY) <= 0.75f)
                {
                    _supportY = surfaceY;                              // ride the platform
                    _y = surfaceY + _cfg.playerFootClearance;          // foot rests just above the surface
                }
                else
                {
                    _airborne = true;             // platform gone / stepped over an edge -> fall
                    _chainBaseY = _supportY;
                }
            }

            ApplyPosition();

            float lean = Mathf.Clamp(_laneXVel / _cfg.laneSpacing, -1f, 1f);
            _visuals?.SetLean(-lean * _cfg.laneLeanAngle);
        }

        void Land(float surfaceY, bool isFinish)
        {
            _supportY = surfaceY;
            _y = surfaceY + _cfg.playerFootClearance;   // foot just above the real surface
            _vy = 0f;
            _airborne = false;
            _chainBaseY = surfaceY;
            _beatsLeft = Mathf.Max(1, _cfg.maxAirJumpBeats);
            _visuals?.OnLand();

            if (isFinish)
            {
                _visuals?.OnLevelComplete();
                _game.CompleteLevel();
            }
        }

        /// <summary>
        /// Raycast straight down under the current lane X, restricted to the
        /// LANDABLE layer - a coin / hazard / decoration can never be a surface.
        /// surfaceY is the real collider top; isFinish flags the Finish Platform.
        /// </summary>
        bool TryGetPlatformBelow(out float surfaceY, out bool isFinish)
        {
            surfaceY = 0f;
            isFinish = false;
            Vector3 origin = new Vector3(_laneX, _y + 0.7f, _cfg.playerZ);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 40f,
                                GameLayers.LandableMask, QueryTriggerInteraction.Ignore))
            {
                surfaceY = hit.point.y;
                var tag = hit.collider.GetComponentInParent<SpawnableTag>();
                isFinish = tag != null && tag.Kind == SpawnableKind.FinishPlatform;
                return true;
            }
            return false;
        }

        void ApplyPosition()
        {
            transform.position = new Vector3(_laneX, _y, _cfg.playerZ);
        }
    }
}

using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using JellyRush.Feedback;
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
        PlayerCollisions _collisions;
        GameplayFeedbackHub _feedback;
        CapsuleCollider _motionEnvelope;
        readonly RaycastHit[] _castHits = new RaycastHit[32];
        readonly Collider[] _overlaps = new Collider[32];

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
                              SwipeTapInput input, PlayerVisuals visuals,
                              PlayerCollisions collisions, GameplayFeedbackHub feedback)
        {
            _cfg = cfg;
            _lanes = lanes;
            _game = game;
            _input = input;
            _visuals = visuals;
            _collisions = collisions;
            _feedback = feedback;
            _motionEnvelope = GetComponent<CapsuleCollider>();

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
            _feedback?.JumpBeat(transform.position, Mathf.Clamp(heightScale, 0.5f, 2f));
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
            float previousX = _laneX;
            float targetX = _lanes.LaneToX(_targetLane);
            float desiredX = Mathf.SmoothDamp(_laneX, targetX, ref _laneXVel,
                                              Mathf.Max(0.02f, _cfg.laneChangeDuration));
            Vector3 previous = new Vector3(previousX, _y, _cfg.playerZ);

            if (_airborne)
            {
                _vy -= Gravity * dt;
                Vector3 desired = new Vector3(desiredX, _y + _vy * dt, _cfg.playerZ);
                ResolveSolidMotion(previous, ref desired, _vy > 0f);
                _laneX = desired.x;
                _y = desired.y;

                if (_game.State == GameState.Failed)
                {
                    ApplyPosition();
                    return;
                }

                bool over = TryGetPlatformBelow(out float surfaceY, out bool isFinish);

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
                Vector3 desired = new Vector3(desiredX, _y, _cfg.playerZ);
                ResolveSolidMotion(previous, ref desired, false);
                _laneX = desired.x;
                _y = desired.y;

                if (_game.State == GameState.Failed)
                {
                    ApplyPosition();
                    return;
                }

                bool over = TryGetPlatformBelow(out float surfaceY, out bool isFinish);
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
            _feedback?.Land(new Vector3(_laneX, surfaceY, _cfg.playerZ), 1f);

            if (isFinish)
            {
                _feedback?.Complete(transform.position);
                _game.CompleteLevel();
            }
        }

        /// <summary>
        /// Sweeps the representative body/head capsule from the previous pose to the
        /// requested pose before committing movement. A short depenetration pass then
        /// catches world/moving-object motion that entered a stationary player.
        /// </summary>
        void ResolveSolidMotion(Vector3 previous, ref Vector3 desired, bool ascending)
        {
            if (_motionEnvelope == null) return;
            Physics.SyncTransforms();

            Vector3 position = previous;
            Vector3 remaining = desired - previous;
            float skin = Mathf.Max(0.005f, _cfg.collisionSkin);

            for (int iteration = 0; iteration < 3 && remaining.sqrMagnitude > 0.0000001f; iteration++)
            {
                Vector3 direction = remaining.normalized;
                float distance = remaining.magnitude;
                GetCapsule(position, out Vector3 top, out Vector3 bottom, out float radius);
                int count = Physics.CapsuleCastNonAlloc(top, bottom, radius, direction, _castHits,
                    distance + skin, GameLayers.SolidMask, QueryTriggerInteraction.Collide);

                int best = -1;
                float bestDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var hit = _castHits[i];
                    if (!ShouldBlock(hit.collider, hit.normal, ascending)) continue;
                    if (hit.distance < bestDistance) { best = i; bestDistance = hit.distance; }
                }

                if (best < 0) { position += remaining; remaining = Vector3.zero; break; }

                var blocking = _castHits[best];
                float travel = Mathf.Max(0f, blocking.distance - skin);
                position += direction * travel;

                if (IsLethalHazard(blocking.collider))
                {
                    desired = position;
                    _collisions?.HandlePredictiveHazard(blocking.collider, blocking.point);
                    return;
                }

                bool underside = ascending && blocking.normal.y < -0.35f;
                if (underside) RegisterHeadHit(blocking.point);
                if (Mathf.Abs(blocking.normal.x) > 0.35f) _laneXVel = 0f;

                Vector3 leftover = remaining - direction * travel;
                float into = Vector3.Dot(leftover, blocking.normal);
                remaining = into < 0f ? leftover - blocking.normal * into : leftover;
                if (underside) remaining.y = Mathf.Min(0f, remaining.y);
            }

            desired = position + remaining;
            ResolveOverlaps(ref desired, ascending);
        }

        bool ShouldBlock(Collider other, Vector3 normal, bool ascending)
        {
            if (other == null || other.transform.IsChildOf(transform)) return false;
            var tag = other.GetComponentInParent<SpawnableTag>();
            if (tag != null && tag.Kind == SpawnableKind.BouncePad) return false;
            if (IsLethalHazard(other)) return true;
            if (!IsLandable(other)) return false;

            // Downward contact with a top face remains owned by the established
            // foot-point landing raycast; undersides and sides are solid.
            if (normal.y > 0.45f) return false;
            return true;
        }

        bool IsLethalHazard(Collider other)
        {
            if (other == null) return false;
            var tag = other.GetComponentInParent<SpawnableTag>();
            if (tag == null || tag.Kind == SpawnableKind.BouncePad) return false;
            if (tag.Kind == SpawnableKind.ClosingGate &&
                tag.TryGetComponent<ClosingGate>(out var gate) && !gate.BlocksPlayerNow(_currentLane))
                return false;
            return tag.Kind == SpawnableKind.Obstacle || tag.Kind == SpawnableKind.RotatingBar ||
                   tag.Kind == SpawnableKind.ClosingGate;
        }

        static bool IsLandable(Collider other)
        {
            if (other == null) return false;
            if (GameLayers.LandableLayer >= 0 && other.gameObject.layer == GameLayers.LandableLayer)
                return true;
            var tag = other.GetComponentInParent<SpawnableTag>();
            return tag != null && (tag.Kind == SpawnableKind.Platform ||
                                   tag.Kind == SpawnableKind.MovingPlatform ||
                                   tag.Kind == SpawnableKind.FinishPlatform);
        }

        void ResolveOverlaps(ref Vector3 position, bool ascending)
        {
            GetCapsule(position, out Vector3 top, out Vector3 bottom, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(top, bottom, radius, _overlaps,
                GameLayers.SolidMask, QueryTriggerInteraction.Collide);
            float skin = Mathf.Max(0.005f, _cfg.collisionSkin);

            for (int i = 0; i < count; i++)
            {
                var other = _overlaps[i];
                if (other == null || other.transform.IsChildOf(transform)) continue;
                var tag = other.GetComponentInParent<SpawnableTag>();
                if (tag != null && tag.Kind == SpawnableKind.BouncePad) continue;

                if (!Physics.ComputePenetration(_motionEnvelope, position, transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float distance)) continue;

                bool lethal = IsLethalHazard(other);
                bool landable = IsLandable(other);
                bool topContact = direction.y > 0.45f;
                // The support/landing ray owns every top-face contact, including
                // the first ascending frame of a jump from that same platform.
                // Depenetrating that contact would pin the capsule to its support.
                if (landable && topContact) continue;
                if (!lethal && !landable) continue;

                position += direction * (distance + skin);
                if (direction.y < -0.35f && ascending) RegisterHeadHit(ClosestContact(other, position));
                if (Mathf.Abs(direction.x) > 0.35f) _laneXVel = 0f;

                if (lethal)
                {
                    _collisions?.HandlePredictiveHazard(other, ClosestContact(other, position));
                    return;
                }
            }
        }

        Vector3 ClosestContact(Collider other, Vector3 rootPosition)
        {
            return other != null ? other.ClosestPoint(rootPosition + Vector3.up * _cfg.collisionCenterY)
                                 : rootPosition;
        }

        void RegisterHeadHit(Vector3 point)
        {
            if (_vy <= 0f) return;
            _vy = -Mathf.Max(0f, _cfg.headHitDownVelocity);
            _airborne = true;
            _feedback?.HeadHit(point);
        }

        void GetCapsule(Vector3 rootPosition, out Vector3 top, out Vector3 bottom, out float radius)
        {
            radius = Mathf.Max(0.05f, _cfg.collisionRadius);
            float height = Mathf.Max(_cfg.collisionHeight, radius * 2f);
            float halfLine = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 center = rootPosition + Vector3.up * _cfg.collisionCenterY;
            top = center + Vector3.up * halfLine;
            bottom = center - Vector3.up * halfLine;
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

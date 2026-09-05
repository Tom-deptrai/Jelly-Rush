using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Player;
using JellyRush.Spawnables;
using JellyRush.Spawning;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Debugging
{
    /// <summary>
    /// DEBUG-ONLY Auto Test bot (round 5). When <see cref="Active"/> it plays the
    /// level hands-free so a level can be watched end to end without a mouse.
    ///
    /// It is NOT a smart AI and it does NOT cheat: it looks a short way ahead,
    /// picks the next landable platform, and drives the pair through the SAME
    /// <see cref="SwipeTapInput.Simulate"/> path a human tap/swipe uses - so all
    /// jump / lane / 4-beat / gravity rules apply exactly. No teleporting, no
    /// passing through obstacles. It can still fail; that is fine.
    ///
    /// Gated by <see cref="PrototypeConfig.enableDebugAutoTest"/> (the button is
    /// only built when that is true).
    /// </summary>
    public class AutoPlayBot : MonoBehaviour
    {
        PrototypeConfig _cfg;
        GameManager _game;
        PlayerController _player;
        SwipeTapInput _input;
        Spawner _spawner;
        LaneSystem _lanes;
        HeightGrid _heights;
        WorldScroller _world;

        bool _active;
        float _decisionTimer;
        const float DecisionInterval = 0.09f;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                if (_input != null) _input.SuppressUserInput = value;
            }
        }

        public void Configure(PrototypeConfig cfg, GameManager game, PlayerController player,
                              SwipeTapInput input, Spawner spawner, LaneSystem lanes,
                              HeightGrid heights, WorldScroller world)
        {
            _cfg = cfg;
            _game = game;
            _player = player;
            _input = input;
            _spawner = spawner;
            _lanes = lanes;
            _heights = heights;
            _world = world;
        }

        void Update()
        {
            if (!_active || _cfg == null) return;

            var st = _game.State;
            if (st == GameState.Warmup) { _input.Simulate(GestureType.Tap); return; }
            if (st != GameState.Running) return;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = DecisionInterval;

            Decide();
        }

        void Decide()
        {
            float pz = _cfg.playerZ;
            int lane = _player.TargetLane;
            bool grounded = _player.Grounded;
            float py = _player.Y;
            float vy = _player.VerticalVelocity;
            float supY = _player.SupportY;

            // --- scan the pipeline ---------------------------------------
            GameObject next = null;
            float nextFrontZ = float.MaxValue, nextTopY = 0f;
            int nextLane = LaneSystem.Center;
            float curBackZ = float.MinValue;
            bool gateBlocks = false;

            foreach (var go in _spawner.LiveObjects)
            {
                if (go == null || !go.activeSelf) continue;
                var tag = go.GetComponent<SpawnableTag>();
                if (tag == null) continue;

                var col = go.GetComponentInChildren<Collider>();
                if (col == null) continue;
                var b = col.bounds;
                float frontZ = b.center.z - b.extents.z;
                float backZ = b.center.z + b.extents.z;
                int goLane = LaneFromX(b.center.x);

                switch (tag.Kind)
                {
                    case SpawnableKind.Platform:
                    case SpawnableKind.MovingPlatform:
                    case SpawnableKind.BouncePad:
                    case SpawnableKind.FinishPlatform:
                        // platform we are currently standing on
                        if (grounded && goLane == lane &&
                            frontZ <= pz + 0.4f && backZ >= pz - 0.4f &&
                            Mathf.Abs(b.max.y - supY) < 1.1f)
                            curBackZ = Mathf.Max(curBackZ, backZ);

                        // the next platform strictly ahead
                        if (frontZ > pz + 0.2f && frontZ < nextFrontZ)
                        {
                            next = go; nextFrontZ = frontZ; nextTopY = b.max.y; nextLane = goLane;
                        }
                        break;

                    case SpawnableKind.ClosingGate:
                        if (frontZ > pz - 1f && frontZ - pz < 7f &&
                            go.TryGetComponent<ClosingGate>(out var gate) && gate.BlocksPlayerNow(lane))
                            gateBlocks = true;
                        break;
                }
            }

            if (next == null) return; // nothing to aim for yet

            float dz = nextFrontZ - pz;

            // --- 1. get into the target lane ---------------------------------
            if (lane != nextLane && dz < 13f)
            {
                _input.Simulate(nextLane < lane ? GestureType.SwipeLeft : GestureType.SwipeRight);
                return;
            }
            if (lane != nextLane) return; // too far to commit the lane change yet

            // --- 2. same lane: jump timing / climb -------------------------
            float climb = nextTopY - supY;
            float jumpTriggerZ = _world.CurrentSpeed * 0.6f + 2.6f + Mathf.Max(0f, climb) * 0.8f;

            if (grounded)
            {
                if (gateBlocks) return; // wait for the gate to open
                bool aboutToFall = (curBackZ - pz) < 2.2f;
                bool inRange = dz <= jumpTriggerZ;
                if (aboutToFall || inRange)
                    _input.Simulate(GestureType.Tap);
            }
            else
            {
                // climb assist: still below the target and no longer rising hard
                if (py < nextTopY - 0.15f && vy < 2.0f && _player.BeatsLeft > 0)
                    _input.Simulate(GestureType.Tap);
            }
        }

        int LaneFromX(float x)
        {
            float spacing = Mathf.Max(0.1f, _lanes.LaneSpacing);
            return _lanes.ClampLane(Mathf.RoundToInt(x / spacing) + LaneSystem.Center);
        }
    }
}

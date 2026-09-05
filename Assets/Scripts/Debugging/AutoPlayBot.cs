using System.Collections.Generic;
using JellyRush.Core;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Level;
using JellyRush.Player;
using JellyRush.Spawnables;
using JellyRush.Spawning;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Debugging
{
    /// <summary>
    /// DEBUG-ONLY Auto Test bot (round 6). It FOLLOWS THE AUTHORED ROUTE
    /// (<see cref="LevelRoute"/>) - it knows the intended platform sequence, so it
    /// never has to guess. For each route step it: aligns the lane, times a jump so
    /// the descent meets the platform as it arrives, adds climb beats if the tier
    /// is higher, and waits out a closing gate.
    ///
    /// It does NOT cheat: every move goes through <see cref="SwipeTapInput.Simulate"/>
    /// so the real jump / lane / 4-beat / gravity / landing rules all apply. No
    /// teleport, no direct position set, no fake beats, no phasing obstacles.
    ///
    /// On Fail it logs the exact route step + player state (never silent).
    /// Gated by <see cref="PrototypeConfig.enableDebugAutoTest"/>.
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
        List<RouteStep> _route;

        bool _active;
        bool _loggedFail;
        float _decisionTimer;
        int _routeIndex;
        const float DecisionInterval = 0.06f;

        public int RouteIndex => _routeIndex;
        public int RouteLength => _route != null ? _route.Count : 0;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                if (_input != null) _input.SuppressUserInput = value;
                if (value) _loggedFail = false;
            }
        }

        public void Configure(PrototypeConfig cfg, GameManager game, PlayerController player,
                              SwipeTapInput input, Spawner spawner, LaneSystem lanes,
                              HeightGrid heights, WorldScroller world, List<RouteStep> route)
        {
            _cfg = cfg;
            _game = game;
            _player = player;
            _input = input;
            _spawner = spawner;
            _lanes = lanes;
            _heights = heights;
            _world = world;
            _route = route;
        }

        void Update()
        {
            if (!_active || _cfg == null || _route == null) return;

            var st = _game.State;
            if (st == GameState.Warmup) { _input.Simulate(GestureType.Tap); return; }
            if (st == GameState.Failed)
            {
                if (!_loggedFail) { LogFail(); _loggedFail = true; }
                return;
            }
            if (st != GameState.Running) return;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = DecisionInterval;

            Decide();
        }

        void Decide()
        {
            if (_routeIndex >= _route.Count) return;

            RouteStep t = _route[_routeIndex];
            float d = _world.DistanceTravelled;
            float speed = Mathf.Max(3f, _world.CurrentSpeed);
            float dz = t.arriveDistance - d;                 // world-Z of the target platform centre
            int lane = _player.TargetLane;
            bool grounded = _player.Grounded;
            float py = _player.Y;
            float vy = _player.VerticalVelocity;
            float supY = _player.SupportY;
            float tierY = _heights.TierToY(t.tier);
            float climb = tierY - supY;

            // --- advance the route index once we are on / past this target ------
            bool onTarget = grounded && Mathf.Abs(supY - tierY) < 0.45f
                            && dz < t.widthZ * 0.5f + 0.7f && dz > -t.widthZ * 0.5f - 2.5f;
            bool passed = dz < -t.widthZ * 0.5f - 2f;
            if (onTarget || passed)
            {
                _routeIndex++;
                return;
            }

            bool gateWait = GateBlocking(lane, dz);

            // --- 1. lane alignment -------------------------------------------
            if (lane != t.lane)
            {
                float commitZ = t.widthZ * 0.5f + speed * (climb < -0.4f ? 0.55f : 0.85f) + 2.5f;
                if (dz < commitZ && !gateWait)
                    _input.Simulate(t.lane < lane ? GestureType.SwipeLeft : GestureType.SwipeRight);
                return;
            }

            // --- 2. jump timing + climb -------------------------------------
            float g = 8f * Mathf.Max(0.01f, _cfg.jumpHeight) / (_cfg.jumpDuration * _cfg.jumpDuration);
            float v0 = g * _cfg.jumpDuration * 0.5f;
            float airtime;
            if (climb <= 0.1f)
            {
                float drop = -climb;
                airtime = (v0 + Mathf.Sqrt(v0 * v0 + 2f * g * drop)) / g;
            }
            else
            {
                airtime = _cfg.jumpDuration * (1f + climb / Mathf.Max(0.5f, _cfg.airChainCeiling) * 1.3f);
            }
            float jumpLeadZ = speed * airtime * 0.92f + t.widthZ * 0.28f + 0.7f;

            if (grounded)
            {
                if (gateWait) return;
                bool aboutToFall = PrevTrailingDz(d) < 1.5f;
                if (dz <= jumpLeadZ || aboutToFall)
                    _input.Simulate(GestureType.Tap);
            }
            else
            {
                if (lane != t.lane && _player.BeatsLeft > 0)
                    _input.Simulate(t.lane < lane ? GestureType.SwipeLeft : GestureType.SwipeRight);
                else if (py < tierY - 0.02f && vy < 2.2f && _player.BeatsLeft > 0)
                    _input.Simulate(GestureType.Tap);   // need more height to reach the tier
            }
        }

        float PrevTrailingDz(float d)
        {
            if (_routeIndex == 0)
                return (_cfg.startPlatformZ - 6f) - d;   // trailing edge of the start platform
            RouteStep p = _route[_routeIndex - 1];
            return (p.arriveDistance + p.widthZ * 0.5f) - d;
        }

        bool GateBlocking(int lane, float dzToTarget)
        {
            var live = _spawner.LiveObjects;
            for (int i = 0; i < live.Count; i++)
            {
                var go = live[i];
                if (go == null || !go.activeSelf) continue;
                var tag = go.GetComponent<SpawnableTag>();
                if (tag == null || tag.Kind != SpawnableKind.ClosingGate) continue;

                float gz = go.transform.position.z;
                if (gz > -1.5f && gz < 9f && gz < dzToTarget + 4f &&
                    go.TryGetComponent<ClosingGate>(out var gate) && gate.BlocksPlayerNow(lane))
                    return true;
            }
            return false;
        }

        void LogFail()
        {
            string target = _routeIndex < _route.Count ? _route[_routeIndex].ToString() : "(past end)";
            Debug.LogWarning(
                $"[AutoTest FAIL] routeStep {_routeIndex}/{_route.Count}  target={{{target}}}\n" +
                $"  player: lane={_player.CurrentLane} grounded={_player.Grounded} y={_player.Y:F2} " +
                $"vy={_player.VerticalVelocity:F2} supportY={_player.SupportY:F2} beats={_player.BeatsLeft}\n" +
                $"  world: distance={_world.DistanceTravelled:F1} speed={_world.CurrentSpeed:F1}");
        }
    }
}

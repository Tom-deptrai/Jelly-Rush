using System.Collections.Generic;
using JellyRush.Core;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Spawning
{
    /// <summary>
    /// Round 2 test spawner: platform-to-platform, no continuous ground.
    ///
    /// Every slot drops exactly ONE landable platform (short / medium / long) in
    /// some lane, with gaps of open space between slots. A guaranteed long start
    /// platform sits under the player, and the visible pipeline is pre-filled at
    /// Configure() so platforms keep arriving from the depth (CAMERA spec 6) the
    /// moment the world starts scrolling. Light decoration (coin, side obstacle,
    /// bounce pad, rare rotating bar / closing gate) never removes the landable
    /// platform for that slot - no unfair situations.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        PrototypeConfig _cfg;
        GameManager _game;
        WorldScroller _world;
        LaneSystem _lanes;
        SpawnableFactory _factory;

        readonly List<GameObject> _live = new();
        readonly Dictionary<SpawnableKind, Stack<GameObject>> _pool = new();

        float _nextSpawnDistance;
        const float FirstSlotDistance = 9f;   // metres ahead where the first gap-platform sits

        public void Configure(PrototypeConfig cfg, GameManager game, WorldScroller world, LaneSystem lanes)
        {
            _cfg = cfg;
            _game = game;
            _world = world;
            _lanes = lanes;
            _factory = new SpawnableFactory(world.WorldRoot);

            SpawnStartPlatform();

            // Pre-fill the pipeline so there is always a platform to reach.
            _nextSpawnDistance = FirstSlotDistance;
            while (_nextSpawnDistance <= _cfg.spawnAheadDistance)
            {
                SpawnSlot(_nextSpawnDistance);
                _nextSpawnDistance += NextInterval();
            }
        }

        void Update()
        {
            if (_cfg == null) return;
            if (_game.State == GameState.Paused || _game.State == GameState.Failed) return;

            Recycle();
            if (_game.State == GameState.Warmup) return;

            while (_world.DistanceTravelled + _cfg.spawnAheadDistance >= _nextSpawnDistance)
            {
                SpawnSlot(_nextSpawnDistance);
                _nextSpawnDistance += NextInterval();
            }
        }

        float NextInterval() => Mathf.Max(3f,
            _cfg.spawnIntervalMeters + Random.Range(-_cfg.spawnIntervalJitter, _cfg.spawnIntervalJitter));

        /// <summary>localZ under the world root for something that reaches the player at DistanceTravelled == atDistance.</summary>
        float SpawnLocalZ(float atDistance) => _cfg.playerZ + atDistance;

        void SpawnStartPlatform()
        {
            var go = Rent(SpawnableKind.Platform);
            go.transform.SetParent(_world.WorldRoot, false);
            go.transform.localScale = new Vector3(2.0f, 0.5f, _cfg.startPlatformZ);
            // extends from just behind the player to a good way ahead
            float centreZ = _cfg.playerZ + _cfg.startPlatformZ * 0.5f - 6f;
            go.transform.localPosition = new Vector3(_lanes.LaneToX(LaneSystem.Center), -0.25f, centreZ);
            go.SetActive(true);
            _live.Add(go);
        }

        void SpawnSlot(float atDistance)
        {
            float z = SpawnLocalZ(atDistance);
            bool gentle = atDistance < FirstSlotDistance + 22f;

            int lane = gentle ? LaneSystem.Center : Random.Range(0, LaneSystem.LaneCount);

            float lengthZ;
            SpawnableKind platformKind = SpawnableKind.Platform;
            if (gentle)
            {
                lengthZ = _cfg.platformLongZ;
            }
            else
            {
                float r = Random.value;
                lengthZ = r < 0.4f ? _cfg.platformShortZ
                        : r < 0.8f ? _cfg.platformMediumZ
                        : _cfg.platformLongZ;
                if (Random.value < 0.12f) platformKind = SpawnableKind.MovingPlatform;
            }

            PlacePlatform(platformKind, lane, z, lengthZ);

            if (gentle) return;

            // --- light decoration on top of / beside the landable platform ---
            float d = Random.value;
            if (d < 0.35f)
                Place(SpawnableKind.Coin, lane, z, yOffset: 1.2f);
            else if (d < 0.50f)
                Place(SpawnableKind.BouncePad, lane, z, yOffset: 0.55f);
            else if (d < 0.62f)
            {
                int sideLane = (lane + 1 + Random.Range(0, 2)) % LaneSystem.LaneCount;
                if (sideLane != lane) Place(SpawnableKind.Obstacle, sideLane, z, yOffset: 0.2f);
            }
            else if (d < 0.68f)
                Place(SpawnableKind.RotatingBar, lane, z, yOffset: 1.7f);
            else if (d < 0.72f)
                Place(SpawnableKind.ClosingGate, lane, z);
        }

        void PlacePlatform(SpawnableKind kind, int lane, float localZ, float lengthZ)
        {
            var go = Rent(kind);
            go.transform.SetParent(_world.WorldRoot, false);
            go.transform.localScale = new Vector3(1.9f, 0.4f, lengthZ);
            go.transform.localPosition = new Vector3(_lanes.LaneToX(lane), -0.2f, localZ);
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            if (go.TryGetComponent<MovingPlatform>(out var mp))
                mp.Init(0.8f, 1.0f, Random.value * Mathf.PI * 2f);
            _live.Add(go);
        }

        void Place(SpawnableKind kind, int lane, float localZ, float yOffset = 0f)
        {
            var go = Rent(kind);
            go.transform.SetParent(_world.WorldRoot, false);
            go.transform.localPosition = new Vector3(_lanes.LaneToX(lane), 0.2f + yOffset, localZ);
            go.SetActive(true);
            _live.Add(go);
        }

        GameObject Rent(SpawnableKind kind)
        {
            if (_pool.TryGetValue(kind, out var stack) && stack.Count > 0)
                return stack.Pop();
            return _factory.Create(kind);
        }

        void Recycle()
        {
            float cutoff = _cfg.playerZ - _cfg.despawnBehindDistance;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var go = _live[i];
                if (go == null) { _live.RemoveAt(i); continue; }
                if (!go.activeSelf || go.transform.position.z < cutoff)
                {
                    _live.RemoveAt(i);
                    go.SetActive(false);
                    var kind = go.GetComponent<SpawnableTag>().Kind;
                    if (!_pool.TryGetValue(kind, out var stack))
                        _pool[kind] = stack = new Stack<GameObject>();
                    stack.Push(go);
                }
            }
        }
    }
}

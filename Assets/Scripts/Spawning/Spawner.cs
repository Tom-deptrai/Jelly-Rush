using System.Collections.Generic;
using JellyRush.Core;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Spawning
{
    /// <summary>
    /// Test spawner (CAMERA_AND_DEPTH_SPEC section 6). Every element is created far
    /// ahead of the player (small on screen) and parented under the world root, so
    /// it travels toward the camera and grows via perspective. Simple pooling keeps
    /// motion smooth under rapid play. Patterns are hand-picked and always leave at
    /// least one clear lane - no unfair situations.
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
        float _elapsed;

        public void Configure(PrototypeConfig cfg, GameManager game, WorldScroller world, LaneSystem lanes)
        {
            _cfg = cfg;
            _game = game;
            _world = world;
            _lanes = lanes;
            _factory = new SpawnableFactory(world.WorldRoot);
            _nextSpawnDistance = cfg.spawnAheadDistance;
        }

        void Update()
        {
            if (_cfg == null) return;
            if (_game.State == GameState.Paused || _game.State == GameState.Failed) return;

            _elapsed += Time.deltaTime;
            Recycle();

            if (_game.State == GameState.Warmup) return;

            while (_world.DistanceTravelled + LookAhead() >= _nextSpawnDistance)
            {
                SpawnSlot(_nextSpawnDistance);
                float interval = _cfg.spawnIntervalMeters +
                                 Random.Range(-_cfg.spawnIntervalJitter, _cfg.spawnIntervalJitter);
                _nextSpawnDistance += Mathf.Max(2.5f, interval);
            }
        }

        float LookAhead() => _cfg.spawnAheadDistance;

        /// <summary>
        /// Local Z under the world root for an element that should reach the player
        /// once DistanceTravelled == atDistance. worldRoot starts at z=0 and moves to
        /// z=-DistanceTravelled, so worldZ = localZ - DistanceTravelled and we want
        /// worldZ = playerZ + (atDistance - DistanceTravelled)  =>  localZ = playerZ + atDistance.
        /// </summary>
        float SpawnLocalZ(float atDistance) => _cfg.playerZ + atDistance;

        void SpawnSlot(float atDistance)
        {
            bool warmClear = _elapsed < _cfg.warmupSeconds;
            int pattern = warmClear ? 0 : Random.Range(0, 8);
            float z = SpawnLocalZ(atDistance);

            switch (pattern)
            {
                case 0: // plain landing row + a coin
                    Place(SpawnableKind.Platform, LaneSystem.Center, z);
                    Place(SpawnableKind.Coin, LaneSystem.Center, z, yOffset: 1.1f);
                    break;
                case 1: // offset platform left/right
                    {
                        int lane = Random.value < 0.5f ? LaneSystem.Left : LaneSystem.Right;
                        Place(SpawnableKind.Platform, lane, z);
                        Place(SpawnableKind.Coin, lane, z, yOffset: 1.1f);
                        break;
                    }
                case 2: // coin trail across the three lanes
                    Place(SpawnableKind.Coin, LaneSystem.Left, z, yOffset: 1.0f);
                    Place(SpawnableKind.Coin, LaneSystem.Center, z + 1.4f, yOffset: 1.0f);
                    Place(SpawnableKind.Coin, LaneSystem.Right, z + 2.8f, yOffset: 1.0f);
                    break;
                case 3: // obstacle blocking ONE lane, others clear
                    {
                        int block = Random.Range(0, 3);
                        Place(SpawnableKind.Obstacle, block, z);
                        int safe = (block + 1) % 3;
                        Place(SpawnableKind.Coin, safe, z, yOffset: 1.1f);
                        break;
                    }
                case 4: // moving platform
                    Place(SpawnableKind.MovingPlatform, LaneSystem.Center, z);
                    break;
                case 5: // rotating bar (sweep) - jump-timing test
                    Place(SpawnableKind.RotatingBar, LaneSystem.Center, z, yOffset: 0.5f);
                    break;
                case 6: // closing gate, center lane safe
                    Place(SpawnableKind.ClosingGate, LaneSystem.Center, z);
                    break;
                case 7: // bounce pad + coin reward high above
                    Place(SpawnableKind.BouncePad, LaneSystem.Center, z);
                    Place(SpawnableKind.Coin, LaneSystem.Center, z + 3f, yOffset: 2.4f);
                    break;
            }
        }

        void Place(SpawnableKind kind, int lane, float localZ, float yOffset = 0f)
        {
            var go = Rent(kind);
            go.transform.SetParent(_world.WorldRoot, false);
            float y = kind == SpawnableKind.Platform || kind == SpawnableKind.MovingPlatform || kind == SpawnableKind.BouncePad
                ? -0.2f + yOffset
                : 0.2f + yOffset;
            go.transform.localPosition = new Vector3(_lanes.LaneToX(lane), y, localZ);
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

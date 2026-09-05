using System.Collections.Generic;
using JellyRush.Core;
using JellyRush.Lanes;
using JellyRush.Level;
using JellyRush.Spawnables;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Spawning
{
    /// <summary>
    /// Round 5: streams the active <see cref="LevelData"/> - its ChallengeSegments
    /// once, in order, then (after a clear runway) a single Finish Platform. No
    /// looping. Everything is emitted far ahead (CAMERA spec 6) under the world
    /// root. Platforms carry a lane (X) and a tier (Y) from <see cref="HeightGrid"/>.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        PrototypeConfig _cfg;
        GameManager _game;
        WorldScroller _world;
        LaneSystem _lanes;
        HeightGrid _heights;
        LevelData _level;
        SpawnableFactory _factory;

        readonly List<GameObject> _live = new();
        readonly Dictionary<SpawnableKind, Stack<GameObject>> _pool = new();

        int _segIndex;
        float _nextSegmentDistance;   // travel distance at which the next segment's START reaches the player
        float _finishDistance;
        bool _segmentsDone;
        bool _finishSpawned;

        /// <summary>Live gameplay objects, for the Auto Test bot to read.</summary>
        public IReadOnlyList<GameObject> LiveObjects => _live;
        public bool FinishSpawned => _finishSpawned;

        public void Configure(PrototypeConfig cfg, GameManager game, WorldScroller world,
                              LaneSystem lanes, HeightGrid heights, WorldThemeData theme, LevelData level)
        {
            _cfg = cfg;
            _game = game;
            _world = world;
            _lanes = lanes;
            _heights = heights;
            _level = level;
            _factory = new SpawnableFactory(world.WorldRoot, theme);

            SpawnStartPlatform();

            _segIndex = 0;
            _nextSegmentDistance = 6f;
            // Pre-fill the visible pipeline.
            while (!_segmentsDone && _nextSegmentDistance <= _cfg.spawnAheadDistance)
                EmitNextSegment();
        }

        void Update()
        {
            if (_cfg == null) return;
            var st = _game.State;
            if (st == GameState.Paused || st == GameState.Failed || st == GameState.Completed) return;

            Recycle();
            if (st == GameState.Warmup) return;

            float reach = _world.DistanceTravelled + _cfg.spawnAheadDistance;

            while (!_segmentsDone && reach >= _nextSegmentDistance)
                EmitNextSegment();

            if (_segmentsDone && !_finishSpawned && reach >= _finishDistance)
                SpawnFinish();
        }

        void EmitNextSegment()
        {
            var seg = _level.segments[_segIndex];
            EmitSegment(seg, _nextSegmentDistance);
            _nextSegmentDistance += Mathf.Max(6f, seg.length);
            _segIndex++;

            if (_segIndex >= _level.segments.Count)
            {
                _segmentsDone = true;
                _finishDistance = _nextSegmentDistance + Mathf.Max(6f, _level.finishRunwayZ);
            }
        }

        void EmitSegment(ChallengeSegment seg, float atDistance)
        {
            foreach (var p in seg.platforms) PlacePlatform(p, atDistance);
            foreach (var d in seg.decos) PlaceDeco(d, atDistance);
        }

        // --- placement -----------------------------------------------------

        float LocalZ(float distance) => _cfg.playerZ + distance;

        float LengthZ(PlatformLength len) => len switch
        {
            PlatformLength.Short => _cfg.platformShortZ,
            PlatformLength.Long => _cfg.platformLongZ,
            _ => _cfg.platformMediumZ,
        };

        void SpawnStartPlatform()
        {
            var go = Rent(SpawnableKind.Platform);
            go.transform.SetParent(_world.WorldRoot, false);
            go.transform.localScale = new Vector3(2.0f, 0.5f, _cfg.startPlatformZ);
            float top = _cfg.startHeight;
            float centreZ = _cfg.playerZ + _cfg.startPlatformZ * 0.5f - 6f;
            go.transform.localPosition = new Vector3(_lanes.LaneToX(LaneSystem.Center), top - 0.25f, centreZ);
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            _live.Add(go);
        }

        void SpawnFinish()
        {
            _finishSpawned = true;
            var go = Rent(SpawnableKind.FinishPlatform);
            go.transform.SetParent(_world.WorldRoot, false);
            go.transform.localScale = Vector3.one;
            float x = _lanes.LaneToX(_lanes.ClampLane(_level.finishLane));
            float y = _heights.TierToY(_level.finishTier);   // pad top sits exactly at the tier
            go.transform.localPosition = new Vector3(x, y, LocalZ(_finishDistance));
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            _live.Add(go);
        }

        void PlacePlatform(PlatformStep s, float atDistance)
        {
            var go = Rent(s.kind);
            go.transform.SetParent(_world.WorldRoot, false);
            float x = _lanes.LaneToX(_lanes.ClampLane(s.lane));
            float topY = _heights.TierToY(s.tier);
            float z = LocalZ(atDistance + s.z);

            if (s.kind == SpawnableKind.BouncePad)
            {
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = new Vector3(x, topY + 0.15f, z);
            }
            else
            {
                go.transform.localScale = new Vector3(1.9f, 0.4f, LengthZ(s.length));
                go.transform.localPosition = new Vector3(x, topY - 0.2f, z);
            }
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);

            if (go.TryGetComponent<MovingPlatform>(out var mp))
                mp.Init(0.8f, 1.0f, Random.value * Mathf.PI * 2f);

            _live.Add(go);
        }

        void PlaceDeco(DecoStep s, float atDistance)
        {
            var go = Rent(s.kind);
            go.transform.SetParent(_world.WorldRoot, false);
            float x = _lanes.LaneToX(_lanes.ClampLane(s.lane));
            float y = _heights.TierToY(s.tier) + s.yOffset + DecoBaseY(s.kind);
            go.transform.localPosition = new Vector3(x, y, LocalZ(atDistance + s.z));
            go.SetActive(true);
            _live.Add(go);
        }

        static float DecoBaseY(SpawnableKind kind) => kind switch
        {
            SpawnableKind.Coin => 0.45f,
            SpawnableKind.Obstacle => 0.75f,
            _ => 0f,
        };

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

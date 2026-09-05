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
    /// Round 3: streams a level made of hand-authored <see cref="ChallengeSegment"/>s
    /// instead of random slots. Each segment's platforms / decos are emitted far
    /// ahead (CAMERA spec 6) and parented under the world root so they travel toward
    /// the camera. Platforms carry a lane (X) AND a tier (Y) from <see cref="HeightGrid"/>.
    /// A guaranteed start platform sits under the player and the visible pipeline is
    /// pre-filled so there is always a reachable platform. The level loops (from the
    /// segment after Start) so playtests keep going.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        PrototypeConfig _cfg;
        GameManager _game;
        WorldScroller _world;
        LaneSystem _lanes;
        HeightGrid _heights;
        SpawnableFactory _factory;

        readonly List<GameObject> _live = new();
        readonly Dictionary<SpawnableKind, Stack<GameObject>> _pool = new();

        List<ChallengeSegment> _level;
        int _segIndex;
        float _nextSegmentDistance;      // travel distance at which the next segment's START reaches the player
        const int LoopFromIndex = 1;     // skip the intro when looping

        public void Configure(PrototypeConfig cfg, GameManager game, WorldScroller world,
                              LaneSystem lanes, HeightGrid heights, WorldThemeData theme)
        {
            _cfg = cfg;
            _game = game;
            _world = world;
            _lanes = lanes;
            _heights = heights;
            _factory = new SpawnableFactory(world.WorldRoot, theme);
            _level = SegmentLibrary.PrototypeLevel();

            SpawnStartPlatform();

            // Pre-fill the pipeline: keep emitting whole segments until the front of
            // the queue is past the spawn-ahead distance.
            _segIndex = 0;
            _nextSegmentDistance = 6f;
            while (_nextSegmentDistance <= _cfg.spawnAheadDistance)
                EmitNextSegment();
        }

        void Update()
        {
            if (_cfg == null) return;
            if (_game.State == GameState.Paused || _game.State == GameState.Failed) return;

            Recycle();
            if (_game.State == GameState.Warmup) return;

            while (_world.DistanceTravelled + _cfg.spawnAheadDistance >= _nextSegmentDistance)
                EmitNextSegment();
        }

        void EmitNextSegment()
        {
            var seg = _level[_segIndex];
            EmitSegment(seg, _nextSegmentDistance);
            _nextSegmentDistance += Mathf.Max(6f, seg.length);

            _segIndex++;
            if (_segIndex >= _level.Count)
                _segIndex = Mathf.Min(LoopFromIndex, _level.Count - 1);
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
            SpawnableKind.Coin => 0.45f,       // floats above the surface
            SpawnableKind.Obstacle => 0.75f,   // base sits on the tier
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

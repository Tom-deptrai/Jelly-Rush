using System.Collections.Generic;
using JellyRush.Core;
using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Level
{
    /// <summary>One platform the Auto Test bot must reach, in order.</summary>
    public struct RouteStep
    {
        public int lane;
        public int tier;
        public float arriveDistance;   // world DistanceTravelled at which this platform's CENTRE sits at playerZ
        public float widthZ;
        public SpawnableKind kind;
        public bool isFinish;

        public override string ToString() =>
            $"lane={lane} tier={tier} arriveD={arriveDistance:F1} w={widthZ:F1} {kind}{(isFinish ? " FINISH" : "")}";
    }

    /// <summary>
    /// The intended, valid path through a <see cref="LevelData"/>, derived from the
    /// same segment accumulation the spawner uses. The Auto Test bot follows this
    /// instead of guessing - it "knows the answer" because it is a debug tool.
    /// </summary>
    public static class LevelRoute
    {
        /// <summary>Must match Spawner._nextSegmentDistance initial value.</summary>
        public const float FirstSegmentDistance = 6f;

        public static List<RouteStep> Build(LevelData level, PrototypeConfig cfg)
        {
            var steps = new List<RouteStep>();
            float d = FirstSegmentDistance;

            foreach (var seg in level.segments)
            {
                var onRoute = new List<PlatformStep>();
                foreach (var p in seg.platforms)
                    if (!p.offRoute) onRoute.Add(p);
                onRoute.Sort((a, b) => a.z.CompareTo(b.z));

                foreach (var p in onRoute)
                    steps.Add(new RouteStep
                    {
                        lane = Mathf.Clamp(p.lane, 0, 2),
                        tier = p.tier,
                        arriveDistance = d + p.z,
                        widthZ = Width(cfg, p.length),
                        kind = p.kind,
                        isFinish = false,
                    });

                d += Mathf.Max(6f, seg.length);
            }

            d += Mathf.Max(6f, level.finishRunwayZ);
            steps.Add(new RouteStep
            {
                lane = Mathf.Clamp(level.finishLane, 0, 2),
                tier = level.finishTier,
                arriveDistance = d,
                widthZ = 13f,
                kind = SpawnableKind.FinishPlatform,
                isFinish = true,
            });

            return steps;
        }

        static float Width(PrototypeConfig c, PlatformLength len) => len switch
        {
            PlatformLength.Short => c.platformShortZ,
            PlatformLength.Long => c.platformLongZ,
            _ => c.platformMediumZ,
        };
    }
}

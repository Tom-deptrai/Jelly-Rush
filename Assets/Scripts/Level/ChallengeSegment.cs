using System;
using System.Collections.Generic;
using JellyRush.Spawnables;

namespace JellyRush.Level
{
    public enum PlatformLength { Short, Medium, Long }

    /// <summary>One platform inside a <see cref="ChallengeSegment"/>.</summary>
    [Serializable]
    public struct PlatformStep
    {
        public int lane;                 // 0 left / 1 center / 2 right
        public int tier;                 // HeightGrid tier (0 Low / 1 Mid / 2 High / ...)
        public PlatformLength length;
        public SpawnableKind kind;       // Platform / MovingPlatform / BouncePad
        public float z;                  // metres from the segment start

        public PlatformStep(int lane, int tier, PlatformLength length, float z,
                            SpawnableKind kind = SpawnableKind.Platform)
        {
            this.lane = lane; this.tier = tier; this.length = length;
            this.z = z; this.kind = kind;
        }
    }

    /// <summary>One collectible / hazard inside a <see cref="ChallengeSegment"/>.</summary>
    [Serializable]
    public struct DecoStep
    {
        public SpawnableKind kind;       // Coin / Obstacle / RotatingBar / ClosingGate
        public int lane;
        public int tier;
        public float z;
        public float yOffset;            // extra Y above the tier (mid-air hazards / coins)

        public DecoStep(SpawnableKind kind, int lane, int tier, float z, float yOffset = 0f)
        {
            this.kind = kind; this.lane = lane; this.tier = tier;
            this.z = z; this.yOffset = yOffset;
        }
    }

    /// <summary>
    /// A hand-authored chunk of level with ONE clear idea (lane change, climb,
    /// short run, rotating obstacle, drop, coin run, ...). A level is a sequence of
    /// these. Internally guaranteed to have a valid path within the 4-beat budget.
    /// Plain serializable data now; can be promoted to a ScriptableObject later.
    /// </summary>
    [Serializable]
    public class ChallengeSegment
    {
        public string name = "Segment";
        public float length = 14f;       // metres this segment occupies along Z
        public List<PlatformStep> platforms = new();
        public List<DecoStep> decos = new();

        public ChallengeSegment(string name, float length)
        {
            this.name = name;
            this.length = length;
        }

        public ChallengeSegment Platform(int lane, int tier, PlatformLength len, float z,
                                         SpawnableKind kind = SpawnableKind.Platform)
        {
            platforms.Add(new PlatformStep(lane, tier, len, z, kind));
            return this;
        }

        public ChallengeSegment Deco(SpawnableKind kind, int lane, int tier, float z, float yOffset = 0f)
        {
            decos.Add(new DecoStep(kind, lane, tier, z, yOffset));
            return this;
        }
    }
}

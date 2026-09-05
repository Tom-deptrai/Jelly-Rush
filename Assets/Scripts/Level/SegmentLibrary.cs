using System.Collections.Generic;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using static JellyRush.Level.PlatformLength;

namespace JellyRush.Level
{
    /// <summary>
    /// Hand-authored <see cref="ChallengeSegment"/>s (round 3). Each has ONE idea and
    /// a valid path within the 4-beat budget. <see cref="PrototypeLevel"/> stitches
    /// them into a level; the spawner streams them and loops the middle.
    ///
    /// Lanes:  L = left, C = center, R = right
    /// Tiers:  0 = Low, 1 = Mid, 2 = High
    /// Convention: first platform at z ~ 2, platforms ~7 m apart, segment length =
    /// last platform z + 5. Every segment starts and ends at Center so any two
    /// chain with a comfortable ~7 m gap.
    /// </summary>
    public static class SegmentLibrary
    {
        const int L = LaneSystem.Left, C = LaneSystem.Center, R = LaneSystem.Right;
        const int Lo = HeightGrid.Low, Mi = HeightGrid.Mid, Hi = HeightGrid.High;

        static ChallengeSegment Seg(string name, float length) => new(name, length);

        // --- individual challenge ideas ----------------------------------------

        public static ChallengeSegment Start() => Seg("Start", 21f)
            .Platform(C, Lo, Long, 2f)
            .Platform(C, Lo, Long, 14f);

        public static ChallengeSegment ShortRun() => Seg("Short Run", 26f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(C, Lo, Medium, 9f, SpawnableKind.MovingPlatform)
            .Platform(C, Lo, Medium, 16f)
            .Platform(C, Lo, Medium, 21f)
            .Deco(SpawnableKind.Coin, C, Lo, 9f, 1.1f);

        public static ChallengeSegment LaneChange() => Seg("Lane Change", 25f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(R, Lo, Medium, 10f)
            .Platform(L, Lo, Medium, 17f)
            .Platform(C, Lo, Medium, 20f)
            .Deco(SpawnableKind.Coin, R, Lo, 10f, 1.1f)
            .Deco(SpawnableKind.Coin, L, Lo, 17f, 1.1f);

        public static ChallengeSegment Climb() => Seg("Climb", 26f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(C, Mi, Medium, 9f)
            .Platform(C, Hi, Medium, 15f)
            .Platform(C, Lo, Long, 21f)               // step back down so the next segment chains
            .Deco(SpawnableKind.Coin, C, Lo, 5f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Mi, 12f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Hi, 17f, 1.1f);

        public static ChallengeSegment Drop() => Seg("Drop", 25f)
            .Platform(C, Lo, Short, 2f)
            .Platform(C, Mi, Short, 8f)
            .Platform(C, Hi, Medium, 13f)
            .Platform(C, Lo, Long, 20f)               // the drop
            .Deco(SpawnableKind.Coin, C, Mi, 17f, 0.9f);

        public static ChallengeSegment RotatingObstacle() => Seg("Rotating Obstacle", 24f)
            .Platform(C, Lo, Long, 2f)
            .Platform(C, Lo, Medium, 19f)
            .Deco(SpawnableKind.RotatingBar, C, Lo, 9f, 2.3f);   // overhead - only clips a mistimed jump

        public static ChallengeSegment LowObstacle() => Seg("Low Obstacle", 23f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(L, Lo, Medium, 10f)
            .Platform(R, Lo, Medium, 10f)
            .Platform(C, Lo, Medium, 18f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 10f, 0f);       // blocks center -> go around

        public static ChallengeSegment Gate() => Seg("Closing Gate", 25f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(C, Lo, Long, 11f)
            .Platform(C, Lo, Medium, 20f)
            .Deco(SpawnableKind.ClosingGate, C, Lo, 11f, 0f);    // opens / closes on a cycle - time it

        public static ChallengeSegment CoinArcUp() => Seg("Coin Arc Up", 21f)
            .Platform(C, Lo, Medium, 2f)
            .Platform(C, Mi, Long, 12f)
            .Platform(C, Lo, Medium, 16f)
            .Deco(SpawnableKind.Coin, C, Lo, 5f, 0.9f)
            .Deco(SpawnableKind.Coin, C, Lo, 7f, 1.6f)
            .Deco(SpawnableKind.Coin, C, Mi, 9f, 1.4f)
            .Deco(SpawnableKind.Coin, C, Mi, 11f, 0.9f);

        public static ChallengeSegment CoinArcDown() => Seg("Coin Arc Down", 21f)
            .Platform(C, Mi, Medium, 2f)
            .Platform(C, Lo, Long, 12f)
            .Platform(C, Lo, Medium, 16f)
            .Deco(SpawnableKind.Coin, C, Mi, 5f, 0.9f)
            .Deco(SpawnableKind.Coin, C, Mi, 7f, 1.5f)
            .Deco(SpawnableKind.Coin, C, Lo, 9f, 1.4f)
            .Deco(SpawnableKind.Coin, C, Lo, 11f, 0.8f);

        public static ChallengeSegment BounceUp() => Seg("Bounce Up", 24f)
            .Platform(C, Lo, Medium, 2f, SpawnableKind.BouncePad)
            .Platform(C, Hi, Medium, 11f)
            .Platform(C, Lo, Long, 19f)
            .Deco(SpawnableKind.Coin, C, Mi, 7f, 1.0f);

        public static ChallengeSegment Finish() => Seg("Finish", 21f)
            .Platform(C, Lo, Long, 2f)
            .Platform(C, Lo, Long, 14f);

        // --- level assembly --------------------------------------------------

        /// <summary>First element is the intro; the spawner loops from index 1 after the last.</summary>
        public static List<ChallengeSegment> PrototypeLevel() => new()
        {
            Start(),
            ShortRun(),
            LaneChange(),
            Climb(),
            CoinArcDown(),
            LowObstacle(),
            RotatingObstacle(),
            BounceUp(),
            Drop(),
            Gate(),
            CoinArcUp(),
            Finish(),
        };
    }
}

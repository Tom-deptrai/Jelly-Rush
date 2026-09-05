using System.Collections.Generic;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using static JellyRush.Level.PlatformLength;

namespace JellyRush.Level
{
    /// <summary>
    /// Hand-authored <see cref="ChallengeSegment"/>s (round 5). Each has ONE clear
    /// idea and a valid path within the 4-beat budget. <see cref="LevelData"/>
    /// chooses which ones and in what order; the spawner streams them once, then a
    /// Finish Platform.
    ///
    /// Lanes:  L left / C center / R right      Tiers:  0 Low / 1 Mid / 2 High
    /// Convention: first platform ~z 3, platforms ~9-11 m apart, last platform near
    /// (length - 5). Every segment starts and ends at Center / Low so any two chain
    /// with a comfortable gap.
    /// </summary>
    public static class SegmentLibrary
    {
        const int L = LaneSystem.Left, C = LaneSystem.Center, R = LaneSystem.Right;
        const int Lo = HeightGrid.Low, Mi = HeightGrid.Mid, Hi = HeightGrid.High;

        static ChallengeSegment Seg(string name, float length) => new(name, length);

        // -------------------------------------------------------------------
        // 0. intro - plain center runway, ~5 seconds to find the rhythm
        public static ChallengeSegment Intro() => Seg("Intro", 42f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Lo, Long, 15f)
            .Platform(C, Lo, Long, 27f)
            .Platform(C, Lo, Long, 38f);

        // 1. same-tier timing hops
        public static ChallengeSegment ShortRun() => Seg("Short Run", 46f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Lo, Medium, 12f)
            .Platform(C, Lo, Medium, 21f, SpawnableKind.MovingPlatform)
            .Platform(C, Lo, Medium, 30f)
            .Platform(C, Lo, Medium, 39f)
            .Deco(SpawnableKind.Coin, C, Lo, 21f, 1.1f);

        // 2. weave left / right
        public static ChallengeSegment LaneChange() => Seg("Lane Change", 48f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(R, Lo, Medium, 13f)
            .Platform(C, Lo, Medium, 23f)
            .Platform(L, Lo, Medium, 33f)
            .Platform(C, Lo, Medium, 43f)
            .Deco(SpawnableKind.Coin, R, Lo, 13f, 1.1f)
            .Deco(SpawnableKind.Coin, L, Lo, 33f, 1.1f);

        // 3. climb Low -> Mid -> High, then step back down
        public static ChallengeSegment Climb() => Seg("Climb", 50f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Mi, Medium, 13f)
            .Platform(C, Hi, Medium, 23f)
            .Platform(C, Mi, Medium, 33f)
            .Platform(C, Lo, Long, 43f)
            .Deco(SpawnableKind.Coin, C, Lo, 6f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Mi, 16f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Hi, 25f, 1.1f);

        // 4. reward - easy long platforms, a coin arc up and down
        public static ChallengeSegment CoinRun() => Seg("Coin Run", 44f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Mi, Long, 18f)
            .Platform(C, Lo, Long, 33f)
            .Deco(SpawnableKind.Coin, C, Lo, 6f, 0.9f)
            .Deco(SpawnableKind.Coin, C, Lo, 9f, 1.6f)
            .Deco(SpawnableKind.Coin, C, Mi, 15f, 1.3f)
            .Deco(SpawnableKind.Coin, C, Mi, 20f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Mi, 25f, 1.3f)
            .Deco(SpawnableKind.Coin, C, Lo, 31f, 1.4f)
            .Deco(SpawnableKind.Coin, C, Lo, 34f, 0.8f);

        // 5. obstacle blocks center -> go around (side platforms provided)
        public static ChallengeSegment LowObstacle() => Seg("Low Obstacle", 48f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(L, Lo, Medium, 15f)
            .Platform(R, Lo, Medium, 15f)
            .Platform(C, Lo, Medium, 27f)
            .Platform(L, Lo, Medium, 37f)
            .Platform(R, Lo, Medium, 37f)
            .Platform(C, Lo, Medium, 46f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 15f, 0f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 37f, 0f);

        // 6. overhead rotating bar - run under it on long platforms
        public static ChallengeSegment Rotating() => Seg("Rotating Bar", 46f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Lo, Long, 22f)
            .Platform(C, Lo, Medium, 40f)
            .Deco(SpawnableKind.RotatingBar, C, Lo, 10f, 2.4f)
            .Deco(SpawnableKind.RotatingBar, C, Lo, 27f, 2.4f);

        // 7. closing gate - time the pass through center
        public static ChallengeSegment Gate() => Seg("Closing Gate", 48f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Lo, Long, 16f)
            .Platform(C, Lo, Medium, 31f)
            .Platform(C, Lo, Medium, 42f)
            .Deco(SpawnableKind.ClosingGate, C, Lo, 16f, 0f);

        // 8. FINAL - lane dodge + bounce climb + drop, all in one
        public static ChallengeSegment FinalChallenge() => Seg("Final Challenge", 52f)
            .Platform(C, Lo, Medium, 3f, SpawnableKind.BouncePad)
            .Platform(C, Hi, Medium, 14f)
            .Platform(R, Lo, Medium, 25f)
            .Platform(C, Lo, Medium, 35f)
            .Platform(L, Lo, Medium, 44f)
            .Platform(C, Lo, Medium, 49f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 25f, 0f)   // forces the R hop after the drop
            .Deco(SpawnableKind.Coin, C, Hi, 14f, 1.1f);

        // -------------------------------------------------------------------
        /// <summary>Prototype Level 1 body: intro + 7 rising-difficulty segments + final.</summary>
        public static List<ChallengeSegment> Level01Segments() => new()
        {
            Intro(),
            ShortRun(),
            LaneChange(),
            Climb(),
            CoinRun(),
            LowObstacle(),
            Rotating(),
            Gate(),
            FinalChallenge(),
        };

        /// <summary>Level 2 reuses the same challenge set for now (only the theme differs).</summary>
        public static List<ChallengeSegment> Level02Segments() => Level01Segments();
    }
}

using System.Collections.Generic;
using JellyRush.Lanes;
using JellyRush.Spawnables;
using static JellyRush.Level.PlatformLength;

namespace JellyRush.Level
{
    /// <summary>
    /// Hand-authored <see cref="ChallengeSegment"/>s (round 6). Each has ONE clear
    /// idea. IMPORTANT invariant for the Auto Test route: between two consecutive
    /// ON-ROUTE platforms the lane changes by at most 1 AND the tier changes by at
    /// most 1, with a ~9-12 m gap - so a single jump (+ one climb beat) always
    /// bridges it. Alternative go-around platforms use AltPlatform (off route).
    ///
    /// Lanes:  L left / C center / R right      Tiers:  0 Low / 1 Mid / 2 High
    /// Every segment starts and ends at Center / Low.
    /// </summary>
    public static class SegmentLibrary
    {
        const int L = LaneSystem.Left, C = LaneSystem.Center, R = LaneSystem.Right;
        const int Lo = HeightGrid.Low, Mi = HeightGrid.Mid, Hi = HeightGrid.High;

        static ChallengeSegment Seg(string name, float length) => new(name, length);

        public static ChallengeSegment Intro() => Seg("Intro", 46f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Lo, Long, 16f)
            .Platform(C, Lo, Long, 29f)
            .Platform(C, Lo, Long, 41f);

        public static ChallengeSegment ShortRun() => Seg("Short Run", 50f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Lo, Medium, 14f)
            .Platform(C, Lo, Medium, 25f, SpawnableKind.MovingPlatform)
            .Platform(C, Lo, Medium, 36f)
            .Platform(C, Lo, Medium, 46f)
            .Deco(SpawnableKind.Coin, C, Lo, 14f, 1.0f);

        public static ChallengeSegment LaneChange() => Seg("Lane Change", 52f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(R, Lo, Medium, 15f)
            .Platform(C, Lo, Medium, 27f)
            .Platform(L, Lo, Medium, 39f)
            .Platform(C, Lo, Medium, 48f)
            .Deco(SpawnableKind.Coin, R, Lo, 15f, 1.0f)
            .Deco(SpawnableKind.Coin, L, Lo, 39f, 1.0f);

        public static ChallengeSegment Climb() => Seg("Climb", 54f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Mi, Medium, 15f)
            .Platform(C, Hi, Long, 27f)
            .Platform(C, Mi, Medium, 40f)
            .Platform(C, Lo, Medium, 49f)
            .Deco(SpawnableKind.Coin, C, Mi, 15f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Hi, 27f, 1.1f);

        public static ChallengeSegment CoinRun() => Seg("Coin Run", 46f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Mi, Long, 19f)
            .Platform(C, Lo, Long, 34f)
            .Deco(SpawnableKind.Coin, C, Lo, 7f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Lo, 10f, 1.7f)
            .Deco(SpawnableKind.Coin, C, Mi, 16f, 1.4f)
            .Deco(SpawnableKind.Coin, C, Mi, 22f, 1.0f)
            .Deco(SpawnableKind.Coin, C, Mi, 27f, 1.4f)
            .Deco(SpawnableKind.Coin, C, Lo, 32f, 1.6f)
            .Deco(SpawnableKind.Coin, C, Lo, 35f, 1.0f);

        public static ChallengeSegment LowObstacle() => Seg("Low Obstacle", 50f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(L, Lo, Medium, 15f)          // route: dodge left
            .AltPlatform(R, Lo, Medium, 15f)
            .Platform(C, Lo, Medium, 27f)
            .Platform(R, Lo, Medium, 39f)          // route: dodge right
            .AltPlatform(L, Lo, Medium, 39f)
            .Platform(C, Lo, Medium, 48f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 15f, 0f)
            .Deco(SpawnableKind.Obstacle, C, Lo, 39f, 0f);

        public static ChallengeSegment Rotating() => Seg("Rotating Bar", 50f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Lo, Long, 24f)
            .Platform(C, Lo, Medium, 44f)
            .Deco(SpawnableKind.RotatingBar, C, Lo, 12f, 2.5f)   // overhead - a grounded run is safe
            .Deco(SpawnableKind.RotatingBar, C, Lo, 32f, 2.5f);

        public static ChallengeSegment BounceUp() => Seg("Bounce Up", 48f)
            .Platform(C, Lo, Long, 3f)
            .Platform(C, Lo, Medium, 16f, SpawnableKind.BouncePad)
            .Platform(C, Hi, Long, 23f)                          // the bounce carries you here
            .Platform(C, Mi, Medium, 36f)
            .Platform(C, Lo, Medium, 45f)
            .Deco(SpawnableKind.Coin, C, Mi, 20f, 1.2f);

        public static ChallengeSegment Gate() => Seg("Closing Gate", 52f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Lo, Long, 16f)
            .Platform(C, Lo, Medium, 34f)
            .Platform(C, Lo, Medium, 45f)
            .Deco(SpawnableKind.ClosingGate, C, Lo, 16f, 0f);

        public static ChallengeSegment FinalChallenge() => Seg("Final Challenge", 58f)
            .Platform(C, Lo, Medium, 3f)
            .Platform(C, Mi, Medium, 15f)
            .Platform(R, Mi, Medium, 27f)          // lane only
            .Platform(R, Hi, Long, 39f)            // tier only
            .Platform(C, Mi, Medium, 50f)          // down + lane
            .Platform(C, Lo, Long, 54f)
            .Deco(SpawnableKind.Coin, R, Hi, 39f, 1.1f);

        // -------------------------------------------------------------------
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
            // BounceUp() is authored above but kept out of the auto-route level for now.
        };

        public static List<ChallengeSegment> Level02Segments() => Level01Segments();
    }
}

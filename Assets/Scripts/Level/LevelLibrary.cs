using JellyRush.World;
using UnityEngine;

namespace JellyRush.Level
{
    /// <summary>
    /// Round 5 stand-in for authored <see cref="LevelData"/> assets. Builds the
    /// prototype levels in memory so the game runs with zero assets. Replace a call
    /// here with a real <c>.asset</c> reference once a level is authored in the
    /// inspector - nothing else changes.
    /// </summary>
    public static class LevelLibrary
    {
        public const string Level01Id = "level-01";
        public const string Level02Id = "level-02";

        public static LevelData Get(string id)
        {
            switch (id)
            {
                case Level02Id: return Level02();
                default:        return Level01();
            }
        }

        static LevelData Level01()
        {
            var d = ScriptableObject.CreateInstance<LevelData>();
            d.name = "Level01 (runtime)";
            d.levelId = Level01Id;
            d.worldThemeId = WorldThemeId.ToyWorkshop;
            d.targetDurationSeconds = 60f;
            d.startScrollSpeed = 7f;
            d.maxScrollSpeed = 10f;
            d.scrollAcceleration = 0.05f;
            d.segments = SegmentLibrary.Level01Segments();
            d.finishRunwayZ = 26f;
            d.finishLane = 1;
            d.finishTier = 0;
            d.nextLevelId = Level02Id;
            return d;
        }

        static LevelData Level02()
        {
            var d = ScriptableObject.CreateInstance<LevelData>();
            d.name = "Level02 (runtime)";
            d.levelId = Level02Id;
            d.worldThemeId = WorldThemeId.CandyFactory;
            d.targetDurationSeconds = 60f;
            d.startScrollSpeed = 7.5f;
            d.maxScrollSpeed = 11f;
            d.scrollAcceleration = 0.06f;
            d.segments = SegmentLibrary.Level02Segments();
            d.finishRunwayZ = 26f;
            d.finishLane = 1;
            d.finishTier = 0;
            d.nextLevelId = Level01Id;   // loop back for the prototype
            return d;
        }
    }
}

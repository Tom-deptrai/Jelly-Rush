using System.Collections.Generic;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Level
{
    /// <summary>
    /// Round 5: a level is DATA, not code. One shared gameplay engine reads a
    /// LevelData: which world theme, which challenge segments in which order, the
    /// scroll pacing, how the finish is laid out, and which level comes next.
    /// Make one via  Assets > Create > JellyRush > Level Data  (or let
    /// <see cref="LevelLibrary"/> build a placeholder at runtime). Adding Level 2,
    /// 3, ... never touches core code.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "JellyRush/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public string levelId = "level-01";
        public WorldThemeId worldThemeId = WorldThemeId.ToyWorkshop;
        [Tooltip("Optional explicit theme asset; overrides worldThemeId when set.")]
        public WorldThemeData themeOverride;

        [Tooltip("Design target in seconds - drives pacing intent, not a hard timer.")]
        public float targetDurationSeconds = 60f;

        [Header("Scroll pacing for this level")]
        public float startScrollSpeed = 7f;
        public float maxScrollSpeed = 10f;
        public float scrollAcceleration = 0.05f;

        [Header("Challenge sequence  (Start ... rising difficulty ... Final)")]
        public List<ChallengeSegment> segments = new();

        [Header("Finish")]
        [Tooltip("Empty space (metres) after the last segment before the Finish Platform.")]
        public float finishRunwayZ = 26f;
        public int finishLane = 1;   // 0 L / 1 C / 2 R
        public int finishTier = 0;   // HeightGrid tier

        [Header("Progression")]
        [Tooltip("Resolved via LevelLibrary when nextLevel is not directly assigned.")]
        public string nextLevelId = "";
        public LevelData nextLevel;
    }
}

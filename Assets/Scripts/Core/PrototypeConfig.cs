using System;
using JellyRush.World;
using UnityEngine;

namespace JellyRush.Core
{
    /// <summary>
    /// All prototype-tuning numbers live here so camera / lane / speed feel can be
    /// adjusted from a single inspector block (on <see cref="PrototypeBootstrap"/>)
    /// without touching gameplay code. Values are V1 starting points, per the specs
    /// (CAMERA_AND_DEPTH_SPEC_V1.md, GAMEPLAY_SPEC_V1.md) and are expected to change
    /// after playtesting.
    /// </summary>
    [Serializable]
    public class PrototypeConfig
    {
        [Header("Camera / Depth (CAMERA_AND_DEPTH_SPEC section 2)")]
        [Tooltip("Perspective FOV. Portrait, needs to read a long corridor.")]
        public float cameraFieldOfView = 58f;

        [Tooltip("Camera local position behind and above the player unit.")]
        public Vector3 cameraOffset = new Vector3(0f, 3.8f, -7.8f);

        [Tooltip("Downward pitch of the camera in degrees. Low enough to see far, " +
                 "high enough to read platforms ahead. NOT looking up at the sky.")]
        public float cameraPitch = 16f;

        [Tooltip("Camera near/far clip.")]
        public float cameraNear = 0.1f;
        public float cameraFar = 400f;

        [Tooltip("How softly the camera follows the player sideways when changing lane.")]
        public float cameraLateralFollow = 2.5f;

        [Tooltip("Fraction of the player's lane offset the camera copies (0 = fixed, " +
                 "1 = fully tracks). Kept small so the 3 lanes always stay readable.")]
        public float cameraLateralAmount = 0.25f;

        [Tooltip("Fraction of the player's height the camera copies, so climbing to " +
                 "the High tier does not push the pair off the top of the screen. " +
                 "Small - the camera must stay calm (CAMERA spec section 10).")]
        public float cameraVerticalAmount = 0.35f;
        public float cameraVerticalFollow = 2.0f;

        [Header("Lanes (CAMERA_AND_DEPTH_SPEC section 4)")]
        [Tooltip("World X of the left / center / right lanes at the player plane. " +
                 "Perspective makes them converge naturally toward the far distance.")]
        public float laneSpacing = 2.15f;

        [Tooltip("Z of the player unit. World scrolls toward -Z past this point.")]
        public float playerZ = 0f;

        [Header("World scroll (CAMERA_AND_DEPTH_SPEC section 7 - option A: world moves)")]
        public float startScrollSpeed = 9f;
        public float maxScrollSpeed = 18f;
        [Tooltip("Units/second added to scroll speed per second of play.")]
        public float scrollAcceleration = 0.15f;

        [Header("Spawning (CAMERA_AND_DEPTH_SPEC section 6)")]
        [Tooltip("Distance ahead of the player where new elements appear (small, far).")]
        public float spawnAheadDistance = 62f;
        [Tooltip("Z behind the camera where elements are recycled.")]
        public float despawnBehindDistance = 16f;
        [Tooltip("Metres of travel between spawn slots.")]
        public float spawnIntervalMeters = 6.5f;
        [Tooltip("Random +/- jitter on the spawn interval.")]
        public float spawnIntervalJitter = 1.5f;
        [Tooltip("Seconds of clear runway before the first hazard.")]
        public float warmupSeconds = 2.5f;

        [Header("Player jump (GAMEPLAY_SPEC section 1, round 3 tuned for climbing)")]
        [Tooltip("Apex of a single tap jump, measured from the platform you left. " +
                 "Just under the Mid tier gap so ONE tap is a normal hop but does " +
                 "NOT reach Mid on its own - climbing needs a beat chain.")]
        public float jumpHeight = 1.5f;
        [Tooltip("Reference airtime of a single jump.")]
        public float jumpDuration = 0.5f;
        [Tooltip("Extra height multiplier for a bounce pad (bypasses the air-chain ceiling).")]
        public float bouncePadMultiplier = 2.2f;

        [Tooltip("Soft ceiling of a rapid-tap chain, measured ABOVE the platform the " +
                 "chain started from. Set a little over the Low->High gap so a full " +
                 "chain can just reach High, then plateaus - never flies to the sky " +
                 "(CAMERA spec section 2 & 10).")]
        public float airChainCeiling = 3.8f;

        [Tooltip("Max consecutive jump beats between two successful platform landings. " +
                 "Every Tap / lane-swipe that fires a beat spends one. Refilled on landing.")]
        public int maxAirJumpBeats = 4;

        [Header("Height tiers (round 3) - Low / Mid / High, extend by adding entries")]
        [Tooltip("Y of each logical tier a platform / item can sit at. Index 0 = Low, " +
                 "1 = Mid, 2 = High. Invisible to the player - no drawn floors.")]
        public float[] heightTiers = { 0f, 1.6f, 3.2f };

        [Header("Platforms / fall (round 3 - no fixed ground)")]
        [Tooltip("Y of the starting platform (the Low tier).")]
        public float startHeight = 0f;
        [Tooltip("Game Over once the pair falls this far BELOW the last platform it " +
                 "stood on (relative, so it works at any climb height).")]
        public float failDropBelowSupport = 5.5f;
        [Tooltip("Absolute Y backstop for Game Over, in case something odd happens.")]
        public float failHeightAbsolute = -60f;
        [Tooltip("Height above a platform top within which a falling player latches on.")]
        public float landSnap = 0.4f;
        [Tooltip("Z length of short / medium / long platforms.")]
        public float platformShortZ = 2.4f;
        public float platformMediumZ = 4.4f;
        public float platformLongZ = 8f;
        [Tooltip("Z length of the guaranteed starting platform under the player.")]
        public float startPlatformZ = 24f;

        [Header("World theme (round 3 - swappable per scenery)")]
        public WorldThemeId startingTheme = WorldThemeId.ToyWorkshop;

        [Header("Lane change (GAMEPLAY_SPEC section 1, CAMERA spec section 3)")]
        [Tooltip("Seconds to slide fully between adjacent lanes. Smooth, never a teleport.")]
        public float laneChangeDuration = 0.16f;
        [Tooltip("Max visual lean angle (degrees) while sliding sideways.")]
        public float laneLeanAngle = 18f;

        [Header("Input (GAMEPLAY_SPEC section 1 - one finger, no min tap interval)")]
        [Tooltip("Screen-distance (fraction of shorter screen side) beyond which a drag " +
                 "counts as a swipe instead of a tap.")]
        public float swipeThresholdFraction = 0.06f;
        [Tooltip("Max seconds a touch can last and still register as a tap/swipe flick.")]
        public float maxGestureTime = 0.6f;

        [Header("Scoring")]
        public int coinValue = 1;
        [Tooltip("Seconds within which a second jump keeps the combo alive.")]
        public float comboWindow = 1.4f;

        [Header("Debug (round 5)")]
        [Tooltip("Shows the AUTO TEST button so the level can be watched hands-free. " +
                 "Turn OFF for a real build.")]
        public bool enableDebugAutoTest = true;
    }
}

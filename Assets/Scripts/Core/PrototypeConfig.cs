using System;
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

        [Header("Player jump (GAMEPLAY_SPEC section 1)")]
        [Tooltip("Peak height of a normal tap jump. Deliberately modest - the feel is " +
                 "moving THROUGH a corridor, not launching into the sky.")]
        public float jumpHeight = 1.7f;
        [Tooltip("Time from take-off to landing for a normal jump.")]
        public float jumpDuration = 0.42f;
        [Tooltip("Extra height multiplier for a bounce pad.")]
        public float bouncePadMultiplier = 2.1f;
        [Tooltip("Resting height of the player unit above the lane floor.")]
        public float groundY = 0f;

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
    }
}

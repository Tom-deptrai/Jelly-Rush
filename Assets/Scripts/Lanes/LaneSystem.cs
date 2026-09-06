using UnityEngine;

namespace JellyRush.Lanes
{
    /// <summary>
    /// Three fixed logical lanes (left / center / right) as trajectories in 3D space.
    /// No lane stripes are drawn - perspective convergence toward the vanishing point
    /// does the readability work (CAMERA_AND_DEPTH_SPEC section 4).
    /// </summary>
    public class LaneSystem : MonoBehaviour
    {
        public const int LaneCount = 3;
        public const int Left = 0;
        public const int Center = 1;
        public const int Right = 2;

        [SerializeField] float _laneSpacing = 2.15f;

        public float LaneSpacing => _laneSpacing;

        public void Configure(float laneSpacing) => _laneSpacing = laneSpacing;

        /// <summary>World-space X for a lane index at the player plane.</summary>
        public float LaneToX(int lane)
        {
            lane = Mathf.Clamp(lane, 0, LaneCount - 1);
            return (lane - Center) * _laneSpacing;
        }

        public int ClampLane(int lane) => Mathf.Clamp(lane, 0, LaneCount - 1);

        public int Step(int lane, int direction) => ClampLane(lane + (direction < 0 ? -1 : 1));
    }
}

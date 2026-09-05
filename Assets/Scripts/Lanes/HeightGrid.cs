using UnityEngine;

namespace JellyRush.Lanes
{
    /// <summary>
    /// Round 3: logical vertical tiers, the Y counterpart of <see cref="LaneSystem"/>.
    /// Platforms, coins and hazards are placed at a lane (X) AND a tier (Y), e.g.
    /// Left+Low, Center+High, Right+Mid. Tiers are invisible - no drawn floors or
    /// bands - the player only reads them through the platforms themselves.
    /// Extend by adding entries to <c>PrototypeConfig.heightTiers</c>.
    /// </summary>
    public class HeightGrid : MonoBehaviour
    {
        // Named indices for the default 3-tier setup. More tiers can be added to the
        // config array without touching these.
        public const int Low = 0;
        public const int Mid = 1;
        public const int High = 2;

        float[] _tiers = { 0f, 1.6f, 3.2f };

        public int TierCount => _tiers.Length;

        public void Configure(float[] tiers)
        {
            if (tiers != null && tiers.Length > 0) _tiers = tiers;
        }

        public int ClampTier(int tier) => Mathf.Clamp(tier, 0, _tiers.Length - 1);

        /// <summary>Top-surface Y for a tier.</summary>
        public float TierToY(int tier) => _tiers[ClampTier(tier)];
    }

    /// <summary>Readable alias for authoring segments.</summary>
    public enum HeightTier { Low = 0, Mid = 1, High = 2 }
}

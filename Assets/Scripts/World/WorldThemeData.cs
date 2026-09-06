using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.World
{
    public enum WorldThemeId { ToyWorkshop, CandyFactory, JungleTemple, SkyStation }

    /// <summary>
    /// Round 3: a World / Theme is more than a background. Each theme can supply its
    /// OWN prefab for every gameplay function, so the same NormalPlatform is a toy
    /// box in Toy Workshop, a candy slab in Candy Factory, a stone slab in Jungle
    /// Temple, a tech pad in Sky Station - while gameplay code only ever asks for a
    /// <see cref="SpawnableKind"/>.
    ///
    /// Make one of these per world via  Assets > Create > JellyRush > World Theme,
    /// then drop the world's prefabs in. When a prefab slot is empty the spawner
    /// falls back to a tinted primitive placeholder using this theme's palette, so
    /// the prototype runs with zero art.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldTheme", menuName = "JellyRush/World Theme")]
    public class WorldThemeData : ScriptableObject
    {
        public WorldThemeId id = WorldThemeId.ToyWorkshop;
        public string displayName = "Toy Workshop";

        [Header("Palette (used for the sky and for placeholder primitives)")]
        public Color skyColor = new Color(0.62f, 0.79f, 0.93f);
        public Color platformColor = new Color(0.95f, 0.86f, 0.62f);
        public Color accentColor = new Color(0.98f, 0.70f, 0.35f);
        public Color hazardColor = new Color(0.90f, 0.25f, 0.28f);
        public Color coinColor = new Color(1.00f, 0.85f, 0.15f);

        [Header("Per-function prefabs (optional; empty = placeholder primitive)")]
        public GameObject normalPlatformPrefab;
        public GameObject movingPlatformPrefab;
        public GameObject bouncePadPrefab;
        public GameObject rotatingBarPrefab;
        public GameObject closingGatePrefab;
        public GameObject obstaclePrefab;
        public GameObject coinPrefab;

        public GameObject PrefabFor(SpawnableKind kind)
        {
            switch (kind)
            {
                case SpawnableKind.Platform:       return normalPlatformPrefab;
                case SpawnableKind.MovingPlatform: return movingPlatformPrefab;
                case SpawnableKind.BouncePad:      return bouncePadPrefab;
                case SpawnableKind.RotatingBar:    return rotatingBarPrefab;
                case SpawnableKind.ClosingGate:    return closingGatePrefab;
                case SpawnableKind.Obstacle:       return obstaclePrefab;
                case SpawnableKind.Coin:           return coinPrefab;
                default:                           return null;
            }
        }
    }
}

using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Core
{
    /// <summary>
    /// Physics layers (round 6). Landing only ever raycasts <see cref="LandableMask"/>
    /// so a coin / hazard / decoration can never be mistaken for a surface. Defined
    /// in ProjectSettings/TagManager.asset at slots 8-11.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Landable = "Landable";
        public const string Hazard = "Hazard";
        public const string Collectible = "Collectible";

        public static int PlayerLayer => LayerMask.NameToLayer(Player);
        public static int LandableLayer => LayerMask.NameToLayer(Landable);
        public static int HazardLayer => LayerMask.NameToLayer(Hazard);
        public static int CollectibleLayer => LayerMask.NameToLayer(Collectible);

        public static int LandableMask
        {
            get
            {
                int l = LandableLayer;
                return l >= 0 ? 1 << l : ~0;   // fall back to "everything" if the layer is missing
            }
        }

        public static void SetRecursively(GameObject go, int layer)
        {
            if (layer < 0 || go == null) return;
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetRecursively(go.transform.GetChild(i).gameObject, layer);
        }

        public static int LayerForKind(SpawnableKind kind)
        {
            switch (kind)
            {
                case SpawnableKind.Platform:
                case SpawnableKind.MovingPlatform:
                case SpawnableKind.FinishPlatform:
                    return LandableLayer;
                case SpawnableKind.Coin:
                    return CollectibleLayer;
                default: // Obstacle / RotatingBar / ClosingGate / BouncePad
                    return HazardLayer;
            }
        }
    }
}

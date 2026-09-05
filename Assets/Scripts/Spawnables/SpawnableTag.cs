using UnityEngine;

namespace JellyRush.Spawnables
{
    public enum SpawnableKind
    {
        Platform,
        Coin,
        Obstacle,
        MovingPlatform,
        RotatingBar,
        ClosingGate,
        BouncePad,
        FinishPlatform
    }

    /// <summary>Lightweight identity marker read by player collisions and the spawner pool.</summary>
    public class SpawnableTag : MonoBehaviour
    {
        [SerializeField] SpawnableKind _kind;
        public SpawnableKind Kind => _kind;
        public void SetKind(SpawnableKind kind) => _kind = kind;
    }
}

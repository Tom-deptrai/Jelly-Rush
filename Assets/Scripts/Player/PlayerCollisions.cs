using JellyRush.Core;
using JellyRush.Spawnables;
using UnityEngine;

namespace JellyRush.Player
{
    /// <summary>
    /// Trigger-based reactions for the player unit. V1 is intentionally forgiving:
    /// hitting a hazard (obstacle / closed gate / rotating bar) fails the run, coins
    /// score, bounce pads launch. Landing accuracy on platforms is cosmetic for now
    /// (see PLACEHOLDER note) so playtesting can focus on depth + lane + input feel.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCollisions : MonoBehaviour
    {
        GameManager _game;
        PlayerController _controller;

        public void Configure(GameManager game, PlayerController controller)
        {
            _game = game;
            _controller = controller;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_game == null || _game.State == GameState.Failed) return;

            var tag = other.GetComponentInParent<SpawnableTag>();
            if (tag == null) return;

            switch (tag.Kind)
            {
                case SpawnableKind.Coin:
                    _game.AddCoin();
                    tag.gameObject.SetActive(false);
                    break;

                case SpawnableKind.BouncePad:
                    _controller.ForceBounce();
                    break;

                case SpawnableKind.Obstacle:
                case SpawnableKind.RotatingBar:
                    _game.Fail(tag.Kind.ToString());
                    break;

                case SpawnableKind.ClosingGate:
                    if (tag.TryGetComponent<ClosingGate>(out var gate) && gate.BlocksPlayerNow(_controller.CurrentLane))
                        _game.Fail("ClosingGate");
                    break;
            }
        }
    }
}

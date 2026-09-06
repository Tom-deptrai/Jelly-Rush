using JellyRush.Core;
using JellyRush.Spawnables;
using UnityEngine;
using JellyRush.Feedback;

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
        GameplayFeedbackHub _feedback;

        public void Configure(GameManager game, PlayerController controller, GameplayFeedbackHub feedback)
        {
            _game = game;
            _controller = controller;
            _feedback = feedback;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_game == null || _game.State == GameState.Failed) return;

            var tag = other.GetComponentInParent<SpawnableTag>();
            if (tag == null) return;

            switch (tag.Kind)
            {
                case SpawnableKind.Coin:
                    _feedback?.Coin(other.ClosestPoint(transform.position));
                    _game.AddCoin();
                    tag.gameObject.SetActive(false);
                    break;

                case SpawnableKind.BouncePad:
                    _controller.ForceBounce();
                    break;

                case SpawnableKind.Obstacle:
                case SpawnableKind.RotatingBar:
                    HandlePredictiveHazard(other, other.ClosestPoint(transform.position));
                    break;

                case SpawnableKind.ClosingGate:
                    if (tag.TryGetComponent<ClosingGate>(out var gate) && gate.BlocksPlayerNow(_controller.CurrentLane))
                        HandlePredictiveHazard(other, other.ClosestPoint(transform.position));
                    break;
            }
        }

        /// <summary>Shared by trigger callbacks and the pre-move sweep.</summary>
        public void HandlePredictiveHazard(Collider other, Vector3 contactPoint)
        {
            if (_game == null || _game.State == GameState.Failed || _game.State == GameState.Completed) return;
            var tag = other != null ? other.GetComponentInParent<SpawnableTag>() : null;
            if (tag == null) return;
            if (tag.Kind == SpawnableKind.ClosingGate &&
                tag.TryGetComponent<ClosingGate>(out var gate) && !gate.BlocksPlayerNow(_controller.CurrentLane))
                return;
            if (tag.Kind != SpawnableKind.Obstacle && tag.Kind != SpawnableKind.RotatingBar &&
                tag.Kind != SpawnableKind.ClosingGate) return;

            _feedback?.Hit(contactPoint);
            _game.Fail(tag.Kind.ToString());
        }
    }
}

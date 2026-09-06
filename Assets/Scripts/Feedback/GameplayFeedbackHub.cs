using System;
using UnityEngine;

namespace JellyRush.Feedback
{
    public readonly struct FeedbackSignal
    {
        public readonly Vector3 position;
        public readonly float strength;
        public readonly int value;

        public FeedbackSignal(Vector3 position, float strength = 1f, int value = 0)
        {
            this.position = position;
            this.strength = strength;
            this.value = value;
        }
    }

    /// <summary>
    /// One-way gameplay-to-presentation event seam. Gameplay publishes semantic
    /// events; audio, particles, camera, haptics, UI and character reactions listen
    /// independently and can be replaced without changing movement rules.
    /// </summary>
    public sealed class GameplayFeedbackHub : MonoBehaviour
    {
        Transform _player;

        public event Action<FeedbackSignal> OnJumpBeat;
        public event Action<FeedbackSignal> OnLand;
        public event Action<FeedbackSignal> OnCoinCollected;
        public event Action<FeedbackSignal> OnHit;
        public event Action<FeedbackSignal> OnFail;
        public event Action<FeedbackSignal> OnLevelComplete;
        public event Action<FeedbackSignal> OnComboChanged;
        public event Action<FeedbackSignal> OnComboMilestone;
        public event Action<FeedbackSignal> OnHeadHit;

        public void BindPlayer(Transform player) => _player = player;
        Vector3 PlayerPosition => _player != null ? _player.position : Vector3.zero;

        public void JumpBeat(Vector3 p, float strength) => OnJumpBeat?.Invoke(new FeedbackSignal(p, strength));
        public void Land(Vector3 p, float strength) => OnLand?.Invoke(new FeedbackSignal(p, strength));
        public void Coin(Vector3 p) => OnCoinCollected?.Invoke(new FeedbackSignal(p));
        public void Hit(Vector3 p, float strength = 1f) => OnHit?.Invoke(new FeedbackSignal(p, strength));
        public void Fail() => OnFail?.Invoke(new FeedbackSignal(PlayerPosition));
        public void Complete(Vector3 p) => OnLevelComplete?.Invoke(new FeedbackSignal(p));
        public void Combo(int value)
        {
            var signal = new FeedbackSignal(PlayerPosition, 1f, value);
            OnComboChanged?.Invoke(signal);
            if (value == 3 || value == 5 || (value >= 10 && value % 10 == 0))
                OnComboMilestone?.Invoke(signal);
        }
        public void HeadHit(Vector3 p, float strength = 1f) => OnHeadHit?.Invoke(new FeedbackSignal(p, strength));
    }
}

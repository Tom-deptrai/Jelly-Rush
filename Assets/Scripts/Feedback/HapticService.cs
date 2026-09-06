using UnityEngine;

namespace JellyRush.Feedback
{
    public enum HapticStrength { Light, Medium, Strong, Success }

    /// <summary>Platform-safe haptic seam. Editor is deliberately a no-op.</summary>
    public sealed class HapticService : MonoBehaviour
    {
        GameplayFeedbackHub _hub;

        public void Configure(GameplayFeedbackHub hub)
        {
            _hub = hub;
            hub.OnCoinCollected += Coin;
            hub.OnLand += Land;
            hub.OnHeadHit += HeadHit;
            hub.OnHit += Hit;
            hub.OnFail += Fail;
            hub.OnLevelComplete += Finish;
        }

        void Coin(FeedbackSignal s) => Pulse(HapticStrength.Light);
        void Land(FeedbackSignal s) => Pulse(HapticStrength.Light);
        void HeadHit(FeedbackSignal s) => Pulse(HapticStrength.Medium);
        void Hit(FeedbackSignal s) => Pulse(HapticStrength.Medium);
        void Fail(FeedbackSignal s) => Pulse(HapticStrength.Strong);
        void Finish(FeedbackSignal s) => Pulse(HapticStrength.Success);

        public static void Pulse(HapticStrength strength)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // Unity's built-in API has one portable intensity. The semantic strength
            // remains at this seam for a future native implementation.
            Handheld.Vibrate();
#endif
        }

        void OnDestroy()
        {
            if (_hub == null) return;
            _hub.OnCoinCollected -= Coin;
            _hub.OnLand -= Land;
            _hub.OnHeadHit -= HeadHit;
            _hub.OnHit -= Hit;
            _hub.OnFail -= Fail;
            _hub.OnLevelComplete -= Finish;
        }
    }
}

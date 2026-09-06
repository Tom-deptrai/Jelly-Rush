using JellyRush.Player;
using UnityEngine;

namespace JellyRush.Feedback
{
    public sealed class CharacterFeedback : MonoBehaviour
    {
        GameplayFeedbackHub _hub;
        PlayerVisuals _visuals;

        public void Configure(GameplayFeedbackHub hub, PlayerVisuals visuals)
        {
            _hub = hub;
            _visuals = visuals;
            hub.OnHeadHit += HeadHit;
            hub.OnHit += Hit;
            hub.OnFail += Fail;
            hub.OnLevelComplete += Finish;
            hub.OnComboMilestone += Combo;
        }

        void HeadHit(FeedbackSignal s) => _visuals.OnHeadHit();
        void Hit(FeedbackSignal s) => _visuals.OnHit();
        void Fail(FeedbackSignal s) => _visuals.OnFail();
        void Finish(FeedbackSignal s) => _visuals.OnLevelComplete();
        void Combo(FeedbackSignal s) => _visuals.OnComboMilestone(s.value);

        void OnDestroy()
        {
            if (_hub == null) return;
            _hub.OnHeadHit -= HeadHit;
            _hub.OnHit -= Hit;
            _hub.OnFail -= Fail;
            _hub.OnLevelComplete -= Finish;
            _hub.OnComboMilestone -= Combo;
        }
    }
}

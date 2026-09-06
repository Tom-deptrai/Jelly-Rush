using JellyRush.CameraRig;
using UnityEngine;

namespace JellyRush.Feedback
{
    public sealed class CameraFeedback : MonoBehaviour
    {
        GameplayFeedbackHub _hub;
        DepthCameraRig _rig;

        public void Configure(GameplayFeedbackHub hub, DepthCameraRig rig)
        {
            _hub = hub;
            _rig = rig;
            hub.OnLand += Land;
            hub.OnHeadHit += HeadHit;
            hub.OnHit += Hit;
            hub.OnFail += Fail;
            hub.OnLevelComplete += Finish;
        }

        void Land(FeedbackSignal s) => _rig.AddImpulse(0.035f * s.strength, 0.10f, 0.25f);
        void HeadHit(FeedbackSignal s) => _rig.AddImpulse(0.065f, 0.13f, -0.35f);
        void Hit(FeedbackSignal s) => _rig.AddImpulse(0.11f, 0.18f, 0.55f);
        void Fail(FeedbackSignal s) => _rig.AddImpulse(0.14f, 0.24f, 0.8f);
        void Finish(FeedbackSignal s) => _rig.AddImpulse(0.045f, 0.28f, -0.65f);

        void OnDestroy()
        {
            if (_hub == null) return;
            _hub.OnLand -= Land;
            _hub.OnHeadHit -= HeadHit;
            _hub.OnHit -= Hit;
            _hub.OnFail -= Fail;
            _hub.OnLevelComplete -= Finish;
        }
    }
}

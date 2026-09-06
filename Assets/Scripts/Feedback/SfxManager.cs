using UnityEngine;

namespace JellyRush.Feedback
{
    /// <summary>Central SFX slots with tiny generated fallbacks; no licensed assets required.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxManager : MonoBehaviour
    {
        [SerializeField] AudioClip _jump;
        [SerializeField] AudioClip _land;
        [SerializeField] AudioClip _coin;
        [SerializeField] AudioClip _headHit;
        [SerializeField] AudioClip _hit;
        [SerializeField] AudioClip _fail;
        [SerializeField] AudioClip _combo;
        [SerializeField] AudioClip _finish;

        GameplayFeedbackHub _hub;
        AudioSource _source;

        public void Configure(GameplayFeedbackHub hub)
        {
            _hub = hub;
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.34f;
            BuildFallbacks();

            hub.OnJumpBeat += Jump;
            hub.OnLand += Land;
            hub.OnCoinCollected += Coin;
            hub.OnHeadHit += HeadHit;
            hub.OnHit += Hit;
            hub.OnFail += Fail;
            hub.OnComboMilestone += Combo;
            hub.OnLevelComplete += Finish;
        }

        void Jump(FeedbackSignal s) => Play(_jump, 0.96f + s.strength * 0.05f, 0.42f);
        void Land(FeedbackSignal s) => Play(_land, 0.95f, Mathf.Lerp(0.35f, 0.65f, s.strength));
        void Coin(FeedbackSignal s) => Play(_coin, 1.12f, 0.62f);
        void HeadHit(FeedbackSignal s) => Play(_headHit, 0.82f, 0.62f);
        void Hit(FeedbackSignal s) => Play(_hit, 0.78f, 0.75f);
        void Fail(FeedbackSignal s) => Play(_fail, 0.72f, 0.72f);
        void Combo(FeedbackSignal s) => Play(_combo, Mathf.Clamp(1f + s.value * 0.025f, 1f, 1.45f), 0.60f);
        void Finish(FeedbackSignal s) => Play(_finish, 1f, 0.8f);

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (clip == null || _source == null) return;
            _source.pitch = pitch;
            _source.PlayOneShot(clip, volume);
        }

        void BuildFallbacks()
        {
            _jump ??= Tone("Sfx_Jump_Procedural", 520f, 0.07f, 0.12f);
            _land ??= Tone("Sfx_Land_Procedural", 150f, 0.09f, 0.32f);
            _coin ??= Tone("Sfx_Coin_Procedural", 920f, 0.09f, 0.05f);
            _headHit ??= Tone("Sfx_HeadHit_Procedural", 210f, 0.08f, 0.38f);
            _hit ??= Tone("Sfx_Hit_Procedural", 105f, 0.12f, 0.62f);
            _fail ??= Tone("Sfx_Fail_Procedural", 120f, 0.24f, 0.18f, -65f);
            _combo ??= Tone("Sfx_Combo_Procedural", 680f, 0.11f, 0.08f);
            _finish ??= Tone("Sfx_Finish_Procedural", 620f, 0.28f, 0.05f, 440f);
        }

        static AudioClip Tone(string name, float frequency, float duration, float noise,
                              float frequencySweep = 120f)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[count];
            uint random = (uint)name.GetHashCode();
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = frequency + frequencySweep * t;
                phase += Mathf.PI * 2f * hz / sampleRate;
                random = random * 1664525u + 1013904223u;
                float n = ((random >> 8) / 16777215f) * 2f - 1f;
                float envelope = (1f - t) * (1f - Mathf.Exp(-t * 45f));
                data[i] = (Mathf.Sin(phase) * (1f - noise) + n * noise) * envelope * 0.32f;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (_hub == null) return;
            _hub.OnJumpBeat -= Jump;
            _hub.OnLand -= Land;
            _hub.OnCoinCollected -= Coin;
            _hub.OnHeadHit -= HeadHit;
            _hub.OnHit -= Hit;
            _hub.OnFail -= Fail;
            _hub.OnComboMilestone -= Combo;
            _hub.OnLevelComplete -= Finish;
        }
    }
}

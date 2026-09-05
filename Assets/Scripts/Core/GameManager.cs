using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyRush.Core
{
    public enum GameState { Warmup, Running, Paused, Failed }

    /// <summary>
    /// Owns run-level state (distance, coins, combo) and the pause / fail / retry
    /// flow. Deliberately tiny - no shop / ads / online, per the V1 brief.
    /// Other modules talk to it through the C# events below.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PrototypeConfig Config { get; private set; }
        public GameState State { get; private set; } = GameState.Warmup;

        public float DistanceMeters { get; private set; }
        public int Coins { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<int> CoinsChanged;
        public event Action<int> ComboChanged;
        public event Action<float> DistanceChanged;

        float _comboTimer;

        public void Init(PrototypeConfig config)
        {
            Instance = this;
            Config = config;
            State = GameState.Warmup;
        }

        void Update()
        {
            if (State == GameState.Warmup || State == GameState.Running)
            {
                if (_comboTimer > 0f)
                {
                    _comboTimer -= Time.deltaTime;
                    if (_comboTimer <= 0f) SetCombo(0);
                }
            }
        }

        public void BeginRunning()
        {
            if (State != GameState.Warmup) return;
            SetState(GameState.Running);
        }

        public void AddDistance(float meters)
        {
            if (State == GameState.Failed) return;
            DistanceMeters += meters;
            DistanceChanged?.Invoke(DistanceMeters);
        }

        public void AddCoin()
        {
            Coins += Mathf.Max(1, Config.coinValue);
            CoinsChanged?.Invoke(Coins);
        }

        /// <summary>Call on every successful jump to feed the (placeholder) combo meter.</summary>
        public void RegisterJump()
        {
            if (State == GameState.Failed) return;
            SetCombo(Combo + 1);
            _comboTimer = Config.comboWindow;
        }

        void SetCombo(int value)
        {
            Combo = Mathf.Max(0, value);
            if (Combo > BestCombo) BestCombo = Combo;
            ComboChanged?.Invoke(Combo);
        }

        public void Fail(string reason)
        {
            if (State == GameState.Failed) return;
            Debug.Log($"[JellyRush] Fail: {reason}");
            SetCombo(0);
            SetState(GameState.Failed);
        }

        public void TogglePause()
        {
            if (State == GameState.Running || State == GameState.Warmup)
            {
                _resumeState = State;
                SetState(GameState.Paused);
                Time.timeScale = 0f;
            }
            else if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(_resumeState);
            }
        }
        GameState _resumeState = GameState.Running;

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void SetState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}

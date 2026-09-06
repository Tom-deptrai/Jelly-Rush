using System;
using JellyRush.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using JellyRush.Feedback;

namespace JellyRush.Core
{
    public enum GameState { Warmup, Running, Paused, Failed, Completed }

    /// <summary>
    /// Owns run-level state (distance, coins, combo) and the pause / fail / retry /
    /// LEVEL COMPLETE flow. Deliberately tiny - no shop / ads / online, per the V1
    /// brief. Other modules talk to it through the C# events below.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PrototypeConfig Config { get; private set; }
        public GameState State { get; private set; } = GameState.Warmup;
        public LevelData CurrentLevel { get; private set; }

        public float DistanceMeters { get; private set; }
        public int Coins { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<int> CoinsChanged;
        public event Action<int> ComboChanged;
        public event Action<float> DistanceChanged;

        /// <summary>Hook for the future victory animation / celebration.</summary>
        public event Action OnLevelCompleted;

        float _comboTimer;
        GameplayFeedbackHub _feedback;

        public void Init(PrototypeConfig config, GameplayFeedbackHub feedback)
        {
            Instance = this;
            Config = config;
            State = GameState.Warmup;
            _feedback = feedback;
        }

        public void SetLevel(LevelData level) => CurrentLevel = level;

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
            if (State == GameState.Failed || State == GameState.Completed) return;
            DistanceMeters += meters;
            DistanceChanged?.Invoke(DistanceMeters);
        }

        public void AddCoin()
        {
            Coins += Mathf.Max(1, Config.coinValue);
            CoinsChanged?.Invoke(Coins);
        }

        public void RegisterJump()
        {
            if (State == GameState.Failed || State == GameState.Completed) return;
            SetCombo(Combo + 1);
            _comboTimer = Config.comboWindow;
        }

        void SetCombo(int value)
        {
            Combo = Mathf.Max(0, value);
            if (Combo > BestCombo) BestCombo = Combo;
            ComboChanged?.Invoke(Combo);
            _feedback?.Combo(Combo);
        }

        public void Fail(string reason)
        {
            if (State == GameState.Failed || State == GameState.Completed) return;
            Debug.Log($"[JellyRush] Fail: {reason}");
            _feedback?.Fail();
            SetCombo(0);
            SetState(GameState.Failed);
        }

        /// <summary>Player landed on the Finish Platform - stop everything, hand off to UI.</summary>
        public void CompleteLevel()
        {
            if (State != GameState.Running && State != GameState.Warmup) return;
            Debug.Log($"[JellyRush] Level complete: {(CurrentLevel != null ? CurrentLevel.levelId : "?")}  coins={Coins}");
            SetState(GameState.Completed);
            OnLevelCompleted?.Invoke();
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
            PrototypeBootstrap.PendingLevel = CurrentLevel;   // reload the SAME level
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            Time.timeScale = 1f;
            PrototypeBootstrap.PendingLevel = ResolveNextLevel();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        LevelData ResolveNextLevel()
        {
            if (CurrentLevel == null) return null;
            if (CurrentLevel.nextLevel != null) return CurrentLevel.nextLevel;
            if (!string.IsNullOrEmpty(CurrentLevel.nextLevelId))
                return LevelLibrary.Get(CurrentLevel.nextLevelId);
            return CurrentLevel; // no next authored -> replay current
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

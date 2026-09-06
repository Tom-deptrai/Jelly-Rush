using JellyRush.Core;
using JellyRush.Debugging;
using UnityEngine;
using UnityEngine.UI;
using JellyRush.Feedback;

namespace JellyRush.UI
{
    /// <summary>
    /// Minimal HUD, built entirely from code against a 1080x1920 portrait reference.
    /// Distance / coins / combo placeholder / pause, a RETRY panel on fail, a LEVEL
    /// COMPLETE panel with NEXT LEVEL on finish, and a debug-only AUTO TEST toggle.
    /// No shop / skins / leaderboard.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        GameManager _game;
        Text _distance;
        Text _coins;
        Text _combo;
        Text _debug;
        Font _font;
        Transform _root;

        GameObject _retryPanel;
        Text _retryStats;
        GameObject _completePanel;
        Text _completeStats;

        AutoPlayBot _bot;
        Text _autoLabel;

        JellyRush.Player.PlayerController _player;
        GameplayFeedbackHub _feedback;
        Vector3 _comboBaseScale = Vector3.one;
        float _comboPunch;

        public void Build(GameManager game, GameplayFeedbackHub feedback)
        {
            _game = game;
            _feedback = feedback;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUD Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _root = canvasGo.transform;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _distance = Label(_root, "Distance", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -40f), TextAnchor.UpperLeft, 54, "0 m");
            _coins = Label(_root, "Coins", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -40f), TextAnchor.UpperRight, 54, "0");
            _combo = Label(_root, "Combo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -120f), TextAnchor.UpperCenter, 44, "");
            _debug = Label(_root, "Debug", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), TextAnchor.LowerLeft, 36, "");

            MakeButton(_root, "PauseButton", new Vector2(1f, 0f), new Vector2(-40f, 40f),
                new Vector2(150f, 110f), "II", () => _game.TogglePause());

            BuildRetryPanel(_root);
            BuildCompletePanel(_root);

            _game.CoinsChanged += c => _coins.text = c.ToString();
            _game.ComboChanged += c => _combo.text = c >= 2 ? $"COMBO x{c}" : "";
            _game.DistanceChanged += d => _distance.text = $"{Mathf.FloorToInt(d)} m";
            _game.StateChanged += OnState;
            _feedback.OnComboMilestone += OnComboMilestone;
        }

        public void BindPlayerDebug(JellyRush.Player.PlayerController player) => _player = player;

        /// <summary>Debug AUTO TEST toggle - only shown when enabled in config.</summary>
        public void BindAutoTest(bool enabled, AutoPlayBot bot)
        {
            if (!enabled || bot == null) return;
            _bot = bot;

            var btn = MakeButton(_root, "AutoTestButton", new Vector2(0f, 1f),
                new Vector2(40f, -130f), new Vector2(360f, 96f),
                "AUTO TEST: OFF", ToggleAuto);
            _autoLabel = btn.GetComponentInChildren<Text>();
            btn.GetComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.28f);
        }

        void ToggleAuto()
        {
            if (_bot == null) return;
            _bot.Active = !_bot.Active;
            if (_autoLabel != null) _autoLabel.text = _bot.Active ? "AUTO TEST: ON" : "AUTO TEST: OFF";
        }

        void Update()
        {
            if (_player != null && _debug != null)
            {
                string mode = _bot != null && _bot.Active ? "AUTO" : "MANUAL";
                _debug.text = $"[{mode}] beats {_player.BeatsLeft}/4  " +
                              $"{(_player.IsAirborne ? "air" : "on platform")}  supY {_player.SupportY:0.0}";
            }

            if (_combo != null)
            {
                _comboPunch = Mathf.Max(0f, _comboPunch - Time.unscaledDeltaTime * 5.5f);
                _combo.rectTransform.localScale = _comboBaseScale * (1f + _comboPunch * 0.28f);
            }
        }

        void OnComboMilestone(FeedbackSignal signal) => _comboPunch = 1f;

        void OnDestroy()
        {
            if (_feedback != null) _feedback.OnComboMilestone -= OnComboMilestone;
        }

        void OnState(GameState s)
        {
            _retryPanel.SetActive(s == GameState.Failed);
            _completePanel.SetActive(s == GameState.Completed);

            if (s == GameState.Failed)
                _retryStats.text = $"Distance  {Mathf.FloorToInt(_game.DistanceMeters)} m\n" +
                                   $"Coins  {_game.Coins}\n" +
                                   $"Best combo  x{_game.BestCombo}";
            else if (s == GameState.Completed)
                _completeStats.text = $"Coins collected  {_game.Coins}\n" +
                                      $"Best combo  x{_game.BestCombo}";
        }

        // --- widgets -----------------------------------------------------

        Text Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                   Vector2 anchoredPos, TextAnchor align, int size, string initial)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = new Vector2(700f, 90f);
            rt.anchoredPosition = anchoredPos;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = initial;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        Button MakeButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos,
                          Vector2 size, string caption, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            var lt = label.GetComponent<Text>();
            lt.font = _font; lt.fontSize = 40; lt.alignment = TextAnchor.MiddleCenter;
            lt.color = Color.white; lt.text = caption;
            return go.GetComponent<Button>();
        }

        GameObject BuildPanel(Transform parent, string name, string title, float titleSize)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var t = Label(panel.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 340f), TextAnchor.MiddleCenter, (int)titleSize, title);
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.SetActive(false);
            return panel;
        }

        void BuildRetryPanel(Transform parent)
        {
            _retryPanel = BuildPanel(parent, "RetryPanel", "RUN OVER", 80);
            _retryStats = Label(_retryPanel.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 110f), TextAnchor.MiddleCenter, 46, "");
            _retryStats.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _retryStats.rectTransform.sizeDelta = new Vector2(820f, 300f);

            var retry = MakeButton(_retryPanel.transform, "RetryButton", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -150f), new Vector2(420f, 140f), "RETRY", () => _game.Retry());
            ((RectTransform)retry.transform).pivot = new Vector2(0.5f, 0.5f);
        }

        void BuildCompletePanel(Transform parent)
        {
            _completePanel = BuildPanel(parent, "CompletePanel", "LEVEL COMPLETE", 68);
            _completeStats = Label(_completePanel.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), TextAnchor.MiddleCenter, 46, "");
            _completeStats.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _completeStats.rectTransform.sizeDelta = new Vector2(820f, 220f);

            var next = MakeButton(_completePanel.transform, "NextButton", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -60f), new Vector2(460f, 140f), "NEXT LEVEL", () => _game.LoadNextLevel());
            ((RectTransform)next.transform).pivot = new Vector2(0.5f, 0.5f);
            next.GetComponent<Image>().color = new Color(0.3f, 0.85f, 0.45f, 0.4f);

            var retry = MakeButton(_completePanel.transform, "CompleteRetryButton", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -230f), new Vector2(360f, 120f), "RETRY", () => _game.Retry());
            ((RectTransform)retry.transform).pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

using JellyRush.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRush.UI
{
    /// <summary>
    /// Minimal V1 HUD (brief item 9): distance, coins, combo placeholder, pause,
    /// and a retry panel on fail. Built entirely from code against a 1080x1920
    /// portrait reference so it needs no scene assets. No shop / skins / leaderboard.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        GameManager _game;
        Text _distance;
        Text _coins;
        Text _combo;
        Text _debug;
        GameObject _retryPanel;
        Text _retryStats;
        Font _font;
        JellyRush.Player.PlayerController _player;

        public void Build(GameManager game)
        {
            _game = game;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUD Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _distance = Label(canvasGo.transform, "Distance", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -40f), TextAnchor.UpperLeft, 54, "0 m");
            _coins = Label(canvasGo.transform, "Coins", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -40f), TextAnchor.UpperRight, 54, "0");
            _combo = Label(canvasGo.transform, "Combo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -120f), TextAnchor.UpperCenter, 44, "");
            _debug = Label(canvasGo.transform, "Debug", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), TextAnchor.LowerLeft, 36, "");

            MakeButton(canvasGo.transform, "PauseButton", new Vector2(1f, 0f), new Vector2(-40f, 40f),
                new Vector2(150f, 110f), "II", () => _game.TogglePause());

            BuildRetryPanel(canvasGo.transform);

            _game.CoinsChanged += c => _coins.text = c.ToString();
            _game.ComboChanged += c => _combo.text = c >= 2 ? $"COMBO x{c}" : "";
            _game.DistanceChanged += d => _distance.text = $"{Mathf.FloorToInt(d)} m";
            _game.StateChanged += OnState;
        }

        /// <summary>Temporary debug readout (round 2): remaining jump-beat budget.</summary>
        public void BindPlayerDebug(JellyRush.Player.PlayerController player) => _player = player;

        void Update()
        {
            if (_player != null && _debug != null)
                _debug.text = $"beats: {_player.BeatsLeft}   {(_player.IsAirborne ? "air" : "grounded")}";
        }

        void OnState(GameState s)
        {
            bool failed = s == GameState.Failed;
            _retryPanel.SetActive(failed);
            if (failed)
                _retryStats.text = $"Distance  {Mathf.FloorToInt(_game.DistanceMeters)} m\n" +
                                   $"Coins  {_game.Coins}\n" +
                                   $"Best combo  x{_game.BestCombo}";
        }

        Text Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                   Vector2 anchoredPos, TextAnchor align, int size, string initial)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = new Vector2(600f, 90f);
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
            lt.font = _font; lt.fontSize = 44; lt.alignment = TextAnchor.MiddleCenter;
            lt.color = Color.white; lt.text = caption;
            return go.GetComponent<Button>();
        }

        void BuildRetryPanel(Transform parent)
        {
            _retryPanel = new GameObject("RetryPanel", typeof(RectTransform), typeof(Image));
            _retryPanel.transform.SetParent(parent, false);
            var rt = (RectTransform)_retryPanel.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            _retryPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var title = Label(_retryPanel.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 320f), TextAnchor.MiddleCenter, 80, "RUN OVER");
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _retryStats = Label(_retryPanel.transform, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), TextAnchor.MiddleCenter, 46, "");
            _retryStats.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _retryStats.rectTransform.sizeDelta = new Vector2(800f, 300f);

            var retry = MakeButton(_retryPanel.transform, "RetryButton", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(420f, 140f), "RETRY", () => _game.Retry());
            ((RectTransform)retry.transform).pivot = new Vector2(0.5f, 0.5f);

            _retryPanel.SetActive(false);
        }
    }
}

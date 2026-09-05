using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyRush.InputSystem
{
    public enum GestureType { Tap, SwipeLeft, SwipeRight }

    /// <summary>
    /// One-finger input (GAMEPLAY_SPEC section 1):
    ///   Tap         -> jump straight in the current lane
    ///   Swipe left  -> jump + move to the left lane
    ///   Swipe right -> jump + move to the right lane
    /// No joystick. No artificial minimum interval between taps - every touch that
    /// ends is emitted, so rapid tapping is fully received. Each active finger is
    /// tracked independently and multi-touch in the same frame produces multiple
    /// gestures. Mouse is supported as an editor stand-in.
    /// </summary>
    public class SwipeTapInput : MonoBehaviour
    {
        public event Action<GestureType> Gesture;

        float _swipeThresholdPixels;
        float _maxGestureTime = 0.6f;

        struct Touched
        {
            public Vector2 startPos;
            public float startTime;
        }

        readonly Dictionary<int, Touched> _active = new();
        const int MouseId = -99;

        /// <summary>Inject a gesture directly (debug / automated smoke tests).</summary>
        public void Simulate(GestureType gesture) => Gesture?.Invoke(gesture);

        public void Configure(float swipeThresholdFraction, float maxGestureTime)
        {
            float shortSide = Mathf.Min(Screen.width, Screen.height);
            _swipeThresholdPixels = Mathf.Max(24f, shortSide * swipeThresholdFraction);
            _maxGestureTime = maxGestureTime;
        }

        void Update()
        {
            // --- Touch (mobile) -------------------------------------------------
            if (Input.touchSupported && Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            _active[t.fingerId] = new Touched { startPos = t.position, startTime = Time.unscaledTime };
                            break;
                        case TouchPhase.Ended:
                            Resolve(t.fingerId, t.position);
                            break;
                        case TouchPhase.Canceled:
                            _active.Remove(t.fingerId);
                            break;
                    }
                }
                return; // don't double-count with mouse emulation
            }

            // --- Mouse (editor / desktop) ------------------------------------
            if (Input.GetMouseButtonDown(0))
                _active[MouseId] = new Touched { startPos = Input.mousePosition, startTime = Time.unscaledTime };
            else if (Input.GetMouseButtonUp(0))
                Resolve(MouseId, Input.mousePosition);
        }

        void Resolve(int id, Vector2 endPos)
        {
            if (!_active.TryGetValue(id, out var data)) return;
            _active.Remove(id);

            Vector2 delta = endPos - data.startPos;
            float elapsed = Time.unscaledTime - data.startTime;

            // A long press that never moved still counts as a tap on release -
            // we only reject it as a swipe if it also dragged far.
            if (Mathf.Abs(delta.x) >= _swipeThresholdPixels &&
                Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) &&
                elapsed <= _maxGestureTime)
            {
                Gesture?.Invoke(delta.x < 0 ? GestureType.SwipeLeft : GestureType.SwipeRight);
            }
            else
            {
                Gesture?.Invoke(GestureType.Tap);
            }
        }
    }
}

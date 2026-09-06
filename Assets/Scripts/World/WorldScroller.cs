using JellyRush.Core;
using UnityEngine;

namespace JellyRush.World
{
    /// <summary>
    /// CAMERA_AND_DEPTH_SPEC section 7, technical option A: the world moves toward
    /// the camera. Every gameplay element is parented under <see cref="WorldRoot"/>,
    /// which slides in -Z. Speed ramps from start to max over the run; the ramp
    /// values come from the active <see cref="Level.LevelData"/> so each level can
    /// pace itself. Stops on Paused / Failed / Completed.
    /// </summary>
    public class WorldScroller : MonoBehaviour
    {
        public Transform WorldRoot { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float DistanceTravelled { get; private set; }

        PrototypeConfig _cfg;
        GameManager _game;
        float _startSpeed;
        float _maxSpeed;
        float _accel;

        public void Configure(PrototypeConfig cfg, GameManager game, Transform worldRoot,
                              float startSpeed, float maxSpeed, float acceleration)
        {
            _cfg = cfg;
            _game = game;
            WorldRoot = worldRoot;
            _startSpeed = startSpeed > 0f ? startSpeed : cfg.startScrollSpeed;
            _maxSpeed = maxSpeed > 0f ? maxSpeed : cfg.maxScrollSpeed;
            _accel = acceleration >= 0f ? acceleration : cfg.scrollAcceleration;
            CurrentSpeed = _startSpeed;
        }

        void Update()
        {
            if (_cfg == null) return;
            var state = _game.State;
            if (state == GameState.Paused || state == GameState.Failed ||
                state == GameState.Completed || state == GameState.Warmup)
                return;

            CurrentSpeed = Mathf.Min(_maxSpeed, CurrentSpeed + _accel * Time.deltaTime);

            float step = CurrentSpeed * Time.deltaTime;
            WorldRoot.position += new Vector3(0f, 0f, -step);
            DistanceTravelled += step;
            _game.AddDistance(step);
        }
    }
}

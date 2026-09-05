using JellyRush.Core;
using UnityEngine;

namespace JellyRush.World
{
    /// <summary>
    /// CAMERA_AND_DEPTH_SPEC section 7, technical option A: the world moves toward
    /// the camera. Every gameplay element is parented under <see cref="WorldRoot"/>,
    /// which slides in -Z. So elements are authored far away (small on screen), then
    /// travel toward the player and grow via perspective - never "falling from the
    /// top edge". Speed ramps up slowly over the run.
    /// </summary>
    public class WorldScroller : MonoBehaviour
    {
        public Transform WorldRoot { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float DistanceTravelled { get; private set; }

        PrototypeConfig _cfg;
        GameManager _game;

        public void Configure(PrototypeConfig cfg, GameManager game, Transform worldRoot)
        {
            _cfg = cfg;
            _game = game;
            WorldRoot = worldRoot;
            CurrentSpeed = cfg.startScrollSpeed;
        }

        void Update()
        {
            if (_cfg == null) return;
            var state = _game.State;
            if (state == GameState.Paused || state == GameState.Failed) return;
            if (state == GameState.Warmup) return; // wait for first input

            CurrentSpeed = Mathf.Min(_cfg.maxScrollSpeed,
                                     CurrentSpeed + _cfg.scrollAcceleration * Time.deltaTime);

            float step = CurrentSpeed * Time.deltaTime;
            WorldRoot.position += new Vector3(0f, 0f, -step);
            DistanceTravelled += step;
            _game.AddDistance(step);
        }
    }
}

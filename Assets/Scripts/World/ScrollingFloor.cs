using UnityEngine;

namespace JellyRush.World
{
    /// <summary>
    /// Scrolls a texture toward the camera at the world speed. NOT used since round 4
    /// (the play space has no floor plane - space below the platforms is empty).
    /// Kept as a ready-made helper for a future far BACKGROUND layer / parallax,
    /// which is allowed to move but must never sit under the player.
    /// </summary>
    public class ScrollingFloor : MonoBehaviour
    {
        WorldScroller _world;
        Material _mat;
        float _offset;
        [SerializeField] float _metersPerTile = 3f;

        public void Configure(WorldScroller world)
        {
            _world = world;
            _mat = GetComponent<Renderer>().material;
        }

        void Update()
        {
            if (_world == null) return;
            _offset += _world.CurrentSpeed * Time.deltaTime / Mathf.Max(0.01f, _metersPerTile);
            if (_mat != null) _mat.mainTextureOffset = new Vector2(0f, -_offset);
        }

        public static Texture2D BuildGrid(int size = 128, Color? bg = null, Color? line = null)
        {
            Color b = bg ?? new Color(0.86f, 0.80f, 0.68f);
            Color l = line ?? new Color(0.72f, 0.64f, 0.5f);
            var tex = new Texture2D(size, size) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 3 || y < 3 || x > size - 4 || y > size - 4;
                tex.SetPixel(x, y, edge ? l : b);
            }
            tex.Apply();
            return tex;
        }
    }
}

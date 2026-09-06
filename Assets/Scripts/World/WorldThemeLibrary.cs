using UnityEngine;

namespace JellyRush.World
{
    /// <summary>
    /// Round 3 stand-in for a set of authored <see cref="WorldThemeData"/> assets.
    /// Builds an in-memory theme (palette only, no prefabs -> primitive fallback)
    /// for each planned world so the prototype can already switch scenery flavour.
    /// Replace a call to this with a real <c>.asset</c> reference once a world has art.
    /// </summary>
    public static class WorldThemeLibrary
    {
        public static WorldThemeData Get(WorldThemeId id)
        {
            var d = ScriptableObject.CreateInstance<WorldThemeData>();
            d.id = id;
            switch (id)
            {
                case WorldThemeId.ToyWorkshop:
                    d.displayName = "Toy Workshop";
                    d.skyColor = new Color(0.62f, 0.79f, 0.93f);
                    d.platformColor = new Color(0.95f, 0.82f, 0.55f);
                    d.accentColor = new Color(0.98f, 0.68f, 0.32f);
                    d.hazardColor = new Color(0.92f, 0.30f, 0.30f);
                    d.coinColor = new Color(1.00f, 0.85f, 0.20f);
                    break;

                case WorldThemeId.CandyFactory:
                    d.displayName = "Candy Factory";
                    d.skyColor = new Color(0.98f, 0.80f, 0.90f);
                    d.platformColor = new Color(0.98f, 0.72f, 0.82f);
                    d.accentColor = new Color(0.75f, 0.55f, 0.95f);
                    d.hazardColor = new Color(0.85f, 0.20f, 0.45f);
                    d.coinColor = new Color(1.00f, 0.90f, 0.35f);
                    break;

                case WorldThemeId.JungleTemple:
                    d.displayName = "Jungle Temple";
                    d.skyColor = new Color(0.55f, 0.72f, 0.55f);
                    d.platformColor = new Color(0.62f, 0.60f, 0.48f);
                    d.accentColor = new Color(0.45f, 0.65f, 0.40f);
                    d.hazardColor = new Color(0.80f, 0.45f, 0.20f);
                    d.coinColor = new Color(1.00f, 0.82f, 0.28f);
                    break;

                case WorldThemeId.SkyStation:
                    d.displayName = "Sky Station";
                    d.skyColor = new Color(0.55f, 0.68f, 0.85f);
                    d.platformColor = new Color(0.78f, 0.84f, 0.92f);
                    d.accentColor = new Color(0.35f, 0.75f, 0.95f);
                    d.hazardColor = new Color(0.95f, 0.45f, 0.25f);
                    d.coinColor = new Color(0.70f, 0.95f, 1.00f);
                    break;
            }
            return d;
        }
    }
}

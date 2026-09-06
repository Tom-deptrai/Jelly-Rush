using JellyRush.CameraRig;
using JellyRush.Debugging;
using JellyRush.InputSystem;
using JellyRush.Lanes;
using JellyRush.Level;
using JellyRush.Player;
using JellyRush.Spawning;
using JellyRush.UI;
using JellyRush.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JellyRush.Core
{
    /// <summary>
    /// Single entry point for the prototype. The scene contains ONLY a GameObject
    /// with this component - the whole playable scene (camera rig, lanes, player
    /// unit, world scroller, spawner, HUD, lighting) is assembled here from code so
    /// modules stay decoupled and art/values can be swapped without editing scenes.
    ///
    /// Layout / feel targets come from:
    ///   - CAMERA_AND_DEPTH_SPEC_V1.md  (perspective depth corridor, 3 lanes)
    ///   - GAMEPLAY_SPEC_V1.md          (tap = jump, swipe = lane jump, one finger)
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] PrototypeConfig _config = new();

        [Tooltip("Optional: an authored World Theme asset. When empty a placeholder " +
                 "theme is built at runtime from the level's worldThemeId.")]
        [SerializeField] JellyRush.World.WorldThemeData _themeAsset;

        [Tooltip("Optional: an authored Level Data asset. When empty, LevelLibrary " +
                 "builds the prototype level at runtime.")]
        [SerializeField] LevelData _levelAsset;

        [Tooltip("Optional: the real Carrier prefab (CarrierRoot > ModelRoot + JellySeat). " +
                 "When assigned it replaces the Carrier_PLACEHOLDER box under LeanPivot; " +
                 "the Jelly placeholder then rides on the prefab's JellySeat. Empty = box.")]
        [SerializeField] GameObject _carrierPrefab;

        [Tooltip("Optional: the real Jelly prefab (JellyRoot > ModelRoot > Jelly_Model). " +
                 "When assigned it replaces the Jelly_PLACEHOLDER cube on the Carrier's " +
                 "JellySeat and is bound to PlayerVisuals for squash/stretch. Empty = cube.")]
        [SerializeField] GameObject _jellyPrefab;

        /// <summary>Set by GameManager before a scene reload (RETRY / NEXT LEVEL) to
        /// pick which level the fresh scene should build. Consumed once in Awake.</summary>
        public static LevelData PendingLevel;

        public PrototypeConfig Config => _config;

        void Awake()
        {
            // Field initializers hold the current defaults. If the scene stored an
            // empty or pre-round-3 config, fall back to a fresh one.
            if (_config == null || _config.jumpDuration <= 0f ||
                _config.heightTiers == null || _config.heightTiers.Length == 0)
                _config = new PrototypeConfig();

            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // The kinematic player only needs raycasts against LANDABLE - never a
            // physical collision with a platform - so silence that pair.
            if (GameLayers.PlayerLayer >= 0 && GameLayers.LandableLayer >= 0)
                Physics.IgnoreLayerCollision(GameLayers.PlayerLayer, GameLayers.LandableLayer, true);

            var game = gameObject.AddComponent<GameManager>();
            game.Init(_config);

            // --- level (round 5): data drives the run ---------------------------
            var level = _levelAsset != null ? _levelAsset
                      : PendingLevel != null ? PendingLevel
                      : LevelLibrary.Get(LevelLibrary.Level01Id);
            PendingLevel = null;                 // consume the one-shot
            game.SetLevel(level);

            // --- world theme: level's override / asset / palette placeholder ----
            var theme = level.themeOverride != null ? level.themeOverride
                      : _themeAsset != null ? _themeAsset
                      : WorldThemeLibrary.Get(level.worldThemeId);

            BuildLighting();

            // --- world ---------------------------------------------------------
            var worldRoot = new GameObject("WorldRoot").transform;
            worldRoot.position = Vector3.zero;

            var scroller = new GameObject("WorldScroller").AddComponent<WorldScroller>();
            scroller.Configure(_config, game, worldRoot,
                               level.startScrollSpeed, level.maxScrollSpeed, level.scrollAcceleration);

            // Round 4: no floor plane, no lane rails. Space below the platforms is
            // genuinely empty - a missed jump falls away into the depth. Depth is
            // carried only by the sky colour + light distance fog and by the
            // platforms / coins / obstacles receding via perspective.

            // --- lanes + height tiers (LOGIC ONLY - nothing drawn) ---------
            var lanes = new GameObject("LaneSystem").AddComponent<LaneSystem>();
            lanes.Configure(_config.laneSpacing);
            var heights = new GameObject("HeightGrid").AddComponent<HeightGrid>();
            heights.Configure(_config.heightTiers);

            // --- player unit -------------------------------------------------
            var player = BuildPlayer(out var visuals, out var collisions);

            // --- input -------------------------------------------------------
            var input = new GameObject("Input").AddComponent<SwipeTapInput>();
            input.Configure(_config.swipeThresholdFraction, _config.maxGestureTime);

            player.Configure(_config, lanes, game, input, visuals);
            collisions.Configure(game, player);

            // --- camera -----------------------------------------------------
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<DepthCameraRig>();
            rig.Configure(_config, player.transform, theme.skyColor);

            BuildAtmosphere(theme);

            // --- spawner (level streamer + Finish) ------------------------
            var spawner = new GameObject("Spawner").AddComponent<Spawner>();
            spawner.Configure(_config, game, scroller, lanes, heights, theme, level);

            // --- debug auto-test bot (follows the authored route) -------
            var route = LevelRoute.Build(level, _config);
            var bot = new GameObject("AutoPlayBot").AddComponent<AutoPlayBot>();
            bot.Configure(_config, game, player, input, spawner, lanes, heights, scroller, route);

            // --- UI --------------------------------------------------------
            EnsureEventSystem();
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Build(game);
            hud.BindPlayerDebug(player);
            hud.BindAutoTest(_config.enableDebugAutoTest, bot);

            Debug.Log($"[JellyRush] Level '{level.levelId}' assembled ({level.segments.Count} segments, " +
                      $"theme {theme.displayName}). Tap = jump, swipe L/R = lane jump.");
        }

        void BuildLighting()
        {
            var lightGo = new GameObject("Sun", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
            lightGo.transform.rotation = Quaternion.Euler(55f, 12f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
        }

        void BuildAtmosphere(JellyRush.World.WorldThemeData theme)
        {
            // The only depth aids now: sky colour + gentle linear fog so far
            // platforms fade into the sky. No geometry below the play space.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.skyColor;
            RenderSettings.fogStartDistance = Mathf.Max(10f, _config.spawnAheadDistance * 0.55f);
            RenderSettings.fogEndDistance = _config.spawnAheadDistance + 35f;
        }

        PlayerController BuildPlayer(out PlayerVisuals visuals, out PlayerCollisions collisions)
        {
            // COORDINATE CONVENTION (round 6):
            //   PlayerUnit.position  = the FOOT POINT (bottom of the pair).
            //   When grounded, PlayerUnit.y = platform surface + playerFootClearance.
            //   EVERY visual sits at localY >= 0, so nothing ever dips through a surface.
            //
            //   PlayerUnit                 foot point; PlayerController, PlayerCollisions,
            //     |                        kinematic Rigidbody, trigger SphereCollider (hitbox)
            //     Visual                   PlayerVisuals; local (0,0,0)
            //       LeanPivot              tilt about the foot on lane change
            //         Carrier_PLACEHOLDER  bottom at localY 0  -> small animal carrier
            //         JellyAnchor
            //           Jelly_PLACEHOLDER  sits on the carrier  -> blue Jelly mascot
            //             Face_PLACEHOLDER faces the camera (-Z)
            var root = new GameObject("PlayerUnit");
            root.transform.position = new Vector3(0f, _config.startHeight + _config.playerFootClearance, _config.playerZ);
            if (GameLayers.PlayerLayer >= 0) root.layer = GameLayers.PlayerLayer;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Hitbox for coin / hazard / bounce detection. Sized to hug the visual
            // body; landing does NOT use this (it uses a Landable-only raycast).
            // Tight to the body so a normal single hop passes UNDER an overhead
            // rotating bar; coins float above the head and are grabbed on jumps.
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;
            trigger.center = new Vector3(0f, 0.52f, 0f);

            var controller = root.AddComponent<PlayerController>();
            collisions = root.AddComponent<PlayerCollisions>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            var leanPivot = new GameObject("LeanPivot").transform;
            leanPivot.SetParent(visual, false);

            // Carrier: real prefab when assigned, otherwise the frozen placeholder box.
            // Either way the gameplay root above is untouched - the art hangs off
            // LeanPivot and obeys the foot point (ASSET_PIPELINE_V1 section 8).
            Transform jellyAnchor;
            if (_carrierPrefab != null)
            {
                var carrierRoot = Instantiate(_carrierPrefab, leanPivot);
                carrierRoot.name = "CarrierRoot";
                carrierRoot.transform.localPosition = Vector3.zero;
                carrierRoot.transform.localRotation = Quaternion.identity;
                carrierRoot.transform.localScale = Vector3.one;

                // The gameplay collider is the trigger sphere on PlayerUnit only -
                // the art mesh must never take part in physics.
                foreach (var col in carrierRoot.GetComponentsInChildren<Collider>(true))
                    Object.Destroy(col);

                var seat = FindDeep(carrierRoot.transform, "JellySeat");
                jellyAnchor = new GameObject("JellyAnchor").transform;
                jellyAnchor.SetParent(seat != null ? seat : leanPivot, false);
                jellyAnchor.localPosition = seat != null
                    ? Vector3.zero                                   // offset lives entirely in JellySeat
                    : new Vector3(0f, 0.5f, -0.1f);
            }
            else
            {
                var carrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carrier.name = "Carrier_PLACEHOLDER";
                carrier.transform.SetParent(leanPivot, false);
                carrier.transform.localScale = new Vector3(1.0f, 0.5f, 1.5f);   // long axis -> depth
                carrier.transform.localPosition = new Vector3(0f, 0.25f, 0f);   // bottom exactly at foot (localY 0)
                Tint(carrier, new Color(0.92f, 0.58f, 0.34f));
                Object.Destroy(carrier.GetComponent<Collider>());

                jellyAnchor = new GameObject("JellyAnchor").transform;
                jellyAnchor.SetParent(leanPivot, false);
                jellyAnchor.localPosition = new Vector3(0f, 0.5f, -0.1f);       // on top of the carrier
            }

            // Jelly: real prefab when assigned, otherwise the frozen placeholder
            // cube + face quad. The mount offset lives entirely in the Carrier's
            // JellySeat - PlayerController holds no Jelly-vs-Carrier offset. The
            // Jelly prefab stays a separate instance so a later victory sequence
            // can detach it from JellySeat without a re-export.
            visuals = root.AddComponent<PlayerVisuals>();
            if (_jellyPrefab != null)
            {
                var jellyRoot = Instantiate(_jellyPrefab, jellyAnchor);
                jellyRoot.name = "JellyRoot";
                jellyRoot.transform.localPosition = Vector3.zero;
                jellyRoot.transform.localRotation = Quaternion.identity;
                jellyRoot.transform.localScale = Vector3.one;

                foreach (var col in jellyRoot.GetComponentsInChildren<Collider>(true))
                    Object.Destroy(col);

                // squash / stretch drives the model-correction node; no flat tint on
                // the textured mesh (expression / colour is the real rig's job later).
                var squashTarget = FindDeep(jellyRoot.transform, "ModelRoot") ?? jellyRoot.transform;
                visuals.Bind(leanPivot, squashTarget, null);
            }
            else
            {
                var jelly = GameObject.CreatePrimitive(PrimitiveType.Cube);
                jelly.name = "Jelly_PLACEHOLDER";
                jelly.transform.SetParent(jellyAnchor, false);
                jelly.transform.localScale = new Vector3(0.9f, 0.85f, 0.85f);
                jelly.transform.localPosition = new Vector3(0f, 0.425f, 0f);    // bottom sits on the carrier top
                var jellyRenderer = jelly.GetComponent<Renderer>();
                Tint(jelly, new Color(0.30f, 0.60f, 0.95f));
                Object.Destroy(jelly.GetComponent<Collider>());

                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.name = "Face_PLACEHOLDER";
                face.transform.SetParent(jelly.transform, false);
                face.transform.localPosition = new Vector3(0f, 0.05f, -0.52f);  // toward camera
                face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                face.transform.localScale = new Vector3(0.7f, 0.5f, 1f);
                Tint(face, new Color(0.96f, 0.98f, 1f));
                Object.Destroy(face.GetComponent<Collider>());

                visuals.Bind(leanPivot, jelly.transform, jellyRenderer);
            }

            return controller;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void Tint(GameObject go, Color c)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            go.GetComponent<Renderer>().material = new Material(shader) { color = c };
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<StandaloneInputModule>();
        }
    }
}

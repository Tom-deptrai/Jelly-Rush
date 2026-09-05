using JellyRush.CameraRig;
using JellyRush.InputSystem;
using JellyRush.Lanes;
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

        public PrototypeConfig Config => _config;

        void Awake()
        {
            // Field initializers hold the V1 defaults. If the scene stored an empty
            // config (older/partial serialization) fall back to a fresh one.
            if (_config == null || _config.jumpDuration <= 0f)
                _config = new PrototypeConfig();

            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            var game = gameObject.AddComponent<GameManager>();
            game.Init(_config);

            BuildLighting();

            // --- world ---------------------------------------------------------
            var worldRoot = new GameObject("WorldRoot").transform;
            worldRoot.position = Vector3.zero;

            var scroller = new GameObject("WorldScroller").AddComponent<WorldScroller>();
            scroller.Configure(_config, game, worldRoot);

            BuildAbyss(scroller);
            BuildLaneRails();

            // --- lanes --------------------------------------------------------
            var lanes = new GameObject("LaneSystem").AddComponent<LaneSystem>();
            lanes.Configure(_config.laneSpacing);

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
            rig.Configure(_config, player.transform);

            // --- spawner ---------------------------------------------------
            var spawner = new GameObject("Spawner").AddComponent<Spawner>();
            spawner.Configure(_config, game, scroller, lanes);

            // --- UI --------------------------------------------------------
            EnsureEventSystem();
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Build(game);
            hud.BindPlayerDebug(player);

            Debug.Log("[JellyRush] Prototype scene assembled. Tap = jump, swipe L/R = lane jump.");
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

        void BuildAbyss(WorldScroller scroller)
        {
            // Round 2: NO continuous ground. This plane sits far BELOW the fail line
            // (config.failY) purely as a distant moving backdrop so a fall reads and
            // depth still has motion. It has no collider - it can never be landed on.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "AbyssBackdrop";
            floor.transform.localScale = new Vector3(3f, 1f, 45f);
            floor.transform.position = new Vector3(0f, _config.failY - 6f, _config.playerZ + 210f);
            var rend = floor.GetComponent<Renderer>();
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            var tex = ScrollingFloor.BuildGrid(128,
                new Color(0.30f, 0.34f, 0.42f), new Color(0.22f, 0.25f, 0.32f));
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(4f, 150f);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            floor.GetComponent<Collider>().enabled = false;
            floor.AddComponent<ScrollingFloor>().Configure(scroller);
        }

        void BuildLaneRails()
        {
            // Faint static rails: the 3 lanes read as converging lines via perspective
            // (CAMERA_AND_DEPTH_SPEC section 4) without drawn lane stripes on the HUD.
            var railMat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(1f, 1f, 1f, 0.14f)
            };
            for (int lane = 0; lane < LaneSystem.LaneCount; lane++)
            {
                float x = (lane - 1) * _config.laneSpacing;
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"LaneRail_{lane}";
                rail.transform.localScale = new Vector3(0.06f, 0.02f, 420f);
                rail.transform.position = new Vector3(x, -0.34f, _config.playerZ + 200f);
                var rr = rail.GetComponent<Renderer>();
                rr.sharedMaterial = railMat;
                rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rr.receiveShadows = false;
                Object.Destroy(rail.GetComponent<Collider>());
            }
        }

        PlayerController BuildPlayer(out PlayerVisuals visuals, out PlayerCollisions collisions)
        {
            var root = new GameObject("PlayerUnit");
            root.transform.position = new Vector3(0f, _config.groundY, _config.playerZ);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;
            trigger.center = new Vector3(0f, 0.55f, 0f);

            var controller = root.AddComponent<PlayerController>();
            collisions = root.AddComponent<PlayerCollisions>();

            // PLACEHOLDER hierarchy - real art swaps in at the marked nodes:
            //   LeanPivot                 (tilt on lane change)
            //     Carrier_PLACEHOLDER     -> small animal carrier, long axis into depth
            //       JellyAnchor           (where the mascot rides)
            //         Jelly_PLACEHOLDER   -> blue Jelly robot mascot (visual focus, on top)
            //           Face_PLACEHOLDER  -> keeps a "face" turned toward the camera (-Z)
            var leanPivot = new GameObject("LeanPivot").transform;
            leanPivot.SetParent(root.transform, false);

            var carrier = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            carrier.name = "Carrier_PLACEHOLDER";
            carrier.transform.SetParent(leanPivot, false);
            carrier.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            carrier.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            carrier.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // long axis -> depth
            Tint(carrier, new Color(0.92f, 0.58f, 0.34f));
            Object.Destroy(carrier.GetComponent<Collider>());

            var jellyAnchor = new GameObject("JellyAnchor").transform;
            jellyAnchor.SetParent(leanPivot, false);
            jellyAnchor.localPosition = new Vector3(0f, 1.0f, -0.05f);

            var jelly = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jelly.name = "Jelly_PLACEHOLDER";
            jelly.transform.SetParent(jellyAnchor, false);
            jelly.transform.localScale = new Vector3(0.85f, 0.8f, 0.8f);
            var jellyRenderer = jelly.GetComponent<Renderer>();
            Tint(jelly, new Color(0.30f, 0.60f, 0.95f));
            Object.Destroy(jelly.GetComponent<Collider>());

            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face_PLACEHOLDER";
            face.transform.SetParent(jelly.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.08f, -0.52f); // toward camera
            face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            face.transform.localScale = new Vector3(0.66f, 0.46f, 1f);
            Tint(face, new Color(0.96f, 0.98f, 1f));
            Object.Destroy(face.GetComponent<Collider>());

            visuals = root.AddComponent<PlayerVisuals>();
            visuals.Bind(leanPivot, jelly.transform, jellyRenderer);

            return controller;
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

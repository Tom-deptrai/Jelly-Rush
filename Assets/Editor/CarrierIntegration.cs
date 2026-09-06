using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyRush.EditorTools
{
    /// <summary>
    /// Round "Carrier 3D": turns the imported Meshy asset in
    /// Assets/Art/Characters/Carrier/V01/ into a gameplay-ready prefab and wires it
    /// into the PlayerUnit visual subtree - WITHOUT touching PlayerController /
    /// PlayerCollisions / lanes / Auto Test / camera / Level System.
    ///
    /// What it produces (all idempotent - safe to re-run):
    ///   - fixed texture import settings (normal = NormalMap, metallic/roughness = linear)
    ///   - Carrier_V01_MetallicSmoothness.png  (packed: RGB metallic, A = 1-roughness)
    ///   - Materials/Carrier_V01_Mat.mat        (Built-in Standard, metallic workflow)
    ///   - Animations/Carrier.controller        (single looping "Walk" state)
    ///   - Prefabs/Carrier_V01.prefab           (CarrierRoot > ModelRoot > model, + JellySeat)
    ///   - assigns the prefab to PrototypeBootstrap._carrierPrefab in Prototype.unity
    ///
    /// Batch:
    ///   Unity -batchmode -projectPath . -quit \
    ///     -executeMethod JellyRush.EditorTools.CarrierIntegration.RunBatch \
    ///     [-carrierYaw 180] [-carrierHeight 0.85]
    /// </summary>
    public static class CarrierIntegration
    {
        const string Dir       = "Assets/Art/Characters/Carrier";
        const string V01       = Dir + "/V01";
        const string ModelFbx  = V01 + "/Carrier_Walk.fbx";
        const string RigFbx    = V01 + "/Meshy_AI_BunBot_Buddy_quadruped_Character_output.fbx";
        const string AlbedoTex = V01 + "/Meshy_AI_BunBot_Buddy_quadruped_texture_0.png";
        const string NormalTex = V01 + "/Meshy_AI_BunBot_Buddy_quadruped_texture_0_normal.png";
        const string RoughTex  = V01 + "/Meshy_AI_BunBot_Buddy_quadruped_texture_0_roughness.png";
        const string MetalTex  = V01 + "/Meshy_AI_BunBot_Buddy_quadruped_texture_0_metallic.png";

        const string MatDir      = Dir + "/Materials";
        const string AnimDir     = Dir + "/Animations";
        const string PrefabDir   = Dir + "/Prefabs";
        const string PackedTex   = MatDir + "/Carrier_V01_MetallicSmoothness.png";
        const string MatPath     = MatDir + "/Carrier_V01_Mat.mat";
        const string CtrlPath    = AnimDir + "/Carrier.controller";
        const string PrefabPath  = PrefabDir + "/Carrier_V01.prefab";

        const string WalkClipName = "Carrier_Walk";
        const string ScenePath    = "Assets/Scenes/Prototype.unity";

        // Defaults - overridable from the command line for quick iteration.
        static float _targetHeight = 0.85f;   // Carrier standing height, metres (spec: pair ~1.35)
        static float _yaw          = 0f;      // ModelRoot yaw correction so Carrier faces +Z

        static readonly StringBuilder Report = new();

        [MenuItem("JellyRush/Integrate Carrier V01")]
        public static void RunMenu()
        {
            try { Execute(); EditorUtility.DisplayDialog("Carrier V01", Report.ToString(), "OK"); }
            catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Carrier V01 - FAILED", e.ToString(), "OK"); }
        }

        public static void RunBatch()
        {
            int code = 0;
            try
            {
                ParseArgs();
                Execute();
                Debug.Log("[CarrierIntegration] REPORT\n" + Report);
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        static void ParseArgs()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-carrierYaw"    && float.TryParse(a[i + 1], out var y)) _yaw = y;
                if (a[i] == "-carrierHeight" && float.TryParse(a[i + 1], out var h)) _targetHeight = h;
            }
        }

        static void Execute()
        {
            Report.Clear();
            Require(File.Exists(ModelFbx), $"missing {ModelFbx}");
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(AnimDir);
            Directory.CreateDirectory(PrefabDir);

            ConfigureTextures();
            ConfigureFbx();
            AssetDatabase.Refresh();

            BuildPackedMetallicSmoothness();
            var mat  = BuildMaterial();
            var ctrl = BuildController();
            var prefab = BuildPrefab(mat, ctrl);
            WireIntoScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Report.Insert(0, $"=== Carrier V01 integration OK (yaw={_yaw}, targetHeight={_targetHeight}m) ===\n");
        }

        // ---------------------------------------------------------------- textures
        static void ConfigureTextures()
        {
            SetTexture(NormalTex, ti =>
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.sRGBTexture = false;
            });
            SetTexture(MetalTex, ti => { ti.sRGBTexture = false; ti.isReadable = true; });
            SetTexture(RoughTex, ti => { ti.sRGBTexture = false; ti.isReadable = true; });
            SetTexture(AlbedoTex, ti => { ti.sRGBTexture = true; ti.textureType = TextureImporterType.Default; });
            Report.AppendLine("textures: normal=NormalMap(linear), metallic/roughness=linear+readable, albedo=sRGB");
        }

        static void SetTexture(string path, Action<TextureImporter> apply)
        {
            if (!File.Exists(path)) { Report.AppendLine($"  WARN texture missing: {path}"); return; }
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            apply(ti);
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }

        // ---------------------------------------------------------------- fbx
        static void ConfigureFbx()
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelFbx);
            mi.animationType    = ModelImporterAnimationType.Generic;
            mi.importAnimation  = true;
            mi.optimizeGameObjects = false;

            var clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
            Require(clips != null && clips.Length > 0, "Carrier_Walk.fbx has no animation take");

            // Use the first (only) take, force it to loop and rename it Carrier_Walk.
            var c = clips[0];
            int frames = Mathf.RoundToInt(c.lastFrame - c.firstFrame);
            Require(frames > 1, $"walk take has only {frames} frame(s) - not a real clip");
            c.name = WalkClipName;
            c.loopTime = true;
            mi.clipAnimations = new[] { c };
            EditorUtility.SetDirty(mi);
            mi.SaveAndReimport();
            Report.AppendLine($"fbx model: Carrier_Walk.fbx  rig=Generic  take='{c.takeName}' -> clip '{WalkClipName}' ({frames} frames, loop)");

            if (File.Exists(RigFbx))
            {
                var ri = (ModelImporter)AssetImporter.GetAtPath(RigFbx);
                if (ri.importAnimation) { ri.importAnimation = false; ri.SaveAndReimport(); }
                Report.AppendLine("fbx rig-only: Character_output.fbx  Import Animation = OFF (empty take ignored)");
            }
        }

        // ---------------------------------------------------------------- packed map
        static void BuildPackedMetallicSmoothness()
        {
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalTex);
            var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(RoughTex);
            if (metal == null || rough == null)
            {
                Report.AppendLine("packed map: SKIPPED (metallic/roughness texture missing) - material uses constant smoothness");
                return;
            }

            int w = metal.width, h = metal.height;
            Color[] mp, rp;
            try { mp = metal.GetPixels(); rp = rough.GetPixels(); }
            catch (Exception e) { Report.AppendLine("packed map: SKIPPED (" + e.Message + ")"); return; }

            // resample roughness if sizes differ
            var outp = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var m = mp[y * w + x];
                float ru = rough.width == w && rough.height == h
                    ? rp[y * w + x].r
                    : rough.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h).r;
                outp[y * w + x] = new Color(m.r, m.g, m.b, Mathf.Clamp01(1f - ru)); // A = smoothness
            }

            var packed = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            packed.SetPixels(outp);
            packed.Apply();
            File.WriteAllBytes(PackedTex, packed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(PackedTex, ImportAssetOptions.ForceUpdate);
            SetTexture(PackedTex, ti => { ti.sRGBTexture = false; ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = false; });
            Report.AppendLine($"packed map: Carrier_V01_MetallicSmoothness.png ({w}x{h}, RGB=metallic, A=1-roughness)");
        }

        // ---------------------------------------------------------------- material
        static Material BuildMaterial()
        {
            var shader = Shader.Find("Standard");
            Require(shader != null, "Standard shader not found (project is not Built-in RP?)");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MatPath); }
            mat.shader = shader;

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoTex);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTex);
            var packed = AssetDatabase.LoadAssetAtPath<Texture2D>(PackedTex);

            mat.SetTexture("_MainTex", albedo);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (packed != null)
            {
                mat.SetTexture("_MetallicGlossMap", packed);
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Glossiness", 1f);
                mat.SetFloat("_GlossMapScale", 1f);
                mat.SetFloat("_SmoothnessTextureChannel", 0f); // 0 = metallic-map alpha
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.35f);
            }
            mat.SetFloat("_Glossiness", packed != null ? 1f : 0.35f);
            EditorUtility.SetDirty(mat);
            Report.AppendLine($"material: Carrier_V01_Mat.mat (Standard) albedo={(albedo!=null)} normal={(normal!=null)} metallicGloss={(packed!=null)}");
            return mat;
        }

        // ---------------------------------------------------------------- controller
        static AnimatorController BuildController()
        {
            var clip = LoadWalkClip();
            Require(clip != null, "could not load the Carrier_Walk clip from the FBX");

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);

            var sm = ctrl.layers[0].stateMachine;
            AnimatorState walk = sm.states.FirstOrDefault(s => s.state.name == "Walk").state;
            if (walk == null) walk = sm.AddState("Walk");
            walk.motion = clip;
            walk.speed = 1f;
            sm.defaultState = walk;
            EditorUtility.SetDirty(ctrl);
            Report.AppendLine($"controller: Carrier.controller  state 'Walk' -> clip '{clip.name}' (loop, default)");
            return ctrl;
        }

        static AnimationClip LoadWalkClip()
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(ModelFbx);
            return all.OfType<AnimationClip>().FirstOrDefault(c => c.name == WalkClipName)
                ?? all.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        }

        static Avatar LoadAvatar()
            => AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().FirstOrDefault();

        // ---------------------------------------------------------------- prefab
        static GameObject BuildPrefab(Material mat, AnimatorController ctrl)
        {
            var fbxGo = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx);
            Require(fbxGo != null, "cannot load Carrier_Walk.fbx as GameObject");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbxGo);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Carrier_Model";

            var carrierRoot = new GameObject("CarrierRoot");
            var modelRoot   = new GameObject("ModelRoot");
            var jellySeat   = new GameObject("JellySeat");
            modelRoot.transform.SetParent(carrierRoot.transform, false);
            jellySeat.transform.SetParent(carrierRoot.transform, false);
            model.transform.SetParent(modelRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale    = Vector3.one;

            // strip anything that could touch gameplay physics
            foreach (var col in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);

            // renderer + material
            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Require(smr != null, "no SkinnedMeshRenderer in Carrier_Walk.fbx");
            smr.updateWhenOffscreen = true;
            var mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            smr.sharedMaterials = mats;

            // animator
            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.avatar = LoadAvatar();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // ---- measure the raw model, then derive ONE correction on ModelRoot ----
            modelRoot.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            modelRoot.transform.localScale    = Vector3.one;
            modelRoot.transform.localPosition = Vector3.zero;

            var b = WorldBounds(model);
            float rawH = Mathf.Max(b.size.y, 1e-4f);
            float scale = _targetHeight / rawH;
            modelRoot.transform.localScale = new Vector3(scale, scale, scale);

            // re-measure after scale, align sole to localY 0 and centre X/Z on the foot
            b = WorldBounds(model);
            var rootInv = carrierRoot.transform.worldToLocalMatrix;
            Vector3 minL = rootInv.MultiplyPoint3x4(b.min);
            Vector3 maxL = rootInv.MultiplyPoint3x4(b.max);
            Vector3 ctrL = (minL + maxL) * 0.5f;
            modelRoot.transform.localPosition = new Vector3(-ctrL.x, -minL.y, -ctrL.z);

            // final bounds in CarrierRoot space (for JellySeat + report)
            b = WorldBounds(model);
            minL = rootInv.MultiplyPoint3x4(b.min);
            maxL = rootInv.MultiplyPoint3x4(b.max);
            float topY = maxL.y, lenZ = maxL.z - minL.z;

            // saddle: upper-back of the Carrier, slightly behind the shoulders
            jellySeat.transform.localPosition = new Vector3(0f, topY * 0.86f, minL.z + lenZ * 0.42f);
            jellySeat.transform.localRotation = Quaternion.identity;

            Vector3 modelRootLocalPos = modelRoot.transform.localPosition;
            Vector3 seatLocalPos = jellySeat.transform.localPosition;

            var prefab = PrefabUtility.SaveAsPrefabAsset(carrierRoot, PrefabPath, out bool ok);
            UnityEngine.Object.DestroyImmediate(carrierRoot);
            Require(ok && prefab != null, "SaveAsPrefabAsset failed");

            Report.AppendLine($"prefab: Carrier_V01.prefab");
            Report.AppendLine($"  raw model height = {rawH:F3} m  ->  uniform scale = {scale:F4}  (target {_targetHeight:F2} m)");
            Report.AppendLine($"  ModelRoot local pos = {modelRootLocalPos} (sole aligned to localY 0, X/Z centred)");
            Report.AppendLine($"  ModelRoot local rot = (0, {_yaw}, 0)  -> Carrier faces +Z");
            Report.AppendLine($"  scaled bounds in CarrierRoot: y[0..{topY:F3}] zlen {lenZ:F3}");
            Report.AppendLine($"  JellySeat local pos = {seatLocalPos} (upper back / saddle)");
            return prefab;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ---------------------------------------------------------------- scene wiring
        static void WireIntoScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bs = UnityEngine.Object.FindAnyObjectByType<JellyRush.Core.PrototypeBootstrap>();
            Require(bs != null, "PrototypeBootstrap not found in Prototype.unity");

            var so = new SerializedObject(bs);
            var prop = so.FindProperty("_carrierPrefab");
            Require(prop != null, "PrototypeBootstrap has no _carrierPrefab field - update the script first");
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bs);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Report.AppendLine("scene: Prototype.unity  PrototypeBootstrap._carrierPrefab = Carrier_V01.prefab");
        }

        static void Require(bool cond, string msg)
        {
            if (!cond) throw new Exception("[CarrierIntegration] " + msg);
        }
    }
}

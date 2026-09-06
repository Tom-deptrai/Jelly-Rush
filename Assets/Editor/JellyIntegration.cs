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
    /// Round "Jelly 3D": turns the imported Meshy asset in
    /// Assets/Art/Characters/Jelly/V01/ into a gameplay-ready prefab and mounts it on
    /// the Carrier's JellySeat - WITHOUT touching PlayerController / PlayerCollisions /
    /// lanes / Auto Test / camera / Level System. Jelly stays a SEPARATE prefab
    /// instance so a later victory sequence can detach it (no merge, no re-export).
    ///
    /// Produces (idempotent):
    ///   - fixed texture import (normal = NormalMap, metallic/roughness = linear)
    ///   - Jelly_V01_MetallicSmoothness.png  (RGB metallic, A = 1-roughness)
    ///   - Materials/Jelly_V01_Mat.mat        (Built-in Standard, metallic workflow)
    ///   - Animations/Jelly.controller        (single looping default state "Ride")
    ///   - Prefabs/Jelly_V01.prefab           (JellyRoot > ModelRoot > Jelly_Model)
    ///   - assigns it to PrototypeBootstrap._jellyPrefab in Prototype.unity
    ///
    /// Batch:
    ///   Unity -batchmode -projectPath . -quit \
    ///     -executeMethod JellyRush.EditorTools.JellyIntegration.RunBatch \
    ///     [-jellyYaw 180] [-jellyHeight 0.80] [-jellySink 0.02] [-jellyAnimSpeed 1]
    /// </summary>
    public static class JellyIntegration
    {
        const string Dir       = "Assets/Art/Characters/Jelly";
        const string V01       = Dir + "/V01";
        const string ModelFbx  = V01 + "/Meshy_AI_Bubbly_Byte_biped_Animation_Running_withSkin.fbx";
        const string RigFbx    = V01 + "/Meshy_AI_Bubbly_Byte_biped_Character_output.fbx";
        const string AlbedoTex = V01 + "/Meshy_AI_Bubbly_Byte_biped_texture_0.png";
        const string NormalTex = V01 + "/Meshy_AI_Bubbly_Byte_biped_texture_0_normal.png";
        const string RoughTex  = V01 + "/Meshy_AI_Bubbly_Byte_biped_texture_0_roughness.png";
        const string MetalTex  = V01 + "/Meshy_AI_Bubbly_Byte_biped_texture_0_metallic.png";

        const string MatDir     = Dir + "/Materials";
        const string AnimDir    = Dir + "/Animations";
        const string PrefabDir  = Dir + "/Prefabs";
        const string PackedTex  = MatDir + "/Jelly_V01_MetallicSmoothness.png";
        const string MatPath    = MatDir + "/Jelly_V01_Mat.mat";
        const string CtrlPath   = AnimDir + "/Jelly.controller";
        const string PrefabPath = PrefabDir + "/Jelly_V01.prefab";

        const string RideClipName = "Jelly_Ride";
        const string ScenePath    = "Assets/Scenes/Prototype.unity";

        // Defaults - overridable from the command line for quick iteration.
        static float _targetHeight = 0.74f;  // Jelly visual height, metres (source rig = 1.0 m)
        static float _yaw          = 180f;   // face the gameplay camera (-Z) while Carrier faces +Z
        static float _sink         = -0.04f; // + sinks into the saddle, - lifts the sole above the back
        static float _z            = -0.05f; // nudge along the Carrier's forward axis (- = toward the rump)
        static float _animSpeed    = 1f;

        static readonly StringBuilder Report = new();

        [MenuItem("JellyRush/Integrate Jelly V01")]
        public static void RunMenu()
        {
            try { Execute(); EditorUtility.DisplayDialog("Jelly V01", Report.ToString(), "OK"); }
            catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Jelly V01 - FAILED", e.ToString(), "OK"); }
        }

        public static void RunBatch()
        {
            int code = 0;
            try
            {
                ParseArgs();
                Execute();
                Debug.Log("[JellyIntegration] REPORT\n" + Report);
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        static void ParseArgs()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-jellyYaw"       && float.TryParse(a[i + 1], out var y)) _yaw = y;
                if (a[i] == "-jellyHeight"    && float.TryParse(a[i + 1], out var h)) _targetHeight = h;
                if (a[i] == "-jellySink"      && float.TryParse(a[i + 1], out var s)) _sink = s;
                if (a[i] == "-jellyZ"         && float.TryParse(a[i + 1], out var z)) _z = z;
                if (a[i] == "-jellyAnimSpeed" && float.TryParse(a[i + 1], out var sp)) _animSpeed = sp;
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
            Report.Insert(0, $"=== Jelly V01 integration OK (yaw={_yaw}, targetHeight={_targetHeight}m, sink={_sink}m, z={_z}m, animSpeed={_animSpeed}) ===\n");
        }

        // ---------------------------------------------------------------- textures
        static void ConfigureTextures()
        {
            SetTexture(NormalTex, ti => { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; });
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
            Require(clips != null && clips.Length > 0, "Jelly running FBX has no animation take");

            var c = clips[0];
            int frames = Mathf.RoundToInt(c.lastFrame - c.firstFrame);
            Require(frames > 1, $"take has only {frames} frame(s) - not a real clip");
            c.name = RideClipName;
            c.loopTime = true;
            mi.clipAnimations = new[] { c };
            EditorUtility.SetDirty(mi);
            mi.SaveAndReimport();
            Report.AppendLine($"fbx model: ...Running_withSkin.fbx  rig=Generic  take='{c.takeName}' -> clip '{RideClipName}' ({frames} frames, loop)");

            if (File.Exists(RigFbx))
            {
                var ri = (ModelImporter)AssetImporter.GetAtPath(RigFbx);
                if (ri.importAnimation) { ri.importAnimation = false; ri.SaveAndReimport(); }
                Report.AppendLine("fbx rig-only: ...Character_output.fbx  Import Animation = OFF (empty take ignored)");
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
            Report.AppendLine($"packed map: Jelly_V01_MetallicSmoothness.png ({w}x{h}, RGB=metallic, A=1-roughness)");
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
                mat.SetFloat("_GlossMapScale", 1f);
                mat.SetFloat("_SmoothnessTextureChannel", 0f); // 0 = metallic-map alpha
                mat.SetFloat("_Glossiness", 1f);
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.4f);
            }
            EditorUtility.SetDirty(mat);
            Report.AppendLine($"material: Jelly_V01_Mat.mat (Standard, opaque) albedo={(albedo!=null)} normal={(normal!=null)} metallicGloss={(packed!=null)}");
            return mat;
        }

        // ---------------------------------------------------------------- controller
        static AnimatorController BuildController()
        {
            var clip = LoadRideClip();
            Require(clip != null, "could not load the Jelly_Ride clip from the FBX");

            // Always rebuild from scratch - fully deterministic, no half-written state.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath) != null)
                AssetDatabase.DeleteAsset(CtrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
            Require(ctrl != null && ctrl.layers != null && ctrl.layers.Length > 0,
                    "failed to create Jelly.controller with a base layer");

            var sm = ctrl.layers[0].stateMachine;
            var ride = sm.AddState("Ride");
            ride.motion = clip;
            ride.speed = _animSpeed;
            sm.defaultState = ride;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Report.AppendLine($"controller: Jelly.controller  state 'Ride' -> clip '{clip.name}' (loop, default, speed {_animSpeed})");
            return ctrl;
        }

        static AnimationClip LoadRideClip()
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(ModelFbx);
            return all.OfType<AnimationClip>().FirstOrDefault(c => c.name == RideClipName)
                ?? all.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        }

        static Avatar LoadAvatar()
            => AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().FirstOrDefault();

        // ---------------------------------------------------------------- prefab
        static GameObject BuildPrefab(Material mat, AnimatorController ctrl)
        {
            var fbxGo = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx);
            Require(fbxGo != null, "cannot load the Jelly FBX as GameObject");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbxGo);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Jelly_Model";

            var jellyRoot = new GameObject("JellyRoot");
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(jellyRoot.transform, false);
            model.transform.SetParent(modelRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale    = Vector3.one;

            foreach (var col in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Require(smr != null, "no SkinnedMeshRenderer in the Jelly FBX");
            smr.updateWhenOffscreen = true;
            var mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            smr.sharedMaterials = mats;

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();   // no ?? with UnityEngine.Object
            anim.runtimeAnimatorController = ctrl;
            anim.avatar = LoadAvatar();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // ---- ONE correction on ModelRoot: yaw, uniform scale, sole -> localY 0 ----
            modelRoot.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            modelRoot.transform.localScale    = Vector3.one;
            modelRoot.transform.localPosition = Vector3.zero;

            var b = WorldBounds(model);
            float rawH = Mathf.Max(b.size.y, 1e-4f);
            float scale = _targetHeight / rawH;
            modelRoot.transform.localScale = new Vector3(scale, scale, scale);

            b = WorldBounds(model);
            var rootInv = jellyRoot.transform.worldToLocalMatrix;
            Vector3 minL = rootInv.MultiplyPoint3x4(b.min);
            Vector3 maxL = rootInv.MultiplyPoint3x4(b.max);
            Vector3 ctrL = (minL + maxL) * 0.5f;
            // centre X on the mount point, sole to localY 0 (minus sink), nudge along Z
            modelRoot.transform.localPosition = new Vector3(-ctrL.x, -minL.y - _sink, -ctrL.z + _z);

            b = WorldBounds(model);
            minL = rootInv.MultiplyPoint3x4(b.min);
            maxL = rootInv.MultiplyPoint3x4(b.max);

            Vector3 modelRootLocalPos = modelRoot.transform.localPosition;
            float jellyH = maxL.y - minL.y;

            var prefab = PrefabUtility.SaveAsPrefabAsset(jellyRoot, PrefabPath, out bool ok);
            UnityEngine.Object.DestroyImmediate(jellyRoot);
            Require(ok && prefab != null, "SaveAsPrefabAsset failed");

            Report.AppendLine("prefab: Jelly_V01.prefab  (JellyRoot > ModelRoot > Jelly_Model)");
            Report.AppendLine($"  raw model height = {rawH:F3} m  ->  uniform scale = {scale:F4}  (target {_targetHeight:F2} m)");
            Report.AppendLine($"  ModelRoot local pos = {modelRootLocalPos}  rot = (0, {_yaw}, 0)  (face -> -Z / camera)");
            Report.AppendLine($"  Jelly stands {jellyH:F3} m tall, sole at JellyRoot localY {-_sink:F2} (sits on JellySeat)");
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
            var prop = so.FindProperty("_jellyPrefab");
            Require(prop != null, "PrototypeBootstrap has no _jellyPrefab field - update the script first");
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bs);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Report.AppendLine("scene: Prototype.unity  PrototypeBootstrap._jellyPrefab = Jelly_V01.prefab");
        }

        static void Require(bool cond, string msg)
        {
            if (!cond) throw new Exception("[JellyIntegration] " + msg);
        }
    }
}

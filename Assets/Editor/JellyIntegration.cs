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
    ///   - Animations/Jelly_RideIdle.anim     (authored seated pose + subtle loop)
    ///   - Animations/Jelly.controller        (single looping default state "RideIdle")
    ///   - Prefabs/Jelly_V01.prefab           (JellyRoot > ModelRoot > Jelly_Model)
    ///   - assigns it to PrototypeBootstrap._jellyPrefab in Prototype.unity
    ///
    /// Batch:
    ///   Unity -batchmode -projectPath . -quit \
    ///     -executeMethod JellyRush.EditorTools.JellyIntegration.RunBatch \
    ///     [-jellyYaw 180] [-jellyHeight 1.27] [-jellySeatLift 0.07] [-jellyZ -0.04]
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

        const string SourceRunClipName = "Jelly_SourceRun";
        const string RideIdlePath = AnimDir + "/Jelly_RideIdle.anim";
        const string ScenePath    = "Assets/Scenes/Prototype.unity";

        // Defaults - overridable from the command line for quick iteration.
        static float _targetHeight = 1.27f;  // tuned from animated Renderer.bounds for actual Carrier:Jelly ~= 1.30:1
        static float _yaw          = 180f;   // face the gameplay camera (-Z) while Carrier faces +Z
        static float _seatLift     = 0.07f;  // pelvis above JellySeat; the soft body can overlap the saddle slightly
        static float _z            = -0.04f; // pelvis nudge along Carrier forward axis (- = toward camera/rump)

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
                if (a[i] == "-jellySeatLift"  && float.TryParse(a[i + 1], out var s)) _seatLift = s;
                if (a[i] == "-jellyZ"         && float.TryParse(a[i + 1], out var z)) _z = z;
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
            var rideIdle = BuildRideIdleClip();
            var ctrl = BuildController(rideIdle);
            var prefab = BuildPrefab(mat, ctrl);
            WireIntoScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Report.Insert(0, $"=== Jelly V01 integration OK (yaw={_yaw}, seatedHeight={_targetHeight}m, seatLift={_seatLift}m, z={_z}m) ===\n");
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
            Require(clips != null && clips.Length > 0, "Jelly source FBX has no animation take");

            var c = clips[0];
            int frames = Mathf.RoundToInt(c.lastFrame - c.firstFrame);
            Require(frames > 1, $"take has only {frames} frame(s) - not a real clip");
            // Keep the Meshy run only as a source/reference take. It is deliberately
            // NOT used by the animator controller; riding has its own authored clip.
            c.name = SourceRunClipName;
            c.loopTime = true;
            mi.clipAnimations = new[] { c };
            EditorUtility.SetDirty(mi);
            mi.SaveAndReimport();
            Report.AppendLine($"fbx model: ...Running_withSkin.fbx rig=Generic source take='{c.takeName}' -> '{SourceRunClipName}' ({frames} frames, NOT used for riding)");

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
        static AnimatorController BuildController(AnimationClip clip)
        {
            Require(clip != null, "could not create Jelly_RideIdle.anim");

            // Always rebuild from scratch - fully deterministic, no half-written state.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath) != null)
                AssetDatabase.DeleteAsset(CtrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
            Require(ctrl != null && ctrl.layers != null && ctrl.layers.Length > 0,
                    "failed to create Jelly.controller with a base layer");

            var sm = ctrl.layers[0].stateMachine;
            var ride = sm.AddState("RideIdle");
            ride.motion = clip;
            ride.speed = 1f;
            sm.defaultState = ride;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Report.AppendLine($"controller: Jelly.controller state 'RideIdle' -> '{clip.name}' (loop, default; source run disconnected)");
            return ctrl;
        }

        static AnimationClip LoadSourceRunClip()
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(ModelFbx);
            return all.OfType<AnimationClip>().FirstOrDefault(c => c.name == SourceRunClipName)
                ?? all.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        }

        /// <summary>
        /// Authors a real seated Generic-rig clip. A single source frame supplies the
        /// rig's valid rest rotations, then the limbs are aimed into a symmetrical
        /// riding pose. Every bone is keyed so no locomotion survives from Meshy's run.
        /// Only a tiny pelvis bob and head sway remain in the loop.
        /// </summary>
        static AnimationClip BuildRideIdleClip()
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx);
            var sourceRun = LoadSourceRunClip();
            Require(sourcePrefab != null && sourceRun != null, "source model/run clip unavailable for RideIdle authoring");

            var go = UnityEngine.Object.Instantiate(sourcePrefab);
            go.name = "Jelly_Model";
            sourceRun.SampleAnimation(go, 0f);
            PoseAsRider(go.transform);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(RideIdlePath) != null)
                AssetDatabase.DeleteAsset(RideIdlePath);
            var clip = new AnimationClip { name = "Jelly_RideIdle", frameRate = 30f };
            const float duration = 1.2f;

            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (t == go.transform) continue;
                string path = AnimationUtility.CalculateTransformPath(t, go.transform);
                Vector3 p = t.localPosition;
                Quaternion q = t.localRotation;
                Vector3 s = t.localScale;

                SetVector3(clip, path, "m_LocalPosition", p, duration);
                SetQuaternion(clip, path, q, q, q, duration);
                SetVector3(clip, path, "m_LocalScale", s, duration);
            }

            var hips = FindDeep(go.transform, "Hips");
            var head = FindDeep(go.transform, "Head");
            Require(hips != null && head != null, "expected Hips and Head bones");

            string hipsPath = AnimationUtility.CalculateTransformPath(hips, go.transform);
            Vector3 hp = hips.localPosition;
            SetCurve(clip, hipsPath, "m_LocalPosition.x", hp.x, hp.x, hp.x, duration);
            SetCurve(clip, hipsPath, "m_LocalPosition.y", hp.y, hp.y + 0.012f, hp.y, duration);
            SetCurve(clip, hipsPath, "m_LocalPosition.z", hp.z, hp.z, hp.z, duration);

            string headPath = AnimationUtility.CalculateTransformPath(head, go.transform);
            Quaternion h0 = head.localRotation;
            Quaternion h1 = h0 * Quaternion.Euler(0f, 0f, 2.5f);
            SetQuaternion(clip, headPath, h0, h1, h0, duration);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, RideIdlePath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            UnityEngine.Object.DestroyImmediate(go);

            Report.AppendLine("animation: Jelly_RideIdle.anim authored from rig: symmetrical bent legs, relaxed riding arms, subtle 1.2s pelvis/head loop");
            return clip;
        }

        static void PoseAsRider(Transform root)
        {
            Vector3 right = root.right, up = root.up, forward = root.forward;
            var leftUp = FindDeep(root, "LeftUpLeg");
            var leftLeg = FindDeep(root, "LeftLeg");
            var leftFoot = FindDeep(root, "LeftFoot");
            var rightUp = FindDeep(root, "RightUpLeg");
            var rightLeg = FindDeep(root, "RightLeg");
            var rightFoot = FindDeep(root, "RightFoot");
            var leftArm = FindDeep(root, "LeftArm");
            var leftFore = FindDeep(root, "LeftForeArm");
            var leftHand = FindDeep(root, "LeftHand");
            var rightArm = FindDeep(root, "RightArm");
            var rightFore = FindDeep(root, "RightForeArm");
            var rightHand = FindDeep(root, "RightHand");
            Require(new[] { leftUp, leftLeg, leftFoot, rightUp, rightLeg, rightFoot,
                            leftArm, leftFore, leftHand, rightArm, rightFore, rightHand }.All(t => t != null),
                    "Jelly skeleton is missing a required limb bone");

            // Thighs move forward and apart; lower legs fold back/down around the
            // Carrier's sides. This produces a genuine seated silhouette, not a
            // frozen stride.
            AimBone(leftUp, leftLeg, (-0.18f * right + 0.78f * forward - 0.60f * up).normalized);
            AimBone(rightUp, rightLeg, (0.18f * right + 0.78f * forward - 0.60f * up).normalized);
            AimBone(leftLeg, leftFoot, (-0.08f * right - 0.34f * forward - 0.94f * up).normalized);
            AimBone(rightLeg, rightFoot, (0.08f * right - 0.34f * forward - 0.94f * up).normalized);

            // Relaxed arms reach toward the saddle/front; elbows remain soft and
            // slightly out so the face and torso stay readable.
            AimBone(leftArm, leftFore, (-0.42f * right + 0.34f * forward - 0.84f * up).normalized);
            AimBone(rightArm, rightFore, (0.42f * right + 0.34f * forward - 0.84f * up).normalized);
            AimBone(leftFore, leftHand, (0.48f * right + 0.44f * forward - 0.76f * up).normalized);
            AimBone(rightFore, rightHand, (-0.48f * right + 0.44f * forward - 0.76f * up).normalized);
        }

        static void AimBone(Transform bone, Transform child, Vector3 desiredWorldDirection)
        {
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(current.normalized, desiredWorldDirection) * bone.rotation;
        }

        static void SetVector3(AnimationClip clip, string path, string property, Vector3 value, float duration)
        {
            SetCurve(clip, path, property + ".x", value.x, value.x, value.x, duration);
            SetCurve(clip, path, property + ".y", value.y, value.y, value.y, duration);
            SetCurve(clip, path, property + ".z", value.z, value.z, value.z, duration);
        }

        static void SetQuaternion(AnimationClip clip, string path, Quaternion a, Quaternion mid, Quaternion end, float duration)
        {
            SetCurve(clip, path, "m_LocalRotation.x", a.x, mid.x, end.x, duration);
            SetCurve(clip, path, "m_LocalRotation.y", a.y, mid.y, end.y, duration);
            SetCurve(clip, path, "m_LocalRotation.z", a.z, mid.z, end.z, duration);
            SetCurve(clip, path, "m_LocalRotation.w", a.w, mid.w, end.w, duration);
        }

        static void SetCurve(AnimationClip clip, string path, string property, float a, float mid, float end, float duration)
        {
            var curve = new AnimationCurve(new Keyframe(0f, a), new Keyframe(duration * 0.5f, mid), new Keyframe(duration, end));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
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

            // Sample the authored seated clip before measuring. Scale is based on
            // the actual visible sitting silhouette, never the Meshy source height.
            var rideIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(RideIdlePath);
            Require(rideIdle != null, "Jelly_RideIdle.anim missing while building prefab");
            rideIdle.SampleAnimation(model, 0f);

            // ---- ONE visual correction on ModelRoot: yaw, uniform scale, pelvis -> seat ----
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
            var hips = FindDeep(model.transform, "Hips");
            Require(hips != null, "Hips bone missing while mounting Jelly");
            Vector3 hipsL = rootInv.MultiplyPoint3x4(hips.position);
            // JellyRoot is the saddle contact: put the pelvis on it, not the feet.
            modelRoot.transform.localPosition = new Vector3(-ctrL.x, _seatLift - hipsL.y, -hipsL.z + _z);

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
            Report.AppendLine($"  seated visual bounds height = {jellyH:F3} m; pelvis at JellySeat + {_seatLift:F2}m (true mounted pose)");
            return prefab;
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

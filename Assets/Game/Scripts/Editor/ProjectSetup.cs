using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RHMG.EditorTools
{
    /// <summary>Idempotent project bootstrap: materials, Main scene, build settings, player settings.</summary>
    public static class ProjectSetup
    {
        public const string ResDir = "Assets/Game/Resources/RHMG";
        public const string ScenePath = "Assets/Game/Scenes/Main.unity";
        public const string BootstrapType = "RHMG.Runtime.GameBootstrap, RHMG.Runtime";
        public const string RootName = "GameRoot";

        [MenuItem("RHMG/Setup Project")]
        public static void EnsureAll()
        {
            EnsureFolders();
            EnsureMaterials();
            EnsureScene();
            EnsureBuildSettings();
            EnsurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[RHMG.Setup] EnsureAll done.");
        }

        public static bool MaterialsMissing()
        {
            return !File.Exists(ResDir + "/Lit.mat") || !File.Exists(ResDir + "/Unlit.mat")
                || !File.Exists(ResDir + "/UnlitTransparent.mat") || !File.Exists(ResDir + "/SpriteUnlit.mat");
        }

        public static bool SceneMissing() { return !File.Exists(ScenePath); }

        static void EnsureFolders()
        {
            foreach (var d in new[] { "Assets/Game/Resources", ResDir, "Assets/Game/Scenes" })
            {
                if (AssetDatabase.IsValidFolder(d)) continue;
                var parent = Path.GetDirectoryName(d).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(d));
            }
        }

        static void EnsureMat(string name, string shaderName, Action<Material> setup)
        {
            string path = ResDir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var sh = Shader.Find(shaderName);
            if (sh == null) { Debug.LogError("[RHMG.Setup] Shader not found: " + shaderName); return; }
            var m = new Material(sh) { name = name };
            if (setup != null) setup(m);
            AssetDatabase.CreateAsset(m, path);
        }

        static void EnsureMaterials()
        {
            EnsureMat("Lit", "Universal Render Pipeline/Simple Lit", null);
            EnsureMat("Unlit", "Universal Render Pipeline/Unlit", null);
            EnsureMat("UnlitTransparent", "Universal Render Pipeline/Unlit", m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
            });
            // Character billboards: 2D unlit sprite shader (works in the 3D URP renderer); Sprites/Default if URP 2D is absent.
            const string spriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
            EnsureMat("SpriteUnlit", Shader.Find(spriteShader) != null ? spriteShader : "Sprites/Default", null);
        }

        static void EnsureScene()
        {
            var bootType = Type.GetType(BootstrapType);
            if (bootType == null) Debug.LogWarning("[RHMG.Setup] GameBootstrap chua ton tai; GameRoot de trong.");

            if (SceneMissing())
            {
                // Batchmode starts with an untitled scene; additive NewScene is refused there.
                bool single = Application.isBatchMode;
                var mode = single ? NewSceneMode.Single : NewSceneMode.Additive;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
                var go = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(go, scene);
                if (bootType != null) go.AddComponent(bootType);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!single) EditorSceneManager.CloseScene(scene, true);
                return;
            }
            if (bootType != null) AttachBootstrapIfMissing(bootType);
        }

        static void AttachBootstrapIfMissing(Type bootType)
        {
            var open = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = open.IsValid() && open.isLoaded;
            var openMode = Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive;
            var scene = wasLoaded ? open : EditorSceneManager.OpenScene(ScenePath, openMode);
            bool dirty = false;
            GameObject root = null;
            foreach (var g in scene.GetRootGameObjects()) if (g.name == RootName) root = g;
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                dirty = true;
            }
            if (root.GetComponent(bootType) == null) { root.AddComponent(bootType); dirty = true; }
            if (dirty) EditorSceneManager.SaveScene(scene);
            if (!wasLoaded && !Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
        }

        static void EnsureBuildSettings()
        {
            var cur = EditorBuildSettings.scenes;
            if (cur.Length == 1 && cur[0].path == ScenePath && cur[0].enabled) return;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "KITTS Games";
            PlayerSettings.productName = "Red Hair Magic Girl";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kitts.redhairmagicgirl");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }

    /// <summary>Runs EnsureAll after load only when Main.unity/materials are missing or bootstrap is not attached yet.</summary>
    [InitializeOnLoad]
    static class ProjectSetupAuto
    {
        static ProjectSetupAuto() { EditorApplication.delayCall += Run; }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            bool need = ProjectSetup.SceneMissing() || ProjectSetup.MaterialsMissing();
            if (!need && Type.GetType(ProjectSetup.BootstrapType) != null) need = !SceneHasBootstrap();
            if (need) ProjectSetup.EnsureAll();
        }

        // Cheap text check on the YAML scene so the scene is not opened on every reload.
        static bool SceneHasBootstrap()
        {
            try { return File.ReadAllText(ProjectSetup.ScenePath).Contains("GameBootstrap") || File.ReadAllText(ProjectSetup.ScenePath).Contains("m_Script"); }
            catch { return true; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using Fusion.Menu;
using Fusion.Photon.Realtime;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Edgegap.SimpleFPS.Editor
{
    /// <summary>
    /// Tools > Edgegap Simple FPS. Sets up Photon's Simple FPS sample for Edgegap dedicated servers without
    /// editing any Simple FPS or Fusion file, and builds the Linux server and a Windows client.
    /// Unity CLI (the editor must be closed on the project):
    ///   -executeMethod Edgegap.SimpleFPS.Editor.SimpleFpsEdgegapSetup.SetUpFromCommandLine
    ///   -executeMethod Edgegap.SimpleFPS.Editor.SimpleFpsEdgegapSetup.BuildServerFromCommandLine
    ///   -executeMethod Edgegap.SimpleFPS.Editor.SimpleFpsEdgegapSetup.BuildClientFromCommandLine
    /// </summary>
    public static class SimpleFpsEdgegapSetup
    {
        const string Log = "[Edgegap Simple FPS]";
        const string StartupScene = "Assets/Scenes/Startup.unity";
        const string DeathmatchScene = "Assets/Scenes/Deathmatch.unity";
        static readonly string[] Scenes = { StartupScene, DeathmatchScene };
        public const string ServerOutput = "Builds/EdgegapServer/ServerBuild";
        public const string ClientOutput = "Builds/Client/SimpleFPS.exe";

        // ---------------------------------------------------------------- menu

        [MenuItem("Tools/Edgegap Simple FPS/Check Project")]
        static void CheckFromMenu()
        {
            var problems = Check();
            EditorUtility.DisplayDialog("Edgegap Simple FPS",
                problems.Count == 0 ? "The project is ready." : string.Join("\n\n", problems), "OK");
        }

        [MenuItem("Tools/Edgegap Simple FPS/Set Up Project")]
        static void SetUpFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetUp();
            EditorUtility.DisplayDialog("Edgegap Simple FPS", "Deathmatch and Startup are set up. See the Console for details.", "OK");
        }

        [MenuItem("Tools/Edgegap Simple FPS/Build Linux Server")]
        static void BuildServerFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (ShowProblems()) return;
            SetUp();
            BuildServer();
        }

        [MenuItem("Tools/Edgegap Simple FPS/Build Windows Client")]
        static void BuildClientFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetUp();
            BuildClient();
        }

        // ---------------------------------------------------------------- CLI

        public static void SetUpFromCommandLine() => Exit(Run(() => { SetUp(); return true; }));

        public static void BuildServerFromCommandLine() => Exit(Run(() =>
        {
            var problems = Check();
            foreach (var p in problems) Debug.LogError($"{Log} {p}");
            if (problems.Count > 0) return false;
            SetUp();
            return BuildServer();
        }));

        public static void BuildClientFromCommandLine() => Exit(Run(() => { SetUp(); return BuildClient(); }));

        static bool Run(Func<bool> action)
        {
            try { return action(); }
            catch (Exception e) { Debug.LogError($"{Log} {e}"); return false; }
        }

        static void Exit(bool ok) => EditorApplication.Exit(ok ? 0 : 1);

        // ---------------------------------------------------------------- checks

        static bool ShowProblems()
        {
            var problems = Check();
            if (problems.Count == 0) return false;
            EditorUtility.DisplayDialog("Edgegap Simple FPS", string.Join("\n\n", problems), "OK");
            return true;
        }

        /// <summary>What blocks a working server build.</summary>
        public static List<string> Check()
        {
            var problems = new List<string>();

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                problems.Add("Install the Linux Dedicated Server build support module in Unity Hub.");

            // Keep the sample's IL2CPP server: Fusion RPCs have failed on Mono servers (MethodAccessException).
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Server) != ScriptingImplementation.IL2CPP)
                problems.Add("Set Project Settings > Player > Dedicated Server > Scripting Backend to IL2CPP.");
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string manifest = File.ReadAllText("Packages/manifest.json");
                bool unity63 = new Version(Application.unityVersion.Split('f', 'b', 'a')[0]) >= new Version(6000, 3);
                if (unity63 && (!manifest.Contains("\"com.unity.toolchain.win-x86_64-linux\"") || !manifest.Contains("\"com.unity.sdk.linux-x86_64\"")))
                    problems.Add("Building the IL2CPP Linux server on Windows needs Unity's Linux toolchain. In Window > Package Manager > + > " +
                                 "Add package by name, add com.unity.toolchain.win-x86_64-linux and com.unity.sdk.linux-x86_64.");
                else if (!unity63 && !manifest.Contains("\"com.unity.toolchain.win-x86_64-linux-x86_64\""))
                    problems.Add("Building the IL2CPP Linux server on Windows needs Unity's Linux toolchain: add the package " +
                                 "com.unity.toolchain.win-x86_64-linux-x86_64 in Window > Package Manager > + > Add package by name.");
            }

            foreach (var scene in Scenes)
                if (!File.Exists(scene))
                    problems.Add($"{scene} is missing. Import Simple FPS Multiplayer (Photon Fusion) from the Asset Store first.");

            if (string.IsNullOrWhiteSpace(PhotonAppSettings.Global?.AppSettings?.AppIdFusion))
                problems.Add("Set your Fusion App ID in Tools > Fusion > Realtime Settings.");

            if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Single)
                problems.Add("Set Peer Mode to Single in Tools > Fusion > Network Project Config.");

            return problems;
        }

        // ---------------------------------------------------------------- setup

        /// <summary>Applies the Edgegap changes to Deathmatch and Startup. Safe to run again.</summary>
        public static void SetUp()
        {
            SetUpDeathmatch();
            SetUpStartup();
            EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            Debug.Log($"{Log} Build profile scenes: Startup (0), Deathmatch (1).");
        }

        static void SetUpDeathmatch()
        {
            var scene = EditorSceneManager.OpenScene(DeathmatchScene, OpenSceneMode.Single);
            var bootstrap = Find<FusionBootstrap>(scene);
            bootstrap = SwapScript<FusionBootstrap>(bootstrap, typeof(EdgegapFusionBootstrap));

            // Players start from the Startup menu. EdgegapFusionBootstrap switches itself to
            // Automatic / Server / 7777 in server builds, so the scene keeps the client setting.
            bootstrap.StartMode = FusionBootstrap.StartModes.UserInterface;
            bootstrap.ServerPort = EdgegapFusionBootstrap.DefaultServerPort;
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"{Log} Deathmatch: FusionBootstrap -> EdgegapFusionBootstrap, Start Mode = User Interface.");
        }

        static void SetUpStartup()
        {
            var scene = EditorSceneManager.OpenScene(StartupScene, OpenSceneMode.Single);
            var party = Find<FusionMenuUIParty>(scene);
            party = SwapScript<FusionMenuUIParty>(party, typeof(EdgegapPartyMenu));

            var joinCard = FindChild(party.transform, "JoinCard");
            var code = FindChild(joinCard, "CodeInputButton");
            var codeField = code.GetComponent<TMP_InputField>();
            ConfigureField(codeField, EdgegapPartyMenu.SessionFieldLength, "Deployment FQDN");

            var region = code.parent.Find("RegionInputButton");
            if (region == null)
            {
                var copy = UnityEngine.Object.Instantiate(code.gameObject, code.parent);
                copy.name = "RegionInputButton";
                copy.transform.SetSiblingIndex(code.GetSiblingIndex() + 1);
                region = copy.transform;
            }
            var regionField = region.GetComponent<TMP_InputField>();
            ConfigureField(regionField, 4, "Region (us, eu...)");

            var so = new SerializedObject(party);
            so.FindProperty("_sessionCodeField").objectReferenceValue = codeField;
            so.FindProperty("_regionCodeField").objectReferenceValue = regionField;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"{Log} Startup: FusionMenuUIParty -> EdgegapPartyMenu, Join card has the FQDN and Region fields.");
        }

        static void ConfigureField(TMP_InputField field, int limit, string placeholder)
        {
            field.characterLimit = limit;
            field.contentType = TMP_InputField.ContentType.Custom;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterValidation = TMP_InputField.CharacterValidation.Regex;
            field.text = string.Empty;
            var so = new SerializedObject(field);
            so.FindProperty("m_RegexValue").stringValue = @"^[A-Za-z0-9\.]";
            so.ApplyModifiedPropertiesWithoutUndo();
            if (field.textComponent != null)
            {
                field.textComponent.fontStyle &= ~FontStyles.UpperCase; // FQDNs are lowercase
                EditorUtility.SetDirty(field.textComponent);
            }
            if (field.placeholder is TMP_Text text)
            {
                text.text = placeholder;
                EditorUtility.SetDirty(text);
            }
            EditorUtility.SetDirty(field);
        }

        /// <summary>
        /// Points a component at a subclass of its script, keeping every serialized value.
        /// A prefab instance can't change a component's script, so its outermost prefab is unpacked first.
        /// </summary>
        static TBase SwapScript<TBase>(TBase component, Type subclass) where TBase : MonoBehaviour
        {
            if (component.GetType() == subclass) return component;

            var go = component.gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                Debug.Log($"{Log} Unpacked prefab instance {root.name} to replace {typeof(TBase).Name}.");
            }

            var script = MonoScript.FromMonoBehaviour(component);
            var target = AssetDatabase.FindAssets($"t:MonoScript {subclass.Name}")
                .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
                .First(s => s != null && s.GetClass() == subclass);

            var so = new SerializedObject(component);
            so.FindProperty("m_Script").objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();

            var swapped = go.GetComponents<TBase>().FirstOrDefault(c => c.GetType() == subclass);
            if (swapped == null)
                throw new InvalidOperationException($"Could not replace {script.name} with {subclass.Name} on {go.name}.");
            return swapped;
        }

        static T Find<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault()
            ?? throw new InvalidOperationException($"No {typeof(T).Name} in {scene.path}.");

        static Transform FindChild(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
            ?? throw new InvalidOperationException($"{name} not found under {root.name}.");

        // ---------------------------------------------------------------- builds

        /// <summary>Linux IL2CPP Server subtarget, same scene list as the client.</summary>
        public static bool BuildServer() =>
            Build(BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Server, NamedBuildTarget.Server, ServerOutput);

        /// <summary>Windows IL2CPP player, for playing and for headless test clients.</summary>
        public static bool BuildClient() =>
            Build(BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player, NamedBuildTarget.Standalone, ClientOutput);

        static bool Build(BuildTarget target, StandaloneBuildSubtarget subtarget, NamedBuildTarget named, string output)
        {
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

            // Building the Server subtarget leaves the editor on it, which would turn the next
            // client build into a headless server, so put the previous subtarget back.
            var previous = EditorUserBuildSettings.standaloneBuildSubtarget;
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes,
                    target = target,
                    subtarget = (int)subtarget,
                    locationPathName = output,
                });
            }
            finally
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = previous;
            }

            var summary = report.summary;
            var built = summary.GetSubtarget<StandaloneBuildSubtarget>();
            bool ok = summary.result == BuildResult.Succeeded && built == subtarget;
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "edgegap-build.txt"),
                $"unity={Application.unityVersion}\ntarget={target}\nsubtarget={built}\n" +
                $"backend={PlayerSettings.GetScriptingBackend(named)}\nscenes={string.Join(",", Scenes)}\n" +
                $"result={summary.result}\nbuilt_utc={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\n");
            Debug.Log($"{Log} {summary.result}: {output} ({target}, {built}, {PlayerSettings.GetScriptingBackend(named)}, " +
                      $"{summary.totalTime.TotalMinutes:0.0} min)");
            return ok;
        }
    }
}

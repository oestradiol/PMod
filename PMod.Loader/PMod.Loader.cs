using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using MelonLoader;
using MonoMod.Utils;

[assembly: AssemblyTitle(PMod.Loader.LInfo.Name)]
[assembly: AssemblyCopyright("Created by " + PMod.Loader.LInfo.Author)]
[assembly: AssemblyVersion(PMod.Loader.LInfo.Version)]
[assembly: AssemblyFileVersion(PMod.Loader.LInfo.Version)]
[assembly: MelonInfo(typeof(PMod.Loader.PModLoader), PMod.Loader.LInfo.Name, PMod.Loader.LInfo.Version, PMod.Loader.LInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]

namespace PMod.Loader
{
    public static class LInfo
    {
        // Loader info
        public const string Name = "PMod.Loader";
        public const string Author = "Davi";
        public const string Version = "1.0.3";

        // PMod info
        internal const string ModName = "PMod";
        internal static string MainLink = "https://davi.codes/vrchat/";
    }

    internal class VersionCheckResponse
    {
        public string Result { get; set; }
        public string Latest { get; set; }
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += PModLoader.VRChat_OnUiManagerInit; }

    public class PModLoader : MelonMod
    {
        private enum ActionCall
        {
            None,
            OnApplicationStart,
            OnApplicationQuit,
            VRChat_OnUiManagerInit,
            OnUpdate,
            OnLateUpdate,
            OnFixedUpdate,
            OnGUI,
            OnPreferencesLoaded,
            OnPreferencesSaved,
            OnSceneWasLoaded,
            OnSceneWasInitialized
        }
        private static Delegate[] ActionCalls = new Delegate[12];
        public override void OnApplicationQuit() => 
            ((Action)ActionCalls[(int)ActionCall.OnApplicationQuit])?.Invoke();
        public static void VRChat_OnUiManagerInit() => 
            ((Action)ActionCalls[(int)ActionCall.VRChat_OnUiManagerInit])?.Invoke();
        public override void OnUpdate() =>
            ((Action)ActionCalls[(int)ActionCall.OnUpdate])?.Invoke();
        public override void OnLateUpdate() => 
            ((Action)ActionCalls[(int)ActionCall.OnLateUpdate])?.Invoke();
        public override void OnFixedUpdate() => 
            ((Action)ActionCalls[(int)ActionCall.OnFixedUpdate])?.Invoke();
        public override void OnGUI() => 
            ((Action)ActionCalls[(int)ActionCall.OnGUI])?.Invoke();
        public override void OnPreferencesLoaded() => 
            ((Action)ActionCalls[(int)ActionCall.OnPreferencesLoaded])?.Invoke();
        public override void OnPreferencesSaved() => 
            ((Action)ActionCalls[(int)ActionCall.OnPreferencesSaved])?.Invoke();
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) => 
            ((Action<int, string>)ActionCalls[(int)ActionCall.OnSceneWasLoaded])?.Invoke(buildIndex, sceneName);
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) => 
            ((Action<int, string>)ActionCalls[(int)ActionCall.OnSceneWasInitialized])?.Invoke(buildIndex, sceneName);

        private static void WaitForUiInit()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
                typeof(UIXManager).GetMethod(nameof(OnApplicationStart))?.Invoke(null, null);
            else
            {
                MelonLogger.Warning("UiExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
                static IEnumerator OnUiManagerInit()
                {
                    while (VRCUiManager.prop_VRCUiManager_0 == null)
                        yield return null;
                    VRChat_OnUiManagerInit();
                }
                MelonCoroutines.Start(OnUiManagerInit());
            }
        }

        public override void OnApplicationStart()
        {
            MelonLogger.Msg(ConsoleColor.Blue, $"Loader's current version: {LInfo.Version}. Checking for updates...");

            Task.Run(async () =>
            {
                try
                {
                    VersionCheckResponse response =
                        JsonConvert.DeserializeObject<VersionCheckResponse>(
                            await new WebClient { Headers = { [HttpRequestHeader.ContentType] = "application/json" } }
                                .UploadStringTaskAsync(new Uri(LInfo.MainLink + "api"), "{\"name\":\"" + LInfo.Name + "\",\"version\":\"" + LInfo.Version + "\"}"));

                    switch (response.Result)
                    {
                        case "UPDATED":
                            MelonLogger.Msg(ConsoleColor.Green, $"The Loader is up to date!");
                            break;
                        case "OUTDATED":
                            MelonLogger.Msg(ConsoleColor.Yellow, $"The Loader is outdated! Latest version: {response.Latest}. Downloading...");
                            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"Mods/{LInfo.Name}.dll"), 
                                new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.Name}.dll"));
                            MelonLogger.Msg(ConsoleColor.Green, $"Successfully updated {LInfo.Name}!");
                            break;
                        default:
                            MelonLogger.Msg(ConsoleColor.DarkRed, $"Welcome, Savitar. What would you want to do today?");
                            break;
                    }
                }
                catch (Exception e) { MelonLogger.Warning("Failed to check and download latest version!\n" + e.ToString()); }
            });

            byte[] bytes = null;
            try
            {
                MelonLogger.Msg(ConsoleColor.Blue, "Attempting to load PMod latest version...");
                bytes = new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.ModName}.dll");
            }
            catch { }
            if (bytes == null) MelonLogger.Error($"Failed to download {LInfo.ModName} from {LInfo.MainLink + $"{LInfo.ModName}.dll"}!");

#if DEBUG
            MelonLogger.Warning("This Assembly was built in Debug Mode! Forcing to load from VRChat main folder.");
            bytes = null;
#endif

            if (bytes == null && File.Exists($"{LInfo.ModName}.dll"))
            {
                MelonLogger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName}.dll in VRChat main folder! Attempting to load it as a last resource...");
                bytes = File.ReadAllBytes($"{LInfo.ModName}.dll");
            }

            if (bytes != null) InitializeAssembly(bytes);
            else MelonLogger.Warning("All attempts to load the assembly failed. PMod won't load.");

            WaitForUiInit();
            ((Action)ActionCalls[(int)ActionCall.OnApplicationStart])?.Invoke();
        }

        private static void InitializeAssembly(byte[] assembly)
        {
            MethodInfo[] methods;
            try
            {
                IEnumerable<Type> types;
                try { types = Assembly.Load(assembly)?.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
                methods = types?.FirstOrDefault(type => type.Name == "Main")?.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (methods == null) throw new NullReferenceException("Couldn't find Main class. Assembly won't load.");
            }
            catch (Exception e) { MelonLogger.Error($"Failed to load Assembly! {e}"); return; }

            foreach (var m in methods)
            {
                var Parameters = m.GetParameters();
                if (Parameters.Length == 0)
                {
                    ActionCalls[(int)(m.Name switch {
                        nameof(OnApplicationStart) => ActionCall.OnApplicationStart,
                        nameof(OnApplicationQuit) => ActionCall.OnApplicationQuit,
                        nameof(VRChat_OnUiManagerInit)  => ActionCall.VRChat_OnUiManagerInit,
                        nameof(OnUpdate) => ActionCall.OnUpdate,
                        nameof(OnLateUpdate) => ActionCall.OnLateUpdate,
                        nameof(OnFixedUpdate) => ActionCall.OnFixedUpdate,
                        nameof(OnGUI) => ActionCall.OnGUI,
                        nameof(OnPreferencesLoaded) => ActionCall.OnPreferencesLoaded,
                        nameof(OnPreferencesSaved) => ActionCall.OnPreferencesSaved,
                        _ => ActionCall.None
                    })] = m.CreateDelegate<Action>();
                }
                else if ((from p in Parameters select p.ParameterType).SequenceEqual(new[] { typeof(int), typeof(string) }))
                {
                    ActionCalls[(int)(m.Name switch
                    {
                        nameof(OnSceneWasLoaded) => ActionCall.OnSceneWasLoaded,
                        nameof(OnSceneWasInitialized) => ActionCall.OnSceneWasInitialized,
                        _ => ActionCall.None
                    })] = m.CreateDelegate<Action<int, string>>();
                }
            }
        }
    }
}
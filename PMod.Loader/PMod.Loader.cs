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
        public const string Version = "1.0.5";

        // PMod info
        internal const string ModName = "PMod";
        internal const string MainLink = "https://davi.codes/vrchat/";
    }

    internal class VersionCheckResponse
    {
        public string Result { get; set; }
        public string Latest { get; set; }
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += PModLoader.VRChat_OnUiManagerInit; }

    public class PModLoader : MelonMod
    {
        internal static MelonLogger.Instance Logger;
        private static readonly Delegate[] ActionCalls = new Delegate[12];
        public override void OnApplicationQuit() => ((Action)ActionCalls[2])?.Invoke();
        public static void VRChat_OnUiManagerInit() => ((Action)ActionCalls[3])?.Invoke();
        public override void OnUpdate() => ((Action)ActionCalls[4])?.Invoke();
        public override void OnLateUpdate() => ((Action)ActionCalls[5])?.Invoke();
        public override void OnFixedUpdate() => ((Action)ActionCalls[6])?.Invoke();
        public override void OnGUI() => ((Action)ActionCalls[7])?.Invoke();
        public override void OnPreferencesLoaded() => ((Action)ActionCalls[8])?.Invoke();
        public override void OnPreferencesSaved() => ((Action)ActionCalls[9])?.Invoke();
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) => ((Action<int, string>)ActionCalls[10])?.Invoke(buildIndex, sceneName);
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) => ((Action<int, string>)ActionCalls[11])?.Invoke(buildIndex, sceneName);

        private static void WaitForUiInit()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
                typeof(UIXManager).GetMethod(nameof(OnApplicationStart))?.Invoke(null, null);
            else
            {
                Logger.Warning("UiExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
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
            Logger = LoggerInstance;
            
            Logger.Msg(ConsoleColor.Blue, $"Loader's current version: {LInfo.Version}. Checking for updates...");

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
                            Logger.Msg(ConsoleColor.Green, $"The Loader is up to date!");
                            break;
                        case "OUTDATED":
                            Logger.Msg(ConsoleColor.Yellow, $"The Loader is outdated! Latest version: {response.Latest}. Downloading...");
                            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"Mods/{LInfo.Name}.dll"), 
                                new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.Name}.dll"));
                            Logger.Msg(ConsoleColor.Green, $"Successfully updated {LInfo.Name}!");
                            break;
                        default:
                            Logger.Msg(ConsoleColor.DarkRed, $"Welcome, Savitar. What would you want to do today?");
                            break;
                    }
                }
                catch (Exception e) { Logger.Warning($"Failed to check and download latest version!\n{e}"); }
            });

            byte[] bytes = null;
            try
            {
                Logger.Msg(ConsoleColor.Blue, "Attempting to load PMod latest version...");
                bytes = new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.ModName}.dll");
            } catch (Exception e) 
            { Logger.Error($"Failed to download {LInfo.ModName} from {LInfo.MainLink + $"{LInfo.ModName}.dll"}! Error: {e}"); }

#if DEBUG
            Logger.Warning("This Assembly was built in Debug Mode! Forcing to load from VRChat main folder.");
            bytes = null;
#endif

            if (bytes == null && File.Exists($"{LInfo.ModName}.dll"))
            {
                Logger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName}.dll in VRChat main folder! Attempting to load it as a last resource...");
                bytes = File.ReadAllBytes($"{LInfo.ModName}.dll");
            }

            if (bytes != null) InitializeAssembly(bytes);
            else Logger.Warning("All attempts to load the assembly failed. PMod won't load.");

            WaitForUiInit();
            ((Action)ActionCalls[1])?.Invoke(); // ActionCall OnApplicationStart
        }

        private static void InitializeAssembly(byte[] assembly)
        {
            MethodInfo[] methods;
            try
            {
                IEnumerable<Type> types;
                try { types = Assembly.Load(assembly).GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
                methods = types.FirstOrDefault(type => type.Name == "Main")?.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (methods == null) throw new NullReferenceException("Couldn't find Main class.");
            }
            catch (Exception e) { Logger.Error($"Failed to load Assembly! Assembly won't load. Error: {e}"); return; }

            foreach (var m in methods)
            {
                var parameters = m.GetParameters();
                if (parameters.Length == 0)
                {
                    ActionCalls[m.Name switch {
                        nameof(OnApplicationStart) => 1,
                        nameof(OnApplicationQuit) => 2,
                        nameof(VRChat_OnUiManagerInit) => 3,
                        nameof(OnUpdate) => 4,
                        nameof(OnLateUpdate) => 5,
                        nameof(OnFixedUpdate) => 6,
                        nameof(OnGUI) => 7,
                        nameof(OnPreferencesLoaded) => 8,
                        nameof(OnPreferencesSaved) => 9,
                        _ => 0
                    }] = m.CreateDelegate<Action>();
                }
                else if ((from p in parameters select p.ParameterType).SequenceEqual(new[] { typeof(int), typeof(string) }))
                {
                    ActionCalls[m.Name switch {
                        nameof(OnSceneWasLoaded) => 10,
                        nameof(OnSceneWasInitialized) => 11,
                        _ => 0
                    }] = m.CreateDelegate<Action<int, string>>();
                }
            }
        }
    }
}
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
        public const string Version = "1.0.1";

        // PMod info
        internal const string ModName = "PMod";
        internal static string MainLink = "https://davi.codes/vrchat/";
    }

    public class VersionCheckResponse
    {
        public string Result { get; set; }
        public string Latest { get; set; }
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += PModLoader.VRChat_OnUiManagerInit; }

    public class PModLoader : MelonMod
    {
        private static Action _onApplicationStart;
        private static Action _onUiManagerInit;
        private static Action _onFixedUpdate;
        private static Action _onUpdate;
        private static Action _onLateUpdate;
        private static Action _onGUI;
        private static Action _onApplicationQuit;
        private static Action _onPreferencesLoaded;
        private static Action _onPreferencesSaved;
        private static Action<int, string> _onSceneWasLoaded;
        private static Action<int, string> _onSceneWasInitialized;
        public static void VRChat_OnUiManagerInit() => _onUiManagerInit?.Invoke();
        public override void OnFixedUpdate() => _onFixedUpdate?.Invoke();
        public override void OnUpdate() => _onUpdate?.Invoke();
        public override void OnLateUpdate() => _onLateUpdate?.Invoke();
        public override void OnGUI() => _onGUI?.Invoke();
        public override void OnApplicationQuit() => _onApplicationQuit?.Invoke();
        public override void OnPreferencesLoaded() => _onPreferencesLoaded?.Invoke();
        public override void OnPreferencesSaved() => _onPreferencesSaved?.Invoke();
        public override void OnSceneWasLoaded(int buildIndex, string sceneName) => _onSceneWasLoaded?.Invoke(buildIndex, sceneName);
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) => _onSceneWasInitialized?.Invoke(buildIndex, sceneName);
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
                            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"Mods/{LInfo.Name}.dll"), new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.Name}.dll"));
                            MelonLogger.Msg(ConsoleColor.Green, $"Successfully updated {LInfo.Name}!");
                            break;
                        default:
                            MelonLogger.Msg(ConsoleColor.DarkRed, $"Welcome, Savitar. What would you want to do today?");
                            break;
                    }
                } 
                catch (Exception e) 
                { MelonLogger.Warning("Failed to check and download latest version!\n" + e.ToString()); }
            });

            byte[] bytes = null;
            try
            {
                MelonLogger.Msg(ConsoleColor.Blue, "Attempting to load PMod latest version...");
                bytes = new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.ModName}.dll");
            } catch { }
            if (bytes == null)
                MelonLogger.Error($"Failed to download {LInfo.ModName} from {LInfo.MainLink + $"{LInfo.ModName}.dll"}!");

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
            _onApplicationStart?.Invoke();
        }

        private static void InitializeAssembly(byte[] assembly)
        {
            MethodInfo[] methods;
            try
            {
                IEnumerable<Type> types;
                try { types = Assembly.Load(assembly)?.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
                methods = types.FirstOrDefault(type => type.Name == "Main")?.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to load Assembly! {e}");
                return;
            }
            if (methods == null)
            {
                MelonLogger.Error($"Couldn't find Main class. Assembly won't load.");
                return;
            }

            foreach (var m in methods)
            {
                var parameters = m.GetParameters();
                switch (m.Name)
                {
                    case nameof(OnApplicationStart) when parameters.Length == 0:
                        _onApplicationStart += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnApplicationQuit) when parameters.Length == 0:
                        _onApplicationQuit += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnSceneWasLoaded) when parameters.Length == 2 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(string):
                        _onSceneWasLoaded += (Action<int, string>)Delegate.CreateDelegate(typeof(Action<int, string>), m);
                        break;
                    case nameof(OnSceneWasInitialized) when parameters.Length == 2 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(string):
                        _onSceneWasInitialized += (Action<int, string>)Delegate.CreateDelegate(typeof(Action<int, string>), m);
                        break;
                    case nameof(OnUpdate) when parameters.Length == 0:
                        _onUpdate += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(VRChat_OnUiManagerInit) when parameters.Length == 0:
                        _onUiManagerInit += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnGUI) when parameters.Length == 0:
                        _onGUI += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnLateUpdate) when parameters.Length == 0:
                        _onLateUpdate += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnFixedUpdate) when parameters.Length == 0:
                        _onFixedUpdate += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnPreferencesLoaded) when parameters.Length == 0:
                        _onPreferencesLoaded += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                    case nameof(OnPreferencesSaved) when parameters.Length == 0:
                        _onPreferencesSaved += (Action)Delegate.CreateDelegate(typeof(Action), m);
                        break;
                }
            }
        }
    }
}

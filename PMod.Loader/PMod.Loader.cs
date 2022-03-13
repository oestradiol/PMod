#region Imports, Info & Namespace
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MelonLoader;
using VRC;
using LInfo = PMod.Loader.LInfo;

[assembly: AssemblyTitle(LInfo.Name)]
[assembly: AssemblyCopyright($"Created by {LInfo.Author}")]
[assembly: AssemblyVersion(LInfo.Version)]
[assembly: AssemblyFileVersion(LInfo.Version)]
[assembly: MelonInfo(typeof(PMod.Loader.PModLoader), LInfo.Name, LInfo.Version, LInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(LInfo.MelonColor)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]

namespace PMod.Loader;

public static class LInfo
{
    // Loader info
    public const string Name = "PMod.Loader";
    public const string Author = "Davi";
    public const string Version = "1.0.7";
    public const ConsoleColor MelonColor = ConsoleColor.DarkMagenta;

    // PMod info
    internal const string ModName = "PMod";
    internal const string MainLink = "https://davi.codes/vrchat/";
}
#endregion

public class PModLoader : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = new(LInfo.Name, LInfo.MelonColor);

    private PModLoader()
    {
        Logger.Msg("Initializing PMod.Loader!");
        
        Task.Run(UpdateLoader);
        if (!InitializeAssembly(GetPModAssembly())) return;

        dynamic onAppStartPMod = Events[nameof(OnApplicationStart)];
        Events[nameof(OnApplicationStart)] = () =>
        {
            CustomEvents.OnApplicationStart(); 
            onAppStartPMod();
        };
    }

    #region Remotes
    private static async void UpdateLoader() {
        try
        {
            Logger.Msg(ConsoleColor.Blue, $"Loader's current version: {LInfo.Version}. Checking for updates...");
            
            var current = new Hashtable { {"name", LInfo.Name}, {"version", LInfo.Version} };
            var response = JsonConvert.DeserializeObject<(string Result, string Latest)>(
                    await new WebClient { Headers = { [HttpRequestHeader.ContentType] = "application/json" } }
                        .UploadStringTaskAsync(new Uri(LInfo.MainLink + "api"), JsonConvert.SerializeObject(current)));
            
            switch (response!.Result)
            {
                case "UPDATED":
                    Logger.Msg(ConsoleColor.Green, "The Loader is up to date!");
                    break;
                case "OUTDATED":
                    Logger.Msg(ConsoleColor.Yellow, $"The Loader is outdated! Latest version: {response.Latest}. Downloading...");
                    File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"Mods/{LInfo.Name}.dll"), new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.Name}.dll"));
                    Logger.Msg(ConsoleColor.Green, $"Successfully updated {LInfo.Name}!");
                    break;
                default:
                    Logger.Msg(ConsoleColor.DarkRed, "Welcome, Savitar. What would you want to do today?");
                    break;
            }
        }
        catch (Exception e) { Logger.Warning($"Failed to check and download latest version!\n{e}"); }
    }

    private static Assembly GetPModAssembly()
    {
        byte[] bytes;
        try
        {
            Logger.Msg(ConsoleColor.Blue, "Attempting to load PMod latest version...");
            bytes = new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.ModName}.dll");
        } 
        catch (Exception e)
        { Logger.Error($"Failed to download {LInfo.ModName} from {LInfo.MainLink + $"{LInfo.ModName}.dll"}! Error: {e}"); }

        #if DEBUG
            Logger.Warning("This Assembly was built in Debug Mode! Forcing to load from VRChat main folder.");
            bytes = null;
        #endif

        if (bytes != null)
            return Assembly.Load(bytes);
        
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName.Contains(LInfo.ModName + ','));
        if (assembly != null)
        {
            Logger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName} loaded in the GAC! Attempting to use it...");
            return assembly;
        }
        
        if (!File.Exists($"{LInfo.ModName}.dll"))
        {
            Logger.Warning("All attempts to load the assembly failed. PMod won't load.");
            return null;
        }
        
        Logger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName}.dll in VRChat folder! Attempting to load it as a last resource...");
        return Assembly.Load(File.ReadAllBytes($"{LInfo.ModName}.dll"));
    }
    #endregion
    
    #region Assembly Initialization
    private static bool InitializeAssembly(Assembly assembly)
    {
        if (assembly == null)
            return false;
        
        IEnumerable<MethodInfo> methods;
        try
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
            
            var desiredMethods = typeof(PModLoader).GetMethods().Select(m => m.Name);
            methods = types.FirstOrDefault(type => type.Name == "Main")?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => desiredMethods.Contains(m.Name));
            
            if (methods == null) 
                throw new NullReferenceException("Couldn't find Main class.");
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to load Assembly! Assembly won't load. Error: {e}"); 
            return false;
        }
        
        foreach (var m in methods)
            Events.Add(m.Name, m.GetDelegateForMethodInfo());

        return true;
    }
    
    private static readonly Hashtable Events = new();
    
    // Melon Events
    public override void OnApplicationStart() => ((dynamic)Events[nameof(OnApplicationStart)]).Invoke();
    public override void OnApplicationQuit() => ((dynamic)Events[nameof(OnApplicationQuit)])?.Invoke();
    public override void OnUpdate() => ((dynamic)Events[nameof(OnUpdate)])?.Invoke();
    public override void OnLateUpdate() => ((dynamic)Events[nameof(OnLateUpdate)])?.Invoke();
    public override void OnFixedUpdate() => ((dynamic)Events[nameof(OnFixedUpdate)])?.Invoke();
    public override void OnGUI() => ((dynamic)Events[nameof(OnGUI)])?.Invoke();
    public override void OnPreferencesLoaded() => ((dynamic)Events[nameof(OnPreferencesLoaded)])?.Invoke();
    public override void OnPreferencesSaved() => ((dynamic)Events[nameof(OnPreferencesSaved)])?.Invoke();
    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => ((dynamic)Events[nameof(OnSceneWasLoaded)])?.Invoke(buildIndex, sceneName);
    public override void OnSceneWasInitialized(int buildIndex, string sceneName) => ((dynamic)Events[nameof(OnSceneWasInitialized)])?.Invoke(buildIndex, sceneName);
    
    // Custom Events
    public static void VRChat_OnUiManagerInit() => ((dynamic)Events[nameof(VRChat_OnUiManagerInit)])?.Invoke();
    public static void OnPlayerJoined(Player player) => ((dynamic)Events[nameof(OnPlayerJoined)])?.Invoke(player);
    public static void OnPlayerLeft(Player player) => ((dynamic)Events[nameof(OnPlayerLeft)])?.Invoke(player);
    #endregion
}
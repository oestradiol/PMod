#region Imports, Info & Namespace
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using MelonLoader;
using VRC;
using VRC.Core;
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
    // General Info
    public const ConsoleColor MelonColor = ConsoleColor.DarkMagenta;
    internal const string MainLink = "https://davi.codes/vrchat/";
    
    // Loader info
    public const string Name = "PMod.Loader";
    public const string Author = "Davi";
    public const string Version = "1.0.9";

    // PMod info
    internal const string ModName = "PMod";
}
#endregion

public class PModLoader : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = new(LInfo.Name, LInfo.MelonColor);
    internal static MelonMod Loader;

    private PModLoader()
    {
        Loader = this;
        Logger.Msg("Initializing PMod.Loader!");
        
        Task.Run(Remotes.UpdateLoader);
        if (!InitializeAssembly(Remotes.GetPModAssembly())) return;

        dynamic onAppStartPMod = Events[nameof(OnApplicationStart)];
        Events[nameof(OnApplicationStart)] = (Action)(() =>
        {
            CustomEvents.OnApplicationStart();
            Remotes.AppHasStarted = true;
            onAppStartPMod?.Invoke();
        });
    }

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
    
    private static readonly Dictionary<string, dynamic> Events = new();
    
    // Melon Events
    public override void OnApplicationStart() => Events[nameof(OnApplicationStart)]?.Invoke();
    public override void OnApplicationQuit() => Events[nameof(OnApplicationQuit)]?.Invoke();
    public override void OnUpdate() => Events[nameof(OnUpdate)]?.Invoke();
    public override void OnLateUpdate() => Events[nameof(OnLateUpdate)]?.Invoke();
    public override void OnFixedUpdate() => Events[nameof(OnFixedUpdate)]?.Invoke();
    public override void OnGUI() => Events[nameof(OnGUI)]?.Invoke();
    public override void OnPreferencesLoaded() => Events[nameof(OnPreferencesLoaded)]?.Invoke();
    public override void OnPreferencesSaved() => Events[nameof(OnPreferencesSaved)]?.Invoke();
    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => Events[nameof(OnSceneWasLoaded)]?.Invoke(buildIndex, sceneName);
    public override void OnSceneWasInitialized(int buildIndex, string sceneName) => Events[nameof(OnSceneWasInitialized)]?.Invoke(buildIndex, sceneName);
    
    // Custom Events
    public static void VRChat_OnUiManagerInit() => Events[nameof(VRChat_OnUiManagerInit)]?.Invoke();
    public static void OnPlayerJoined(Player player) => Events[nameof(OnPlayerJoined)]?.Invoke(player);
    public static void OnPlayerLeft(Player player) => Events[nameof(OnPlayerLeft)]?.Invoke(player);
    public static void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => Events[nameof(OnInstanceChanged)]?.Invoke(world, instance);
}
#region Imports, Info & Namespace
using PMod.Loader;
using System;
using System.Reflection;
using MelonLoader;
using UIExpansionKit.API;
using VRC;
using VRC.Core;

[assembly: AssemblyTitle(PMod.BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {PMod.BuildInfo.Author}")]
[assembly: AssemblyVersion(PMod.BuildInfo.Version)]
[assembly: AssemblyFileVersion(PMod.BuildInfo.Version)]

namespace PMod;

public static class BuildInfo
{
    public const string Name = "PMod";
    public const string Author = "Davi";
    public const string Version = "1.4.3";
}
#endregion

// TODO: Run benchmark on each event // Was done, might have a memory leakage problem.
// TODO: ItemGrabber Patch for Udon components. 
// TODO: OrbitItem to MonoBehaviour

public static class Main
{
    internal static readonly MelonLogger.Instance Logger = new(BuildInfo.Name, LInfo.MelonColor);
    internal static ICustomShowableLayoutedMenu ClientMenu;

    public static void OnApplicationStart()
    {
        MelonPreferences.CreateCategory(BuildInfo.Name, $"{BuildInfo.Name} - Base");
        ClientMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        ClientMenu.AddSimpleButton("Close Menu", ClientMenu.Hide);
        ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton(BuildInfo.Name, () => ClientMenu.Show());
        ModulesManager.Initialize();
        Patches.OnApplicationStart();
        Logger.Msg(ConsoleColor.Green, "Successfully loaded!");
    }
    
    public static void OnPreferencesSaved() => ModulesManager.OnPreferencesSaved?.Invoke();
    public static void OnSceneWasLoaded(int buildIndex, string sceneName) => ModulesManager.OnSceneWasLoaded?.Invoke(buildIndex, sceneName);
    public static void OnUpdate() => ModulesManager.OnUpdate?.Invoke();
    public static void OnFixedUpdate() { }
    public static void OnLateUpdate() { }
    public static void OnGUI() { }
    public static void OnApplicationQuit() { }
    public static void OnPreferencesLoaded() { }
    public static void OnSceneWasInitialized(int buildIndex, string sceneName) { }
    public static void VRChat_OnUiManagerInit() => ModulesManager.OnUiManagerInit?.Invoke();
    public static void OnPlayerJoined(Player player) => ModulesManager.OnPlayerJoined?.Invoke(player);
    public static void OnPlayerLeft(Player player) => ModulesManager.OnPlayerLeft?.Invoke(player);
    public static void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => ModulesManager.OnInstanceChanged?.Invoke(world, instance);
}
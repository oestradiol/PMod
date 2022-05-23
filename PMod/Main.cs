#region Imports, Info & Namespace
using PMod.Loader;
using System;
using System.Reflection;
using MelonLoader;
using PMod.Modules;
using PMod.Utils;
using VRC;
using VRC.Core;

using BuildInfo = PMod.BuildInfo;
[assembly: AssemblyTitle(BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {BuildInfo.Author}")]
[assembly: AssemblyVersion(BuildInfo.Version)]
[assembly: AssemblyFileVersion(BuildInfo.Version)]

namespace PMod;

public static class BuildInfo
{
    public const string Name = "PMod";
    public const string Author = "Davi";
    public const string Version = "1.4.4";
}
#endregion

// TODO: Implement support for constructors.
public static class Main
{
    internal static readonly MelonLogger.Instance Logger = new(BuildInfo.Name, LInfo.MelonColor);

    public static void OnPreSupportModule() => Manager.OnPreSupportModule();
    public static void OnApplicationStart() // TODO: Clean up when implementing constructors
    {
        MelonPreferences.CreateCategory(BuildInfo.Name, $"{BuildInfo.Name} - Base");
        UiUtils.Init();
        Manager.Init();
        Patches.Init();
        Logger.Msg(ConsoleColor.Green, "Successfully loaded!");
        Manager.OnApplicationStart();
    }
    public static void OnApplicationLateStart() => Manager.OnApplicationLateStart();
    // TODO: Save settings on application quit.
    public static void OnApplicationQuit() => Manager.OnApplicationQuit();
    public static void OnUpdate() => Manager.OnUpdate();
    public static void OnLateUpdate() => Manager.OnLateUpdate();
    public static void OnFixedUpdate() => Manager.OnFixedUpdate();
    public static void OnGUI() => Manager.OnGUI();
    public static void OnPreferencesLoaded(string filePath) => Manager.OnPreferencesLoaded(filePath);
    public static void OnPreferencesSaved(string filePath) => Manager.OnPreferencesSaved(filePath);
    public static void OnSceneWasLoaded(int buildIndex, string sceneName) => Manager.OnSceneWasLoaded(buildIndex, sceneName);
    public static void OnSceneWasInitialized(int buildIndex, string sceneName) => Manager.OnSceneWasInitialized(buildIndex, sceneName);
    public static void OnSceneWasUnloaded(int buildIndex, string sceneName) => Manager.OnSceneWasUnloaded(buildIndex, sceneName);
    
    
    // TODO: Add Unity and VRChat UiManagerInits.
    public static void VRChat_OnUiManagerInit() => Manager.OnUiManagerInit();
    public static void OnPlayerJoined(Player player) => Manager.OnPlayerJoined(player);
    public static void OnPlayerLeft(Player player) => Manager.OnPlayerLeft(player);
    public static void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => Manager.OnInstanceChanged(world, instance);
}
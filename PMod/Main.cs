#region Imports, Info & Namespace
using PMod.Loader;
using System;
using System.Reflection;
using MelonLoader;
using UIExpansionKit.API;
using VRC;
using VRC.Core;
using Utilities = PMod.Utils.Utilities;

[assembly: AssemblyTitle(PMod.BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {PMod.BuildInfo.Author}")]
[assembly: AssemblyVersion(PMod.BuildInfo.Version)]
[assembly: AssemblyFileVersion(PMod.BuildInfo.Version)]

namespace PMod;

public static class BuildInfo
{
    public const string Name = "PMod";
    public const string Author = "Davi";
    public const string Version = "1.4.1";
}
#endregion

public static class Main
{
    internal static readonly MelonLogger.Instance Logger = new(BuildInfo.Name, LInfo.MelonColor);
    internal static ICustomShowableLayoutedMenu ClientMenu;

    public static void OnApplicationStart()
    {
        ModulesManager.Initialize();
        Patches.OnApplicationStart();
        Logger.Msg(ConsoleColor.Green, "Loaded Successfully!");
    }
    public static void VRChat_OnUiManagerInit()
    {
        ModulesManager.OnUiManagerInit();
        ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton(BuildInfo.Name, ShowModMenu);
    }
    public static void OnPreferencesSaved() => ModulesManager.OnPreferencesSaved();
    public static void OnSceneWasLoaded(int buildIndex, string sceneName) => ModulesManager.OnSceneWasLoaded(buildIndex, sceneName);
    public static void OnUpdate() => ModulesManager.OnUpdate();
    public static void OnFixedUpdate() { }
    public static void OnLateUpdate() { }
    public static void OnGUI() { }
    public static void OnApplicationQuit() { }
    public static void OnPreferencesLoaded() { }
    public static void OnSceneWasInitialized(int buildIndex, string sceneName) { }
    public static void OnPlayerJoined(Player player) => ModulesManager.OnPlayerJoined(player);
    public static void OnPlayerLeft(Player player) => ModulesManager.OnPlayerLeft(player);
    public static void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => ModulesManager.OnInstanceChanged(world, instance);

    private static void ShowModMenu() {
        if (ClientMenu == null)
        {
            ClientMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            ClientMenu.AddSimpleButton("Close Menu", ClientMenu.Hide);
            ClientMenu.AddSimpleButton("Orbit", () =>
            {
                if (ModulesManager.orbit.IsOn.Value) ModulesManager.orbit.OrbitMenu.Show();
                else Utilities.RiskyFuncAlert("Orbit");
            });
            ClientMenu.AddSimpleButton("ItemGrabber", () =>
            {
                if (ModulesManager.itemGrabber.IsOn.Value) ModulesManager.itemGrabber.PickupMenu.Show();
                else Utilities.RiskyFuncAlert("ItemGrabber");
            });
            ClientMenu.AddSimpleButton("PhotonFreeze", () =>
            {
                if (ModulesManager.photonFreeze.IsOn.Value) ModulesManager.photonFreeze.ShowFreezeMenu();
                else Utilities.RiskyFuncAlert("PhotonFreeze");
            });
            ClientMenu.AddSimpleButton("Triggers", () => ModulesManager.triggers.ShowTriggersMenu());
        }
        ClientMenu.Show();
    }
}
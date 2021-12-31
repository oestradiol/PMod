using PMod.Utils;
using PMod.Loader;
using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UIExpansionKit.API;
using VRC;
using VRC.Core;
using Utilities = PMod.Utils.Utilities;

[assembly: AssemblyTitle(PMod.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + PMod.BuildInfo.Author)]
[assembly: AssemblyVersion(PMod.BuildInfo.Version)]
[assembly: AssemblyFileVersion(PMod.BuildInfo.Version)]

namespace PMod
{
    public static class BuildInfo
    {
        public const string Name = "PMod";
        public const string Author = "Davi, Lily, Arion";
        public const string Version = "1.3.3";
    }

    public static class Main
    {
        internal static HarmonyLib.Harmony HInstance => MelonHandler.Mods.First(m => m.Info.Name == LInfo.Name).HarmonyInstance;
        internal static ICustomShowableLayoutedMenu ClientMenu;

        public static void OnApplicationStart()
        {
            NetworkEvents.OnPlayerJoinedAction += OnPlayerJoined;
            NetworkEvents.OnPlayerLeftAction += OnPlayerLeft;
            NetworkEvents.OnInstanceChangedAction += OnInstanceChanged;
            ModulesManager.Initialize();
            NativePatches.OnApplicationStart();
            PLogger.Msg(ConsoleColor.Green, $"{BuildInfo.Name} Loaded Successfully!");
        }
        public static void VRChat_OnUiManagerInit()
        {
            NetworkEvents.OnUiManagerInit();
            ModulesManager.OnUiManagerInit();
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton($"{BuildInfo.Name}", () => {
                ClientMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
                ClientMenu.AddSimpleButton("Close Menu", ClientMenu.Hide);
                //ClientMenu.AddSimpleButton("Beep", () => SendRPC("GetBeepedLol"));
                if (ModulesManager.invisibleJoin.IsOn.Value)
                    ClientMenu.AddToggleButton("Always Join Invisible?", (isOn) => ModulesManager.invisibleJoin.SetJoinMode(isOn), () => !ModulesManager.invisibleJoin.onceOnly);
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
                ClientMenu.Show();
            });
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

        // Got from KiraiLib https://github.com/xKiraiChan/KiraiLibs/blob/9ccba43ca646860ec87f07ae364b81a87f568f5d/KiraiRPC/SendRPC.cs#L28
        // Be careful using this since I literally didn't add check for Moderators lol
        //public static void SendRPC(string raw)
        //{
        //    PLogger.Msg("Sending RPC...");
        //    var handler = UnityEngine.Object.FindObjectOfType<VRC_EventHandler>();
        //    handler.TriggerEvent(
        //        new VRC_EventHandler.VrcEvent
        //        {
        //            EventType = VRC_EventHandler.VrcEventType.SendRPC,
        //            Name = "SendRPC",
        //            ParameterObject = handler.gameObject,
        //            ParameterInt = Player.prop_Player_0.field_Private_VRCPlayerApi_0.playerId,
        //            ParameterFloat = 0f,
        //            ParameterString = "UdonSyncRunProgramAsRPC",
        //            ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.Unused,
        //            ParameterBytes = Networking.EncodeParameters(new Il2CppSystem.Object[] {raw})
        //        },
        //        VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered, 
        //        Utilities.GetLocalVRCPlayer().gameObject, 
        //        0f);
        //}
    }
}
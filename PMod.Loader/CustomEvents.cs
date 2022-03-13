using System;
using System.Collections;
using System.Linq;
using MelonLoader;
using VRC;

namespace PMod.Loader;

internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += PModLoader.VRChat_OnUiManagerInit; }

internal static class CustomEvents
{
    internal static void OnApplicationStart()
    {
        if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
            typeof(UIXManager).GetMethod(nameof(PModLoader.OnApplicationStart))?.Invoke(null, null);
        else
        {
            PModLoader.Logger.Warning("UiExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
            static IEnumerator OnUiManagerInitIEnum()
            {
                while (VRCUiManager.prop_VRCUiManager_0 == null)
                    yield return null;
                OnUiManagerInit();
            }
            MelonCoroutines.Start(OnUiManagerInitIEnum());
        }
    }
    
    private static void OnUiManagerInit()
    {
        NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.
            field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerA);
        NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.
            field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerB);
        PModLoader.VRChat_OnUiManagerInit();
    }

    private static Action<Player> _eventHandlerA;
    private static Action<Player> _eventHandlerB;
    private static Action<Player> EventHandlerA
    {
        get
        {
            _eventHandlerB ??= PModLoader.OnPlayerLeft;
            return _eventHandlerA ??= PModLoader.OnPlayerJoined;
        }
    }
    private static Action<Player> EventHandlerB
    {
        get
        {
            _eventHandlerA ??= PModLoader.OnPlayerLeft;
            return _eventHandlerB ??= PModLoader.OnPlayerJoined;
        }
    }
}
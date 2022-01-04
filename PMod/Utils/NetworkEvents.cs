using System;
using VRC;

namespace PMod.Utils
{
    internal static class NetworkEvents
    {
        internal static void OnUiManagerInit()
        {
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.
                field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerA);
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.
                field_Private_HashSet_1_UnityAction_1_T_0.Add(EventHandlerB);
        }

        private static Action<Player> _eventHandlerA;
        private static Action<Player> _eventHandlerB;
        private static Action<Player> EventHandlerA
        {
            get
            {
                _eventHandlerB ??= Main.OnPlayerLeft;
                return _eventHandlerA ??= Main.OnPlayerJoined;
            }
        }
        private static Action<Player> EventHandlerB
        {
            get
            {
                _eventHandlerA ??= Main.OnPlayerLeft;
                return _eventHandlerB ??= Main.OnPlayerJoined;
            }
        }
    }
}

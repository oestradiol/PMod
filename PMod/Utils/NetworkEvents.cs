using System;
using System.Reflection;
using HarmonyLib;
using VRC;
using VRC.Core;

namespace PMod.Utils
{
    internal static class NetworkEvents
    {
        internal static event Action<Player> OnPlayerJoinedAction;
        internal static event Action<Player> OnPlayerLeftAction;
        internal static event Action<ApiWorld, ApiWorldInstance> OnInstanceChangedAction;
        private static void OnInstanceChangeMethod(ApiWorld __0, ApiWorldInstance __1) => OnInstanceChangedAction?.Invoke(__0, __1);
        internal static void OnUiManagerInit()
        {
            //PatchMethods();
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.
                field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)EventHandlerA);
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.
                field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)EventHandlerB);
            Main.HInstance.Patch(typeof(RoomManager).GetMethod(nameof(RoomManager.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_String_Int32_0)), null,
                new HarmonyMethod(typeof(NetworkEvents).GetMethod(nameof(OnInstanceChangeMethod), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static bool _seenFire;
        private static bool _aFiredFirst;
        private static void EventHandlerA(Player player)
        {
            if (!_seenFire)
            {
                _aFiredFirst = true;
                _seenFire = true;
            }

            (_aFiredFirst ? OnPlayerJoinedAction : OnPlayerLeftAction)?.Invoke(player);
        }
        private static void EventHandlerB(Player player)
        {
            if (!_seenFire)
            {
                _aFiredFirst = false;
                _seenFire = true;
            }

            (_aFiredFirst ? OnPlayerLeftAction : OnPlayerJoinedAction)?.Invoke(player);
        }

        //// Needs change to which method it patches, because only works at first instance join for now.
        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate void PlayerActionDelegate(IntPtr thisPtr, IntPtr playerPtr, IntPtr nativeMInfo);
        //private static readonly System.Collections.Generic.List<PlayerActionDelegate> dontGCDelegates = new();
        //private unsafe static void PatchMethods()
        //{
        //    void ApplyPatch(MethodInfo mInfo, bool IsJoin)
        //    {
        //        PlayerActionDelegate tempMethod, originalMethod = null;
        //        dontGCDelegates.Add(tempMethod = (thisPtr, playerPtr, nativeMInfo) =>
        //        {   
        //            var Player = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<Player>(playerPtr);
        //            (IsJoin ? OnPlayerJoinedAction : OnPlayerLeftAction)?.Invoke(Player);
        //            originalMethod.Invoke(thisPtr, playerPtr, nativeMInfo);
        //            Loader.PLogger.Msg("Called OnVRCPlayer" + (IsJoin ? "Join" : "Left") + $" for {Player.field_Private_APIUser_0.displayName}! MethodInfo: {mInfo.Name}.");
        //        });
        //        originalMethod = NativePatchUtils.Patch<PlayerActionDelegate>(mInfo, Marshal.GetFunctionPointerForDelegate(tempMethod));
        //    }

        //    var mIEnum = typeof(NetworkManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
        //        m => m.ReturnType == typeof(void) && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Player));
        //    bool FirstIsJoin = XrefScanner.XrefScan(mIEnum.First()).Where(instance => instance.Type == XrefType.Global)
        //            .Select(instance => instance.ReadAsObject()?.ToString()).Any(s => s == "OnPlayerJoined {0}");
        //    ApplyPatch(mIEnum.First(), FirstIsJoin);
        //    ApplyPatch(mIEnum.Last(), !FirstIsJoin);
        //}
    }
}

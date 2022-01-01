using PMod.Loader;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using MelonLoader;
using VRC.Core;
using VRC.SDKBase;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace PMod.Utils
{
    internal static class NativePatchUtils
    {
        internal static unsafe TDelegate Patch<TDelegate>(MethodInfo originalMethod, IntPtr patchDetour) where TDelegate : Delegate
        {
            var original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
            MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        }

        internal static IntPtr GetDetour<TClass>(string patchName)
            where TClass : class => typeof(TClass).GetMethod(patchName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
            .MethodHandle.GetFunctionPointer();
    }

    internal class NativePatches
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr OnEventDelegate(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr RaiseEventDelegate(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptionsPtr, IntPtr sOptionsPtr, IntPtr nativeMethodInfoPtr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LocalToGlobalDelegate(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr);
        private static OnEventDelegate _onEventDelegate;
        private static RaiseEventDelegate _raiseEventDelegate;
        private static LocalToGlobalDelegate _localToGlobalDelegate;
        internal static void OnApplicationStart()
        {
            _onEventDelegate = NativePatchUtils.Patch<OnEventDelegate>(typeof(VRCNetworkingClient)
                .GetMethod(nameof(VRCNetworkingClient.OnEvent)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(OnEventSetup)));

            _raiseEventDelegate = NativePatchUtils.Patch<RaiseEventDelegate>(typeof(LoadBalancingClient)
                .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(RaiseEventSetup)));

            _localToGlobalDelegate = NativePatchUtils.Patch<LocalToGlobalDelegate>(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(LocalToGlobalSetup)));
        }

        private static bool _turnOffNext;
        private static void OnEventSetup(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
        {
            var eventData = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<EventData>(eventDataPtr);
            switch (eventData.Code)
            {
                case 7:
                    try
                    {
                        Timer entry = null;
                        var key = Utilities.GetPlayerFromPhotonID(eventData.Sender)?.field_Private_APIUser_0.id;
                        if (key != null) ModulesManager.frozenPlayersManager.EntryDict.TryGetValue(key, out entry);
                        entry?.RestartTimer();
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in OnEvent7 Detour (FrozenPlayersManager)");
                        PLogger.Error($"{e}");
                    }
                    break;
                case 253: // Thanks to Yui! <3
                    try
                    {
                        if (!ModulesManager.softClone.IsSoftClone || ModulesManager.softClone.CurrAvatarDict == null ||
                            eventData.Sender != Utilities.GetLocalVRCPlayerApi().playerId) break;

                        eventData.Parameters[251].Cast<Il2CppSystem.Collections.Hashtable>()["avatarDict"] = ModulesManager.softClone.CurrAvatarDict;

                        if (_turnOffNext)
                        {
                            ModulesManager.softClone.CurrAvatarDict = null;
                            ModulesManager.softClone.IsSoftClone = false;
                            _turnOffNext = false;
                        }
                        else _turnOffNext = true;
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in OnEvent253 Detour (SoftClone)");
                        PLogger.Error($"{e}");
                    }
                    break;
            }
            _onEventDelegate(instancePtr, eventDataPtr, nativeMethodInfoPtr);
        }

        // Please don't use InvisibleJoin, it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep
        private static Il2CppSystem.Object _lastSent;
        internal static bool triggerInvisible;
        private static IntPtr RaiseEventSetup(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptions, IntPtr sOptions, IntPtr nativeMethodInfoPtr)
        {
            var @return = IntPtr.Zero;
            switch (eType)
            {
                case 7:
                    try
                    {
                        if (Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(objPtr)[0] != ModulesManager.photonFreeze.PhotonID) break;

                        if (!ModulesManager.photonFreeze.IsFreeze)
                            _lastSent = new Il2CppSystem.Object(objPtr);
                        else
                            @return = _raiseEventDelegate(instancePtr, eType, _lastSent.Pointer, eOptions, sOptions, nativeMethodInfoPtr);
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in RaiseEvent7 Detour (FreezeSetup)");
                        PLogger.Error($"{e}");
                    }
                    break;
                case 202: // InvisibleJoin
                    try
                    {
                        if (!triggerInvisible) break;

                        var reOptions = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<RaiseEventOptions>(eOptions);
                        reOptions.field_Public_ReceiverGroup_0 = (ReceiverGroup)3;
                        @return = _raiseEventDelegate(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);
                        reOptions.field_Public_ReceiverGroup_0 = ReceiverGroup.Others;

                        if (ModulesManager.invisibleJoin.onceOnly) triggerInvisible = false;
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in RaiseEvent202 Detour (InvisibleJoinSetup)");
                        PLogger.Error($"{e}");
                    }
                    break;
            }
            return @return != IntPtr.Zero ? @return : _raiseEventDelegate(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);
        }

        internal static bool triggerOnceLtg;
        private static void LocalToGlobalSetup(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr)
        {
            try
            {
                if ((ModulesManager.triggers.IsAlwaysForceGlobal || triggerOnceLtg) && broadcast == VRC_EventHandler.VrcBroadcastType.Local)
                {
                    broadcast = VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered;
                    triggerOnceLtg = false;
                }
            }
            catch (Exception e)
            {
                PLogger.Warning("Something went wrong in LocalToGlobalSetup Detour");
                PLogger.Error($"{e}");
            }
            _localToGlobalDelegate(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
        }
    }
}
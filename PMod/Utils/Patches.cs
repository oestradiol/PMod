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
            IntPtr original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
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
        private delegate IntPtr RaiseEventDelegate(IntPtr instancePtr, byte EType, IntPtr ObjPtr, IntPtr EOptionsPtr, IntPtr SOptionsPtr, IntPtr nativeMethodInfoPtr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LocalToGlobalDelegate(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr);
        private static OnEventDelegate onEventDelegate;
        private static RaiseEventDelegate raiseEventDelegate;
        private static LocalToGlobalDelegate localToGlobalDelegate;
        internal unsafe static void OnApplicationStart()
        {
            onEventDelegate = NativePatchUtils.Patch<OnEventDelegate>(typeof(VRCNetworkingClient)
                .GetMethod(nameof(VRCNetworkingClient.OnEvent)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(OnEventSetup)));

            raiseEventDelegate = NativePatchUtils.Patch<RaiseEventDelegate>(typeof(LoadBalancingClient)
                .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(RaiseEventSetup)));

            localToGlobalDelegate = NativePatchUtils.Patch<LocalToGlobalDelegate>(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(LocalToGlobalSetup)));
        }

        private static bool turnOffNext = false;
        private static void OnEventSetup(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
        {
            EventData eventData = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<EventData>(eventDataPtr);
            switch (eventData.Code)
            {
                case 7:
                    try
                    {
                        Timer entry = null;
                        try { entry = ModulesManager.frozenPlayersManager.EntryDict[Utilities.GetPlayerFromPhotonID(eventData.Sender)?.field_Private_APIUser_0.id]; } catch { };
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

                        if (turnOffNext)
                        {
                            ModulesManager.softClone.CurrAvatarDict = null;
                            ModulesManager.softClone.IsSoftClone = false;
                            turnOffNext = false;
                        }
                        else turnOffNext = true;
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in OnEvent253 Detour (SoftClone)");
                        PLogger.Error($"{e}");
                    }
                    break;
            }
            onEventDelegate(instancePtr, eventDataPtr, nativeMethodInfoPtr);
        }

        // Please don't use InvisibleJoin, it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep
        private static Il2CppSystem.Object LastSent;
        internal static bool triggerInvisible = false;
        private static IntPtr RaiseEventSetup(IntPtr instancePtr, byte EType, IntPtr Obj, IntPtr EOptions, IntPtr SOptions, IntPtr nativeMethodInfoPtr)
        {
            IntPtr _return = IntPtr.Zero;
            switch (EType)
            {
                case 7:
                    try
                    {
                        if (Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(Obj)[0] != ModulesManager.photonFreeze.PhotonID) break;

                        if (!ModulesManager.photonFreeze.IsFreeze)
                            LastSent = new Il2CppSystem.Object(Obj);
                        else
                            _return = raiseEventDelegate(instancePtr, EType, LastSent.Pointer, EOptions, SOptions, nativeMethodInfoPtr);
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

                        RaiseEventOptions REOptions = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<RaiseEventOptions>(EOptions);
                        REOptions.field_Public_ReceiverGroup_0 = (ReceiverGroup)3;
                        _return = raiseEventDelegate(instancePtr, EType, Obj, EOptions, SOptions, nativeMethodInfoPtr);
                        REOptions.field_Public_ReceiverGroup_0 = ReceiverGroup.Others;

                        if (ModulesManager.invisibleJoin.onceOnly) triggerInvisible = false;
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in RaiseEvent202 Detour (InvisibleJoinSetup)");
                        PLogger.Error($"{e}");
                    }
                    break;
            }
            return _return != IntPtr.Zero ? _return : raiseEventDelegate(instancePtr, EType, Obj, EOptions, SOptions, nativeMethodInfoPtr);
        }

        internal static bool triggerOnceLTG = false;
        private static void LocalToGlobalSetup(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr)
        {
            try
            {
                if ((ModulesManager.triggers.IsAlwaysForceGlobal || triggerOnceLTG) && broadcast == VRC_EventHandler.VrcBroadcastType.Local)
                {
                    VRC_EventHandler.VrcEvent @event = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<VRC_EventHandler.VrcEvent>(eventPtr);
                    broadcast = VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered;
                    triggerOnceLTG = false;
                }
            }
            catch (Exception e)
            {
                PLogger.Warning("Something went wrong in LocalToGlobalSetup Detour");
                PLogger.Error($"{e}");
            }
            localToGlobalDelegate(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
        }
    }
}
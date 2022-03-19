using PMod.Loader;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using MelonLoader;
using VRC.Core;
using VRC.SDKBase;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PMod.Modules;
using Utilities = PMod.Utils.Utilities;

namespace PMod;

internal class Patches
{
    private static dynamic _onEventDelegate;
    private static dynamic _raiseEventDelegate;
    private static dynamic _triggerEventDelegate;
    internal static void OnApplicationStart()
    {
        _onEventDelegate = NativePatchUtils.Patch(typeof(VRCNetworkingClient)
                .GetMethod(nameof(VRCNetworkingClient.OnEvent)),
            NativePatchUtils.GetDetour<Patches>(nameof(OnEventSetup)));

        _raiseEventDelegate = NativePatchUtils.Patch(typeof(LoadBalancingClient)
                .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0)),
            NativePatchUtils.GetDetour<Patches>(nameof(RaiseEventSetup)));

        _triggerEventDelegate = NativePatchUtils.Patch(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
            NativePatchUtils.GetDetour<Patches>(nameof(TriggerEventSetup)));
        
        _frozenPlayersManager = ModulesManager.GetModule<FrozenPlayersManager>();
        _softClone = ModulesManager.GetModule<SoftClone>();
        _photonFreeze = ModulesManager.GetModule<PhotonFreeze>();
        _triggers = ModulesManager.GetModule<Triggers>();
    }

    private static FrozenPlayersManager _frozenPlayersManager;
    private static SoftClone _softClone;
    private static bool _turnOffNext;
    private static void OnEventSetup(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
    {
        var eventData = eventDataPtr.TryGetIl2CppPtrToObj<EventData>();
        switch (eventData.Code)
        {
            case 7:
                try
                {
                    if (!_frozenPlayersManager.IsOn.Value) break;
                    
                    FrozenPlayersManager.Timer entry = null;
                    var key = Utilities.GetPlayerFromPhotonID(eventData.Sender)?.field_Private_APIUser_0.id;
                    if (key != null) _frozenPlayersManager.EntryDict.TryGetValue(key, out entry);
                    
                    entry?.RestartTimer();
                }
                catch (Exception e)
                {
                    Main.Logger.Warning("Something went wrong in OnEvent7 Detour (FrozenPlayersManager)");
                    Main.Logger.Error($"{e}");
                }
                break;
            case 253: // Thanks to Yui! <3
                try
                {
                    if (!_softClone.IsOn.Value ||
                        !_softClone.IsSoftClone || 
                        _softClone.CurrAvatarDict == null ||
                        eventData.Sender != Utilities.GetLocalVRCPlayerApi().playerId) break;

                    eventData.Parameters[251].Cast<Il2CppSystem.Collections.Hashtable>()["avatarDict"] = _softClone.CurrAvatarDict;

                    if (_turnOffNext)
                    {
                        _softClone.CurrAvatarDict = null;
                        _softClone.IsSoftClone = false;
                        _turnOffNext = false;
                    }
                    else _turnOffNext = true;
                }
                catch (Exception e)
                {
                    Main.Logger.Warning("Something went wrong in OnEvent253 Detour (SoftClone)");
                    Main.Logger.Error($"{e}");
                }
                break;
        }
        _onEventDelegate.Invoke(instancePtr, eventDataPtr, nativeMethodInfoPtr);
    }

    // Please don't use InvisibleJoin, it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep // Deactivated.
    //internal static bool triggerInvisible;
    private static Il2CppSystem.Object _lastSent;
    private static PhotonFreeze _photonFreeze;
    private static bool RaiseEventSetup(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptions, SendOptions sOptions, IntPtr nativeMethodInfoPtr)
    {
        bool? @return = null;
        switch (eType)
        {
            case 7:
                try
                {
                    if (!_photonFreeze.IsOn.Value || 
                        Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(objPtr)[0] != _photonFreeze.PhotonID) break;
                    
                    if (!_photonFreeze.IsFreeze)
                        _lastSent = new Il2CppSystem.Object(objPtr);
                    else
                        @return = _raiseEventDelegate.Invoke(instancePtr, eType, _lastSent.Pointer, eOptions, sOptions, nativeMethodInfoPtr);
                }
                catch (Exception e)
                {
                    Main.Logger.Warning("Something went wrong in RaiseEvent7 Detour (FreezeSetup)");
                    Main.Logger.Error($"{e}");
                }
                break;
            // case 202: // InvisibleJoin // Update: Deactivating because fuck this
                // try
                // {
                //     if (!triggerInvisible) break;
                //
                //     var reOptions = eOptions.TryGetIl2CppPtrToObj<RaiseEventOptions>();
                //     reOptions.field_Public_ReceiverGroup_0 = (ReceiverGroup)3;
                //     @return = _raiseEventDelegate.Invoke(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);
                //     reOptions.field_Public_ReceiverGroup_0 = ReceiverGroup.Others;
                //
                //     if (ModulesManager.invisibleJoin.onceOnly) triggerInvisible = false;
                // }
                // catch (Exception e)
                // {
                //     PLogger.Warning("Something went wrong in RaiseEvent202 Detour (InvisibleJoinSetup)");
                //     PLogger.Error($"{e}");
                // }
                // break;
        }
        return @return ?? _raiseEventDelegate.Invoke(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);
    }

    internal static bool TriggerOnceLtg;
    private static Triggers _triggers;
    private static void TriggerEventSetup(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr)
    {
        try
        {
            if (_triggers.IsOn.Value && 
                (_triggers.IsAlwaysForceGlobal || TriggerOnceLtg) && 
                broadcast == VRC_EventHandler.VrcBroadcastType.Local)
            {
                broadcast = VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered;
                TriggerOnceLtg = false;
            }
        }
        catch (Exception e)
        {
            Main.Logger.Warning("Something went wrong in LocalToGlobalSetup Detour");
            Main.Logger.Error($"{e}");
        }
        _triggerEventDelegate.Invoke(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
    }
}

internal static class NativePatchUtils // Used my own custom extension methods: https://gist.github.com/d-magit/b760d1580ed77a03843e168e182a4ff6
{
    internal static Delegate Patch(MethodInfo originalMethod, IntPtr patchDetour) =>
        Patch(originalMethod, patchDetour, originalMethod.GetTypeArr().MakeNewCustomDelegate());
    //internal static TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour) where TDelegate : Delegate => 
    //    (TDelegate)Patch(originalMethod, patchDetour, typeof(TDelegate));
    private static unsafe Delegate Patch(MethodBase originalMethod, IntPtr patchDetour, Type delType)
    {
        var original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
        MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
        return Marshal.GetDelegateForFunctionPointer(original, delType);
    }
        
    internal static IntPtr GetDetour<TClass>(string patchName)
        where TClass : class => typeof(TClass).GetMethod(patchName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
        .MethodHandle.GetFunctionPointer();

    internal static T TryGetIl2CppPtrToObj<T>(this IntPtr ptr)
    { try { return UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<T>(ptr); } catch { return default; } }

    private static Type[] GetTypeArr(this MethodInfo methodInfo)
    {
        var args = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        return DelegateExtensions.StackPush(
                DelegateExtensions.StackPush(methodInfo.IsStatic ? args : DelegateExtensions.QueuePush(methodInfo.DeclaringType, args), typeof(IntPtr)), methodInfo.ReturnType)
            .Select(t => t.IsValueType ? t : typeof(IntPtr)).ToArray();
    }
}

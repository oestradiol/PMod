using System;
using VRC.Core;
using VRC.SDKBase;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PMod.Modules;
using PMod.Utils;

namespace PMod;

internal class Patches
{
    private static dynamic _onEventDelegate;
    public static dynamic RaiseEventDelegate;
    private static dynamic _triggerEventDelegate;
    internal static void OnApplicationStart()
    {
        _onEventDelegate = NativePatchUtils.Patch(typeof(VRCNetworkingClient)
                .GetMethod(nameof(VRCNetworkingClient.OnEvent)),
            NativePatchUtils.GetDetour<Patches>(nameof(OnEventSetup)));

        RaiseEventDelegate = NativePatchUtils.Patch(typeof(LoadBalancingClient)
                .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0)),
            NativePatchUtils.GetDetour<Patches>(nameof(RaiseEventSetup)));

        _triggerEventDelegate = NativePatchUtils.Patch(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
            NativePatchUtils.GetDetour<Patches>(nameof(TriggerEventSetup)));
    }

    private static void OnEventSetup(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
    {
        var eventData = eventDataPtr.TryGetIl2CppPtrToObj<EventData>();

        switch (eventData.Code)
        {
            case 7:
                ModulesManager.GetModule<FrozenPlayersManager>().OnEvent7(eventData);
                break;
            case 253:
                ModulesManager.GetModule<SoftClone>().OnEvent253(eventData);
                break;
        }
        
        _onEventDelegate.Invoke(instancePtr, eventDataPtr, nativeMethodInfoPtr);
    }

    private static bool RaiseEventSetup(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptions, SendOptions sOptions, IntPtr nativeMethodInfoPtr) =>
        eType switch
        {
            7 => ModulesManager.GetModule<PhotonFreeze>().RaiseEvent7(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr),
            _ => null
        } ?? RaiseEventDelegate(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);

    private static void TriggerEventSetup(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr)
    {
        broadcast = ModulesManager.GetModule<Triggers>().OnTriggerEvent(broadcast);
        _triggerEventDelegate.Invoke(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
    }
}
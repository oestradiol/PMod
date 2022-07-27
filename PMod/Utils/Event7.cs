using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon;
using VRC.Core;

namespace PMod.Utils;

internal abstract class OnEvent
{
    private static MethodInfo _onEventMethod;
    private static MethodInfo OnEventMethod =>
        _onEventMethod ??= typeof(VRCNetworkingClient).GetMethod(nameof(VRCNetworkingClient.OnEvent));
    
    protected dynamic OnEventDelegate;
    private IntPtr _patchDetour;

    // We have to use Marshal.GetFunctionPointerForDelegate here to avoid the current instance being passed as a parameter since the Detour method is non-static.
    // Instead, we pass the instance method as a delegate to Marshal. That way the delegate will already be bound to the current instance.
    internal void Patch() =>
        OnEventDelegate = NativePatchUtils.Patch(OnEventMethod, _patchDetour = Marshal.GetFunctionPointerForDelegate(Detour));
    
    internal void Unpatch() => NativePatchUtils.Unpatch(OnEventMethod, _patchDetour);
    
    protected abstract void Detour(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr);
}

public interface IOnEvent7
{
    void OnEvent7(EventData eventData);
}

internal class OnEvent7 : OnEvent
{
    private readonly IOnEvent7 _handler;
    internal OnEvent7(IOnEvent7 handler) => _handler = handler;

    protected override void Detour(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
    {
        var eventData = eventDataPtr.TryGetIl2CppPtrToObj<EventData>();

        if (eventData.Code == 7)
            _handler.OnEvent7(eventData);

        OnEventDelegate.Invoke(instancePtr, eventDataPtr, nativeMethodInfoPtr);
    }
}
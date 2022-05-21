using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using PMod.Loader;
using UnhollowerBaseLib;

namespace PMod.Utils;

internal static class NativePatchUtils
{
    internal static Delegate Patch(MethodInfo originalMethod, IntPtr patchDetour) =>
        Patch(originalMethod, patchDetour, originalMethod.GetTypeArr().MakeNewCustomDelegate());
    internal static TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour) where TDelegate : Delegate => 
        (TDelegate)Patch(originalMethod, patchDetour, typeof(TDelegate));
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

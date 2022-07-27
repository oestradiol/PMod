using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using PMod.Loader;
using UnhollowerBaseLib;

namespace PMod.Utils;

public static class NativePatchUtils
{
    private static readonly Dictionary<int, List<IntPtr>> Patches = new();
    private static List<IntPtr> GetTargetPatches(int patchedMethod)
    {
        if (Patches.TryGetValue(patchedMethod, out var methodPatches))
            return methodPatches;
        
        methodPatches = new List<IntPtr>();
        Patches.Add(patchedMethod, methodPatches);
        return methodPatches;
    }
    
    public static Delegate Patch(MethodInfo originalMethod, IntPtr patchDetour) =>
        Patch(originalMethod, patchDetour, originalMethod.GetTypeArr().MakeNewCustomDelegate());
    public static TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour) where TDelegate : Delegate => 
        (TDelegate)Patch(originalMethod, patchDetour, typeof(TDelegate));
    private static unsafe Delegate Patch(MethodBase originalMethod, IntPtr patchDetour, Type delType)
    {
        var original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
        MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
        GetTargetPatches(originalMethod.MetadataToken).Add(patchDetour);
        return Marshal.GetDelegateForFunctionPointer(original, delType);
    }
    
    public static unsafe void Unpatch(MethodBase patchedMethod, IntPtr patchDetour) //TODO: Debug this.
    {
        if (!Patches.TryGetValue(patchedMethod.MetadataToken, out var methodPatches))
            return;
        
        var original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(patchedMethod).GetValue(null);
        MelonUtils.NativeHookDetach(original, patchDetour);
        methodPatches.Remove(patchDetour);
    }
    
    public static IntPtr GetDetour<TClass>(string patchName)
        where TClass : class => typeof(TClass).GetMethod(patchName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
        .MethodHandle.GetFunctionPointer();

    public static T TryGetIl2CppPtrToObj<T>(this IntPtr ptr)
    { try { return UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<T>(ptr); } catch { return default; } }

    private static Type[] GetTypeArr(this MethodInfo methodInfo)
    {
        var args = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        return DelegateExtensions.StackPush(
                DelegateExtensions.StackPush(methodInfo.IsStatic ? args : DelegateExtensions.QueuePush(methodInfo.DeclaringType, args), typeof(IntPtr)), methodInfo.ReturnType)
            .Select(t => t.IsValueType ? t : typeof(IntPtr)).ToArray();
    }
}
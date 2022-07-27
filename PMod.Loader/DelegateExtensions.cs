using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PMod.Loader;

public static class DelegateExtensions
{
    private static readonly Func<Type[], Type> InternalMakeNewCustomDelegate = 
        (Func<Type[],Type>)Delegate.CreateDelegate(typeof(Func<Type[],Type>), 
            typeof(System.Linq.Expressions.Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers")
                .GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static)!); //Linq should be loaded by default so this shouldn't be null.

    private static readonly Dictionary<Type[], Type> CachedCustomDelegateTypes = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type MakeNewCustomDelegate(this Type[] types)
    {
        // Cache checking
        if (CachedCustomDelegateTypes.TryGetValue(types, out var isCached))
            return isCached;
        
        // Delegate type creation
        var delType = InternalMakeNewCustomDelegate(types);
        CachedCustomDelegateTypes.Add(types, delType);
        return delType;
    }

    private static readonly Dictionary<int, Delegate> CachedMethodDelegates = new();
    public static Delegate CreateDelegate(this MethodInfo methodInfo)
    {
        // Cache checking
        if (CachedMethodDelegates.TryGetValue(methodInfo.MetadataToken, out var isCached))
            return isCached;
            
        var args = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        var paramTypes = methodInfo.IsStatic ? args : QueuePush(methodInfo.DeclaringType, args); // decType shouldn't be null if Non-Static

        // Delegate type creation
        var del = methodInfo.CreateDelegate(StackPush(paramTypes, methodInfo.ReturnType).MakeNewCustomDelegate());
        CachedMethodDelegates.Add(methodInfo.MetadataToken, del);
        return del;
    }

    public static Type[] StackPush(Type[] parameters, Type ret)
    {
        var offset = parameters.Length;
        Array.Resize(ref parameters, offset + 1);
        parameters[offset] = ret;
        return parameters;
    }

    public static Type[] QueuePush(Type dec, params Type[] parameters)
    {
        var argsTypes = new Type[parameters.Length + 1];
        parameters.CopyTo(argsTypes, 1);
        argsTypes[0] = dec;
        return argsTypes;
    }

    public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo) where TDelegate : Delegate =>
        (TDelegate)methodInfo.CreateDelegate(typeof(TDelegate));
}
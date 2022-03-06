using PMod.Loader;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using MelonLoader;
using VRC.Core;
using VRC.SDKBase;
using Photon.Realtime;
using ExitGames.Client.Photon;
using HarmonyLib;

// ReSharper disable CollectionNeverUpdated.Local

namespace PMod.Utils
{ 
    internal static class DelegateExtensions // Used my own custom extension methods: https://gist.github.com/d-magit/d9cf4a02d6591746f7fe9c2c2a0c0b3f
    {
        private static readonly Func<Type[], Type> InternalMakeNewCustomDelegate = 
            (Func<Type[],Type>)Delegate.CreateDelegate(typeof(Func<Type[],Type>), 
                typeof(System.Linq.Expressions.Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers")
                    .GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static)!); //Linq should be loaded by default so this shouldn't be null.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type MakeNewCustomDelegate(this Type[] types) => InternalMakeNewCustomDelegate(types);

        private static readonly System.Collections.Hashtable CachedDelegates = new();
        internal static Delegate GetDelegateForMethodInfo(this MethodInfo methodInfo)
        {
            // Cache checking
            var isCached = CachedDelegates.Contains(methodInfo.MetadataToken);
            if (isCached)
            {
                Console.WriteLine($"Found delegate for {methodInfo.Name} and token {methodInfo.MetadataToken}!");
                return (Delegate)CachedDelegates[methodInfo.MetadataToken];
            }
            
            // Type[] creation
            var args = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var paramTypes = methodInfo.IsStatic ? args : QueuePush(methodInfo.DeclaringType, args); // decType shouldn't be null if Non-Static

            // Delegate type creation
            var del = methodInfo.CreateDelegate(StackPush(paramTypes, methodInfo.ReturnType).MakeNewCustomDelegate());
            CachedDelegates.Add(methodInfo.MetadataToken, del);
            return del;
        }

        internal static Type[] StackPush(Type[] parameters, Type ret)
        {
            var offset = parameters.Length;
            Array.Resize(ref parameters, offset + 1);
            parameters[offset] = ret;
            return parameters;
        }
        internal static Type[] QueuePush(Type dec, params Type[] parameters)
        {
            var argsTypes = new Type[parameters.Length + 1];
            parameters.CopyTo(argsTypes, 1);
            argsTypes[0] = dec;
            return argsTypes;
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

    internal class Patches
    {
        private static readonly HarmonyLib.Harmony HInstance = MelonHandler.Mods.First(m => m.Info.Name == LInfo.Name).HarmonyInstance;
        private static void OnInstanceChangeMethod(ApiWorld __0, ApiWorldInstance __1) => Main.OnInstanceChanged(__0, __1);
        private static Delegate _onEventDelegate;
        private static Delegate _raiseEventDelegate;
        private static Delegate _triggerEventDelegate;
        internal static void OnApplicationStart()
        {
            _onEventDelegate = NativePatchUtils.Patch(typeof(VRCNetworkingClient)
                .GetMethod(nameof(VRCNetworkingClient.OnEvent)),
                NativePatchUtils.GetDetour<Patches>(nameof(OnEventSetup)));
            
            // For some reason the function below doesn't break, but doesn't get called, no matter how I run the NativePatch, be it with my extensions or with Melon defaults, so I had to use Harmony...🤔
            HInstance.Patch(typeof(RoomManager)
                    .GetMethod(nameof(RoomManager.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_String_Int32_0)),
                null, new HarmonyMethod(typeof(Patches).GetMethod(nameof(OnInstanceChangeMethod), BindingFlags.NonPublic | BindingFlags.Static)));

            _raiseEventDelegate = NativePatchUtils.Patch(typeof(LoadBalancingClient)
                    .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0)),
                NativePatchUtils.GetDetour<Patches>(nameof(RaiseEventSetup)));

            _triggerEventDelegate = NativePatchUtils.Patch(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
                NativePatchUtils.GetDetour<Patches>(nameof(TriggerEventSetup)));
            
            // TODO: Search for an alternative method to hook safely to OnPlayerJoin
            // void ApplyPatch(MethodInfo mInfo, bool isJoin)
            // {
            //     PlayerActionDelegate tempMethod, originalMethod = null;
            //     _dontGCDelegates.Add(tempMethod = (thisPtr, playerPtr, nativeMInfo) =>
            //     {   
            //         var player = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<VRC.Player>(playerPtr);
            //         if (isJoin) Main.OnPlayerJoined(player); else Main.OnPlayerLeft(player);
            //         originalMethod!(thisPtr, playerPtr, nativeMInfo);
            //         // Loader.PLogger.Msg("Called OnVRCPlayer" + (isJoin ? "Join" : "Left") + $" for {player.field_Private_APIUser_0.displayName}! MethodInfo: {mInfo.Name}.");
            //     });
            //     originalMethod = NativePatchUtils.Patch<PlayerActionDelegate>(mInfo, Marshal.GetFunctionPointerForDelegate(tempMethod));
            // }
            //
            // var mIEnum = typeof(NetworkManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
            //     m => m.ReturnType == typeof(void) && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Player)).ToArray();
            // var firstIsJoin = XrefScanner.XrefScan(mIEnum.First()).Where(instance => instance.Type == XrefType.Global)
            //     .Select(instance => instance.ReadAsObject()?.ToString()).Any(s => s == "OnPlayerJoined {0}");
            // ApplyPatch(mIEnum.First(), firstIsJoin);
            // ApplyPatch(mIEnum.Last(), !firstIsJoin);
        }

        private static bool _turnOffNext;
        private static void OnEventSetup(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
        {
            var eventData = eventDataPtr.TryGetIl2CppPtrToObj<EventData>();
            switch (eventData.Code)
            {
                case 7:
                    try
                    {
                        Modules.FrozenPlayersManager.Timer entry = null;
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
            _onEventDelegate.DynamicInvoke(instancePtr, eventDataPtr, nativeMethodInfoPtr);
        }

        // Please don't use InvisibleJoin, it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep // Deactivated.
        private static Il2CppSystem.Object _lastSent;
        //internal static bool triggerInvisible;
        private static bool RaiseEventSetup(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptions, SendOptions sOptions, IntPtr nativeMethodInfoPtr)
        {
            object @return = null;
            switch (eType)
            {
                case 7:
                    try
                    {
                        if (Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(objPtr)[0] != ModulesManager.photonFreeze.PhotonID) break;

                        if (!ModulesManager.photonFreeze.IsFreeze)
                            _lastSent = new Il2CppSystem.Object(objPtr);
                        else
                            @return = _raiseEventDelegate.DynamicInvoke(instancePtr, eType, _lastSent.Pointer, eOptions, sOptions, nativeMethodInfoPtr);
                    }
                    catch (Exception e)
                    {
                        PLogger.Warning("Something went wrong in RaiseEvent7 Detour (FreezeSetup)");
                        PLogger.Error($"{e}");
                    }
                    break;
                // case 202: // InvisibleJoin // Update: Deactivating because fuck this
                    // try
                    // {
                    //     if (!triggerInvisible) break;
                    //
                    //     var reOptions = eOptions.TryGetIl2CppPtrToObj<RaiseEventOptions>();
                    //     reOptions.field_Public_ReceiverGroup_0 = (ReceiverGroup)3;
                    //     @return = _raiseEventDelegate.DynamicInvoke(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr);
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
            return (bool)(@return ?? _raiseEventDelegate.DynamicInvoke(instancePtr, eType, objPtr, eOptions, sOptions, nativeMethodInfoPtr));
        }

        internal static bool triggerOnceLtg;
        private static void TriggerEventSetup(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfoPtr)
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
            _triggerEventDelegate.DynamicInvoke(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
        }
    }
}
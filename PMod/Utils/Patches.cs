using PMod.Loader;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppSystem.Collections.Generic;
using UnhollowerBaseLib;
using MelonLoader;
using UnityEngine.UI;
using VRC.SDKBase;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace PMod.Utils
{
    internal static class DelegateMethods
    {
        internal static void PopupV2(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction) => GetPopupV2Delegate(title, body, submitButtonText, submitButtonAction);
        private delegate void PopupV2Delegate(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction, Il2CppSystem.Action<VRCUiPopup> additionalSetup = null);
        private static PopupV2Delegate popupV2Delegate;
        private static PopupV2Delegate GetPopupV2Delegate => 
            popupV2Delegate ??= (PopupV2Delegate)Delegate.CreateDelegate(typeof(PopupV2Delegate), 
                VRCUiPopupManager.prop_VRCUiPopupManager_0, 
                typeof(VRCUiPopupManager).GetMethods()
                    .First(methodBase => methodBase.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") &&
                    !methodBase.Name.Contains("PDM") &&
                    Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/StandardPopupV2") &&
                    Utilities.WasUsedBy(methodBase, "OpenSaveSearchPopup")));

        internal static void InputPopup(string title, string submitButtonText, Il2CppSystem.Action<string, List<UnityEngine.KeyCode>, Text> submitButtonAction, string placeholderText = "Enter text....", 
            bool useNumericKeypad = false, Il2CppSystem.Action cancelButtonAction = null, string body = null, InputField.InputType inputType = InputField.InputType.Standard) => // Extra shit
                GetInputPopupDelegate(title, body, inputType, useNumericKeypad, submitButtonText, submitButtonAction, cancelButtonAction, placeholderText);
        private delegate void InputPopupDelegate(string title, string body, InputField.InputType inputType, bool useNumericKeypad, string submitButtonText, Il2CppSystem.Action<string, List<UnityEngine.KeyCode>, Text> submitButtonAction, 
            Il2CppSystem.Action cancelButtonAction, string placeholderText = "Enter text....", bool hidePopupOnSubmit = true, Il2CppSystem.Action<VRCUiPopup> additionalSetup = null, bool param_11 = false, int param_12 = 0);
        private static InputPopupDelegate inputPopupDelegate;
        private static InputPopupDelegate GetInputPopupDelegate =>
            inputPopupDelegate ??= (InputPopupDelegate)Delegate.CreateDelegate(
                typeof(InputPopupDelegate),
                VRCUiPopupManager.prop_VRCUiPopupManager_0,
                typeof(VRCUiPopupManager).GetMethods().First(methodBase =>
                    methodBase.Name.StartsWith("Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_") &&
                    !methodBase.Name.Contains("PDM") &&
                    Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/InputPopup")));
    }

    internal class NativePatches
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr RaiseEventDelegate(IntPtr instancePointer, byte EType, IntPtr Obj, IntPtr EOptions, IntPtr SOptions, IntPtr nativeMethodInfo);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr OnPlayerNetDecodeDelegate(IntPtr instancePointer, IntPtr objectsPointer, int objectIndex, float sendTime, IntPtr nativeMethodPointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LocalToGlobalSetupDelegate(IntPtr instancePtr, IntPtr eventPtr, VRC_EventHandler.VrcBroadcastType broadcast, int instigatorId, float fastForward, IntPtr nativeMethodInfo);
        private static RaiseEventDelegate raiseEventDelegate;
        private static LocalToGlobalSetupDelegate localToGlobalSetupDelegate;
        private static readonly System.Collections.Generic.List<OnPlayerNetDecodeDelegate> dontGCDelegates = new();
        internal unsafe static void OnApplicationStart()
        {
            raiseEventDelegate = NativePatchUtils.Patch<RaiseEventDelegate>(typeof(LoadBalancingClient)
                .GetMethod(nameof(LoadBalancingClient.Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0),
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(byte), typeof(Il2CppSystem.Object), typeof(RaiseEventOptions), typeof(SendOptions) }, null),
                NativePatchUtils.GetDetour<NativePatches>(nameof(RaiseEventSetup)));

            localToGlobalSetupDelegate = NativePatchUtils.Patch<LocalToGlobalSetupDelegate>(typeof(VRC_EventHandler)
                .GetMethod(nameof(VRC_EventHandler.InternalTriggerEvent)),
                NativePatchUtils.GetDetour<NativePatches>(nameof(LocalToGlobalSetup)));

            //(from p in m.GetParameters() select p.ParameterType).ToArray() == new[] { typeof(), typeof(), typeof() }
            var mIEnum = typeof(PlayerNet).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetParameters().Length == 3 && m.Name.StartsWith("Method_Public_Virtual_Final_New_Void_ValueTypePublicSealed"));
            foreach (var mInfo in mIEnum)
            {
                OnPlayerNetDecodeDelegate tempMethod, originalMethod = null;
                dontGCDelegates.Add(tempMethod = (instancePointer, objectsPointer, objectIndex, sendTime, nativeMethodPointer) =>
                    PlayerNetPatch(instancePointer, objectsPointer, objectIndex, sendTime, nativeMethodPointer, originalMethod));
                originalMethod = NativePatchUtils.Patch<OnPlayerNetDecodeDelegate>(mInfo, Marshal.GetFunctionPointerForDelegate(tempMethod));
            }
        }

        // Please don't use InvisibleJoin, it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep
        private static Il2CppSystem.Object LastSent;
        internal static bool triggerInvisible = false;
        private static IntPtr RaiseEventSetup(IntPtr instancePtr, byte EType, IntPtr Obj, IntPtr EOptions, IntPtr SOptions, IntPtr nativeMethodInfoPtr)
        {
            IntPtr _return = IntPtr.Zero;
            switch (EType)
            {
                case 7: // PhotonFreeze
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
                        PLogger.Warning("Something went wrong in FreezeSetup Patch");
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
                        PLogger.Warning("Something went wrong in InvisibleJoinSetup Patch");
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
                    if (triggerOnceLTG) triggerOnceLTG = false;
                }
            }
            catch (Exception e)
            {
                PLogger.Warning("Something went wrong in LocalToGlobalSetup Patch");
                PLogger.Error($"{e}");
            }
            localToGlobalSetupDelegate(instancePtr, eventPtr, broadcast, instigatorId, fastForward, nativeMethodInfoPtr);
        }

        private static IntPtr PlayerNetPatch(IntPtr instancePointer, IntPtr objectsPointer, int objectIndex, float sendTime, IntPtr nativeMethodPointer, OnPlayerNetDecodeDelegate originalDecodeDelegate)
        {
            IntPtr result = originalDecodeDelegate(instancePointer, objectsPointer, objectIndex, sendTime, nativeMethodPointer);
            try
            {
                if (result == IntPtr.Zero) return result;

                PlayerNet playerNet = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<PlayerNet>(instancePointer);
                if (playerNet == null) return result;

                Timer entry = null;
                try { entry = ModulesManager.frozenPlayersManager.EntryDict[playerNet.prop_Player_0.prop_APIUser_0.id]; } catch { };
                entry?.RestartTimer();
            }
            catch (Exception e)
            {
                PLogger.Warning("Something went wrong in PlayerNetPatch");
                PLogger.Error($"{e}");
            }
            return result;
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;
using MonoMod.Utils;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.SDKBase;

namespace PMod.Utils
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static VRCPlayer GetLocalVRCPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static VRCPlayerApi GetLocalVRCPlayerApi() => Player.prop_Player_0.prop_VRCPlayerApi_0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static APIUser GetLocalAPIUser() => Player.prop_Player_0.field_Private_APIUser_0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Player GetPlayerFromID(string id) => PlayerManager.Method_Public_Static_Player_String_0(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Player GetPlayerFromPhotonID(int id) => DelegateMethods.GetPlayerFromPhotonID(id);
        
        internal enum WorldSDKVersion { None, SDK2, SDK3 }
        internal static WorldSDKVersion GetWorldSDKVersion()
        {
            if (!VRC_SceneDescriptor._instance) return WorldSDKVersion.None;
            if (VRC_SceneDescriptor._instance.TryCast<VRCSDK2.VRC_SceneDescriptor>() != null) return WorldSDKVersion.SDK2;
            return VRC_SceneDescriptor._instance.TryCast<VRC.SDK3.Components.VRCSceneDescriptor>() != null ? WorldSDKVersion.SDK3 : WorldSDKVersion.None;
        }

        internal static void RiskyFuncAlert(string funcName) => DelegateMethods.PopupV2(
            funcName,
            "You have to first activate the mod on Melon Preferences menu! Be aware that this is a risky function.",
            "Close",
            new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }));

        internal static bool ContainsStr(MethodBase methodBase, string match)
        {
            try
            {
                return XrefScanner.XrefScan(methodBase)
                    .Any(instance => instance.Type == XrefType.Global &&
                         instance.ReadAsObject()?.ToString().IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0);
            } catch { return false; } 
        }

        internal static bool WasUsedBy(MethodBase methodBase, string methodName)
        {
            try
            {
                return XrefScanner.UsedBy(methodBase)
                    .Any(instance => instance.TryResolve() != null &&
                         instance.TryResolve().Name.Equals(methodName, StringComparison.Ordinal));
            } catch { return false; } 
        }

        internal static Transform GetBoneTransform(Player player, HumanBodyBones bone)
        {
            var playerPosition = player.transform;
            var avatarManager = player.prop_VRCPlayer_0.prop_VRCAvatarManager_0;
            if (!avatarManager) return playerPosition;
            var animator = avatarManager.field_Private_Animator_0;
            if (!animator) return playerPosition;
            var boneTransform = animator.GetBoneTransform(bone);
            return boneTransform ? playerPosition : boneTransform;
        }
    }

    internal static class DelegateMethods
    {
        private static Delegate _popupV2Delegate;
        internal static void PopupV2(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction) =>
            (_popupV2Delegate ??= typeof(VRCUiPopupManager)
                .GetMethods().First(methodBase => 
                    methodBase.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") &&
                    !methodBase.Name.Contains("PDM") &&
                    Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/StandardPopupV2") &&
                    Utilities.WasUsedBy(methodBase, "OpenSaveSearchPopup"))
                .GetDelegateForMethodInfo()).DynamicInvoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, title, body, submitButtonText, submitButtonAction, null);

        private static Delegate _inputPopupDelegate;
        internal static void InputPopup(string title, string submitButtonText, Il2CppSystem.Action<string, List<KeyCode>, Text> submitButtonAction, string placeholderText = "Enter text....",
            bool useNumericKeypad = false, Il2CppSystem.Action cancelButtonAction = null, string body = null, InputField.InputType inputType = InputField.InputType.Standard) => // Extra shit
            (_inputPopupDelegate ??= typeof(VRCUiPopupManager).GetMethods().First(methodBase =>
                methodBase.Name.StartsWith("Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_") &&
                !methodBase.Name.Contains("PDM") && Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/InputPopup")).GetDelegateForMethodInfo())
            .DynamicInvoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, title, body, inputType, useNumericKeypad, submitButtonText, submitButtonAction, cancelButtonAction, placeholderText, true, null, false, 0);

        private static Func<int, Player> _playerFromPhotonIDMethod;
        internal static Player GetPlayerFromPhotonID(int id) =>
            (_playerFromPhotonIDMethod ??= typeof(PlayerManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi => mi.Name.Contains("Method_Public_Static_Player_Int32_"))
                .OrderBy(UnhollowerSupport.GetIl2CppMethodCallerCount).Last().CreateDelegate<Func<int, Player>>())(id);
    }

    internal class OrbitItem
    {
        private readonly Vector3 _initialPos;
        private readonly Quaternion _initialRot;
        private readonly double _index;
        internal readonly bool InitialTheft;
        internal readonly bool InitialPickupable;
        internal readonly bool InitialKinematic;
        internal readonly Vector3 InitialVelocity;
        internal readonly bool InitialActive;
        internal bool IsOn { get; set; } = true;

        internal OrbitItem(System.Collections.Generic.IList<VRC_Pickup> pickups, int i)
        {
            var pickup = pickups[i];
            var rigidBody = pickup.GetComponent<Rigidbody>();
            InitialTheft = pickup.DisallowTheft;
            InitialPickupable = pickup.pickupable;
            InitialKinematic = rigidBody.isKinematic;
            InitialVelocity = rigidBody.velocity;
            InitialActive = pickup.gameObject.active;
            var transform = pickup.transform;
            _initialPos = transform.position;
            _initialRot = transform.rotation;
            _index = (double)i / pickups.Count;
        }

        private Vector3 CircularRot()
        {
            var angle = ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value + 2 * Math.PI * _index;
            return ModulesManager.orbit.OrbitCenter + ModulesManager.orbit.rotationy * (ModulesManager.orbit.rotation * new Vector3((float)Math.Cos(angle) * ModulesManager.orbit.radius.Value, 0,
                (float)Math.Sin(angle) * ModulesManager.orbit.radius.Value));
        }

        private Vector3 CylindricalRot()
        {
            var angle = ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value + 2 * Math.PI * _index;
            return ModulesManager.orbit.OrbitCenter + new Vector3(0, (float)(ModulesManager.orbit.PlayerHeight * _index), 0) + ModulesManager.orbit.rotationy *
                (ModulesManager.orbit.rotation * new Vector3((float)Math.Cos(angle) * ModulesManager.orbit.radius.Value, 0, (float)Math.Sin(angle) * ModulesManager.orbit.radius.Value));
        }

        private Vector3 SphericalRot()
        {
            var angle = (ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value) / (4 * Math.PI) + _index * 360;
            var height = ModulesManager.orbit.PlayerHeight * ((ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value / 2 + _index) % 1);
            var rotation = Quaternion.Euler(0, (float)angle, 0);
            return ModulesManager.orbit.OrbitCenter + ModulesManager.orbit.rotationy * (ModulesManager.orbit.rotation *
                (rotation * new Vector3((float)(4 * Math.Sqrt(height * ModulesManager.orbit.PlayerHeight - Math.Pow(height, 2)) * ModulesManager.orbit.radius.Value), (float)height, 0)));
        }

        internal Vector3 CurrentPos() => IsOn
            ? ModulesManager.orbit.rotType switch
            {
                Modules.Orbit.RotType.CircularRot => CircularRot(),
                Modules.Orbit.RotType.CylindricalRot => CylindricalRot(),
                _ => SphericalRot()
            } : _initialPos;

        internal Quaternion CurrentRot()
        {
            var angle = (float)(ModulesManager.orbit.Timer * 50f * ModulesManager.orbit.speed.Value + 2 * Math.PI * _index);
            return IsOn ? Quaternion.Euler(-angle, 0, -angle) : _initialRot;
        }
    }

    //Add this to OnApplicationStart!! ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();
    //internal class EnableDisableListener : MonoBehaviour
    //{
    //    [method: HideFromIl2Cpp]
    //    internal event Action OnEnabled;

    //    [method: HideFromIl2Cpp]
    //    internal event Action OnDisabled;

    //    private void OnEnable() => OnEnabled?.Invoke();

    //    private void OnDisable() => OnDisabled?.Invoke();
    //}
}
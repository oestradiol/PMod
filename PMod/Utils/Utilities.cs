using System;
using System.Collections;
using System.Diagnostics;
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
using VRC.UI;

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

        internal static void ChangeToAVByID(string id)
        {
            var AviMenu = Resources.FindObjectsOfTypeAll<PageAvatar>()[1];
            AviMenu.field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = new ApiAvatar { id = id };
            AviMenu.ChangeToSelectedAvatar();
        }

        internal enum WorldSDKVersion { None, SDK2, SDK3 }
        internal static WorldSDKVersion GetWorldSDKVersion()
        {
            if (!VRC_SceneDescriptor._instance) return WorldSDKVersion.None;
            if (VRC_SceneDescriptor._instance.TryCast<VRCSDK2.VRC_SceneDescriptor>() != null) return WorldSDKVersion.SDK2;
            if (VRC_SceneDescriptor._instance.TryCast<VRC.SDK3.Components.VRCSceneDescriptor>() != null) return WorldSDKVersion.SDK3;
            return WorldSDKVersion.None;
        }

        internal static void RiskyFuncAlert(string FuncName) => DelegateMethods.PopupV2(
            FuncName,
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
            }
            catch { }
            return false;
        }

        internal static bool WasUsedBy(MethodBase methodBase, string methodName)
        {
            try
            {
                return XrefScanner.UsedBy(methodBase)
                    .Any(instance => instance.TryResolve() != null &&
                         instance.TryResolve().Name.Equals(methodName, StringComparison.Ordinal));
            }
            catch { }
            return false;
        }

        internal static Transform GetBoneTransform(Player player, HumanBodyBones bone)
        {
            Transform playerPosition = player.transform;
            VRCAvatarManager avatarManager = player.prop_VRCPlayer_0.prop_VRCAvatarManager_0;
            if (!avatarManager) return playerPosition;
            Animator animator = avatarManager.field_Private_Animator_0;
            if (!animator) return playerPosition;
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (!boneTransform) return playerPosition;
            return boneTransform;
        }
    }

    internal static class DelegateMethods
    {
        private delegate void PopupV2Delegate(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction, Il2CppSystem.Action<VRCUiPopup> additionalSetup = null);
        private static PopupV2Delegate popupV2Delegate;
        internal static void PopupV2(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction) =>
            (popupV2Delegate ??= typeof(VRCUiPopupManager)
                .GetMethods().First(methodBase => 
                    methodBase.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") &&
                    !methodBase.Name.Contains("PDM") &&
                    Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/StandardPopupV2") &&
                    Utilities.WasUsedBy(methodBase, "OpenSaveSearchPopup"))
                .CreateDelegate<PopupV2Delegate>(VRCUiPopupManager.prop_VRCUiPopupManager_0))(title, body, submitButtonText, submitButtonAction);

        private delegate void InputPopupDelegate(string title, string body, InputField.InputType inputType, bool useNumericKeypad, string submitButtonText, Il2CppSystem.Action<string, List<KeyCode>, Text> submitButtonAction,
            Il2CppSystem.Action cancelButtonAction, string placeholderText = "Enter text....", bool hidePopupOnSubmit = true, Il2CppSystem.Action<VRCUiPopup> additionalSetup = null, bool param_11 = false, int param_12 = 0);
        private static InputPopupDelegate inputPopupDelegate;
        internal static void InputPopup(string title, string submitButtonText, Il2CppSystem.Action<string, List<KeyCode>, Text> submitButtonAction, string placeholderText = "Enter text....",
            bool useNumericKeypad = false, Il2CppSystem.Action cancelButtonAction = null, string body = null, InputField.InputType inputType = InputField.InputType.Standard) => // Extra shit
            (inputPopupDelegate ??= typeof(VRCUiPopupManager).GetMethods().First(methodBase =>
                methodBase.Name.StartsWith("Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_") &&
                !methodBase.Name.Contains("PDM") && Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/InputPopup"))
            .CreateDelegate<InputPopupDelegate>(VRCUiPopupManager.prop_VRCUiPopupManager_0))(title, body, inputType, useNumericKeypad, submitButtonText, submitButtonAction, cancelButtonAction, placeholderText);

        private static Func<int, Player> playerFromPhotonIDMethod;
        internal static Player GetPlayerFromPhotonID(int id) =>
            (playerFromPhotonIDMethod ??= typeof(PlayerManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi => mi.Name.Contains("Method_Public_Static_Player_Int32_"))
                .OrderBy(mi => UnhollowerSupport.GetIl2CppMethodCallerCount(mi)).Last().CreateDelegate<Func<int, Player>>())(id);
    }

    internal class OrbitItem
    {
        private Vector3 InitialPos;
        private Quaternion InitialRot;
        private readonly double Index;
        internal readonly bool InitialTheft;
        internal readonly bool InitialPickupable;
        internal readonly bool InitialKinematic;
        internal readonly Vector3 InitialVelocity;
        internal readonly bool InitialActive;
        internal bool IsOn { get; set; } = true;

        internal OrbitItem(VRC_Pickup pickup, double i)
        {
            var pickupComponent = pickup.GetComponent<VRC_Pickup>();
            var rigidBody = pickup.GetComponent<Rigidbody>();
            InitialTheft = pickupComponent.DisallowTheft;
            InitialPickupable = pickupComponent.pickupable;
            InitialKinematic = rigidBody.isKinematic;
            InitialVelocity = rigidBody.velocity;
            InitialActive = pickup.gameObject.active;
            InitialPos = pickup.transform.position;
            InitialRot = pickup.transform.rotation;
            Index = i / ModulesManager.orbit.Pickups.Count;
        }

        private Vector3 CircularRot()
        {
            double Angle = ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value + 2 * Math.PI * Index;
            return ModulesManager.orbit.OrbitCenter + ModulesManager.orbit.rotationy * (ModulesManager.orbit.rotation * new Vector3((float)Math.Cos(Angle) * ModulesManager.orbit.radius.Value, 0,
                (float)Math.Sin(Angle) * ModulesManager.orbit.radius.Value));
        }

        private Vector3 CylindricalRot()
        {
            double Angle = ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value + 2 * Math.PI * Index;
            return ModulesManager.orbit.OrbitCenter + new Vector3(0, (float)(ModulesManager.orbit.PlayerHeight * Index), 0) + ModulesManager.orbit.rotationy *
                (ModulesManager.orbit.rotation * new Vector3((float)Math.Cos(Angle) * ModulesManager.orbit.radius.Value, 0, (float)Math.Sin(Angle) * ModulesManager.orbit.radius.Value));
        }

        private Vector3 SphericalRot()
        {
            double Angle = (ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value) / (4 * Math.PI) + Index * 360;
            double Height = ModulesManager.orbit.PlayerHeight * ((ModulesManager.orbit.Timer * ModulesManager.orbit.speed.Value / 2 + Index) % 1);
            Quaternion Rotation = Quaternion.Euler(0, (float)Angle, 0);
            return ModulesManager.orbit.OrbitCenter + ModulesManager.orbit.rotationy * (ModulesManager.orbit.rotation *
                (Rotation * new Vector3((float)(4 * Math.Sqrt(Height * ModulesManager.orbit.PlayerHeight - Math.Pow(Height, 2)) * ModulesManager.orbit.radius.Value), (float)Height, 0)));
        }

        internal Vector3 CurrentPos()
        {
            if (IsOn)
            {
                if (ModulesManager.orbit.rotType == Modules.Orbit.RotType.CircularRot) return CircularRot();
                else if (ModulesManager.orbit.rotType == Modules.Orbit.RotType.CylindricalRot) return CylindricalRot();
                else return SphericalRot();
            }
            return InitialPos;
        }

        internal Quaternion CurrentRot()
        {
            float Angle = (float)(ModulesManager.orbit.Timer * 50f * ModulesManager.orbit.speed.Value + 2 * Math.PI * Index);
            if (IsOn) return Quaternion.Euler(-Angle, 0, -Angle);
            else return InitialRot;
        }
    }

    internal class Timer
    {
        private Stopwatch timer;
        internal GameObject text;
        internal bool IsFrozen;

        internal Timer()
        {
            IsFrozen = true;
            RestartTimer();
        }

        internal void RestartTimer()
        {
            timer = Stopwatch.StartNew();
            if (IsFrozen) MelonCoroutines.Start(Checker());
        }

        private IEnumerator Checker()
        {
            IsFrozen = false;
            ModulesManager.frozenPlayersManager.NametagSet(this);

            while (timer.ElapsedMilliseconds <= 1000)
                yield return null;

            IsFrozen = true;
            ModulesManager.frozenPlayersManager.NametagSet(this);
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
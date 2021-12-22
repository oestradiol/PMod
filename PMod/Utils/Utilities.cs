using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRC.UI;

namespace PMod.Utils
{
    internal static class Utilities
    {
        internal static VRCPlayer GetLocalVRCPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        internal static VRCPlayerApi GetLocalVRCPlayerApi() => Player.prop_Player_0.prop_VRCPlayerApi_0;
        internal static Player GetPlayerFromID(string id) => PlayerManager.Method_Public_Static_Player_String_0(id);
        internal static void ChangeToAVByID(string id)
        {
            var AviMenu = Resources.FindObjectsOfTypeAll<PageAvatar>()[1];
            AviMenu.field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = new ApiAvatar { id = id };
            AviMenu.ChangeToSelectedAvatar();
        }
        internal enum WorldSDKVersion
        {
            None,
            SDK2,
            SDK3
        }

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

    internal static class NativePatchUtils
    {
        internal static unsafe TDelegate Patch<TDelegate>(MethodInfo originalMethod, IntPtr patchDetour) where TDelegate : Delegate
        {
            IntPtr original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
            MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        }

        internal static IntPtr GetDetour<TClass>(string patchName)
            where TClass : class => typeof(TClass).GetMethod(patchName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
            .MethodHandle.GetFunctionPointer();
    }

    internal static class NetworkEvents
    {
        internal static event Action<Player> OnPlayerJoinedAction;
        internal static event Action<Player> OnPlayerLeftAction;
        internal static event Action<ApiWorld, ApiWorldInstance> OnInstanceChangedAction;
        private static void OnInstanceChangeMethod(ApiWorld __0, ApiWorldInstance __1) => OnInstanceChangedAction?.Invoke(__0, __1);
        internal static void OnUiManagerInit()
        {
            //PatchMethods();
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.
                field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)EventHandlerA);
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.
                field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)EventHandlerB);
            Main.HInstance.Patch(typeof(RoomManager).GetMethod(nameof(RoomManager.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_String_Int32_0)), null,
                new HarmonyMethod(typeof(NetworkEvents).GetMethod(nameof(OnInstanceChangeMethod), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static bool SeenFire;
        private static bool AFiredFirst;
        private static void EventHandlerA(Player player)
        {
            if (!SeenFire)
            {
                AFiredFirst = true;
                SeenFire = true;
            }

            (AFiredFirst ? OnPlayerJoinedAction : OnPlayerLeftAction)?.Invoke(player);
        }
        private static void EventHandlerB(Player player)
        {
            if (!SeenFire)
            {
                AFiredFirst = false;
                SeenFire = true;
            }
                
            (AFiredFirst ? OnPlayerLeftAction : OnPlayerJoinedAction)?.Invoke(player);
        }

        //// Needs change to which method it patches, because only works at first instance join for now.
        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate void PlayerActionDelegate(IntPtr thisPtr, IntPtr playerPtr, IntPtr nativeMInfo);
        //private static readonly System.Collections.Generic.List<PlayerActionDelegate> dontGCDelegates = new();
        //private unsafe static void PatchMethods()
        //{
        //    void ApplyPatch(MethodInfo mInfo, bool IsJoin)
        //    {
        //        PlayerActionDelegate tempMethod, originalMethod = null;
        //        dontGCDelegates.Add(tempMethod = (thisPtr, playerPtr, nativeMInfo) =>
        //        {   
        //            var Player = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<Player>(playerPtr);
        //            (IsJoin ? OnPlayerJoinedAction : OnPlayerLeftAction)?.Invoke(Player);
        //            originalMethod.Invoke(thisPtr, playerPtr, nativeMInfo);
        //            Loader.PLogger.Msg("Called OnVRCPlayer" + (IsJoin ? "Join" : "Left") + $" for {Player.field_Private_APIUser_0.displayName}! MethodInfo: {mInfo.Name}.");
        //        });
        //        originalMethod = NativePatchUtils.Patch<PlayerActionDelegate>(mInfo, Marshal.GetFunctionPointerForDelegate(tempMethod));
        //    }

        //    var mIEnum = typeof(NetworkManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
        //        m => m.ReturnType == typeof(void) && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Player));
        //    bool FirstIsJoin = XrefScanner.XrefScan(mIEnum.First()).Where(instance => instance.Type == XrefType.Global)
        //            .Select(instance => instance.ReadAsObject()?.ToString()).Any(s => s == "OnPlayerJoined {0}");
        //    ApplyPatch(mIEnum.First(), FirstIsJoin);
        //    ApplyPatch(mIEnum.Last(), !FirstIsJoin);
        //}
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
            InitialTheft = pickup.GetComponent<VRC_Pickup>().DisallowTheft;
            InitialPickupable = pickup.GetComponent<VRC_Pickup>().pickupable;
            InitialKinematic = pickup.GetComponent<Rigidbody>().isKinematic;
            InitialVelocity = pickup.GetComponent<Rigidbody>().velocity;
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
}
﻿using MelonLoader;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRC.UI;

namespace Client.Utils
{
    internal static class Utilities
    {
        internal static VRCPlayer GetLocalVRCPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        internal static VRCPlayerApi GetLocalVRCPlayerApi() => Player.prop_Player_0.prop_VRCPlayerApi_0;
        internal static Player GetPlayerFromID(string id) => (Player)Methods.PlayerFromID.Invoke(null, new object[] { id });
        internal static void ChangeToAVByID(string id)
        {
            var AviMenu = GameObject.Find("UserInterface/MenuContent/Screens/Avatar").GetComponent<PageAvatar>();
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

        internal static bool ContainsStr(MethodBase methodBase, string match)
        {
            try
            {
                return XrefScanner.XrefScan(methodBase)
                    .Any(instance => instance.Type == XrefType.Global && instance.ReadAsObject() != null &&
                         instance.ReadAsObject().ToString().Equals(match, StringComparison.OrdinalIgnoreCase));
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
            yield break;
        }
    }
}
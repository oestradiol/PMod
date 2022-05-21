using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;

namespace PMod.Utils;

internal static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static VRCPlayer GetLocalVrcPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;

    internal static VRCPlayerApi GetLocalVrcPlayerApi() => Player.prop_Player_0.prop_VRCPlayerApi_0;

    internal static APIUser GetLocalAPIUser() => Player.prop_Player_0.field_Private_APIUser_0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Player GetPlayerFromID(string id) => PlayerManager.Method_Public_Static_Player_String_0(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Player GetPlayerFromPhotonID(int id) => DelegateMethods.GetPlayerFromPhotonID(id);
        
    internal enum WorldSDKVersion { None, SDK2, SDK3 }
    internal static WorldSDKVersion GetWorldSDKVersion() => 
        VRC_SceneDescriptor._instance switch
        {
            VRC.SDK3.Components.VRCSceneDescriptor => WorldSDKVersion.SDK3,
            VRCSDK2.VRC_SceneDescriptor => WorldSDKVersion.SDK2,
            _ => WorldSDKVersion.None
        };

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
}

//OnApplicationStart: ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();
//internal class EnableDisableListener : MonoBehaviour
//{
//    [method: HideFromIl2Cpp]
//    internal event Action OnEnabled;

//    [method: HideFromIl2Cpp]
//    internal event Action OnDisabled;

//    private void OnEnable() => OnEnabled?.Invoke();

//    private void OnDisable() => OnDisabled?.Invoke();
//}
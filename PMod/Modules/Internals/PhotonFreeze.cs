using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using PMod.Utils;
using UIExpansionKit.API;
using UnhollowerBaseLib;
using UnityEngine;
using VRC;
using VRC.Core;
using Object = UnityEngine.Object;

namespace PMod.Modules.Internals;

internal class PhotonFreeze : VrcMod
{
    private ICustomShowableLayoutedMenu _freezeMenu;
    private Transform _cloneObj;
    private Vector3 _originalPos;
    private Quaternion _originalRot;
    private int _photonID;
    private bool _isFreeze;

    public override void OnApplicationStart() =>
        UiUtils.ClientMenu.AddSimpleButton("PhotonFreeze", ShowFreezeMenu);

    public override void OnPlayerJoined(Player player)
    {
        if (player.prop_APIUser_0.id == Utilities.GetLocalAPIUser().id)
            _photonID = player.gameObject.GetComponent<PhotonView>().viewIdField;
    }

    private Vector3 _previousPos;
    private bool _isMaxD;

    public override void OnUpdate()
    {
        if (!IsOn.Value) return;
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            var temp = Utilities.GetLocalVrcPlayer().gameObject.transform;
            _isMaxD = !_isMaxD;
            if (_isMaxD)
            {
                _previousPos = temp.position;
                temp.position = new Vector3(10000000, 10000000, 10000000);
            }
            else
                temp.position = _previousPos;
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.K))
        {
            ToggleFreeze();
            _freezeMenu.Hide();
        }
    }

    public override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => _isFreeze = false;

    private void ShowFreezeMenu()
    {
        _freezeMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        _freezeMenu.AddSimpleButton("Go back", () => UiUtils.ClientMenu.Show());
        _freezeMenu.AddToggleButton("PhotonFreeze", _ => ToggleFreeze(), () => _isFreeze);
        if (_isFreeze) _freezeMenu.AddSimpleButton("TP To Frozen Position", () => { TpPlayerToPos(_originalPos, _originalRot); });
        _freezeMenu.Show();
    }

    private static void TpPlayerToPos(Vector3 originalPos, Quaternion originalRot)
    {
        Utilities.GetLocalVrcPlayer().transform.position = originalPos;
        Utilities.GetLocalVrcPlayer().transform.rotation = originalRot;
    }

    private void ToggleFreeze()
    {
        _isFreeze = !_isFreeze;
        if (_isFreeze)
        {
            _originalPos = Utilities.GetLocalVrcPlayerApi().GetPosition();
            _originalRot = Utilities.GetLocalVrcPlayer().transform.rotation;
        }
        Clone(_isFreeze);
        ShowFreezeMenu();
    }

    private void Clone(bool toggle)
    {
        if (toggle)
        {
            _cloneObj = Object.Instantiate(Utilities.GetLocalVrcPlayer().prop_VRCAvatarManager_0.transform.Find("Avatar"), null, true);
            _cloneObj.name = "Cloned Frozen Avatar";
            _cloneObj.position = Utilities.GetLocalVrcPlayer().transform.position;
            _cloneObj.rotation = Utilities.GetLocalVrcPlayer().transform.rotation;

            var animator = _cloneObj.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                var boneTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                if (boneTransform != null) boneTransform.localScale = Vector3.one;
            }
            foreach (var component in _cloneObj.GetComponents<Component>())
                if (component is not Transform) Object.Destroy(component);
            Tools.SetLayerRecursively(_cloneObj.gameObject, LayerMask.NameToLayer("Player"));
        }
        else Object.Destroy(_cloneObj.gameObject);
    }

    private Il2CppSystem.Object _lastSentEv7;
    public bool? RaiseEvent7(IntPtr instancePtr, byte eType, IntPtr objPtr, IntPtr eOptions, SendOptions sOptions, IntPtr nativeMethodInfoPtr)
    {
        try
        {
            if (IsOn.Value && Il2CppArrayBase<int>.WrapNativeGenericArrayPointer(objPtr)[0] == _photonID)
            {
                if (!_isFreeze)
                    _lastSentEv7 = new Il2CppSystem.Object(objPtr);
                else
                    return Patches.RaiseEventDelegate.Invoke(instancePtr, eType, _lastSentEv7.Pointer, eOptions, sOptions, nativeMethodInfoPtr);
            }
        }
        catch (Exception e)
        {
            Main.Logger.Warning("Something went wrong in RaiseEvent7 Detour (PhotonFreeze)");
            Main.Logger.Error(e);
        }

        return null;
    }
}
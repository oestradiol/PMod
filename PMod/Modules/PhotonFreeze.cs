using PMod.Utils;
using UnityEngine;
using MelonLoader;
using Photon.Pun;
using VRC;
using VRC.Core;
using UIExpansionKit.API;
using Object = UnityEngine.Object;

namespace PMod.Modules;

internal class PhotonFreeze : ModuleBase
{
    private ICustomShowableLayoutedMenu _freezeMenu;
    private Transform _cloneObj;
    private Vector3 _originalPos;
    private Quaternion _originalRot;
    internal int PhotonID;
    internal bool IsFreeze;
    internal readonly MelonPreferences_Entry<bool> IsOn;

    internal PhotonFreeze()
    {
        MelonPreferences.CreateCategory("PhotonFreeze", "PM - Photon Freeze");
        IsOn = MelonPreferences.CreateEntry("PhotonFreeze", "IsOn", false, "Activate Mod? This is a risky function.");
        useOnPlayerJoined = true;
        useOnUpdate = true;
        useOnInstanceChanged = true;
        RegisterSubscriptions();
    }

    protected override void OnPlayerJoined(Player player) 
    { 
        if (player.prop_APIUser_0.id == Utilities.GetLocalAPIUser().id) 
            PhotonID = player.gameObject.GetComponent<PhotonView>().viewIdField;
    }

    private Vector3 _previousPos;
    private bool _isMaxD;

    protected override void OnUpdate()
    {
        if (!IsOn.Value) return;
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
        {
            var temp = Utilities.GetLocalVRCPlayer().gameObject.transform;
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

    protected override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => IsFreeze = false;

    internal void ShowFreezeMenu()
    {
        _freezeMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        _freezeMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
        _freezeMenu.AddToggleButton("PhotonFreeze", (_) => ToggleFreeze(), () => IsFreeze);
        if (IsFreeze) _freezeMenu.AddSimpleButton("TP To Frozen Position", () => { TpPlayerToPos(_originalPos, _originalRot); });
        _freezeMenu.Show();
    }

    private static void TpPlayerToPos(Vector3 originalPos, Quaternion originalRot)
    {
        Utilities.GetLocalVRCPlayer().transform.position = originalPos;
        Utilities.GetLocalVRCPlayer().transform.rotation = originalRot;
    }

    private void ToggleFreeze()
    {
        IsFreeze = !IsFreeze;
        if (IsFreeze)
        {
            _originalPos = Utilities.GetLocalVRCPlayerApi().GetPosition();
            _originalRot = Utilities.GetLocalVRCPlayer().transform.rotation;
        }
        Clone(IsFreeze);
        ShowFreezeMenu();
    }

    private void Clone(bool toggle)
    {
        if (toggle)
        {
            _cloneObj = Object.Instantiate(Utilities.GetLocalVRCPlayer().prop_VRCAvatarManager_0.transform.Find("Avatar"), null, true);
            _cloneObj.name = "Cloned Frozen Avatar";
            _cloneObj.position = Utilities.GetLocalVRCPlayer().transform.position;
            _cloneObj.rotation = Utilities.GetLocalVRCPlayer().transform.rotation;

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
}
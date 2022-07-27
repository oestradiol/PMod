using System.Collections.Generic;
using MelonLoader;
using PMod.Utils;
using UIExpansionKit.API;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using Object = UnityEngine.Object;
using Utilities = PMod.Utils.Utilities;

namespace PMod.Modules.Internals;

internal class Orbit : VrcMod
{
    private ICustomShowableLayoutedMenu _selectionMenu;
    private VRCPlayer _currentPlayer;
    private MelonPreferences_Entry<bool> _patch;
    private Dictionary<VRC_Pickup, OrbitItem> _pickupOrbits;

    private ICustomShowableLayoutedMenu _orbitMenu;
    internal Quaternion rotation;
    internal Quaternion rotationy;
    internal Vector3 OrbitCenter;
    internal float PlayerHeight;
    internal float Timer;
    internal MelonPreferences_Entry<float> radius;
    internal MelonPreferences_Entry<float> speed;
    private MelonPreferences_Entry<float> _rotx;
    private MelonPreferences_Entry<float> _roty;
    private MelonPreferences_Entry<float> _rotz;
    internal RotType rotType;
    internal enum RotType
    {
        CircularRot,
        CylindricalRot,
        SphericalRot,
    }

    public override void OnApplicationStart()
    {
        var thisModuleName = GetType().Name;
        MelonPreferences.CreateCategory(thisModuleName, $"{BuildInfo.Name} - {thisModuleName}");
        radius = MelonPreferences.CreateEntry(thisModuleName, "Radius", 1.0f, "Radius");
        speed = MelonPreferences.CreateEntry(thisModuleName, "Speed", 1.0f, "Speed");
        _patch = MelonPreferences.CreateEntry(thisModuleName, "Patch", true, "Patch items on Orbit");
        _rotx = MelonPreferences.CreateEntry(thisModuleName, "RotationX", 0.0f, "X Rotation");
        _roty = MelonPreferences.CreateEntry(thisModuleName, "RotationY", 0.0f, "Y Rotation");
        _rotz = MelonPreferences.CreateEntry(thisModuleName, "RotationZ", 0.0f, "Z Rotation");
        _orbitMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
        _orbitMenu.AddSimpleButton("Go back", () => UiUtils.ClientMenu.Show());
        _orbitMenu.AddSimpleButton("Stop Orbit", StopOrbit);
        _orbitMenu.AddSimpleButton("Circular Orbit", () => SelectOrbit(RotType.CircularRot));
        _orbitMenu.AddSimpleButton("Spherical Orbit", () => SelectOrbit(RotType.SphericalRot));
        _orbitMenu.AddSimpleButton("Cylindrical Orbit", () => SelectOrbit(RotType.CylindricalRot));
        UiUtils.ClientMenu.AddSimpleButton("Orbit", () => _orbitMenu.Show());
        OnPreferencesSaved();
    }

    public override void OnPreferencesSaved(string filePath = null)
    {
        rotation = Quaternion.Euler(_rotx.Value, 0, _rotz.Value);
        rotationy = Quaternion.Euler(0, _roty.Value, 0);
    }

    public override void OnUpdate()
    {
        if (_pickupOrbits == null || _currentPlayer == null) return;
        Timer += Time.deltaTime;
        OrbitCenter = GetCenter();
        foreach (var pickupOrbit in _pickupOrbits)
        {
            var key = pickupOrbit.Key;
            var value = pickupOrbit.Value;
            if (key == null)
                _pickupOrbits.Remove(key);
            else
            {
                if (_patch.Value) Patch(key);
                key.transform.position = value.CurrentPos();
                key.transform.rotation = value.CurrentRot();
            }
        }
    }

    public override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => StopOrbit();

    public override void OnPlayerLeft(Player player) { if (_currentPlayer != null && _currentPlayer._player.prop_APIUser_0.id == player.prop_APIUser_0.id) StopOrbit(); }

    private void SelectOrbit(RotType type)
    {
        rotType = type;
        _selectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
        _selectionMenu.AddSimpleButton("Go back", () => _orbitMenu.Show());
        foreach (var player in Object.FindObjectsOfType<VRCPlayer>()) 
            _selectionMenu.AddSimpleButton($"{player.prop_Player_0.prop_APIUser_0.displayName}", () => ToOrbit(player));
        _selectionMenu.Show();
    }

    private void ToOrbit(VRCPlayer player)
    {
        StopOrbit();
        _currentPlayer = player;
        Timer = 0f;
        var pickups = Object.FindObjectsOfType<VRC_Pickup>();
        OrbitCenter = GetCenter();
        _pickupOrbits = new Dictionary<VRC_Pickup, OrbitItem>();
        for (var i = 0; i < pickups.Count; i++)
            _pickupOrbits.Add(pickups[i], new OrbitItem(pickups, i));
    }

    private void StopOrbit()
    {
        if (_pickupOrbits == null) return;
        foreach (var keyValuePair in _pickupOrbits)
        {
            var key = keyValuePair.Key;
            var value = keyValuePair.Value;
            value.IsOn = false;
            if (key == null) continue;
            key.transform.position = value.CurrentPos();
            key.transform.rotation = value.CurrentRot();
            Unpatch(key, value);
        }
        _pickupOrbits = null;
        _currentPlayer = null;
        Timer = 0;
    }

    private static void Patch(VRC_Pickup item)
    {
        item.DisallowTheft = true;
        item.pickupable = false;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        item.gameObject.SetActive(true);
        if (Networking.GetOwner(item.gameObject).playerId == Utilities.GetLocalVrcPlayerApi().playerId) return;
        item.currentlyHeldBy = null; 
        Networking.SetOwner(Utilities.GetLocalVrcPlayerApi(), item.gameObject);
    }

    private static void Unpatch(VRC_Pickup key, OrbitItem value)
    {
        key.DisallowTheft = value.InitialTheft;
        key.pickupable = value.InitialPickupable;
        key.GetComponent<Rigidbody>().isKinematic = value.InitialKinematic;
        key.GetComponent<Rigidbody>().velocity = value.InitialVelocity;
        key.gameObject.SetActive(value.InitialActive);
    }

    private Vector3 GetCenter()
    {
        var head = Utilities.GetBoneTransform(_currentPlayer.prop_Player_0, HumanBodyBones.Head).position;
        if (rotType == RotType.CircularRot) return head;
        var pos = _currentPlayer.transform.position;
        PlayerHeight = (head - pos).y;
        return pos;
    }
}
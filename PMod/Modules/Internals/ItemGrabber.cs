using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using PMod.Utils;
using UIExpansionKit.API;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using Object = UnityEngine.Object;
using Utilities = PMod.Utils.Utilities;

namespace PMod.Modules.Internals;

// TODO: ItemGrabber Patch for Udon components.
internal class ItemGrabber : VrcMod
{
    private readonly Dictionary<IntPtr, object[]> _previousStates = new();
    private ICustomShowableLayoutedMenu _selectionMenu;
    private MelonPreferences_Entry<float> _minDistance;
    private MelonPreferences_Entry<bool> _patchAll;
    private MelonPreferences_Entry<bool> _takeOwnership;
    private ICustomShowableLayoutedMenu _pickupMenu;
    private VRC_Pickup[] _pickups;

    public override void OnApplicationStart()
    {
        var thisModuleName = GetType().Name;
        MelonPreferences.CreateCategory(thisModuleName, $"{BuildInfo.Name} - {thisModuleName}");
        _minDistance = MelonPreferences.CreateEntry(thisModuleName, "GrabDistance", -1.0f, "Distance (meters) for grabbing all, set to -1 for unlimited.");
        _patchAll = MelonPreferences.CreateEntry(thisModuleName, "PatchAllOnLoad", false, "Patch All on Scene Load");
        _takeOwnership = MelonPreferences.CreateEntry(thisModuleName, "TakeOwnership", true, "Take Ownership of Object on Grab");
        _pickupMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        _pickupMenu.AddSimpleButton("Go back", () => UiUtils.ClientMenu.Show());
        _pickupMenu.AddSimpleButton("Patch", () => Select("Patch"));
        _pickupMenu.AddSimpleButton("Unpatch", () => Select("Unpatch"));
        _pickupMenu.AddSimpleButton("Grab", () => Select("Grab"));
        UiUtils.ClientMenu.AddSimpleButton("ItemGrabber", () => _pickupMenu.Show());
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (!_patchAll.Value) return;
        _pickups = Object.FindObjectsOfType<VRC_Pickup>();
        PatchAll();
    }

    private void Select(string type)
    {
        _selectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
        _selectionMenu.AddSimpleButton("Go back", () => _pickupMenu.Show());
        _pickups = Object.FindObjectsOfType<VRC_Pickup>();
        switch (type)
        {
            case "Patch":
            {
                _selectionMenu.AddSimpleButton("Patch All", PatchAll);
                foreach (var pickup in _pickups) 
                    _selectionMenu.AddSimpleButton(pickup.name, () => Patch(pickup));
                break;
            }
            case "Unpatch":
            {
                _selectionMenu.AddSimpleButton("Unpatch All", UnpatchAll);
                if (_previousStates.Count != 0) 
                    foreach (var pickup in _previousStates.Values.Where(p => p[0] != null).Select(p => (VRC_Pickup)p[0]))
                        _selectionMenu.AddSimpleButton(pickup.name, () => { Unpatch(pickup); Select("Unpatch"); });
                break;
            }
            default:
            {
                _selectionMenu.AddSimpleButton("Grab in Range", () => Trigger(null));
                foreach (var pickup in _pickups) _selectionMenu.AddSimpleButton(pickup.name, () => Trigger(pickup));
                break;
            }
        }
        _selectionMenu.Show();
    }

    private void Patch(VRC_Pickup item)
    {
        if (!_previousStates.ContainsKey(item.Pointer)) _previousStates.Add(item.Pointer, new object[] 
        {
            item,
            item.DisallowTheft,
            item.allowManipulationWhenEquipped,
            item.pickupable,
            item.gameObject.active
        });
        item.DisallowTheft = false;
        item.allowManipulationWhenEquipped = true;
        item.pickupable = true;
        item.gameObject.SetActive(true);
    }

    private void PatchAll() { foreach (var pickup in _pickups) Patch(pickup); }

    private void Unpatch(VRC_Pickup item)
    {
        if (!_previousStates.ContainsKey(item.Pointer)) return;
        var previousState = _previousStates[item.Pointer];
        item.DisallowTheft = (bool)previousState[1];
        item.allowManipulationWhenEquipped = (bool)previousState[2];
        item.pickupable = (bool)previousState[3];
        item.gameObject.SetActive((bool)previousState[4]);
        _previousStates.Remove(item.Pointer);
    }

    private void UnpatchAll() 
    { 
        while (_previousStates.Count != 0) Unpatch((VRC_Pickup)_previousStates.First().Value[0]);
        Select("Unpatch");
    }

    private void Trigger(VRC_Pickup item)
    {
        if (item == null) foreach (var pickup in _pickups)
        {
            var dist = Vector3.Distance(Utilities.GetLocalVrcPlayer().transform.position, pickup.transform.position);
            if (Math.Abs(_minDistance.Value + 1) < 0.01 || dist <= _minDistance.Value) PickupItem(pickup);
        }
        else PickupItem(item);
    }
        
    private void PickupItem(VRC_Pickup item)
    {
        try
        {
            Patch(item);
            if (_takeOwnership.Value && Networking.GetOwner(item.gameObject).playerId != Utilities.GetLocalVrcPlayerApi().playerId)
            {
                item.GetComponent<VRC_Pickup>().currentlyHeldBy = null;
                Networking.SetOwner(Utilities.GetLocalVrcPlayerApi(), item.gameObject);
            }
            item.transform.position = Utilities.GetBoneTransform(Player.prop_Player_0, HumanBodyBones.Hips).position;
        }
        catch (Exception e) { Main.Logger.Error($"Failed to grab item {item.name}! {e}"); }
    }
}
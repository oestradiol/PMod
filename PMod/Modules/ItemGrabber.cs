using PMod.Loader;
using System;
using System.Linq;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using UIExpansionKit.API;
using Object = UnityEngine.Object;
using Utilities = PMod.Utils.Utilities;

namespace PMod.Modules
{
    internal class ItemGrabber : ModuleBase
    {
        private ICustomShowableLayoutedMenu _selectionMenu;
        private VRC_Pickup[] _pickups;
        private readonly Dictionary<IntPtr, object[]> _previousStates = new();
        private readonly MelonPreferences_Entry<float> _minDistance;
        private readonly MelonPreferences_Entry<bool> _patchAll;
        private readonly MelonPreferences_Entry<bool> _takeOwnership;
        internal readonly ICustomShowableLayoutedMenu PickupMenu;
        internal readonly MelonPreferences_Entry<bool> IsOn;

        internal ItemGrabber()
        {
            MelonPreferences.CreateCategory("ItemGrabber", "PM - Item Grabber");
            IsOn = MelonPreferences.CreateEntry("ItemGrabber", "IsOn", false, "Activate Mod? This is a risky function.");
            _minDistance = MelonPreferences.CreateEntry("ItemGrabber", "GrabDistance", -1.0f, "Distance (meters) for grabbing all, set to -1 for unlimited.");
            _patchAll = MelonPreferences.CreateEntry("ItemGrabber", "PatchAllOnLoad", false, "Patch All on Scene Load");
            _takeOwnership = MelonPreferences.CreateEntry("ItemGrabber", "TakeOwnership", true, "Take Ownership of Object on Grab");
            PickupMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            PickupMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
            PickupMenu.AddSimpleButton("Patch", () => Select("Patch"));
            PickupMenu.AddSimpleButton("Unpatch", () => Select("Unpatch"));
            PickupMenu.AddSimpleButton("Grab", () => Select("Grab"));
            useOnSceneWasLoaded = true;
            RegisterSubscriptions();
        }

        protected override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!_patchAll.Value) return;
            _pickups = Object.FindObjectsOfType<VRC_Pickup>();
            PatchAll();
        }

        private void Select(string type)
        {
            _selectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            _selectionMenu.AddSimpleButton("Go back", () => PickupMenu.Show());
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
                var dist = Vector3.Distance(Utilities.GetLocalVRCPlayer().transform.position, pickup.transform.position);
                if (Math.Abs(_minDistance.Value + 1) < 0.01 || dist <= _minDistance.Value) PickupItem(pickup);
            }
            else PickupItem(item);
        }
        
        private void PickupItem(VRC_Pickup item)
        {
            try
            {
                Patch(item);
                if (_takeOwnership.Value && Networking.GetOwner(item.gameObject).playerId != Utilities.GetLocalVRCPlayerApi().playerId)
                {
                    item.GetComponent<VRC_Pickup>().currentlyHeldBy = null;
                    Networking.SetOwner(Utilities.GetLocalVRCPlayerApi(), item.gameObject);
                }
                item.transform.position = Utilities.GetBoneTransform(Player.prop_Player_0, HumanBodyBones.Hips).position;
            }
            catch (Exception e) { PLogger.Error($"Failed to grab item {item.name}! {e}"); }
        }
    }
}
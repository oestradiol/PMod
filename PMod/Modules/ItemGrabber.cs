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
        private ICustomShowableLayoutedMenu SelectionMenu;
        private VRC_Pickup[] Pickups;
        private Dictionary<VRC_Pickup, bool[]> PreviousStates = new();
        private MelonPreferences_Entry<float> min_distance;
        private MelonPreferences_Entry<bool> patch_all;
        private MelonPreferences_Entry<bool> take_ownership;
        internal ICustomShowableLayoutedMenu PickupMenu;
        internal MelonPreferences_Entry<bool> IsOn;

        internal ItemGrabber()
        {
            MelonPreferences.CreateCategory("ItemGrabber", "PM - Item Grabber");
            IsOn = MelonPreferences.CreateEntry("ItemGrabber", "IsOn", false, "Activate Mod? This is a risky function.");
            min_distance = MelonPreferences.CreateEntry("ItemGrabber", "GrabDistance", -1.0f, "Distance (meters) for grabbing all, set to -1 for unlimited.");
            patch_all = MelonPreferences.CreateEntry("ItemGrabber", "PatchAllOnLoad", false, "Patch All on Scene Load");
            take_ownership = MelonPreferences.CreateEntry("ItemGrabber", "TakeOwnership", true, "Take Ownership of Object on Grab");
            PickupMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            PickupMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
            PickupMenu.AddSimpleButton("Patch", () => Select("Patch"));
            PickupMenu.AddSimpleButton("Unpatch", () => Select("Unpatch"));
            PickupMenu.AddSimpleButton("Grab", () => Select("Grab"));
            useOnSceneWasLoaded = true;
            RegisterSubscriptions();
        }

        internal override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (patch_all.Value)
            {
                Pickups = Object.FindObjectsOfType<VRC_Pickup>();
                PatchAll();
            }
        }

        private void Select(string Type)
        {
            SelectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            SelectionMenu.AddSimpleButton("Go back", () => PickupMenu.Show());
            Pickups = Object.FindObjectsOfType<VRC_Pickup>();
            if (Type == "Patch")
            {
                SelectionMenu.AddSimpleButton("Patch All", () => PatchAll());
                foreach (var Pickup in Pickups) SelectionMenu.AddSimpleButton(Pickup.name, () => Patch(Pickup));

            }
            else if (Type == "Unpatch")
            {
                SelectionMenu.AddSimpleButton("Unpatch All", () => UnpatchAll());
                if (PreviousStates.Count != 0) foreach (var Pickup in PreviousStates.Keys) SelectionMenu.AddSimpleButton(Pickup.name, () => { Unpatch(Pickup); Select("Unpatch"); });
            }
            else
            {
                SelectionMenu.AddSimpleButton("Grab in Range", () => Trigger(null));
                foreach (var Pickup in Pickups) SelectionMenu.AddSimpleButton(Pickup.name, () => Trigger(Pickup));
            }
            SelectionMenu.Show();
        }

        private void Patch(VRC_Pickup Item)
        {
            var pickup = Item.GetComponent<VRC_Pickup>();
            if (!PreviousStates.ContainsKey(Item)) PreviousStates.Add(Item, new [] 
            {
                 pickup.DisallowTheft,
                 pickup.allowManipulationWhenEquipped,
                 pickup.pickupable,
                 Item.gameObject.active
            });
            pickup.DisallowTheft = false;
            pickup.allowManipulationWhenEquipped = true;
            pickup.pickupable = true;
            Item.gameObject.SetActive(true);
        }

        private void PatchAll() { foreach (var Pickup in Pickups) Patch(Pickup); }

        private void Unpatch(VRC_Pickup Item)
        {
            if (PreviousStates.ContainsKey(Item))
            {
                var pickup = Item.GetComponent<VRC_Pickup>();
                var PreviousState = PreviousStates[Item];
                pickup.DisallowTheft = PreviousState[0];
                pickup.allowManipulationWhenEquipped = PreviousState[1];
                pickup.pickupable = PreviousState[2];
                Item.gameObject.SetActive(PreviousState[3]);
                PreviousStates.Remove(Item);
            }
        }

        private void UnpatchAll() 
        { 
            while (PreviousStates.Count != 0) Unpatch(PreviousStates.First().Key);
            Select("Unpatch");
        }

        private void Trigger(VRC_Pickup Item)
        {
            if (Item == null) foreach (var Pickup in Pickups)
            {
                float dist = Vector3.Distance(Utilities.GetLocalVRCPlayer().transform.position, Pickup.transform.position);
                if (min_distance.Value == -1 || dist <= min_distance.Value) PickupItem(Pickup);
            }
            else PickupItem(Item);
        }
        
        private void PickupItem(VRC_Pickup Item)
        {
            try
            {
                Patch(Item);
                if (take_ownership.Value && Networking.GetOwner(Item.gameObject).playerId != Utilities.GetLocalVRCPlayerApi().playerId)
                {
                    Item.GetComponent<VRC_Pickup>().currentlyHeldBy = null;
                    Networking.SetOwner(Utilities.GetLocalVRCPlayerApi(), Item.gameObject);
                }
                Item.transform.position = Utilities.GetBoneTransform(Player.prop_Player_0, HumanBodyBones.Hips).position;
            }
            catch (Exception e)
            {
                PLogger.Error($"Failed to grab item {Item.name}! {e}");
            }
        }
    }
}
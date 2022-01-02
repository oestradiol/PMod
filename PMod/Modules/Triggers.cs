using PMod.Utils;
using System;
using MelonLoader;
using UnityEngine;
using VRC.SDKBase;
using UIExpansionKit.API;

namespace PMod.Modules
{
    internal class Triggers : ModuleBase
    {
        private bool _isForceGlobal;
        private readonly MelonPreferences_Entry<bool> _isOn;
        internal bool IsAlwaysForceGlobal;

        internal Triggers()
        {
            MelonPreferences.CreateCategory("LocalToGlobal", "PM - Local To Global");
            _isOn = MelonPreferences.CreateEntry("LocalToGlobal", "IsOn", false, "Activate Mod? This is a risky function.");
            RegisterSubscriptions();
        }

        internal void ShowTriggersMenu()
        {
            var triggersMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            triggersMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
            var btnName = "LocalToGlobal - ";
            Action action;
            switch (_isOn.Value)
            {
                case true when Utils.Utilities.GetWorldSDKVersion() == Utils.Utilities.WorldSDKVersion.SDK2:
                    btnName += "On";
                    action = ShowLocalToGlobalMenu;
                    break;
                case false:
                    btnName += "Off";
                    action = () => Utils.Utilities.RiskyFuncAlert("LocalToGlobal");
                    break;
                default:
                    btnName += "Off";
                    action = () => DelegateMethods.PopupV2(
                        "LocalToGlobal",
                        "Sorry, this world is an SDK3 Udon world.\nThis function won't work in here.",
                        "Close",
                        new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }));
                    break;
            }
            triggersMenu.AddSimpleButton(btnName, action);
            foreach (var trigger in Resources.FindObjectsOfTypeAll<VRC_Trigger>()) triggersMenu.AddSimpleButton(trigger.name, () => TriggerMenu(trigger));
            triggersMenu.Show();
        }

        private void ShowLocalToGlobalMenu()
        {
            var localToGlobalMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            localToGlobalMenu.AddSimpleButton("Go back", ShowTriggersMenu);
            localToGlobalMenu.AddToggleButton("Always Force Local to Global", (_) => IsAlwaysForceGlobal = !IsAlwaysForceGlobal, () => IsAlwaysForceGlobal);
            localToGlobalMenu.AddToggleButton("Force Local to Global on Trigger", (_) => _isForceGlobal = !_isForceGlobal, () => _isForceGlobal);
            localToGlobalMenu.Show();
        }

        private void TriggerMenu(VRC_Trigger trigger)
        {
            var triggerMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            triggerMenu.AddSimpleButton("Go back", ShowTriggersMenu);
            foreach (var @event in trigger.Triggers) triggerMenu.AddSimpleButton(@event.Name, () => 
            { 
                NativePatches.triggerOnceLtg |= _isForceGlobal;
                trigger.ExecuteTrigger?.Invoke(@event);
            });
            triggerMenu.Show();
        }
    }
}
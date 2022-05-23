using System;
using PMod.Utils;
using UIExpansionKit.API;
using UnityEngine;
using VRC.SDKBase;

namespace PMod.Modules.Internals;

internal class Triggers : VrcMod
{
    private bool _isForceGlobal;
    private bool _isAlwaysForceGlobal;
    private bool _triggerOnceLtg;

    public override void OnApplicationStart() =>
        UiUtils.ClientMenu.AddSimpleButton("Triggers", ShowTriggersMenu);
        
    private void ShowTriggersMenu()
    {
        var triggersMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        triggersMenu.AddSimpleButton("Go back", () => UiUtils.ClientMenu.Show());
        var btnName = "LocalToGlobal - ";
        Action action;
        switch (Utils.Utilities.GetWorldSDKVersion() == Utils.Utilities.WorldSDKVersion.SDK2)
        {
            case true:
                btnName += "On";
                action = ShowLocalToGlobalMenu;
                break;
            case false:
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
        localToGlobalMenu.AddToggleButton("Always Force Local to Global", _ => _isAlwaysForceGlobal = !_isAlwaysForceGlobal, () => _isAlwaysForceGlobal);
        localToGlobalMenu.AddToggleButton("Force Local to Global on Trigger", _ => _isForceGlobal = !_isForceGlobal, () => _isForceGlobal);
        localToGlobalMenu.Show();
    }

    private void TriggerMenu(VRC_Trigger trigger)
    {
        var triggerMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
        triggerMenu.AddSimpleButton("Go back", ShowTriggersMenu);
        foreach (var @event in trigger.Triggers) triggerMenu.AddSimpleButton(@event.Name, () => 
        { 
            _triggerOnceLtg |= _isForceGlobal;
            trigger.ExecuteTrigger?.Invoke(@event);
        });
        triggerMenu.Show();
    }
    
    public VRC_EventHandler.VrcBroadcastType OnTriggerEvent(VRC_EventHandler.VrcBroadcastType broadcast)
    {
        try
        {
            if (!IsOn.Value || (!_isAlwaysForceGlobal && !_triggerOnceLtg) || broadcast != VRC_EventHandler.VrcBroadcastType.Local) 
                return broadcast;
            
            broadcast = VRC_EventHandler.VrcBroadcastType.AlwaysUnbuffered;
            _triggerOnceLtg = false;
        }
        catch (Exception e)
        {
            Main.Logger.Warning("Something went wrong in LocalToGlobalSetup Detour");
            Main.Logger.Error(e);
        }

        return broadcast;
    }
}
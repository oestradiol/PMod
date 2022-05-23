using System;
using System.Linq;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

namespace PMod.Utils;

internal static class UiUtils
{
    internal static ICustomShowableLayoutedMenu ClientMenu;
    internal static void Init()
    {
        ClientMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
        ClientMenu.AddSimpleButton("Close Menu", ClientMenu.Hide);
        ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton(BuildInfo.Name, () => ClientMenu.Show());
    }
    
    internal static void RiskyFuncAlert(string funcName) => DelegateMethods.PopupV2(
        funcName,
        "You have to first activate the mod on Melon Preferences menu! Be aware that this is a risky function.",
        "Close",
        new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }));
    
    private static Transform _selectedUserMenuQm;
    internal static Transform SelectedUserMenuQm => _selectedUserMenuQm ??= Resources.FindObjectsOfTypeAll<VRC.UI.Elements.Menus.SelectedUserMenuQM>()[1].transform;
    private static Button _baseButton;
    private static Button BaseButton => _baseButton ??= new Func<Button>(() =>
    {
        var button = UnityEngine.Object.Instantiate( SelectedUserMenuQm
                .Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_AvatarActions/Button_AddToFavorites"))
            .GetComponent<Button>();
        UnityEngine.Object.DestroyImmediate(button.transform.Find("Favorite Disabled Button").gameObject);
        button.onClick = new Button.ButtonClickedEvent();
        button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Placeholder";
        button.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 = "Placeholder";
        button.name = "Button_Base";
        return button;
    }).Invoke();
    internal enum Menu { InteractMenu }
    internal static Button CreateButton(Menu menu, string uiButtonText, string uiTooltip, Action onClick) => CreateButton(menu switch
    {
        Menu.InteractMenu => SelectedUserMenuQm.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"),
        _ => throw new ArgumentException("Menu not found.", nameof(menu))
    }, uiButtonText, uiTooltip, onClick);
    internal static Button CreateButton(Transform parent, string uiButtonText, string uiTooltip, Action onClick)
    {
        var button = UnityEngine.Object.Instantiate(BaseButton, parent);
        button.onClick.AddListener(onClick);
        button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = uiButtonText;
        button.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 = uiTooltip;
        button.name = "Button_" + uiButtonText.Split(' ').Aggregate("", (current, str) => current + str);
        return button;
    }
}
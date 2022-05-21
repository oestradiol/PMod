using PMod.Loader;
using System;
using System.Linq;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using VRC;

namespace PMod.Utils;

internal static class DelegateMethods
{
    private static dynamic _popupV2Delegate;
    internal static void PopupV2(string title, string body, string submitButtonText, Il2CppSystem.Action submitButtonAction) =>
        (_popupV2Delegate ??= typeof(VRCUiPopupManager)
            .GetMethods().First(methodBase => 
                methodBase.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") &&
                !methodBase.Name.Contains("PDM") &&
                Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/StandardPopupV2") &&
                Utilities.WasUsedBy(methodBase, "OpenSaveSearchPopup"))
            .CreateDelegate())
        (VRCUiPopupManager.prop_VRCUiPopupManager_0, title, body, submitButtonText, submitButtonAction, null);

    private static dynamic _inputPopupDelegate;
    internal static void InputPopup(string title, string submitButtonText, Il2CppSystem.Action<string, List<KeyCode>, Text> submitButtonAction, string placeholderText = "Enter text....",
        bool useNumericKeypad = false, Il2CppSystem.Action cancelButtonAction = null, string body = null, InputField.InputType inputType = InputField.InputType.Standard) => // Extra shit
        (_inputPopupDelegate ??= typeof(VRCUiPopupManager)
            .GetMethods().First(methodBase => 
                methodBase.Name.StartsWith("Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_") && 
                !methodBase.Name.Contains("PDM") && Utilities.ContainsStr(methodBase, "UserInterface/MenuContent/Popups/InputPopup"))
            .CreateDelegate())
        (VRCUiPopupManager.prop_VRCUiPopupManager_0, title, body, inputType, useNumericKeypad, submitButtonText, submitButtonAction, cancelButtonAction, placeholderText, true, null, false, 0);

    private static Func<int, Player> _playerFromPhotonIDMethod;
    internal static Player GetPlayerFromPhotonID(int id) =>
        (_playerFromPhotonIDMethod ??= typeof(PlayerManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(mi => mi.Name.Contains("Method_Public_Static_Player_Int32_"))
            .OrderBy(UnhollowerSupport.GetIl2CppMethodCallerCount).Last()
            .CreateDelegate<Func<int, Player>>())(id);
}
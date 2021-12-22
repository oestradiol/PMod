using System;
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;
using PMod.Utils;

namespace PMod.Modules
{
    internal class AvatarFromID : ModuleBase
    {
        internal AvatarFromID()
        {
            useOnUiManagerInit = true;
            RegisterSubscriptions();
        }

        internal override void OnUiManagerInit() =>
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.AvatarMenu).AddSimpleButton("Avatar from ID", () =>
            { DelegateMethods.InputPopup("Avatar from ID", "Change Avatar", (Action<string, Il2CppSystem.Collections.Generic.List<KeyCode>, Text>)((ID, _, _) => Utilities.ChangeToAVByID(ID)), "Insert Avatar ID"); });
    }
}
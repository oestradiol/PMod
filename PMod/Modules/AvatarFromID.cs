using PMod.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;
using VRC.Core;
using VRC.UI;

namespace PMod.Modules
{
    internal class AvatarFromID : ModuleBase
    {
        internal AvatarFromID()
        {
            useOnUiManagerInit = true;
            RegisterSubscriptions();
        }

        protected override void OnUiManagerInit() =>
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.AvatarMenu).AddSimpleButton("Avatar from ID", () =>
            {
                DelegateMethods.InputPopup("Avatar from ID", "Change Avatar",
                    (Action<string, Il2CppSystem.Collections.Generic.List<KeyCode>, Text>)
                    ((id, _, _) => ChangeToAvByID(id)), "Insert Avatar ID");
            });

        private static void ChangeToAvByID(string id)
        {
            var aviMenu = Resources.FindObjectsOfTypeAll<PageAvatar>()[0];
            aviMenu.field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = new ApiAvatar { id = id };
            aviMenu.ChangeToSelectedAvatar();
        }
    }
}
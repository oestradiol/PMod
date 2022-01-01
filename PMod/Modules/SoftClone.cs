using PMod.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine.UI;
using VRC.DataModel;

namespace PMod.Modules
{
    internal class SoftClone : ModuleBase // Thanks to Yui! <3
    {
        internal Il2CppSystem.Object CurrAvatarDict;
        private readonly MethodInfo _reloadAvMethod;
        internal bool IsSoftClone;

        internal SoftClone()
        {
            MethodBase currentInstance;
            _reloadAvMethod = typeof(VRCPlayer)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(mi => mi.Name.StartsWith("Method_Private_Void_Boolean_") &&
                    mi.GetParameters().Any(pi => pi.IsOptional) &&
                    XrefScanner.UsedBy(mi)
						.Any(instance => instance.Type == XrefType.Method &&
                            (currentInstance = instance.TryResolve()) != null &&
                            currentInstance.Name == "ReloadAvatarNetworkedRPC"));
			useOnUiManagerInit = true;
			RegisterSubscriptions();
        }

        protected override void OnUiManagerInit()
        {
            var selectedUserMenuQm = Resources.FindObjectsOfTypeAll<VRC.UI.Elements.Menus.SelectedUserMenuQM>()[1];
            var addToFavoritesButton = selectedUserMenuQm.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_AvatarActions/Button_AddToFavorites");
            var softCloneButton = UnityEngine.Object.Instantiate(addToFavoritesButton, addToFavoritesButton.parent.parent.Find("Buttons_UserActions")).GetComponent<Button>();
            UnityEngine.Object.DestroyImmediate(softCloneButton.transform.Find("Favorite Disabled Button").gameObject);
            softCloneButton.onClick = new Button.ButtonClickedEvent();
            softCloneButton.onClick.AddListener(new Action(_SoftClone));
            softCloneButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Soft Clone Avatar";
            softCloneButton.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 = "Locally clones the selected user's Avatar.";
            softCloneButton.name = "Button_CopyAssetButton";
        }

        private void _SoftClone()
        {
            var targetID = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1.id;
            if (targetID == null)
            {
                Loader.PLogger.Warning("Selected player was invalid! Failed to local clone.");
                return;
            }

            CurrAvatarDict = Utilities.GetPlayerFromID(targetID)?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
            IsSoftClone = true;

            _reloadAvMethod.Invoke(Utilities.GetLocalVRCPlayer(), new object[] { true });
        }
    }
}

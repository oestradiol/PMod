using PMod.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnhollowerRuntimeLib.XrefScans;
using VRC.DataModel;

namespace PMod.Modules
{
    internal class SoftClone : ModuleBase // Thanks to Yui! <3
    {
        internal Il2CppSystem.Object CurrAvatarDict;
        private MethodInfo ReloadAVMethod;
        internal bool IsSoftClone = false;

        internal SoftClone()
        {
            MethodBase CurrentInstance;
            ReloadAVMethod = typeof(VRCPlayer)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(mi => mi.Name.StartsWith("Method_Private_Void_Boolean_") &&
                    mi.GetParameters().Any(pi => pi.IsOptional) &&
                    XrefScanner.UsedBy(mi)
						.Any(instance => instance.Type == XrefType.Method &&
                            (CurrentInstance = instance.TryResolve()) != null &&
                            CurrentInstance.Name == "ReloadAvatarNetworkedRPC"));
			useOnUiManagerInit = true;
			RegisterSubscriptions();
        }

        internal override void OnUiManagerInit()
        {
            var selectedUserMenuQM = Resources.FindObjectsOfTypeAll<VRC.UI.Elements.Menus.SelectedUserMenuQM>()[1];
            var AddToFavoritesButton = selectedUserMenuQM.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_AvatarActions/Button_AddToFavorites");
            var SoftCloneButton = UnityEngine.Object.Instantiate(AddToFavoritesButton, AddToFavoritesButton.parent.parent.Find("Buttons_UserActions")).GetComponent<UnityEngine.UI.Button>();
            UnityEngine.Object.DestroyImmediate(SoftCloneButton.transform.Find("Favorite Disabled Button").gameObject);
            SoftCloneButton.onClick = new();
            SoftCloneButton.onClick.AddListener(new Action(() => _SoftClone()));
            SoftCloneButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Soft Clone Avatar";
            SoftCloneButton.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 = "Locally clones the selected user's Avatar.";
            SoftCloneButton.name = "Button_CopyAssetButton";
        }

        private void _SoftClone()
        {
            var TargetID = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1.id;
            if (TargetID == null)
            {
                Loader.PLogger.Warning("Selected player was invalid! Failed to local clone.");
                return;
            }

            CurrAvatarDict = Utilities.GetPlayerFromID(TargetID)?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
            IsSoftClone = true;

            ReloadAVMethod.Invoke(Utilities.GetLocalVRCPlayer(), new object[] { true });
        }
    }
}

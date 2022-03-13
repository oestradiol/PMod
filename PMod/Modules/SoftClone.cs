using PMod.Utils;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
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

        protected override void OnUiManagerInit() =>
            Utilities.CreateButton(Utilities.Menu.InteractMenu, "Soft Clone Avatar", "Locally clones the selected user's Avatar.", _SoftClone);

        private void _SoftClone()
        {
            var targetID = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1.id;
            if (targetID == null)
            {
                Main.Logger.Warning("Selected player was invalid! Failed to local clone.");
                return;
            }

            CurrAvatarDict = Utilities.GetPlayerFromID(targetID)?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
            IsSoftClone = true;

            _reloadAvMethod.Invoke(Utilities.GetLocalVRCPlayer(), new object[] { true });
        }
    }
}

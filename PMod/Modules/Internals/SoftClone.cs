using System;
using System.Linq;
using System.Reflection;
using ExitGames.Client.Photon;
using PMod.Utils;
using UnhollowerRuntimeLib.XrefScans;
using VRC.DataModel;

namespace PMod.Modules.Internals;

internal class SoftClone : VrcMod // Thanks to Yui! <3
{
    private Il2CppSystem.Object _currAvatarDict;
    private MethodInfo _reloadAvMethod;
    private bool _isSoftClone;

    public override void OnApplicationStart()
    {
        MethodBase currentInstance;
        _reloadAvMethod = typeof(VRCPlayer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(mi => mi.Name.StartsWith("Method_Private_Void_Boolean_") &&
                 mi.GetParameters().Any(pi => pi.IsOptional) &&
                 XrefScanner.UsedBy(mi).Any(instance => instance.Type == XrefType.Method &&
                      (currentInstance = instance.TryResolve()) != null &&
                      currentInstance.Name == "ReloadAvatarNetworkedRPC"));
    }

    public override void OnUiManagerInit() =>
        UiUtils.CreateButton(UiUtils.Menu.InteractMenu, "Soft Clone Avatar", "Locally clones the selected user's Avatar.", _SoftClone);

    private void _SoftClone()
    {
        var targetID = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1.id;
        if (targetID == null)
        {
            Main.Logger.Warning("Selected player was invalid! Failed to local clone.");
            return;
        }

        _currAvatarDict = Utilities.GetPlayerFromID(targetID)?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
        _isSoftClone = true;

        _reloadAvMethod.Invoke(Utilities.GetLocalVrcPlayer(), new object[] { true });
    }

    private bool _turnOffNext;
    public void OnEvent253(EventData eventData)
    {
        try
        {
            if (!IsOn.Value || !_isSoftClone || _currAvatarDict == null || eventData.Sender != Utilities.GetLocalVrcPlayerApi().playerId)
                return;

            eventData.Parameters[251].Cast<Il2CppSystem.Collections.Hashtable>()["avatarDict"] = _currAvatarDict;

            if (_turnOffNext)
            {
                _currAvatarDict = null;
                _isSoftClone = false;
                _turnOffNext = false;
            }
            else _turnOffNext = true;
        }
        catch (Exception e)
        {
            Main.Logger.Warning("Something went wrong in OnEvent253 Detour (SoftClone)");
            Main.Logger.Error(e);
        }
    }
}
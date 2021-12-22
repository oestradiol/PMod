using PMod.Loader;
using PMod.Utils;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using MelonLoader;
using UnityEngine;
using VRC.Core;
using System.Text.RegularExpressions;

namespace PMod.Modules
{
    internal class UserInteractUtils : ModuleBase
    {
        private MelonPreferences_Entry<string> ToPath;

        internal static VRC.UI.Elements.Menus.SelectedUserMenuQM selectedUserMenuQM;
        internal static UnityEngine.UI.Button CopyAssetButton;

        internal UserInteractUtils()
        {
            MelonPreferences.CreateCategory("UserInteractUtils", "PM - User Interact Utils");
            ToPath = MelonPreferences.CreateEntry("UserInteractUtils", "ToPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Assets"), "Path to save Assets");
            useOnUiManagerInit = true;
            RegisterSubscriptions();
        }

        internal override void OnUiManagerInit()
        {
            selectedUserMenuQM = Resources.FindObjectsOfTypeAll<VRC.UI.Elements.Menus.SelectedUserMenuQM>()[1];
            var AddToFavoritesButton = selectedUserMenuQM.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_AvatarActions/Button_AddToFavorites");
            CopyAssetButton = UnityEngine.Object.Instantiate(AddToFavoritesButton, AddToFavoritesButton.parent.parent.Find("Buttons_UserActions")).GetComponent<UnityEngine.UI.Button>();
            UnityEngine.Object.DestroyImmediate(CopyAssetButton.transform.Find("Favorite Disabled Button").gameObject);
            CopyAssetButton.onClick = new();
            CopyAssetButton.onClick.AddListener(new Action(() => CopyAsset()));
            CopyAssetButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Copy Asset";
            CopyAssetButton.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 = "Copies the asset file to the destined folder.";
            CopyAssetButton.name = "Button_CopyAssetButton";
        }

        private void CopyAsset()
        {
            ApiAvatar avatar = Utilities.GetPlayerFromID(selectedUserMenuQM.field_Private_IUser_0.prop_String_0).prop_ApiAvatar_0;
            if (!Directory.Exists(ToPath.Value)) Directory.CreateDirectory(ToPath.Value);
            try
            {
                ToCopyAsset(avatar);
                DelegateMethods.PopupV2(
                    "Copy Asset",
                    $"Successfully copied avatar \"{avatar.name}\" to folder \"{ToPath.Value}\"!",
                    "Close",
                    new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }));
            }
            catch (Exception e)
            {
                DelegateMethods.PopupV2(
                    "Copy Asset",
                    $"Failed to copy avatar \"{avatar.name}\" :(\nIf you see this message, please send the devs your last Melon log.",
                    "Close",
                    new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }));
                PLogger.Error(e);
            }
        }

        private string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new(ba.Length * 2);
            foreach (byte b in ba) hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private string ComputeVersionString(string assetUrl)
        {
            string result = "";
            foreach (byte b in BitConverter.GetBytes(int.Parse(new Regex("(?:\\/file_[0-9A-Za-z-]+\\/)([0-9]+)", RegexOptions.Compiled)?.Match(assetUrl)?.Groups[1]?.Value)))
                result += b.ToString("X2");
            return string.Concat(Enumerable.Repeat("0", (32 - result.Length))) + result;
        }

        private void ToCopyAsset(ApiAvatar avatar) =>
            File.Copy(new DirectoryInfo(
                    Path.Combine(
                        AssetBundleDownloadManager.prop_AssetBundleDownloadManager_0.field_Private_Cache_0.path,
                        ByteArrayToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(avatar.assetUrl.Substring(avatar.assetUrl.IndexOf("file_"), 41)))).ToUpper().Substring(0, 16),
                        ComputeVersionString(avatar.assetUrl)))
                    .GetFiles("*.*", SearchOption.AllDirectories).First(file => file.Name.Contains("__data")).FullName,
                    Path.Combine(ToPath.Value, $"{avatar.id}.vrca"),
                    true);
    }
}
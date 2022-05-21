using PMod.Utils;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MelonLoader;
using VRC.Core;

namespace PMod.Modules;

internal class CopyAsset : ModuleBase
{
    private MelonPreferences_Entry<string> _toPath;

    public CopyAsset() : base(true)
    {
        useOnApplicationStart = true;
        useOnUiManagerInit = true;
        RegisterSubscriptions();
    }

    protected override void OnApplicationStart()
    {
        MelonPreferences.CreateCategory(ThisModuleName, $"{BuildInfo.Name} - {ThisModuleName}");
        _toPath = MelonPreferences.CreateEntry(ThisModuleName, "ToPath", 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Assets"), "Path to save Assets");
    }

    protected override void OnUiManagerInit() =>
        UiUtils.CreateButton(UiUtils.Menu.InteractMenu, "Copy Asset", "Copies the asset file to the destined folder.", _CopyAsset);

    private void _CopyAsset()
    {
        var avatar = Utilities.GetPlayerFromID(UiUtils.SelectedUserMenuQm.GetComponent<VRC.UI.Elements.Menus.SelectedUserMenuQM>().field_Private_IUser_0.prop_String_0).prop_ApiAvatar_0;
        if (!Directory.Exists(_toPath.Value)) Directory.CreateDirectory(_toPath.Value);
        try
        {
            ToCopyAsset(avatar);
            DelegateMethods.PopupV2(
                "Copy Asset",
                $"Successfully copied avatar \"{avatar.name}\" to folder \"{_toPath.Value}\"!",
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
            Main.Logger.Error(e);
        }
    }

    private static string ByteArrayToString(IReadOnlyCollection<byte> ba)
    {
        StringBuilder hex = new(ba.Count * 2);
        foreach (var b in ba) hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }

    private static string ComputeVersionString(string assetUrl)
    {
        string result = BitConverter.GetBytes(
                int.Parse(new Regex("(?:\\/file_[0-9A-Za-z-]+\\/)([0-9]+)", RegexOptions.Compiled).Match(assetUrl).Groups[1].Value))
            .Aggregate("", (current, b) => current + b.ToString("X2"));
        return string.Concat(Enumerable.Repeat("0", (32 - result.Length))) + result;
    }

    private void ToCopyAsset(ApiAvatar avatar) =>
        File.Copy(new DirectoryInfo(Path.Combine(
                AssetBundleDownloadManager.prop_AssetBundleDownloadManager_0.field_Private_Cache_0.path,
                ByteArrayToString(SHA256.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(avatar.assetUrl.Substring(avatar.assetUrl.IndexOf("file_", StringComparison.Ordinal), 41))))
                    .ToUpper().Substring(0, 16),
                ComputeVersionString(avatar.assetUrl)))
            .GetFiles("*.*", SearchOption.AllDirectories)
            .First(file => file.Name.Contains("__data")).FullName, Path.Combine(_toPath.Value, $"{avatar.id}.vrca"), true);
}
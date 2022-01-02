using PMod.Utils;
using System.Linq;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using TMPro;
using VRC;
using Object = UnityEngine.Object;

namespace PMod.Modules
{
    internal class FrozenPlayersManager : ModuleBase
    {
        internal readonly Dictionary<string, Timer> EntryDict = new();

        internal FrozenPlayersManager()
        {
            useOnPlayerJoined = true;
            useOnPlayerLeft = true;
            RegisterSubscriptions();
        }

        protected override void OnPlayerJoined(Player player)
        {
            var id = player.prop_APIUser_0.id;
            if (id == Player.prop_Player_0.prop_APIUser_0.id) return;
            Timer timer = new();
            EntryDict.Add(id, timer);
            var text = player.transform.Find("Player Nameplate/Canvas/Nameplate/Contents/Main/Text Container/Sub Text").gameObject;
            timer.text = Object.Instantiate(text, text.transform.parent);
            Object.DestroyImmediate(timer.text.transform.Find("Icon").gameObject);
            var tm = timer.text.GetComponentInChildren<TextMeshProUGUI>();
            tm.text = "Frozen";
            tm.color = Color.cyan;
            if (MelonHandler.Mods.Any(m => m.Info.Name.Contains("Mint")))
                timer.text.transform.gameObject.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 30);
            timer.text.SetActive(false);
        }

        protected override void OnPlayerLeft(Player player) => EntryDict.Remove(player.prop_APIUser_0.id);
    }
}
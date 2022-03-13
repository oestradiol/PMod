using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using MelonLoader;
using UnityEngine;
using TMPro;
using VRC;
using Object = UnityEngine.Object;

namespace PMod.Modules;

internal class FrozenPlayersManager : ModuleBase
{
    internal class Timer
    {
        private bool _isFrozen;
        private Stopwatch _timer;
        internal GameObject text;

        internal Timer()
        {
            _isFrozen = true;
            RestartTimer();
        }

        private void NametagSet() { if (text != null) text.SetActive(_isFrozen); }

        internal void RestartTimer()
        {
            _timer = Stopwatch.StartNew();
            if (_isFrozen) MelonCoroutines.Start(Checker());
        }

        private System.Collections.IEnumerator Checker()
        {
            _isFrozen = false;
            NametagSet();

            while (_timer.ElapsedMilliseconds <= 1000)
                yield return null;

            _isFrozen = true;
            NametagSet();
        }
    }
        
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
        if (id == Utils.Utilities.GetLocalAPIUser().id) return;
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
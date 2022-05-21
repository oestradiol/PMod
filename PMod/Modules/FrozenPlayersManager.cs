using PMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MelonLoader;
using UnityEngine;
using ExitGames.Client.Photon;
using TMPro;
using VRC;
using Object = UnityEngine.Object;

namespace PMod.Modules;

internal class FrozenPlayersManager : ModuleBase
{
    private readonly Dictionary<string, Timer> _entryDict = new();

    public FrozenPlayersManager() : base(true)
    {
        useOnPlayerJoined = true;
        useOnPlayerLeft = true;
        RegisterSubscriptions();
    }

    protected override void OnPlayerJoined(Player player)
    {
        var id = player.prop_APIUser_0.id;
        
        if (id == Utilities.GetLocalAPIUser().id) return;
        
        Timer timer = new();
        _entryDict.Add(id, timer);
        
        var text = player.transform.Find("Player Nameplate/Canvas/Nameplate/Contents/Main/Text Container/Sub Text").gameObject;
        timer.Text = Object.Instantiate(text, text.transform.parent);
        Object.DestroyImmediate(timer.Text.transform.Find("Icon").gameObject);
        var tm = timer.Text.GetComponentInChildren<TextMeshProUGUI>();
        tm.text = "Frozen";
        tm.color = Color.cyan;
        timer.Text.SetActive(false);
    }

    protected override void OnPlayerLeft(Player player) => _entryDict.Remove(player.prop_APIUser_0.id);

    public void OnEvent7(EventData eventData)
    {
        try
        {
            if (!IsOn.Value) return;
            
            var key = Utilities.GetPlayerFromPhotonID(eventData.Sender)?.field_Private_APIUser_0?.id;
            if (key != null && _entryDict.TryGetValue(key, out var entry))
                entry.RestartTimer();
        }
        catch (Exception e)
        {
            Main.Logger.Warning("Something went wrong in OnEvent7 Detour (FrozenPlayersManager)");
            Main.Logger.Error(e);
        }
    }

    private class Timer
    {
        private bool _isFrozen;
        private Stopwatch _timer;
        internal GameObject Text;

        internal Timer()
        {
            _isFrozen = true;
            RestartTimer();
        }

        private void NametagSet() { if (Text != null) Text.SetActive(_isFrozen); }

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
}
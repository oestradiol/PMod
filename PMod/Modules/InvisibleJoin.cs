// Please don't use this it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep

using System;
using PMod.Utils;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PMod.Modules
{
    internal class InvisibleJoin : ModuleBase
    {
        internal bool onceOnly = true;
        internal readonly MelonPreferences_Entry<bool> IsOn;
        private Transform _joinButton;

        internal InvisibleJoin()
        {
            MelonPreferences.CreateCategory("InvisibleJoin", "PM - Invisible Join");
            IsOn = MelonPreferences.CreateEntry("InvisibleJoin", "IsOn", false, "Activate Mod? This is a risky function.");
            useOnUiManagerInit = true;
            useOnPreferencesSaved = true;
            RegisterSubscriptions();
        }

        protected override void OnUiManagerInit()
        {
            if (!IsOn.Value) return;
            InstantiateJoinButton();
        }

        protected override void OnPreferencesSaved()
        {
            var flag = _joinButton == null;
            if (!IsOn.Value && !flag)
                Object.DestroyImmediate(_joinButton.gameObject);
            else if (flag)
                InstantiateJoinButton();
        }

        private void InstantiateJoinButton()
        {
            var progressPanel = GameObject.Find("UserInterface/MenuContent/Popups/LoadingPopup/ProgressPanel").transform;
            var parentLoadingProgress = progressPanel.Find("Parent_Loading_Progress");
            var goButton = parentLoadingProgress.Find("GoButton");
            _joinButton = Object.Instantiate(goButton, progressPanel);
            parentLoadingProgress.localPosition = new Vector3(0, 17, 0);
            _joinButton.localPosition = new Vector3(-2.4f, -124f, 0);
            _joinButton.GetComponentInChildren<Text>().text = "Join Invisible";
            var join = _joinButton.GetComponent<Button>();
            join.onClick = new Button.ButtonClickedEvent();
            join.GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                NativePatches.triggerInvisible = true;
                goButton.GetComponent<Button>().onClick.Invoke();
            }));
            join.interactable = true;
        }

        internal void SetJoinMode(bool alwaysInvisible)
        {
            onceOnly = !alwaysInvisible;
            NativePatches.triggerInvisible = alwaysInvisible;
            _joinButton.gameObject.SetActive(!alwaysInvisible);
        }
    }
}
// Please don't use this it's dangerous af lol u r gonna get banned XD // Also, why would u even use this? creep

using PMod.Utils;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace PMod.Modules
{
    internal class InvisibleJoin : ModuleBase
    {
        internal bool onceOnly = true;
        internal MelonPreferences_Entry<bool> IsOn;
        private Transform JoinButton;

        internal InvisibleJoin()
        {
            MelonPreferences.CreateCategory("InvisibleJoin", "PM - Invisible Join");
            IsOn = MelonPreferences.CreateEntry("InvisibleJoin", "IsOn", false, "Activate Mod? This is a risky function.");
            useOnUiManagerInit = true;
            useOnPreferencesSaved = true;
            RegisterSubscriptions();
        }

        internal override void OnUiManagerInit()
        {
            if (!IsOn.Value) return;
            InstantiateJoinButton();
        }

        internal override void OnPreferencesSaved()
        {
            if (IsOn.Value)
                if (JoinButton == null)
                    InstantiateJoinButton();
            else
                Object.DestroyImmediate(JoinButton.gameObject);
        }

        private void InstantiateJoinButton()
        {
            Transform ProgressPanel = GameObject.Find("UserInterface/MenuContent/Popups/LoadingPopup/ProgressPanel").transform;
            Transform Parent_Loading_Progress = ProgressPanel.Find("Parent_Loading_Progress");
            Transform GoButton = Parent_Loading_Progress.Find("GoButton");
            JoinButton = Object.Instantiate(GoButton, ProgressPanel);
            Parent_Loading_Progress.localPosition = new Vector3(0, 17, 0);
            JoinButton.localPosition = new Vector3(-2.4f, -124f, 0);
            JoinButton.GetComponentInChildren<Text>().text = "Join Invisible";
            Button join = JoinButton.GetComponent<Button>();
            join.onClick = new Button.ButtonClickedEvent();
            join.GetComponent<Button>().onClick.AddListener((UnityAction)(() =>
            {
                NativePatches.triggerInvisible = true;
                GoButton.GetComponent<Button>().onClick.Invoke();
            }));
            join.interactable = true;
        }

        internal void SetJoinMode(bool alwaysInvisible)
        {
            onceOnly = !alwaysInvisible;
            NativePatches.triggerInvisible = alwaysInvisible;
            JoinButton.gameObject.SetActive(!alwaysInvisible);
        }
    }
}
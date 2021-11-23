//using PMod.Utils;
//using System;
//using MelonLoader;
//using UnityEngine;
//using Object = UnityEngine.Object;
//using ApiAvatar = VRC.Core.ApiAvatar;
//using APIUser = VRC.Core.APIUser;
//using PMod.Loader;

//namespace PMod.Modules
//{
//    internal class ForceClone : ModuleBase
//    {
//        private QuickMenu QM;
//        private ApiAvatar toClone;
//        private QMSingleButton cameraClone;
//        private GameObject platformAny;
//        private GameObject platformPC;
//        private GameObject platformOculus;
//        private GameObject platformIconAny;
//        private GameObject platformIconPC;
//        private GameObject platformIconOculus;
//        private Transform cloneButton;
//        private bool isFar = true, ranOnce;
//        private MelonPreferences_Entry<bool> IsOn;
//        private Transform UserInteract() => Resources.FindObjectsOfTypeAll<VRC.UI.Elements.Menus.SelectedUserMenuQM>()[0].transform;
//        private Vector3 GetOriginalPos() => UserInteract().Find("ShowAuthorButton").position + 
//            UserInteract().Find("ViewAvatarThreeToggle/Button_UseSafetySettings").position - UserInteract().Find("MuteButton").position;

//        internal ForceClone() // This is for PM not conflicting with RubyClient or ReMod
//        {
//            MelonPreferences.CreateCategory("ForceClone", "PM - Force Clone");
//            IsOn = MelonPreferences.CreateEntry("ForceClone", "IsOn", true, "Activate Mod? (Disabling requires restart)");
//        }

//        internal override void OnUiManagerInit()
//        {
//            if (!IsOn.Value) return;
//            CreateButtons();
//            Main.listener.OnEnabled += delegate 
//            {
//                APIUser QMUser = QM.field_Private_APIUser_0;
//                toClone = Utilities.GetPlayerFromID(QMUser.id).prop_ApiAvatar_0;
//                if (toClone.releaseStatus == "public" && !QMUser.allowAvatarCopying && toClone.id != Utilities.GetLocalVRCPlayer().prop_ApiAvatar_0.id)
//                {
//                    if (!isFar)
//                    {
//                        cloneButton.position += new Vector3(100, 100, 100);
//                        cameraClone.setActive(true);
//                        isFar = true;
//                    }
//                }
//                else if (isFar)
//                {
//                    cloneButton.position = GetOriginalPos();
//                    cameraClone.setActive(false);
//                    isFar = false;
//                }
//            };
//            Main.listener.OnDisabled += delegate 
//            {
//                if (isFar)
//                {
//                    cloneButton.position = GetOriginalPos();
//                    cameraClone.setActive(false);
//                    isFar = false;
//                }
//            };

//            ranOnce = true;
//        }

//        internal override void OnUpdate()
//        {
//            if (!IsOn.Value) return;
//            if (platformAny != null)
//            {
//                if (platformAny.active != platformIconAny.active) platformAny.SetActive(platformIconAny.active);
//                if (platformPC.active != platformIconPC.active) platformPC.SetActive(platformIconPC.active);
//                if (platformOculus.active != platformIconOculus.active) platformOculus.SetActive(platformIconOculus.active);
//            }
//        }

//        internal override void OnPreferencesSaved()
//        {
//            if (IsOn.Value && !ranOnce) OnUiManagerInit();
//        }

//        private void CreateButtons()
//        {
//            QM = QuickMenu.prop_QuickMenu_0;
//            cloneButton = GameObject.Find("UserInterface/QuickMenu/UserInteractMenu/CloneAvatarButton").transform;
//            cameraClone = new QMSingleButton("UserInteractMenu", 5, 0, "Clone\nPublic\nAvatar", () => {
//                Utilities.ChangeToAVByID(toClone.id);
//                PLogger.Msg(ConsoleColor.Red, "Avatar cloned: " + toClone.id);
//            }, "Force Clone Avatar.", null, new Color(1, .8f, 1));

//            Transform platform = Object.Instantiate(cloneButton.Find("PlatformIcon"), cameraClone.getGameObject().transform);
//            Transform platformIcon = cloneButton.Find("PlatformIcon");

//            platformAny = platform.Find("AnyIcon").gameObject;
//            platformPC = platform.Find("PCIcon").gameObject;
//            platformOculus = platform.Find("QuestIcon").gameObject;
//            platformIconAny = platformIcon.Find("AnyIcon").gameObject;
//            platformIconPC = platformIcon.Find("PCIcon").gameObject;
//            platformIconOculus = platformIcon.Find("QuestIcon").gameObject;
//            platform.position = cloneButton.Find("PlatformIcon").position;
//        }
//    }
//}
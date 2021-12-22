using System;
using UnityEngine;
using MelonLoader;

namespace PMod.Modules
{
    internal class TeleportToCursor : ModuleBase
    {
        private MelonPreferences_Entry<bool> IsOn;

        internal TeleportToCursor()
        {
            MelonPreferences.CreateCategory("TeleportToCursor", "PM - Teleport To Cursor");
            IsOn = MelonPreferences.CreateEntry("TeleportToCursor", "IsOn", false, "Activate Mod? This is a risky function.");
            useOnUpdate = true;
            RegisterSubscriptions();
        }

        // Check for button trigger and teleports
        internal override void OnUpdate()
        {
            if (!IsOn.Value) return;
            if (Input.GetKeyDown(KeyCode.Mouse4) &&
                    VRCPlayer.field_Internal_Static_VRCPlayer_0 != null &&
                    Physics.Raycast(CameraComponent.ScreenPointToRay(Input.mousePosition), out var hitInfo))
                VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position = hitInfo.point;
        }

        // Gets the center of the eye (camera)
        private static Camera cameraComponent;
        private static Camera CameraComponent =>
                cameraComponent ??= Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0]
                    .transform.Find(Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : "Camera (head)/Camera (eye)").GetComponent<Camera>();
    }
}
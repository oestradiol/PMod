using PMod.Utils;
using System;
using UnityEngine;
using MelonLoader;

namespace PMod.Modules
{
    internal class TeleportToCursor : ModuleBase
    {
        private readonly MelonPreferences_Entry<bool> _isOn;

        internal TeleportToCursor()
        {
            MelonPreferences.CreateCategory("TeleportToCursor", "PM - Teleport To Cursor");
            _isOn = MelonPreferences.CreateEntry("TeleportToCursor", "IsOn", false, "Activate Mod? This is a risky function.");
            useOnUpdate = true;
            RegisterSubscriptions();
        }

        // Check for button trigger and teleports
        protected override void OnUpdate()
        {
            if (!_isOn.Value) return;
            if (Input.GetKeyDown(KeyCode.Mouse4) &&
                    Utilities.GetLocalVRCPlayer() != null &&
                    Physics.Raycast(CameraComponent.ScreenPointToRay(Input.mousePosition), out var hitInfo))
                Utilities.GetLocalVRCPlayer().transform.position = hitInfo.point;
        }

        // Gets the center of the eye (camera)
        private static Camera _cameraComponent;
        private static Camera CameraComponent =>
                _cameraComponent ??= Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0]
                    .transform.Find(Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : "Camera (head)/Camera (eye)").GetComponent<Camera>();
    }
}
using PMod.Utils;
using System;
using UnityEngine;

namespace PMod.Modules;

internal class TeleportToCursor : ModuleBase
{
    public TeleportToCursor() : base(false)
    {
        useOnUpdate = true;
        RegisterSubscriptions();
    }

    // Check for button trigger and teleports
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Mouse4) &&
            Utilities.GetLocalVRCPlayer() != null &&
            Physics.Raycast(CameraComponent.ScreenPointToRay(Input.mousePosition), out var hitInfo))
            Utilities.GetLocalVRCPlayer().transform.position = hitInfo.point;
    }

    // Gets the center of the eye (camera)
    private static Camera _cameraComponent;
    private static Camera CameraComponent =>
        _cameraComponent ??= Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0].transform.Find(
            Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : 
                (UnityEngine.XR.XRDevice.isPresent ? "" : "Camera (head)/") + "Camera (eye)").GetComponent<Camera>();
            
}
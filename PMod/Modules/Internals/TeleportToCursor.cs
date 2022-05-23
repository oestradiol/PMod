using System;
using PMod.Utils;
using UnityEngine;

namespace PMod.Modules.Internals;

internal class TeleportToCursor : VrcMod
{
    // Check for button trigger and teleports
    public override void OnUpdate()
    {
        var player = Utilities.GetLocalVrcPlayer();
        if (player != null && Input.GetKeyDown(KeyCode.Mouse4) &&
            Physics.Raycast(CameraComponent.ScreenPointToRay(Input.mousePosition), out var hitInfo))
            player.transform.position = hitInfo.point;
    }

    // Gets the center of the eye (camera)
    private static Camera _cameraComponent;
    private static Camera CameraComponent =>
        _cameraComponent ??= Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0].transform.Find(
            Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" :
                (UnityEngine.XR.XRDevice.isPresent ? "" : "Camera (head)/") + "Camera (eye)").GetComponent<Camera>();
}
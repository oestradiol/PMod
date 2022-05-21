using System;
using UnityEngine;
using VRC.SDKBase;

namespace PMod.Modules.Orbit;

internal class OrbitItem
{
    private static Orbit _orbit;
    private static Orbit Orbit => _orbit ??= ModulesManager.GetModule<Orbit>();
    
    private readonly Vector3 _initialPos;
    private readonly Quaternion _initialRot;
    private readonly double _index;
    internal readonly bool InitialTheft;
    internal readonly bool InitialPickupable;
    internal readonly bool InitialKinematic;
    internal readonly Vector3 InitialVelocity;
    internal readonly bool InitialActive;
    internal bool IsOn { get; set; } = true;

    internal OrbitItem(System.Collections.Generic.IList<VRC_Pickup> pickups, int i)
    {
        var pickup = pickups[i];
        var rigidBody = pickup.GetComponent<Rigidbody>();
        InitialTheft = pickup.DisallowTheft;
        InitialPickupable = pickup.pickupable;
        InitialKinematic = rigidBody.isKinematic;
        InitialVelocity = rigidBody.velocity;
        InitialActive = pickup.gameObject.active;
        var transform = pickup.transform;
        _initialPos = transform.position;
        _initialRot = transform.rotation;
        _index = (double)i / pickups.Count;
    }

    private Vector3 CircularRot()
    {
        var angle = Orbit.Timer * Orbit.speed.Value + 2 * Math.PI * _index;
        return Orbit.OrbitCenter + Orbit.rotationy * (Orbit.rotation * new Vector3((float)Math.Cos(angle) * Orbit.radius.Value, 0,
            (float)Math.Sin(angle) * Orbit.radius.Value));
    }

    private Vector3 CylindricalRot()
    {
        var angle = Orbit.Timer * Orbit.speed.Value + 2 * Math.PI * _index;
        return Orbit.OrbitCenter + new Vector3(0, (float)(Orbit.PlayerHeight * _index), 0) + Orbit.rotationy *
            (Orbit.rotation * new Vector3((float)Math.Cos(angle) * Orbit.radius.Value, 0, (float)Math.Sin(angle) * Orbit.radius.Value));
    }

    private Vector3 SphericalRot()
    {
        var angle = (Orbit.Timer * Orbit.speed.Value) / (4 * Math.PI) + _index * 360;
        var height = Orbit.PlayerHeight * ((Orbit.Timer * Orbit.speed.Value / 2 + _index) % 1);
        var rotation = Quaternion.Euler(0, (float)angle, 0);
        return Orbit.OrbitCenter + Orbit.rotationy * (Orbit.rotation *
        (rotation * new Vector3((float)(4 * Math.Sqrt(height * Orbit.PlayerHeight - Math.Pow(height, 2)) * Orbit.radius.Value), (float)height, 0)));
    }

    internal Vector3 CurrentPos() => IsOn
        ? Orbit.rotType switch
        {
            Orbit.RotType.CircularRot => CircularRot(),
            Orbit.RotType.CylindricalRot => CylindricalRot(),
            _ => SphericalRot()
        } : _initialPos;

    internal Quaternion CurrentRot()
    {
        var angle = (float)(Orbit.Timer * 50f * Orbit.speed.Value + 2 * Math.PI * _index);
        return IsOn ? Quaternion.Euler(-angle, 0, -angle) : _initialRot;
    }
}
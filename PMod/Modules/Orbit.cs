using PMod.Utils;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using UIExpansionKit.API;
using Object = UnityEngine.Object;
using Utilities = PMod.Utils.Utilities;

namespace PMod.Modules
{
    internal class Orbit : ModuleBase
    {
        private ICustomShowableLayoutedMenu _selectionMenu;
        private VRCPlayer _currentPlayer;
        private readonly MelonPreferences_Entry<bool> _patch;
        private Dictionary<VRC_Pickup, OrbitItem> _pickupOrbits;

        internal readonly ICustomShowableLayoutedMenu OrbitMenu;
        internal Quaternion rotation;
        internal Quaternion rotationy;
        internal Vector3 OrbitCenter;
        internal float PlayerHeight;
        internal float Timer;
        internal readonly MelonPreferences_Entry<float> radius;
        internal readonly MelonPreferences_Entry<float> speed;
        private readonly MelonPreferences_Entry<float> _rotx;
        private readonly MelonPreferences_Entry<float> _roty;
        private readonly MelonPreferences_Entry<float> _rotz;
        internal readonly MelonPreferences_Entry<bool> IsOn;
        internal RotType rotType;
        internal enum RotType
        {
            CircularRot,
            CylindricalRot,
            SphericalRot,
        }

        internal Orbit()
        {
            MelonPreferences.CreateCategory("Orbit", "PM - Orbit");
            IsOn = MelonPreferences.CreateEntry("Orbit", "IsOn", false, "Activate Mod? This is a risky function.");
            radius = MelonPreferences.CreateEntry("Orbit", "Radius", 1.0f, "Radius");
            speed = MelonPreferences.CreateEntry("Orbit", "Speed", 1.0f, "Speed");
            _patch = MelonPreferences.CreateEntry("Orbit", "Patch", true, "Patch items on Orbit");
            _rotx = MelonPreferences.CreateEntry("Orbit", "RotationX", 0.0f, "X Rotation");
            _roty = MelonPreferences.CreateEntry("Orbit", "RotationY", 0.0f, "Y Rotation");
            _rotz = MelonPreferences.CreateEntry("Orbit", "RotationZ", 0.0f, "Z Rotation");
            OrbitMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            OrbitMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
            OrbitMenu.AddSimpleButton("Stop Orbit", StopOrbit);
            OrbitMenu.AddSimpleButton("Circular Orbit", () => SelectOrbit(RotType.CircularRot));
            OrbitMenu.AddSimpleButton("Spherical Orbit", () => SelectOrbit(RotType.SphericalRot));
            OrbitMenu.AddSimpleButton("Cylindrical Orbit", () => SelectOrbit(RotType.CylindricalRot));
            OnPreferencesSaved();
            useOnPreferencesSaved = true;
            useOnUiManagerInit = true;
            useOnUpdate = true;
            useOnInstanceChanged = true;
            useOnPlayerLeft = true;
            RegisterSubscriptions();
        } 

        protected sealed override void OnPreferencesSaved()
        {
            rotation = Quaternion.Euler(_rotx.Value, 0, _rotz.Value);
            rotationy = Quaternion.Euler(0, _roty.Value, 0);
        }

        protected override void OnUpdate()
        {
            if (_pickupOrbits == null || _currentPlayer == null) return;
            Timer += Time.deltaTime;
            OrbitCenter = GetCenter();
            foreach (var pickupOrbit in _pickupOrbits)
            {
                var key = pickupOrbit.Key;
                var value = pickupOrbit.Value;
                if (key == null)
                    _pickupOrbits.Remove(key);
                else
                {
                    if (_patch.Value) Patch(key);
                    key.transform.position = value.CurrentPos();
                    key.transform.rotation = value.CurrentRot();
                }
            }
        }

        protected override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => StopOrbit();

        protected override void OnPlayerLeft(Player player) { if (_currentPlayer != null && _currentPlayer._player.prop_APIUser_0.id == player.prop_APIUser_0.id) StopOrbit(); }

        private void SelectOrbit(RotType type)
        {
            rotType = type;
            _selectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            _selectionMenu.AddSimpleButton("Go back", () => OrbitMenu.Show());
            foreach (var player in Object.FindObjectsOfType<VRCPlayer>()) 
                _selectionMenu.AddSimpleButton($"{player.prop_Player_0.prop_APIUser_0.displayName}", () => ToOrbit(player));
            _selectionMenu.Show();
        }

        private void ToOrbit(VRCPlayer player)
        {
            StopOrbit();
            _currentPlayer = player;
            Timer = 0f;
            var pickups = Object.FindObjectsOfType<VRC_Pickup>();
            OrbitCenter = GetCenter();
            _pickupOrbits = new Dictionary<VRC_Pickup, OrbitItem>();
            for (var i = 0; i < pickups.Count; i++)
                _pickupOrbits.Add(pickups[i], new OrbitItem(pickups, i));
        }

        private void StopOrbit()
        {
            if (_pickupOrbits == null) return;
            foreach (var keyValuePair in _pickupOrbits)
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;
                value.IsOn = false;
                if (key == null) continue;
                key.transform.position = value.CurrentPos();
                key.transform.rotation = value.CurrentRot();
                Unpatch(key, value);
            }
            _pickupOrbits = null;
            _currentPlayer = null;
            Timer = 0;
        }

        private static void Patch(VRC_Pickup item)
        {
            item.DisallowTheft = true;
            item.pickupable = false;
            item.GetComponent<Rigidbody>().isKinematic = true;
            item.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
            item.gameObject.SetActive(true);
            if (Networking.GetOwner(item.gameObject).playerId == Utilities.GetLocalVRCPlayerApi().playerId) return;
            item.currentlyHeldBy = null; 
            Networking.SetOwner(Utilities.GetLocalVRCPlayerApi(), item.gameObject);
        }

        private static void Unpatch(VRC_Pickup key, OrbitItem value)
        {
            key.DisallowTheft = value.InitialTheft;
            key.pickupable = value.InitialPickupable;
            key.GetComponent<Rigidbody>().isKinematic = value.InitialKinematic;
            key.GetComponent<Rigidbody>().velocity = value.InitialVelocity;
            key.gameObject.SetActive(value.InitialActive);
        }

        private Vector3 GetCenter()
        {
            var head = Utilities.GetBoneTransform(_currentPlayer.prop_Player_0, HumanBodyBones.Head).position;
            if (rotType == RotType.CircularRot) return head;
            else
            {
                var pos = _currentPlayer.transform.position;
                PlayerHeight = (head - pos).y;
                return pos;
            }
        }
    }
}

// //On Development:
// using System;
// using MelonLoader;
// using UnityEngine;
// using VRC;
// using VRC.Core;
// using VRC.SDKBase;
// using UIExpansionKit.API;
// using Object = UnityEngine.Object;
// using Utilities = PMod.Utils.Utilities;
//
// namespace PMod.Modules
// {
//     internal class Orbit : ModuleBase
//     {
//         internal class OrbitItem : MonoBehaviour
//         {
//             internal bool Patch;
//             internal double Index;
//             
//             private VRC_Pickup _thisPickup;
//             private Rigidbody _thisRigidBody;
//             private Transform _thisTransform;
//             private Vector3 _initialPos;
//             private Quaternion _initialRot;
//             private bool _initialTheft;
//             private bool _initialPickupable;
//             private bool _initialKinematic;
//
//             public OrbitItem(IntPtr obj0) : base(obj0) { }
//             
//             private void Awake()
//             {
//                 _thisPickup = GetComponent<VRC_Pickup>();
//                 _thisRigidBody = GetComponent<Rigidbody>();
//                 _thisTransform = transform;
//                 
//                 // Saving initial state
//                 _initialPos = _thisTransform.position;
//                 _initialRot = _thisTransform.rotation;
//                 _initialTheft = _thisPickup.DisallowTheft;
//                 _initialPickupable = _thisPickup.pickupable;
//                 _initialKinematic = _thisRigidBody.isKinematic;
//             }
//
//             private void Update()
//             {
//                 // Updating position and rotation + internal variables and control
//                 if (Patch) DoPatch();
//                 _thisTransform.position = CurrentPos();
//                 _thisTransform.rotation = CurrentRot();
//             }
//
//             private void OnDestroy()
//             {
//                 // Restoring initial state
//                 _thisTransform.position = _initialPos;
//                 _thisTransform.rotation = _initialRot;
//                 _thisPickup.DisallowTheft = _initialTheft;
//                 _thisPickup.pickupable = _initialPickupable;
//                 _thisRigidBody.isKinematic = _initialKinematic;
//             }
//             
//             private void DoPatch()
//             {
//                 // Updating internal variables
//                 _thisPickup.DisallowTheft = true;
//                 _thisPickup.pickupable = false;
//                 _thisRigidBody.isKinematic = true;
//                 gameObject.SetActive(true);
//                 
//                 // Forcing control of object
//                 if (Networking.GetOwner(gameObject).playerId == Utilities.GetLocalVRCPlayerApi().playerId) return;
//                 Networking.SetOwner(Utilities.GetLocalVRCPlayerApi(), gameObject);
//                 _thisPickup.currentlyHeldBy = null;
//             }
//
//             [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
//             private Vector3 CurrentPos() =>  ModulesManager.orbit._rotType switch
//             {
//                 RotType.CircularRot => CircularPos(),
//                 RotType.CylindricalRot => CylindricalPos(),
//                 _ => SphericalPos()
//             };
//
//             [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
//             private Quaternion CurrentRot()
//             {
//                 var angle = (float)(ModulesManager.orbit._timer * 50f * ModulesManager.orbit._speed.Value + 2 * Math.PI * Index);
//                 return Quaternion.Euler(-angle, 0, -angle);
//             }
//
//             [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
//             private Vector3 CircularPos()
//             {
//                 var angle = ModulesManager.orbit._timer * ModulesManager.orbit._speed.Value + 2 * Math.PI * Index;
//                 return ModulesManager.orbit._orbitCenter + ModulesManager.orbit._rotationy * (ModulesManager.orbit._rotation * new Vector3((float)Math.Cos(angle) * ModulesManager.orbit._radius.Value, 0,
//                     (float)Math.Sin(angle) * ModulesManager.orbit._radius.Value));
//             }
//
//             [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
//             private Vector3 CylindricalPos()
//             {
//                 var angle = ModulesManager.orbit._timer * ModulesManager.orbit._speed.Value + 2 * Math.PI * Index;
//                 return ModulesManager.orbit._orbitCenter + new Vector3(0, (float)(ModulesManager.orbit._playerHeight * Index), 0) + ModulesManager.orbit._rotationy *
//                     (ModulesManager.orbit._rotation * new Vector3((float)Math.Cos(angle) * ModulesManager.orbit._radius.Value, 0, (float)Math.Sin(angle) * ModulesManager.orbit._radius.Value));
//             }
//
//             [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
//             private Vector3 SphericalPos()
//             {
//                 var angle = ModulesManager.orbit._timer * ModulesManager.orbit._speed.Value / (4 * Math.PI) + Index * 360;
//                 var height = ModulesManager.orbit._playerHeight * ((ModulesManager.orbit._timer * ModulesManager.orbit._speed.Value / 2 + Index) % 1);
//                 var rotation = Quaternion.Euler(0, (float)angle, 0);
//                 return ModulesManager.orbit._orbitCenter + ModulesManager.orbit._rotationy * (ModulesManager.orbit._rotation * 
//                     (rotation * new Vector3((float)(4 * Math.Sqrt(height * ModulesManager.orbit._playerHeight - Math.Pow(height, 2)) * ModulesManager.orbit._radius.Value), (float)height, 0)));
//             }
//         }
//         
//         #region  VariableInit
//         internal readonly ICustomShowableLayoutedMenu OrbitMenu;
//         internal readonly MelonPreferences_Entry<bool> IsOn;
//         
//         private Player _currentPlayer;
//         private ICustomShowableLayoutedMenu _selectionMenu;
//         private readonly MelonPreferences_Entry<bool> _patch;
//         private readonly MelonPreferences_Entry<float> _rotx;
//         private readonly MelonPreferences_Entry<float> _roty;
//         private readonly MelonPreferences_Entry<float> _rotz;
//         private readonly MelonPreferences_Entry<float> _speed;
//         private readonly MelonPreferences_Entry<float> _radius;
//         
//         private Quaternion _rotation;
//         private Quaternion _rotationy;
//         private Vector3 _orbitCenter;
//         private float _playerHeight;
//         private float _timer;
//         private RotType _rotType;
//         private enum RotType
//         {
//             CircularRot,
//             CylindricalRot,
//             SphericalRot
//         }
//         #endregion
//
//         internal Orbit()
//         {
//             UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<OrbitItem>();
//             MelonPreferences.CreateCategory("Orbit", "PM - Orbit");
//             IsOn = MelonPreferences.CreateEntry("Orbit", "IsOn", false, "Activate Mod? This is a risky function.");
//             _radius = MelonPreferences.CreateEntry("Orbit", "Radius", 1.0f, "Radius");
//             _speed = MelonPreferences.CreateEntry("Orbit", "Speed", 1.0f, "Speed");
//             _patch = MelonPreferences.CreateEntry("Orbit", "Patch", true, "Patch items on Orbit");
//             _rotx = MelonPreferences.CreateEntry("Orbit", "RotationX", 0.0f, "X Rotation");
//             _roty = MelonPreferences.CreateEntry("Orbit", "RotationY", 0.0f, "Y Rotation");
//             _rotz = MelonPreferences.CreateEntry("Orbit", "RotationZ", 0.0f, "Z Rotation");
//             OrbitMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
//             OrbitMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
//             OrbitMenu.AddSimpleButton("Stop Orbit", StopOrbit);
//             OrbitMenu.AddSimpleButton("Circular Orbit", () => SelectOrbit(RotType.CircularRot));
//             OrbitMenu.AddSimpleButton("Spherical Orbit", () => SelectOrbit(RotType.SphericalRot));
//             OrbitMenu.AddSimpleButton("Cylindrical Orbit", () => SelectOrbit(RotType.CylindricalRot));
//             OnPreferencesSaved();
//             useOnPreferencesSaved = true;
//             useOnUpdate = true;
//             useOnInstanceChanged = true;
//             useOnPlayerLeft = true;
//             RegisterSubscriptions();
//         } 
//
//         protected sealed override void OnPreferencesSaved()
//         {
//             _rotation = Quaternion.Euler(_rotx.Value, 0, _rotz.Value);
//             _rotationy = Quaternion.Euler(0, _roty.Value, 0);
//         }
//         
//         protected override void OnUpdate()
//         {
//             if (_currentPlayer == null) return;
//             _timer += Time.deltaTime;
//             _orbitCenter = GetCenter();
//         }
//
//         protected override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => StopOrbit();
//
//         protected override void OnPlayerLeft(Player player) { if (_currentPlayer != null && _currentPlayer.prop_APIUser_0.id == player.prop_APIUser_0.id) StopOrbit(); }
//
//         private void SelectOrbit(RotType type)
//         {
//             _rotType = type;
//             _selectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
//             _selectionMenu.AddSimpleButton("Go back", () => OrbitMenu.Show());
//             foreach (var player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
//                 _selectionMenu.AddSimpleButton($"{player.prop_APIUser_0.displayName}", () => ToOrbit(player));
//             _selectionMenu.Show();
//         }
//
//         private void ToOrbit(Player player)
//         {
//             StopOrbit();
//             _timer = 0f;
//             _currentPlayer = player;
//             _orbitCenter = GetCenter();
//             var pickups = Object.FindObjectsOfType<VRC_Pickup>();
//             var count = pickups.Count;
//             for (var i = 0; i < count; i++)
//             {
//                 var orbitItem = pickups[i].gameObject.AddComponent<OrbitItem>();
//                 orbitItem.Patch = _patch.Value;
//                 orbitItem.Index = (double) i / count;
//             }
//         }
//
//         private void StopOrbit()
//         {
//             if (_currentPlayer == null) return;
//             foreach (var orbitItem in Resources.FindObjectsOfTypeAll<OrbitItem>())
//                 Object.DestroyImmediate(orbitItem);
//             _currentPlayer = null;
//             _timer = 0;
//         }
//
//         private Vector3 GetCenter()
//         {
//             var head = Utilities.GetBoneTransform(_currentPlayer, HumanBodyBones.Head).position;
//             if (_rotType == RotType.CircularRot) return head;
//             else
//             {
//                 var pos = _currentPlayer.transform.position;
//                 _playerHeight = (head - pos).y;
//                 return pos;
//             }
//         }
//     }
// }
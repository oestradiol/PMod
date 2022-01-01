using PMod.Utils;
using System.Linq;
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

        protected override void OnUiManagerInit() => NetworkEvents.OnInstanceChangedAction += OnInstanceChanged;

        protected override void OnUpdate()
        {
            if (_pickupOrbits != null && _currentPlayer != null)
            {
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
            Timer += Time.deltaTime;
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
            if (_currentPlayer != null) StopOrbit();
            _currentPlayer = player;
            Timer = 0f;
            var pickups = Object.FindObjectsOfType<VRC_Pickup>();
            OrbitCenter = GetCenter();
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
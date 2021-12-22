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
        private ICustomShowableLayoutedMenu SelectionMenu;
        private List<OrbitItem> Orbits;
        private VRCPlayer CurrentPlayer;
        private MelonPreferences_Entry<bool> patch;

        internal ICustomShowableLayoutedMenu OrbitMenu;
        internal List<VRC_Pickup> Pickups;
        internal Quaternion rotation;
        internal Quaternion rotationy;
        internal Vector3 OrbitCenter;
        internal float PlayerHeight;
        internal float Timer = 0f;
        internal MelonPreferences_Entry<float> radius;
        internal MelonPreferences_Entry<float> speed;
        internal MelonPreferences_Entry<float> rotx;
        internal MelonPreferences_Entry<float> roty;
        internal MelonPreferences_Entry<float> rotz;
        internal MelonPreferences_Entry<bool> IsOn;
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
            patch = MelonPreferences.CreateEntry("Orbit", "Patch", true, "Patch items on Orbit");
            rotx = MelonPreferences.CreateEntry("Orbit", "RotationX", 0.0f, "X Rotation");
            roty = MelonPreferences.CreateEntry("Orbit", "RotationY", 0.0f, "Y Rotation");
            rotz = MelonPreferences.CreateEntry("Orbit", "RotationZ", 0.0f, "Z Rotation");
            OrbitMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            OrbitMenu.AddSimpleButton("Go back", () => Main.ClientMenu.Show());
            OrbitMenu.AddSimpleButton("Stop Orbit", () => StopOrbit());
            OrbitMenu.AddSimpleButton("Circular Orbit", () => SelectOrbit("Circular"));
            OrbitMenu.AddSimpleButton("Spherical Orbit", () => SelectOrbit("Spherical"));
            OrbitMenu.AddSimpleButton("Cylindrical Orbit", () => SelectOrbit("Cylindrical"));
            OnPreferencesSaved();
            useOnPreferencesSaved = true;
            useOnUiManagerInit = true;
            useOnUpdate = true;
            useOnInstanceChanged = true;
            useOnPlayerLeft = true;
            RegisterSubscriptions();
        }

        internal override void OnPreferencesSaved()
        {
            rotation = Quaternion.Euler(rotx.Value, 0, rotz.Value);
            rotationy = Quaternion.Euler(0, roty.Value, 0);
        }

        internal override void OnUiManagerInit() => NetworkEvents.OnInstanceChangedAction += OnInstanceChanged;
        
        internal override void OnUpdate()
        {
            if (Pickups != null && Orbits != null && CurrentPlayer != null)
            {
                OrbitCenter = GetCenter();
                for (int i = 0; i < Pickups.Count; i++)
                {
                    if (Pickups[i] == null)
                    {
                        Pickups.RemoveAt(i);
                        Orbits.RemoveAt(i);
                    }
                    else
                    {
                        if (patch.Value) Patch(Pickups[i]);
                        Pickups[i].transform.position = Orbits[i].CurrentPos();
                        Pickups[i].transform.rotation = Orbits[i].CurrentRot();
                    }
                }
            }
            Timer += Time.deltaTime;
        }

        internal override void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => StopOrbit();

        internal override void OnPlayerLeft(Player player) { if (CurrentPlayer == player) StopOrbit(); }

        private void SelectOrbit(string Type)
        {
            SelectionMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
            SelectionMenu.AddSimpleButton("Go back", () => OrbitMenu.Show());
            if (Type == "Circular") rotType = RotType.CircularRot;
            else if (Type == "Cylindrical") rotType = RotType.CylindricalRot;
            else rotType = RotType.SphericalRot;
            foreach (var player in Object.FindObjectsOfType<VRCPlayer>()) 
                SelectionMenu.AddSimpleButton($"{player.prop_Player_0.prop_APIUser_0.displayName}", () => ToOrbit(player));
            SelectionMenu.Show();
        }

        private void ToOrbit(VRCPlayer Player)
        {
            if (CurrentPlayer != null) StopOrbit();
            CurrentPlayer = Player;
            Timer = 0f;
            Pickups = Object.FindObjectsOfType<VRC_Pickup>().ToList();
            OrbitCenter = GetCenter();
            Orbits = new List<OrbitItem>();
            for (int i = 0; i < Pickups.Count; i++)
                Orbits.Add(new OrbitItem(Pickups[i], i));
        }

        private void StopOrbit()
        {
            if (Pickups != null)
            {
                for (int i = 0; i < Pickups.Count; i++)
                {
                    Orbits[i].IsOn = false;
                    if (Pickups[i])
                    {
                        Pickups[i].transform.position = Orbits[i].CurrentPos();
                        Pickups[i].transform.rotation = Orbits[i].CurrentRot();
                        Unpatch(i);
                    }
                }
            }
            Pickups = null;
            Orbits = null;
            CurrentPlayer = null;
        }

        private void Patch(VRC_Pickup Item)
        {
            VRCPlayerApi GetOwner() => Networking.GetOwner(Item.gameObject);
            Item.GetComponent<VRC_Pickup>().DisallowTheft = true;
            Item.GetComponent<VRC_Pickup>().pickupable = false;
            Item.GetComponent<Rigidbody>().isKinematic = true;
            Item.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
            Item.gameObject.SetActive(true);
            if (GetOwner().playerId != Utilities.GetLocalVRCPlayerApi().playerId)
            {
                Item.GetComponent<VRC_Pickup>().currentlyHeldBy = null; 
                Networking.SetOwner(Utilities.GetLocalVRCPlayerApi(), Item.gameObject);
            }
        }

        private void Unpatch(int i)
        {
            Pickups[i].GetComponent<VRC_Pickup>().DisallowTheft = Orbits[i].InitialTheft;
            Pickups[i].GetComponent<VRC_Pickup>().pickupable = Orbits[i].InitialPickupable;
            Pickups[i].GetComponent<Rigidbody>().isKinematic = Orbits[i].InitialKinematic;
            Pickups[i].GetComponent<Rigidbody>().velocity = Orbits[i].InitialVelocity;
            Pickups[i].gameObject.SetActive(Orbits[i].InitialActive);
        }

        private Vector3 GetCenter()
        {
            Vector3 Head = Utilities.GetBoneTransform(CurrentPlayer.prop_Player_0, HumanBodyBones.Head).position;
            if (rotType == RotType.CircularRot) return Head;
            else
            {
                Vector3 Pos = CurrentPlayer.transform.position;
                PlayerHeight = (Head - Pos).y;
                return Pos;
            }
        }
    }
}
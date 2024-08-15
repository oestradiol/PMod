﻿using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using UIExpansionKit.API;
using Client.Functions.Utils;
using Object = UnityEngine.Object;
using Utilities = Client.Functions.Utils.Utilities;

namespace Client.Functions
{
    internal static class Orbit
    {
        private static ICustomShowableLayoutedMenu SelectionMenu;
        private static List<OrbitItem> Orbits;
        private static VRCPlayer CurrentPlayer;
        private static MelonPreferences_Entry<bool> patch;

        public static ICustomShowableLayoutedMenu OrbitMenu;
        public static UnhollowerBaseLib.Il2CppArrayBase<VRC_Pickup> Pickups;
        public static Quaternion rotation;
        public static Quaternion rotationy;
        public static Vector3 OrbitCenter;
        public static float PlayerHeight;
        public static float Timer = 0f;
        public static MelonPreferences_Entry<float> radius;
        public static MelonPreferences_Entry<float> speed;
        public static MelonPreferences_Entry<float> rotx;
        public static MelonPreferences_Entry<float> roty;
        public static MelonPreferences_Entry<float> rotz;
        public static MelonPreferences_Entry<bool> IsOn;
        public static RotType rotType;
        public enum RotType
        {
            CircularRot,
            CylindricalRot,
            SphericalRot,
        }

        public static void OnApplicationStart()
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
        }

        public static void OnPreferencesSaved()
        {
            rotation = Quaternion.Euler(rotx.Value, 0, rotz.Value);
            rotationy = Quaternion.Euler(0, roty.Value, 0);
        }

        public static void OnUiManagerInit()
        {
            NetworkEvents.OnLeave += OnLeave;
            NetworkEvents.OnInstanceChange += OnInstanceChange;
        }

        public static void OnUpdate()
        {
            if (Pickups != null && Orbits != null && CurrentPlayer != null)
            {
                OrbitCenter = GetCenter();
                for (int i = 0; i < Pickups.Count; i++)
                {
                    if (patch.Value) Patch(Pickups[i]);
                    Pickups[i].transform.position = Orbits[i].CurrentPos();
                    Pickups[i].transform.rotation = Orbits[i].CurrentRot();
                }
            }
            Timer += Time.deltaTime;
        }

        private static void OnInstanceChange(ApiWorld world, ApiWorldInstance instance) => StopOrbit();

        private static void OnLeave(Player player) { if (CurrentPlayer == player) StopOrbit(); }

        private static void SelectOrbit(string Type)
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

        private static void ToOrbit(VRCPlayer Player)
        {
            if (CurrentPlayer != null) StopOrbit();
            CurrentPlayer = Player;
            Timer = 0f;
            Pickups = Object.FindObjectsOfType<VRC_Pickup>();
            OrbitCenter = GetCenter();
            Orbits = new List<OrbitItem>();
            for (int i = 0; i < Pickups.Count; i++)
            {
                Orbits.Add(new OrbitItem(Pickups[i], i));
            }
        }

        private static void StopOrbit()
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

        private static void Patch(VRC_Pickup Item)
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

        private static void Unpatch(int i)
        {
            Pickups[i].GetComponent<VRC_Pickup>().DisallowTheft = Orbits[i].InitialTheft;
            Pickups[i].GetComponent<VRC_Pickup>().pickupable = Orbits[i].InitialPickupable;
            Pickups[i].GetComponent<Rigidbody>().isKinematic = Orbits[i].InitialKinematic;
            Pickups[i].GetComponent<Rigidbody>().velocity = Orbits[i].InitialVelocity;
            Pickups[i].gameObject.SetActive(Orbits[i].InitialActive);
        }

        private static Vector3 GetCenter()
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
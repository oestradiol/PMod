using PMod.Modules;
using System.Collections.Generic;
using UnityEngine;
using VRC;
using VRC.Core;

namespace PMod
{
	internal abstract class ModuleBase
	{
		internal virtual void OnUiManagerInit() { }

		internal virtual void OnPreferencesSaved() { }

		internal virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }

		internal virtual void OnUpdate() { }

		internal virtual void OnPlayerJoined(Player player) { }

		internal virtual void OnPlayerLeft(Player player) { }

		internal virtual void OnInstanceChange(ApiWorld world, ApiWorldInstance instance) { }

		internal virtual void OnAvatarInitialized(GameObject avatar, VRCAvatarManager manager) { }
	}

	internal static class ModulesManager
	{
		internal static List<ModuleBase> modules = new();

		internal static AvatarFromID avatarFromID;
		internal static EmmAllower emmAllower;
		//internal static ForceClone forceClone;
		internal static FrozenPlayersManager frozenPlayersManager;
		internal static InvisibleJoin invisibleJoin;
		internal static ItemGrabber itemGrabber;
		internal static Orbit orbit;
		internal static PhotonFreeze photonFreeze;
		internal static Triggers triggers;
		internal static UserInteractUtils userInteractUtils;

		public static void Initialize()
		{
			modules.Add(avatarFromID = new());
			modules.Add(emmAllower = new());
			//modules.Add(forceClone = new());
			modules.Add(frozenPlayersManager = new());
		    modules.Add(invisibleJoin = new());
			modules.Add(itemGrabber = new());
			modules.Add(orbit = new());
			modules.Add(photonFreeze = new());
			modules.Add(triggers = new());
			modules.Add(userInteractUtils = new());
		}

		public static void OnUiManagerInit()
		{ foreach (ModuleBase module in modules) module.OnUiManagerInit(); }

		public static void OnPreferencesSaved()
		{ foreach (ModuleBase module in modules) module.OnPreferencesSaved(); }

		public static void OnSceneWasLoaded(int buildIndex, string sceneName)
		{ foreach (ModuleBase module in modules) module.OnSceneWasLoaded(buildIndex, sceneName); }

		public static void OnUpdate()
		{ foreach (ModuleBase module in modules) module.OnUpdate(); }

		public static void OnPlayerJoined(Player player)
		{ foreach (ModuleBase module in modules) module.OnPlayerJoined(player); }

		public static void OnPlayerLeft(Player player)
		{ foreach (ModuleBase module in modules) module.OnPlayerLeft(player); }

		public static void OnInstanceChange(ApiWorld world, ApiWorldInstance instance)
		{ foreach (ModuleBase module in modules) module.OnInstanceChange(world, instance); }

		public static void OnAvatarInitialized(GameObject avatar, VRCAvatarManager manager)
		{ foreach (ModuleBase module in modules) module.OnAvatarInitialized(avatar, manager); }
    }

	// // Something like this would be a better and more optimized alternative maybe?
	//internal static class ModulesManager
	//{
	//	internal static AvatarFromID avatarFromID;
	//	internal static EmmAllower emmAllower;
	//	//internal static ForceClone forceClone;
	//	internal static FrozenPlayersManager frozenPlayersManager;
	//	internal static InvisibleJoin invisibleJoin;
	//	internal static ItemGrabber itemGrabber;
	//	internal static Orbit orbit;
	//	internal static PhotonFreeze photonFreeze;
	//	internal static Triggers triggers;
	//	internal static UserInteractUtils userInteractUtils;

	//	internal static Action OnUiManagerInit;
	//	internal static Action OnPreferencesSaved;
	//	internal static Action<int, string> OnSceneWasLoaded;
	//	internal static Action OnUpdate;
	//	internal static Action<Player> OnPlayerJoined;
	//	internal static Action<Player> OnPlayerLeft;
	//	internal static Action<ApiWorld, ApiWorldInstance> OnInstanceChange;
	//	internal static Action<GameObject, VRCAvatarManager> OnAvatarInitialized;

	//	public static void Initialize()
	//	{
	//		avatarFromID = new();
	//		emmAllower = new();
	//		//forceClone = new();
	//		frozenPlayersManager = new();
	//		invisibleJoin = new();
	//		itemGrabber = new();
	//		orbit = new());
	//		photonFreeze = new();
	//		triggers = new();
	//		userInteractUtils = new();
	//	}
	//}
}
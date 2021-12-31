using PMod.Modules;
using System;
using VRC;
using VRC.Core;

namespace PMod
{
	internal abstract class ModuleBase
	{
		internal bool useOnUiManagerInit, useOnPreferencesSaved, useOnSceneWasLoaded, useOnUpdate, useOnPlayerJoined, useOnPlayerLeft, useOnInstanceChanged;
		internal void RegisterSubscriptions()
        {
			if (useOnUiManagerInit) ModulesManager.OnUiManagerInit += OnUiManagerInit;
			if (useOnPreferencesSaved) ModulesManager.OnPreferencesSaved += OnPreferencesSaved;
			if (useOnSceneWasLoaded) ModulesManager.OnSceneWasLoaded += OnSceneWasLoaded;
			if (useOnUpdate) ModulesManager.OnUpdate += OnUpdate;
			if (useOnPlayerJoined) ModulesManager.OnPlayerJoined += OnPlayerJoined;
			if (useOnPlayerLeft) ModulesManager.OnPlayerLeft += OnPlayerLeft;
			if (useOnInstanceChanged) ModulesManager.OnInstanceChanged += OnInstanceChanged;
		}

		internal virtual void OnUiManagerInit() { }
		internal virtual void OnPreferencesSaved() { }
		internal virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
		internal virtual void OnUpdate() { }
		internal virtual void OnPlayerJoined(Player player) { }
		internal virtual void OnPlayerLeft(Player player) { }
		internal virtual void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) { }
	}

	internal static class ModulesManager
	{
		internal static AvatarFromID avatarFromID;
		internal static CopyAsset copyAsset;
		internal static ModsAllower emmAllower;
		//internal static ForceClone forceClone;
		internal static FrozenPlayersManager frozenPlayersManager;
		internal static InvisibleJoin invisibleJoin;
		internal static ItemGrabber itemGrabber;
		internal static Orbit orbit;
		internal static PhotonFreeze photonFreeze;
		internal static SoftClone softClone;
		internal static TeleportToCursor teleportToCursor;
		internal static Triggers triggers;

        internal static Action OnUiManagerInit;
        internal static Action OnPreferencesSaved;
        internal static Action<int, string> OnSceneWasLoaded;
        internal static Action OnUpdate;
        internal static Action<Player> OnPlayerJoined;
        internal static Action<Player> OnPlayerLeft;
        internal static Action<ApiWorld, ApiWorldInstance> OnInstanceChanged;

		internal static void Initialize()
		{
			avatarFromID = new();
			copyAsset = new();
			emmAllower = new();
			//forceClone = new();
			frozenPlayersManager = new();
			invisibleJoin = new();
			itemGrabber = new();
			orbit = new();
			photonFreeze = new();
			softClone = new();
			teleportToCursor = new();
			triggers = new();
		}
    }
}
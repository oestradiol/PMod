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

		protected virtual void OnUiManagerInit() { }
		protected virtual void OnPreferencesSaved() { }
		protected virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
		protected virtual void OnUpdate() { }
		protected virtual void OnPlayerJoined(Player player) { }
		protected virtual void OnPlayerLeft(Player player) { }
		protected virtual void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) { }
	}

	internal static class ModulesManager
	{
		internal static AvatarFromID avatarFromID;
		internal static CopyAsset copyAsset;
		internal static ModsAllower modsAllower;
		// internal static ForceClone forceClone;
		internal static FrozenPlayersManager frozenPlayersManager;
		// internal static InvisibleJoin invisibleJoin;
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
			avatarFromID = new AvatarFromID();
			copyAsset = new CopyAsset();
			modsAllower = new ModsAllower();
			// forceClone = new();
			frozenPlayersManager = new FrozenPlayersManager();
			// invisibleJoin = new InvisibleJoin();
			itemGrabber = new ItemGrabber();
			orbit = new Orbit();
			photonFreeze = new PhotonFreeze();
			softClone = new SoftClone();
			teleportToCursor = new TeleportToCursor();
			triggers = new Triggers();
		}
    }
}
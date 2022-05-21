using PMod.Modules;
using System;
using System.Collections.Generic;
using VRC;
using VRC.Core;

namespace PMod;

internal static class ModulesManager
{
	private static readonly Dictionary<Type, ModuleBase> Modules = new();
	private static void InitializeModule<T>() where T : ModuleBase, new() => Modules.Add(typeof(T), new T());
	internal static T GetModule<T>() where T : ModuleBase => (T)Modules[typeof(T)];

	internal static Action OnPreferencesSaved;
	internal static Action<int, string> OnSceneWasLoaded;
	internal static Action OnUpdate;
	internal static Action OnUiManagerInit;
	internal static Action<Player> OnPlayerJoined;
	internal static Action<Player> OnPlayerLeft;
	internal static Action<ApiWorld, ApiWorldInstance> OnInstanceChanged;

	internal static void Initialize()
	{
		InitializeModule<AvatarFromID>();
		InitializeModule<CopyAsset>();
		InitializeModule<ModsAllower>();
		// InitializeModule<ForceClone>();
		InitializeModule<FrozenPlayersManager>();
		// InitializeModule<InvisibleJoin>();
		InitializeModule<ItemGrabber>();
		InitializeModule<Orbit>(); // Not working fully!
		InitializeModule<PhotonFreeze>(); // Questionable..?
		InitializeModule<SoftClone>(); //
		InitializeModule<TeleportToCursor>();
		InitializeModule<Triggers>(); //
	}
}
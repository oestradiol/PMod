using System;
using System.Collections.Generic;
using System.Linq;
using PMod.Modules.Internals;

namespace PMod.Modules;

internal static partial class Manager
{
	private static readonly Dictionary<Type, VrcMod> Modules = new();
	private static void CacheModule(Type moduleType, VrcMod module) => Modules.Add(moduleType, module);
	private static void InitializeModule<T>() where T : VrcMod, new() => CacheModule(typeof(T), new T());
	internal static T GetModule<T>() where T : VrcMod => (T)Modules[typeof(T)];

	internal static void Init()
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
		
		LoadExternalModules();
	}

	internal static void UnloadModules() =>
		Modules.Values.OfType<IOnModUnload>().ToList().ForEach(m => m.OnModUnload());
}
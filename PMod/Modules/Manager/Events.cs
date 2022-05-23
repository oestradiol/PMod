using System;
using VRC;
using VRC.Core;

namespace PMod.Modules;

internal static partial class Manager
{
    #region Melon Events
    internal static event Action PreSupportModule;
    internal static event Action ApplicationStart;
    internal static event Action ApplicationLateStart;
    internal static event Action ApplicationQuit;
    internal static event Action Update;
    internal static event Action LateUpdate;
    internal static event Action FixedUpdate;
    internal static event Action GUI;
    internal static event Action<string> PreferencesLoaded;
    internal static event Action<string> PreferencesSaved;
    internal static event Action<int, string> SceneWasLoaded;
    internal static event Action<int, string> SceneWasInitialized;
    internal static event Action<int, string> SceneWasUnloaded;
    internal static void OnPreSupportModule() => PreSupportModule?.Invoke();
    internal static void OnApplicationStart() => ApplicationStart?.Invoke();
    internal static void OnApplicationLateStart() => ApplicationLateStart?.Invoke();
    internal static void OnApplicationQuit() => ApplicationQuit?.Invoke();
    internal static void OnUpdate() => Update?.Invoke();
    internal static void OnLateUpdate() => LateUpdate?.Invoke();
    internal static void OnFixedUpdate() => FixedUpdate?.Invoke();
    internal static void OnGUI() => GUI?.Invoke();
    // TODO: Save settings on application quit.
    internal static void OnPreferencesLoaded(string filePath) => PreferencesLoaded?.Invoke(filePath);
    internal static void OnPreferencesSaved(string filePath) => PreferencesSaved?.Invoke(filePath);
    internal static void OnSceneWasLoaded(int buildIndex, string sceneName) => SceneWasLoaded?.Invoke(buildIndex, sceneName);
    internal static void OnSceneWasInitialized(int buildIndex, string sceneName) => SceneWasInitialized?.Invoke(buildIndex, sceneName);
    internal static void OnSceneWasUnloaded(int buildIndex, string sceneName) => SceneWasUnloaded?.Invoke(buildIndex, sceneName);
    #endregion

    #region Custom Events
    internal static event Action UiManagerInit;
    internal static event Action<Player> PlayerJoined;
    internal static event Action<Player> PlayerLeft;
    internal static event Action<ApiWorld, ApiWorldInstance> InstanceChanged;
    // TODO: Add Unity and VRChat UiManagerInits.
    internal static void OnUiManagerInit() => UiManagerInit?.Invoke();
    internal static void OnPlayerJoined(Player player) => PlayerJoined?.Invoke(player);
    internal static void OnPlayerLeft(Player player) => PlayerLeft?.Invoke(player);
    internal static void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) => InstanceChanged?.Invoke(world, instance);
    #endregion
}

// internal enum Events
// {
//     // Melon events
//     OnPreSupportModule,
//     OnApplicationStart,
//     OnApplicationLateStart,
//     OnApplicationQuit,
//     OnUpdate,
//     OnLateUpdate,
//     OnFixedUpdate,
//     OnGUI,
//     OnPreferencesLoaded,
//     OnPreferencesSaved,
//     OnSceneWasLoaded,
//     OnSceneWasInitialized,
//     OnSceneWasUnloaded,
// 	
//     // Custom events
//     OnUiManagerInit,
//     OnPlayerJoined,
//     OnPlayerLeft,
//     OnInstanceChanged
// }
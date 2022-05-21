using System;
using MelonLoader;
using VRC;
using VRC.Core;

namespace PMod;

internal abstract class ModuleBase
{
	protected virtual void OnApplicationStart() { }
	protected virtual void OnPreferencesSaved() { }
	protected virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
	protected virtual void OnUpdate() { }
	protected virtual void OnUiManagerInit() { }
	protected virtual void OnPlayerJoined(Player player) { }
	protected virtual void OnPlayerLeft(Player player) { }
	protected virtual void OnInstanceChanged(ApiWorld world, ApiWorldInstance instance) { }

	internal bool useOnApplicationStart, useOnUiManagerInit, useOnPreferencesSaved, useOnSceneWasLoaded, useOnUpdate, useOnPlayerJoined, useOnPlayerLeft, useOnInstanceChanged;
	internal readonly MelonPreferences_Entry<bool> IsOn;
	internal readonly string ThisModuleName;

	internal ModuleBase(bool defaultIsOn)
	{
		ThisModuleName = GetType().Name;
		IsOn = MelonPreferences.CreateEntry(BuildInfo.Name, $"{ThisModuleName}IsOn", defaultIsOn, $"Activate {ThisModuleName}?");
	}
	
	internal void RegisterSubscriptions()
	{
		if (!IsOn.Value) return;
		if (useOnApplicationStart)
		{
			try
			{ OnApplicationStart(); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnApplicationStart. Exception: {e}"); }
		}
		if (useOnUiManagerInit) ModulesManager.OnUiManagerInit += () =>
		{
			try
			{ OnUiManagerInit(); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnUiManagerInit. Exception: {e}"); }
		};
		if (useOnPreferencesSaved) ModulesManager.OnPreferencesSaved += () =>
		{
			try
			{ OnPreferencesSaved(); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnPreferencesSaved. Exception: {e}"); }
		};
		if (useOnSceneWasLoaded) ModulesManager.OnSceneWasLoaded += (buildIndex, sceneName) =>
		{
			try
			{ OnSceneWasLoaded(buildIndex, sceneName); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnSceneWasLoaded. Exception: {e}"); }
		};
		if (useOnUpdate) ModulesManager.OnUpdate += () =>
		{
			try
			{ OnUpdate(); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnUpdate. Exception: {e}"); }
		};
		if (useOnPlayerJoined) ModulesManager.OnPlayerJoined += player =>
		{
			try
			{ OnPlayerJoined(player); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnPlayerJoined. Exception: {e}"); }
		};
		if (useOnPlayerLeft) ModulesManager.OnPlayerLeft += player =>
		{
			try
			{ OnPlayerLeft(player); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnPlayerLeft. Exception: {e}"); }
		};
		if (useOnInstanceChanged) ModulesManager.OnInstanceChanged += (apiWorld, apiWorldInstance) =>
		{
			try
			{ OnInstanceChanged(apiWorld, apiWorldInstance); }
			catch (Exception e)
			{ Main.Logger.Error($"Something went wrong in {ThisModuleName} OnInstanceChanged. Exception: {e}"); }
		};
	}
}
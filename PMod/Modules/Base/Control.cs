using System;
using System.Reflection;
using MelonLoader;

namespace PMod.Modules;

public abstract partial class VrcMod
{
	internal readonly MelonPreferences_Entry<bool> IsOn;
	
	// TODO: Update this to stop using MelonPrefs.
	protected VrcMod(bool defaultIsOn = true)
	{
		var moduleName = GetType().Name;
		IsOn = MelonPreferences.CreateEntry(BuildInfo.Name, $"{moduleName}IsOn", defaultIsOn, $"Activate {moduleName}?");
		RegisterEvents();
	}
	
	private void RegisterEvents()
	{
		if (!IsOn.Value) return;
		RegisterMelonEvents();
		RegisterCustomEvents();
	}

	private bool IsOverriding(string methodName)
	{
		var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Attributes;
		return method != null && !method.HasFlag(MethodAttributes.NewSlot) && method.HasFlag(MethodAttributes.Virtual);
	}
	
	private void LogInternalError(Exception e, string methodName) => 
		Main.Logger.Error($"Something went wrong in {GetType().Name} during {methodName} execution. Exception: {e}");
}
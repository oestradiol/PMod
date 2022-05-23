using PMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace PMod.Modules;

internal static partial class Manager
{
    private static readonly string ModulesFolder = Path.Combine(Utilities.ModFolder, "Modules");
    private static readonly Dictionary<string, List<ModuleEntry>> ToSelectEntries = new();
    private static List<ModuleEntry> GetModuleEntries(string moduleName)
    {
        if (ToSelectEntries.TryGetValue(moduleName, out var moduleEntry))
            return moduleEntry;
        
        moduleEntry = new List<ModuleEntry>();
        ToSelectEntries.Add(moduleName, moduleEntry);
        return moduleEntry;
    }
    private static AppDomain _cachedLocals;
    private static AppDomain CachedLocals
    {
        get => _cachedLocals ??= AppDomain.CreateDomain(BuildInfo.Name);
        set
        {
            try
            {
                if (value == null)
                    AppDomain.Unload(_cachedLocals);
                
                _cachedLocals = value;
            }
            catch { /* ignored */ }
        }
    }

    private static void LoadExternalModules()
    {
        GetLocals();
        GetRemotes();
        SelectAndLoad();
    }

    #region Locals
    private static void GetLocals()
    {
        if (!Utilities.EnsureFolderExists(ModulesFolder))
            return;

        IEnumerable<string> filePaths;
        try
        { filePaths = Directory.GetFiles(ModulesFolder, "*.dll"); }
        catch
        { return; }

        var gac = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var path in filePaths)
        {
            // Checks first if already is on GAC since Melon tends to load stuff beforehand.
            // TODO: Debug this.
            // TODO: Possibly add MelonPref to ignore GAC here, forcing double caching: one from folder, one from GAC later on.
            var assembly = gac.FirstOrDefault(i => string.Equals(i.Location, path, StringComparison.OrdinalIgnoreCase));
            
            ModuleEntry entry;
            AssemblyName assemblyName;
            if (assembly != null)
            {
                assemblyName = assembly.GetName();
                entry = new ModuleEntry
                {
                    Type = ModuleType.Gac,
                    Assembly = assembly,
                    Version = assemblyName.Version
                };
            }
            else
            {
                if (!LoadFromCachedAppDomain(path, out assembly))
                    continue;
                
                assemblyName = assembly.GetName();
                entry = new ModuleEntry
                {
                    Type = ModuleType.Local,
                    Path = path,
                    Version = assemblyName.Version
                };
            }
            
            GetModuleEntries(assemblyName.Name).Add(entry);
        }
        
        // Attempts to unload assemblies to avoid memory leaks
        CachedLocals = null;
    }
    
    // TODO: Debug this
    private static bool LoadFromCachedAppDomain(string path, out Assembly result)
    {
        result = null;
        Assembly assembly = null;
        
        try
        { CachedLocals.DoCallBack(() => assembly = Assembly.LoadFile(path)); }
        catch
        { return false; }

        result = assembly;
        return result != null;
    }
    #endregion

    #region Remotes
    private static void GetRemotes()
    {
        var providers = Settings.Instance.Modules.GroupBy(m => m.Provider).ToList();
        var selfHosted = providers.FirstOrDefault(p => string.IsNullOrEmpty(p.Key));
        if (selfHosted != null)
        {
            providers.Remove(selfHosted);
            // TODO: Enqueue self hosted modules. Make sure it's thread safe and low priority.
        }
        
        providers.ForEach(GetFromRemoteProvider);
    }

    private static void GetFromRemoteProvider(IGrouping<string, ModuleConfigEntry> provider)
    {
        if (!Settings.Instance.Providers.TryGetValue(provider.Key, out var providerAddress))
        {
            Main.Logger.Warning($"Failed to get all modules from provider {provider.Key}: Provider not found.");
            return;
        }

        Uri providerUri;
        try
        { providerUri = new Uri(providerAddress); }
        catch
        {
            Main.Logger.Warning($"Failed to get all modules from provider {provider.Key}: Provided URI is invalid.");
            return;
        }
        
        var moduleNames = provider.Select(m => m.AssemblyName).ToArray();
        var versions = GetRemoteVersions(providerUri, moduleNames);
        if (versions == null)
            Main.Logger.Warning($"Failed to get all modules from provider {provider.Key}: Failed to get versions.");
        else
        {
            foreach (var module in moduleNames)
            {
                if (!versions.TryGetValue(module, out var version))
                    continue;

                var moduleEntry = new ModuleEntry
                {
                    Type = ModuleType.Remote,
                    Path = new Uri(providerUri, $"{module}.dll").ToString(),
                    Version = version
                };
                GetModuleEntries(module).Add(moduleEntry);
            }
        }
    }
    
    private static Dictionary<string, Version> GetRemoteVersions(Uri uri, string[] data) {
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, Version>>(
                new WebClient {Headers = {[HttpRequestHeader.ContentType] = "application/json"}}
                    .UploadString(uri, JsonConvert.SerializeObject(data)))!;
        }
        catch
        { return null; }
    }
    #endregion

    #region Loading
    private static void SelectAndLoad()
    {
        var toLoadEntries = new List<ModuleEntry>();
        
        foreach (var m in ToSelectEntries)
        {
            var groupedByVersions = m.Value
                .GroupBy(a => a.Version)
                .OrderBy(a => a.Key)
                .Select(g => g.OrderBy(e => (int)e.Type));

            if (!TryGetBestPossibleEntry(groupedByVersions, out var entry))
            {
                Main.Logger.Warning($"Failed to load {m.Key} from all resources. This module won't load.");
                continue;
            }
            
            toLoadEntries.Add(entry);
        }
        
        // TODO: Make this run on a different thread asynchronously? Might have to check interactions with UiManagerInit.
        foreach (var entry in toLoadEntries.OrderByDescending(e => e.Priority))
            LoadModule(entry.Assembly);
    }

    private static bool TryGetBestPossibleEntry(IEnumerable<IOrderedEnumerable<ModuleEntry>> versionsEnum, out ModuleEntry result)
    {
        foreach (var versions in versionsEnum)
        {
            foreach (var entry in versions)
            {
                try
                {
                    entry.Assembly = entry.Type switch
                    {
                        ModuleType.Remote => GetRemoteAssembly(entry),
                        // If cachedLocals failed to unload, will try to take advantage of it.
                        ModuleType.Local when _cachedLocals != null => CachedLocals.GetAssemblies().FirstOrDefault() ?? Assembly.LoadFrom(entry.Path),
                        ModuleType.Local when _cachedLocals == null => Assembly.LoadFrom(entry.Path),
                        ModuleType.Gac => entry.Assembly,
                        _ => null
                    };
                } catch { /* ignored */ }
                
                if (entry.Assembly == null) continue;
                result = entry;
                return true;
            }
        }

        result = null;
        return false;
    }
    
    private static Assembly GetRemoteAssembly(ModuleEntry entry)
    {
        byte[] bytes = null;
        try
        { bytes = new WebClient().DownloadData(entry.Path); }
        catch { /* ignored */ }
        
        if (bytes == null) 
            return null;

        try
        {
            var loadedAsm = Assembly.Load(bytes);
            
            if (!Utilities.EnsureFolderExists(ModulesFolder)) return loadedAsm;
            try
            { File.WriteAllBytes(Path.Combine(ModulesFolder, $"{loadedAsm.GetName().Name}.dll"), bytes); } 
            catch 
            { /* ignored */ }
            
            return loadedAsm;
        }
        catch { return null; }
    }
    
    private static void LoadModule(Assembly moduleAsm)
    {
        if (!TryGetInheritedType(moduleAsm, typeof(VrcMod), out var moduleType))
        {
            Main.Logger.Warning($"Failed to load assembly {moduleAsm.GetName().FullName}: No ModuleBase inherited class found.");
            return;
        }

        VrcMod instance;
        try
        {
            instance = (VrcMod)Activator.CreateInstance(moduleType);
            if (instance == null)
                throw new NullReferenceException();
        }
        catch
        {
            Main.Logger.Warning($"Failed to load ModuleBase {moduleType.FullName}: Failed to create module instance.");
            return;
        }
        
        CacheModule(moduleType, instance);
    }
    
    private static bool TryGetInheritedType(Assembly module, Type baseType, out Type type)
    {
        type = null;
        
        if (module == null)
            return false;
    
        try
        {
            IEnumerable<Type> types;
            try { types = module.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
        
            type = types.FirstOrDefault(t => t.IsSubclassOf(baseType));
        }
        catch
        { return false; }

        return true;
    }
    #endregion
}
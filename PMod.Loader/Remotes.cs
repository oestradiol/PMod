using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace PMod.Loader;

public static class Remotes
{
    internal static async void UpdateLoader() {
        try
        {
            PModLoader.Logger.Msg(ConsoleColor.Blue, $"Loader's current version: {LInfo.Version}. Checking for updates...");
            
            var data = new Dictionary<string, string>() { {"name", LInfo.Name}, {"version", LInfo.Version} };
            data = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                await new WebClient { Headers = { [HttpRequestHeader.ContentType] = "application/json" } }
                    .UploadStringTaskAsync(new Uri(LInfo.MainLink + "api"), JsonConvert.SerializeObject(data)))!;

            switch (data["result"])
            {
                case "UPDATED":
                    PModLoader.Logger.Msg(ConsoleColor.Green, "The Loader is up to date!");
                    break;
                case "OUTDATED":
                    PModLoader.Logger.Msg(ConsoleColor.Yellow, $"The Loader is outdated! Latest version: {data["latest"]}. Downloading...");
                    File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"Mods/{LInfo.Name}.dll"), new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.Name}.dll"));
                    PModLoader.Logger.Msg(ConsoleColor.Green, $"Successfully updated {LInfo.Name}!");
                    Restart();
                    break;
                case "Welcome, HR.":
                    PModLoader.Logger.Msg(ConsoleColor.DarkRed, "Welcome, Savitar. What would you want to do today?");
                    break;
                default:
                    throw new Exception($"Unrecognizable response detected! Response: {{\"result\":{data["result"]}, \"latest\":{data["latest"]}}}. If you got here, PLEASE report this...");
            }
        }
        catch (Exception e) { PModLoader.Logger.Warning($"Failed to check and download latest version!\n{e}"); }
    }

    internal static bool AppHasStarted;
    private static void Restart()
    {
        try {
            PModLoader.Logger.Msg("Attempting to restart VRChat...");
            Process.Start(Environment.CurrentDirectory + "\\VRChat.exe", Environment.CommandLine);
            if (AppHasStarted) PModLoader.Loader.OnApplicationQuit();
            Process.GetCurrentProcess().Kill();
        }
        catch (Exception e) {
            PModLoader.Logger.Error(e);
        }
    }

    internal static Assembly GetPModAssembly()
    {
        byte[] bytes = null;
        try
        {
            PModLoader.Logger.Msg(ConsoleColor.Blue, "Attempting to load PMod latest version...");
            bytes = new WebClient().DownloadData(LInfo.MainLink + $"{LInfo.ModName}.dll");
        } 
        catch (Exception e)
        { PModLoader.Logger.Error($"Failed to download {LInfo.ModName} from {LInfo.MainLink + $"{LInfo.ModName}.dll"}! Error: {e}"); }

        #if DEBUG
            PModLoader.Logger.Warning("This Assembly was built in Debug Mode! Forcing to load from VRChat main folder.");
            bytes = null;
        #endif

        if (bytes != null)
            return Assembly.Load(bytes);
        
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName.Contains(LInfo.ModName + ','));
        if (assembly != null)
        {
            PModLoader.Logger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName} loaded in the GAC! Attempting to use it...");
            return assembly;
        }
        
        if (File.Exists($"{LInfo.ModName}.dll"))
        {
            PModLoader.Logger.Msg(ConsoleColor.Green, $"Found {LInfo.ModName}.dll in VRChat folder! Attempting to load it as a last resource...");
            return Assembly.Load(File.ReadAllBytes($"{LInfo.ModName}.dll"));
        }
        
        PModLoader.Logger.Warning("All attempts to load the assembly failed. PMod won't load.");
        return null;
    }
}
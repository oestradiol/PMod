using System.Collections.Generic;

namespace PMod;

public class Config
{
    public List<ModuleConfigEntry> Modules { get; set; } = new()
    {
        new ModuleConfigEntry
        {
            Name = "Example Self-provided Module",
            AssemblyName = "ExampleSelfProvidedModule",
            Uri = "https://davi.codes/api/v1/VRCModules/ExampleSelfProvidedModule.dll"
        },
        new ModuleConfigEntry
        {
            Provider = "DaviCodes",
            Name = "Example Module",
            AssemblyName = "ExampleModule",
        }
    };
    
    public Dictionary<string, string> Providers { get; set; } = new()
    {
        { "DaviCodes", "https://davi.codes/api/v1/VRCModules" }
    };
}

public class ModuleConfigEntry
{
    public string Provider { get; set; }
    public string Name { get; set; }
    public string AssemblyName { get; set; }
    public string Uri { get; set; } // Won't be used unless module is self provided.
}
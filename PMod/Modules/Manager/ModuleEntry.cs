using System;
using System.Reflection;

namespace PMod.Modules;

public enum ModuleType
{
    Gac,
    Local,
    Remote
}

public class ModuleEntry
{
    public ModuleType Type { get; set; }
    public string Path { get; set; }
    public Assembly Assembly { get; set; }
    public Version Version { get; set; }
    public int Priority => 0;//Assembly.GetCustomAttribute<PriorityAttribute>().Priority;
}

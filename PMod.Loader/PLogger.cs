using System;
using static PMod.Loader.PModLoader;

namespace PMod.Loader
{
    // Straight skidded it from ReModCE https://github.com/RequiDev/ReModCE/blob/master/ReModCE.Loader/ReLogger.cs
    public static class PLogger
    {
        public static void Msg(string text) => Logger.Msg($"[{LInfo.ModName}] " + text);
        public static void Msg(string text, params object[] args) => Logger.Msg($"[{LInfo.ModName}] " + text, args);
        public static void Msg(object obj) => Logger.Msg(obj);
        public static void Msg(ConsoleColor textColor, string text) => Logger.Msg(textColor, $"[{LInfo.ModName}] " + text);
        public static void Msg(ConsoleColor textColor, string text, params object[] args) => Logger.Msg(textColor, $"[{LInfo.ModName}] " + text, args);
        public static void Msg(ConsoleColor textColor, object obj) => Logger.Msg(textColor, obj);

        public static void Warning(string text) => Logger.Warning($"[{LInfo.ModName}] " + text);
        public static void Warning(string text, params object[] args) => Logger.Warning($"[{LInfo.ModName}] " + text, args);
        public static void Warning(object obj) => Logger.Warning(obj);

        public static void Error(string text) => Logger.Error($"[{LInfo.ModName}] " + text);
        public static void Error(string text, params object[] args) => Logger.Error($"[{LInfo.ModName}] " + text, args);
        public static void Error(object obj) => Logger.Error(obj);
    }
}
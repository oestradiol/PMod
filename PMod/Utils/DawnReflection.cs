/*
namespace Dawn.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using PMod.Loader;

    internal class ReflectionTools
    {
        //Basically Dict of Class<returnType>
        private static readonly Dictionary<Type, Dictionary<Type, PropertyInfo>> PropertyInstances = new ();
        /// I STG if anyone uses non (Class || Struct) on this I will bonk you.
        internal static T FindProperty<T>(object instanceOrClass = null) where T : notnull
        {
            T _result = default;
            try
            {
                instanceOrClass ??= typeof(T);
                
                var @class = instanceOrClass.GetType();

                // if (@class.ToString() == "System.RuntimeType") @class = instanceOrClass.TryCast<Type>();
                
                @class = @class.ToString() == "System.RuntimeType" ? (Type) instanceOrClass : @class; /// !!
                
                PropertyInstances.TryGetValue(@class, out var _value);
                if (_value?[typeof(T)] != null) return _value[typeof(T)].GetValue(instanceOrClass).TryCast<T>(); // If our cache system has the type on record.
                
                var firstPropertyFound = @class.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) // GetProperties = 400ns / 600ns total of this method
                    .Where(m => m.PropertyType == typeof(T))
                    .OrderBy(m => m.Name.StartsWith("prop_"))
                    .FirstOrDefault();
                
                if (firstPropertyFound == null) 
                    throw new NullReferenceException($"Unable to Find a Property {@class.Name}[{typeof(T).Name}].");

                Task.Run(()=> //We don't need this crap running on the main thread. Though im pretty sure it takes longer to create a Task Wrapper...
                {
                    #if DEBUG
                    PLogger.Msg($"Internal Caching: Cached Property {@class.Name}[{typeof(T).Name}].");
                    #endif
                    // If the class was added already but not the instance.
                    if (PropertyInstances.ContainsKey(@class)) PropertyInstances[@class].Add(typeof(T), firstPropertyFound);
                    //If we need to completely add the entry.
                    else PropertyInstances.Add(@class, new Dictionary<Type, PropertyInfo> {{typeof(T), firstPropertyFound}});
                });


                if (firstPropertyFound.isStatic()) return (T)firstPropertyFound.GetValue(null);
                return (T)firstPropertyFound.GetValue(instanceOrClass);
                //LN84 errors here
            }
            catch (Exception e) { PLogger.Error(e); }
            return _result;
        }
    }
    public static class ReflectionExtensions
    {
        public static dynamicType TryCast<dynamicType>(this object obj) { try { return (dynamicType) obj; } catch { return default; } }

        /// 100ns
        public static bool isStatic(this PropertyInfo info) => info.GetAccessors(true).FirstOrDefault() != null && info.GetAccessors(true)[0].IsStatic;

        internal static IEnumerable<MethodInfo> ContainsAtLeast(this IEnumerable<MethodInfo> mInfos, Type[] parameterTypes)
        {
            if (parameterTypes == null) return mInfos;
            mInfos = mInfos.Where(xa => xa.GetParameters().Length >= parameterTypes.Length);
            var mInfosHS = new List<MethodInfo>();
            foreach (var asd in mInfos)
            {
                var uwu = 0;
                foreach (var dsa in asd.GetParameters())
                {
                    uwu += parameterTypes.Count(ara => dsa.ParameterType == ara);
                    if (uwu < parameterTypes.Length) continue;
                    mInfosHS.Add(asd);
                    break;
                }
            }
            return mInfosHS;
        }
        
    }
}
*/

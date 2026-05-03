using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Cave.IO;

/// <summary>Provides fast instantiation of types via cached parameterless constructor delegates.</summary>
public static class TypeActivator
{
    static readonly Dictionary<Type, Func<object>> cache = new();

    /// <summary>Creates an instance of the specified type using a cached parameterless constructor delegate for fast activation.</summary>
    /// <param name="type">The type to create an instance of.</param>
    /// <returns>An instance of the specified type.</returns>
    public static object CreateFast(Type type)
    {
        if (!cache.TryGetValue(type, out var func))
        {
            func = CreateCtor(type);
            cache[type] = func;
        }
        return func();
    }

#if (NET5_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER)

    static Func<object> CreateCtor(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        System.Linq.Expressions.Expression body;
        if (type.IsValueType)
        {
            // struct: default(T)
            body = System.Linq.Expressions.Expression.Default(type);
        }
        else
        {
            // class
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null);
            if (ctor == null) throw new InvalidOperationException($"Type has no parameterless constructor: {type.FullName}");
            body = System.Linq.Expressions.Expression.New(ctor);
        }

        var cast = System.Linq.Expressions.Expression.Convert(body, typeof(object));
        return System.Linq.Expressions.Expression.Lambda<Func<object>>(cast).Compile();
    }

#elif (NET20_OR_GREATER || NETSTANDARD2_1_OR_GREATER)

    static Func<object> CreateCtor(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var dm = new DynamicMethod("FastCtor_" + type.Name, typeof(object), Type.EmptyTypes, type, skipVisibility: true);
        var il = dm.GetILGenerator();
        if (type.IsValueType)
        {
            // struct: default(T)
            var local = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Box, type);
        }
        else
        {
            // class:
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (ctor == null) throw new InvalidOperationException($"Type has no parameterless constructor: {type.FullName}");
            il.Emit(OpCodes.Newobj, ctor);
        }
        il.Emit(OpCodes.Ret);
        return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
    }
#endif
}

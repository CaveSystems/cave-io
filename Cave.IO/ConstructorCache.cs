using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Cave.IO;

/// <summary>Provides a cache for constructor information and a factory method for creating instances of a type using the cached constructor.</summary>
public sealed class ConstructorCache
{
    static FactoryFunction BuildFactory(Type declaringType, ConstructorInfo ctor, Type[] paramTypes)
    {
        var dm = new DynamicMethod(name: "ConstructorCache_" + declaringType.Name, returnType: typeof(object), parameterTypes: new[] { typeof(object[]) }, m: declaringType.Module, skipVisibility: true);
        var il = dm.GetILGenerator();
        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_0);    // object[]
            il.Emit(OpCodes.Ldc_I4, i);  // index
            il.Emit(OpCodes.Ldelem_Ref); // args[i]
            var pType = paramTypes[i];
            if (pType.IsValueType) il.Emit(OpCodes.Unbox_Any, pType); else il.Emit(OpCodes.Castclass, pType);
        }
        il.Emit(OpCodes.Newobj, ctor);
        if (declaringType.IsValueType) il.Emit(OpCodes.Box, declaringType);
        il.Emit(OpCodes.Ret);
        return (FactoryFunction)dm.CreateDelegate(typeof(FactoryFunction));
    }

    /// <summary>Invokes the underlying function with the specified parameters.</summary>
    /// <remarks>
    /// Each element in the <paramref name="parameters"/> array must correspond to the type and position of the function's parameters. Null values are not
    /// allowed for parameters of non-nullable value types.
    /// </remarks>
    /// <param name="parameters">An array of arguments to pass to the function. The number and types of elements must match the expected parameter types.</param>
    /// <returns>The result returned by the invoked function.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="parameters"/> is null or if any element corresponding to a non-nullable value type parameter is null.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if the number of elements in <paramref name="parameters"/> does not match the expected parameter count.</exception>
    public object CreateFast(object?[] parameters)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        if (parameters.Length != ParamTypes.Length) throw new ArgumentException($"Parameter count mismatch. Expected {ParamTypes.Length}, got {parameters.Length}.", nameof(parameters));
        for (var i = 0; i < ParamTypes.Length; i++)
        {
            if (parameters[i] == null)
            {
                var t = ParamTypes[i];
                if (t.IsValueType && Nullable.GetUnderlyingType(t) == null) throw new ArgumentNullException($"parameters[{i}]", $"Null not allowed for value type parameter '{t.FullName}'.");
            }
        }
        return Function(parameters);
    }

    /// <summary>Delegate type for the factory function that creates instances of the target type using the cached constructor.</summary>
    public delegate object FactoryFunction(object?[] parameters);

    /// <summary>Gets the delegate used to create new instances of the target type's constructor.</summary>
    public readonly FactoryFunction Function;

    /// <summary>Gets the <see cref="ConstructorInfo"/> object representing the cached constructor</summary>
    public readonly ConstructorInfo Constructor;

    /// <summary>Gets the <see cref="Type"/> object representing the type that declares the cached constructor.</summary>
    public readonly Type DeclaringType;

    /// <summary>Gets the array of <see cref="Type"/> objects representing the parameter types of the cached constructor.</summary>
    public readonly Type[] ParamTypes;

    /// <summary>Initializes a new instance of the ConstructorCache class for the specified constructor.</summary>
    /// <remarks>This constructor validates that the provided constructor is suitable for activation and does not support constructors with ref or out parameters.</remarks>
    /// <param name="ctor">The constructor metadata to cache. Must not be null and must have a declaring type.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctor"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="ctor"/> does not have a declaring type.</exception>
    /// <exception cref="NotSupportedException">Thrown if the constructor has any ref or out parameters.</exception>
    public ConstructorCache(ConstructorInfo ctor)
    {
        if (ctor == null) throw new ArgumentNullException(nameof(ctor));
        if (ctor.DeclaringType == null) throw new ArgumentException("ctor has no DeclaringType", nameof(ctor));
        Constructor = ctor;
        DeclaringType = ctor.DeclaringType;
        var ps = ctor.GetParameters();
        ParamTypes = new Type[ps.Length];
        for (var i = 0; i < ps.Length; i++)
        {
            var pt = ps[i].ParameterType;
            if (pt.IsByRef) throw new NotSupportedException("ref/out parameters are not supported for constructor activation.");
            ParamTypes[i] = pt;
        }
        Function = BuildFactory(DeclaringType, Constructor, ParamTypes);
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Cave.IO;

/// <summary>
/// Provides a cache for method information and a delegate for invoking the method with arguments, allowing for fast method invocation without the overhead of
/// reflection on each call. The cached delegate handles both instance and static methods, and can return the result as an object (boxed if it's a value type)
/// for uniform handling of return values.
/// </summary>
public sealed class MethodCache
{
    #region Delegates

    /// <summary>Defines a delegate type for invoking the cached method with a target object and an array of arguments, returning the result as an object.</summary>
    public delegate object? MethodFunction(object? target, object?[] args);

    #endregion Delegates

    #region Private Methods

    MethodFunction Build()
    {
        var dm = new DynamicMethod(
            "MethodCache_" + DeclaringType.Name,
            typeof(object),
            new[] { typeof(object), typeof(object[]) },
            DeclaringType.Module,
            skipVisibility: true);

        var il = dm.GetILGenerator();

        EmitLoadArgs(il, Method, DeclaringType, ParameterTypes);

        il.Emit(Method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, Method);

        if (ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Ldnull);
        }
        else if (ReturnType.IsValueType)
        {
            il.Emit(OpCodes.Box, ReturnType);
        }

        il.Emit(OpCodes.Ret);

        return (MethodFunction)dm.CreateDelegate(typeof(MethodFunction));
    }

    static void EmitLoadArgs(ILGenerator il, MethodInfo method, Type declaringType, Type[] paramTypes)
    {
        if (!method.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0);
            if (declaringType.IsValueType)
            {
                il.Emit(OpCodes.Unbox, declaringType);
                if (method.IsVirtual || method.DeclaringType!.IsInterface)
                {
                    il.Emit(OpCodes.Constrained, declaringType);
                }
            }
            else
            {
                il.Emit(OpCodes.Castclass, declaringType);
            }
        }

        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);

            var t = paramTypes[i];
            if (t.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, t);
            }
            else
            {
                il.Emit(OpCodes.Castclass, t);
            }
        }
    }

    #endregion Private Methods

    #region Protected Constructors

    /// <summary>Initializes a new instance of the <see cref="MethodCache"/> class with the specified method.</summary>
    public MethodCache(MethodInfo method)
    {
        Method = method;
        DeclaringType = method.DeclaringType!;
        ParameterTypes = [.. method.GetParameters().Select(p => p.ParameterType)];
        ReturnType = method.ReturnType;
        Function = Build();
    }

    #endregion Protected Constructors

    #region Protected Methods

    /// <summary>Emits the IL instructions to load the arguments for the cached method.</summary>
    /// <param name="il">The IL generator used to emit the instructions.</param>
    /// <param name="declaringType">The type that declares the cached method.</param>
    /// <param name="paramTypes">The types of the parameters for the cached method.</param>
    static void EmitLoadArgs(ILGenerator il, Type declaringType, Type[] paramTypes)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, declaringType);

        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);

            var t = paramTypes[i];
            if (t.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, t);
            }
            else
            {
                il.Emit(OpCodes.Castclass, t);
            }
        }
    }

    #endregion Protected Methods

    #region Fields

    /// <summary>Gets the type that declares the cached method.</summary>
    public readonly Type DeclaringType;

    /// <summary>Gets the cached delegate for the method with return value, which returns the result as an object (boxed if it's a value type).</summary>
    public readonly MethodFunction Function;

    /// <summary>Represents the metadata information for the cached method. attributes.</summary>
    public readonly MethodInfo Method;

    /// <summary>Gets the types of the parameters associated with the current member.</summary>
    public readonly Type[] ParameterTypes;

    /// <summary>
    /// Gets the return type of the cached method, used to determine if boxing is necessary when invoking the method and returning the result as an object.
    /// </summary>
    public readonly Type ReturnType;

    #endregion Fields

    #region Public Methods

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MethodCache other && Method.Equals(other.Method);

    /// <inheritdoc/>
    public override int GetHashCode() => Method.GetHashCode();

    /// <summary>Invokes the represented method or operation on the specified target object using the provided arguments.</summary>
    /// <remarks>
    /// If the method is static, the target parameter should be null. If the method requires parameters, the args array must contain the appropriate values in
    /// the correct order. Exceptions thrown by the invoked method will be propagated to the caller.
    /// </remarks>
    /// <param name="target">The object on which to invoke the method. This must be an instance of the declaring type, or null for static methods.</param>
    /// <param name="args">An array of arguments to pass to the method. The number, order, and type of the elements must match the method's parameters.</param>
    /// <returns>The return value of the invoked method, or null if the method has no return value.</returns>
    public object? InvokeFast(object? target, object?[] args) => Function(target, args);

    #endregion Public Methods
}

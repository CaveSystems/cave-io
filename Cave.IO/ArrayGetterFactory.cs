using System;
using System.Reflection.Emit;

namespace Cave.IO;

/// <summary>Provides a cache for fast array element access using dynamically generated methods to avoid the overhead of reflection on each access.</summary>
public static class ArrayGetterFactory
{
    #region Public Methods

    /// <summary>Creates a dynamic method that retrieves an element from an array at a specified index.</summary>
    /// <param name="elementType">The type of elements in the array.</param>
    /// <returns>A compiled delegate that gets an array element and returns it as <see cref="object"/>.</returns>
    public static ArrayGetter CreateGetter(Type elementType)
    {
        var dm = new DynamicMethod(
            "ArrayGetter_" + elementType.Name,
            typeof(object),
            [typeof(Array), typeof(int)],
            typeof(ArrayGetterFactory).Module,
            skipVisibility: true);

        var il = dm.GetILGenerator();

        // array
        il.Emit(OpCodes.Ldarg_0);
        // index
        il.Emit(OpCodes.Ldarg_1);

        // Referenztypen → Ldelem_Ref
        if (!elementType.IsValueType)
        {
            il.Emit(OpCodes.Ldelem_Ref);
        }
        else
        {
            // ValueTypes → Ldelem <T>
            il.Emit(OpCodes.Ldelem, elementType);
            il.Emit(OpCodes.Box, elementType);
        }

        il.Emit(OpCodes.Ret);

        return (ArrayGetter)dm.CreateDelegate(typeof(ArrayGetter));
    }

    #endregion Public Methods
}

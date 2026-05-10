using System;
using System.Diagnostics;
using System.Reflection;
using Cave.Logging;

namespace Cave.IO.Blob.Converters;

[DebuggerDisplay("{ArrayType.Name}")]
record BlobEnumerableConverterData : BaseRecord
{
    public BlobEnumerableConverterData(Type elementType, ConstructorInfo? constructor)
    {
        ElementType = elementType;
        Constructor = constructor;
        ArrayType = ElementType.MakeArrayType();
        try
        {
            BlobSerializer.GetPrimitiveType(elementType, out var elementPrimitiveType);
            if (elementPrimitiveType != default)
            {
                var array = Array.CreateInstance(elementType, 0);
                BlobSerializer.GetPrimitiveType(ArrayType, out PrimitiveType);
            }
        }
        catch 
        {
            Trace.TraceWarning("Failed to create array instance or get primitive type.");
            Debugger.Break(); 
        }
    }

    internal readonly Type ElementType;
    internal readonly ConstructorInfo? Constructor;
    internal readonly BlobPrimitiveType PrimitiveType;
    internal readonly Type ArrayType;
}

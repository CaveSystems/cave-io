using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobDictionaryConverterState
{
    #region Fields

    internal readonly ConstructorCache? Constructor;
    internal readonly BlobConverterBundle KeyBundle;
    internal readonly PropertyInfo KeyProperty;
    internal readonly Type KeyValuePairType;
    internal readonly BlobDictionaryConverterMode Mode;
    internal readonly BlobConverterBundle ValueBundle;
    internal readonly PropertyInfo ValueProperty;
    internal MethodCache? DictionaryAddMethod;
    internal Type? DictionaryType;
    internal bool ValueCanBeNull;
    internal ConstructorCache KeyValuePairConstructor;

    #endregion Fields

    #region Public Constructors

    public BlobDictionaryConverterState(ConstructorInfo? cctor, BlobConverterBundle keyBundle, BlobConverterBundle valueBundle, BlobDictionaryConverterMode mode)
    {
        Constructor = cctor is null ? null : new ConstructorCache(cctor);
        KeyBundle = keyBundle;
        ValueBundle = valueBundle;
        Mode = mode;
        //--- additional properties
        KeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(KeyBundle.Type, ValueBundle.Type);
        KeyProperty = KeyValuePairType.GetProperty("Key")!;
        ValueProperty = KeyValuePairType.GetProperty("Value")!;
        ValueCanBeNull = !valueBundle.Type.IsValueType;
        KeyValuePairConstructor = new ConstructorCache(KeyValuePairType.GetConstructor([KeyBundle.Type, ValueBundle.Type]) ?? throw new InvalidOperationException($"Could not create {KeyValuePairType.ToShortName()}!"));
    }

    #endregion Public Constructors
}

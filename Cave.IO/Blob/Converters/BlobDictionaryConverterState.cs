using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobDictionaryConverterState
{
    #region Fields

    readonly internal ConstructorInfo Constructor;
    internal readonly BlobConverterBundle KeyBundle;
    readonly internal PropertyInfo KeyProperty;
    readonly internal Type KeyValuePairType;
    readonly internal BlobDictionaryConverterMode Mode;
    internal readonly BlobConverterBundle ValueBundle;
    readonly internal PropertyInfo ValueProperty;
    internal MethodInfo? DictionaryAddMethod;
    internal Type? DictionaryType;
    internal bool ValueCanBeNull;

    #endregion Fields

    #region Public Constructors

    public BlobDictionaryConverterState(ConstructorInfo cctor, BlobConverterBundle keyBundle, BlobConverterBundle valueBundle, BlobDictionaryConverterMode mode)
    {
        Constructor = cctor;
        KeyBundle = keyBundle;
        ValueBundle = valueBundle;
        Mode = mode;
        //--- additional properties
        KeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(KeyBundle.Type, ValueBundle.Type);
        KeyProperty = KeyValuePairType.GetProperty("Key")!;
        ValueProperty = KeyValuePairType.GetProperty("Value")!;
        ValueCanBeNull = !valueBundle.Type.IsValueType || Nullable.GetUnderlyingType(valueBundle.Type) != null;
    }

    #endregion Public Constructors
}

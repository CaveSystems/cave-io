using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobDictionaryConverterState
{
    #region Fields

    internal readonly ConstructorInfo Constructor;
    internal readonly BlobConverterBundle KeyBundle;
    internal readonly PropertyInfo KeyProperty;
    internal readonly Type KeyValuePairType;
    internal readonly BlobDictionaryConverterMode Mode;
    internal readonly BlobConverterBundle ValueBundle;
    internal readonly PropertyInfo ValueProperty;
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
        ValueCanBeNull = !valueBundle.Type.IsValueType;
    }

    #endregion Public Constructors
}

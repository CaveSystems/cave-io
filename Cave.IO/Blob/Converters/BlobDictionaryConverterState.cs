using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobDictionaryConverterState
{
    static MethodInfo explodeDictionaryMethod = typeof(BlobDictionaryConverterState).GetMethod(nameof(ExplodeDictionary)) ?? throw new InvalidOperationException($"Method {nameof(ExplodeDictionary)} not found!");
    static MethodInfo explodeCollectionMethod = typeof(BlobDictionaryConverterState).GetMethod(nameof(ExplodeCollection)) ?? throw new InvalidOperationException($"Method {nameof(ExplodeCollection)} not found!");

    public static void ExplodeDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict, out TKey[] keys, out TValue[] values) { keys = dict.Keys.ToArray(); values = dict.Values.ToArray(); }

    public static void ExplodeCollection<TKey, TValue>(ICollection<KeyValuePair<TKey, TValue>> items, out TKey[] keys, out TValue[] values)
    {
        var count = items.Count;
        keys = new TKey[count];
        values = new TValue[count];

        var i = 0;
        foreach (var kv in items)
        {
            keys[i] = kv.Key;
            values[i] = kv.Value;
            i++;
        }
    }

    delegate void ExplodeDictionaryDelegate<TKey, TValue>(IDictionary<TKey, TValue> dict, out TKey[] keys, out TValue[] values);

    delegate void ExplodeCollectionDelegate<TKey, TValue>(ICollection<KeyValuePair<TKey, TValue>> items, out TKey[] keys, out TValue[] values);

    static Delegate GetExplodeDictionaryDelegate(Type keyType, Type valueType)
    {
        var method = explodeDictionaryMethod.MakeGenericMethod(keyType, valueType);
        var delegateType = typeof(ExplodeDictionaryDelegate<,>).MakeGenericType(keyType, valueType);
        return Delegate.CreateDelegate(delegateType, method);
    }

    static Delegate GetExplodeCollectionDelegate(Type keyType, Type valueType)
    {
        var method = explodeCollectionMethod.MakeGenericMethod(keyType, valueType);
        var delegateType = typeof(ExplodeCollectionDelegate<,>).MakeGenericType(keyType, valueType);
        return Delegate.CreateDelegate(delegateType, method);
    }


    #region Fields

    internal readonly BlobDictionaryConverterData Data;
    internal readonly ConstructorCache? Constructor;
    internal readonly BlobConverterBundle KeyArrayBundle;
    internal readonly PropertyInfo KeyProperty;
    internal readonly Type KeyValuePairType;
    internal readonly BlobConverterBundle ValueArrayBundle;
    internal readonly PropertyInfo ValueProperty;
    internal MethodCache? DictionaryAddMethod;
    internal Type? DictionaryType;
    internal bool ValueCanBeNull;
    internal ConstructorCache KeyValuePairConstructor;
    internal readonly Delegate? ExplodeDelegate;
    internal readonly Type ListType;

    #endregion Fields

    #region Public Constructors

    public BlobDictionaryConverterState(BlobDictionaryConverterData data, BlobConverterBundle keyArrayBundle, BlobConverterBundle valueArrayBundle)
    {
        Data = data;
        Constructor = data.Constructor is null ? null : new ConstructorCache(data.Constructor);
        KeyArrayBundle = keyArrayBundle;
        ValueArrayBundle = valueArrayBundle;
        //--- additional properties
        KeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(data.KeyType, data.ValueType);
        KeyProperty = KeyValuePairType.GetProperty("Key")!;
        ValueProperty = KeyValuePairType.GetProperty("Value")!;
        ValueCanBeNull = !valueArrayBundle.Type.IsValueType;
        KeyValuePairConstructor = new ConstructorCache(KeyValuePairType.GetConstructor([data.KeyType, data.ValueType]) ?? throw new InvalidOperationException($"Could not create {KeyValuePairType.ToShortName()}!"));
        ListType = typeof(List<>).MakeGenericType(KeyValuePairType);
        ExplodeDelegate = data.Mode switch
        {
            BlobDictionaryConverterMode.UseIDictionary => GetExplodeDictionaryDelegate(data.KeyType, data.ValueType),
            BlobDictionaryConverterMode.UseArray or BlobDictionaryConverterMode.UseIEnumerable => GetExplodeCollectionDelegate(data.KeyType, data.ValueType),
            _ => throw new NotImplementedException($"Cannot handle this type of collection: {data.Mode}")
        };
    }

    #endregion Public Constructors
}

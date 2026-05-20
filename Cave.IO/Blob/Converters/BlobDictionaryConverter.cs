using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Provides a converter for dictionary-like types for blob serialization and deserialization.</summary>
public class BlobDictionaryConverter : BlobConverterBase
{
    #region Private Methods

    /// <summary>Gets key and value types, suitable constructor, and mode for a dictionary-like type.</summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="data">Detected converter data.</param>
    /// <returns>True if suitable constructor and types found, otherwise false.</returns>
    static bool GetElementTypesAndConstructor(Type type, out BlobDictionaryConverterData? data)
    {
        //test if is IDictionary and has empty constructor
        {
            var hasParameterlessCtor = type.GetConstructor(Type.EmptyTypes) != null;
            if (hasParameterlessCtor)
            {
                var dictInterface = type.GetInterfaces().FirstOrDefault(
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                if (dictInterface != null)
                {
                    var args = dictInterface.GetGenericArguments();
                    data = new BlobDictionaryConverterData(null, args[0], args[1], BlobDictionaryConverterMode.UseIDictionary);
                    return true;
                }
            }
        }
        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length != 1) continue;

            var paramType = parameters[0].ParameterType;

            // Array: KeyValuePair<TKey, TValue>[]
            if (paramType.IsArray)
            {
                var elemType = paramType.GetElementType();
                if (elemType?.IsGenericType == true && elemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = elemType.GetGenericArguments();
                    data = new BlobDictionaryConverterData(ctor, args[0], args[1], BlobDictionaryConverterMode.UseArray);
                    return true;
                }
            }

            {
                // IDictionary<TKey, TValue>
                var dictInterface = paramType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
                if (dictInterface != null)
                {
                    var args = dictInterface.GetGenericArguments();
                    data = new BlobDictionaryConverterData(ctor, args[0], args[1], BlobDictionaryConverterMode.UseIDictionary);
                    return true;
                }
            }

            {
                // IEnumerable<KeyValuePair<TKey, TValue>>
                var elemType = GetIEnumerableElementType(paramType);
                if (elemType != null && elemType.IsGenericType && elemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = elemType.GetGenericArguments();
                    data = new BlobDictionaryConverterData(ctor, args[0], args[1], BlobDictionaryConverterMode.UseIEnumerable);
                    return true;
                }
            }
        }

        data = null;
        return false;
    }

    /// <summary>Gets the element type of an IEnumerable&lt;T&gt; type.</summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Element type if found, otherwise null.</returns>
    static Type? GetIEnumerableElementType(Type type)
    {
        // direct IEnumerable<T> check
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        // check via implemented interfaces
        var enumInterface = type.GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumInterface?.GetGenericArguments()[0];
    }

    #endregion Private Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type)
    {
        var isDictionaryLike =
            typeof(IDictionary).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
#endif
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));

        if (isDictionaryLike && GetElementTypesAndConstructor(type, out var data))
        {
            return data;
        }
        return null;
    }

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type)
    {
        GetHandlingData(type, out BlobDictionaryConverterData data);
        return [data.KeyType, data.ValueType];
    }

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        if (bundle.State is not BlobDictionaryConverterState myState) throw new InvalidOperationException("Invalid state for dictionary converter.");
        var keys = (Array)myState.KeyArrayBundle.Converter.ReadContent(state, myState.KeyArrayBundle);
        var values = (Array)myState.ValueArrayBundle.Converter.ReadContent(state, myState.ValueArrayBundle);
        if (values.Length != keys.Length) throw new InvalidOperationException($"Value array length {values.Length} does not match key array length {keys.Length}.");
        switch (myState.Data.Mode)
        {
            case BlobDictionaryConverterMode.UseIEnumerable:
            case BlobDictionaryConverterMode.UseArray:
            {
                var count = keys.Length;
                var array = Array.CreateInstance(myState.KeyValuePairType, count);
                for (var i = 0; i < count; i++)
                {
                    var value = myState.ValueGetter!(values, i);
                    var key = myState.KeyGetter!(keys, i);
                    var keyValuePair = myState.KeyValuePairConstructor.CreateFast([key, value])!;
                    array.SetValue(keyValuePair, i);
                }
                return myState.Constructor is null ? array : myState.Constructor.CreateFast([array]);
            }
            case BlobDictionaryConverterMode.UseIDictionary:
            {
                myState.DictionaryType ??= myState.Constructor is null ? bundle.Type : typeof(Dictionary<,>).MakeGenericType(myState.Data.KeyType, myState.Data.ValueType);
                myState.DictionaryAddMethod ??= new MethodCache(myState.DictionaryType.GetMethod("Add") ??
                    throw new InvalidOperationException($"Dictionary type {myState.DictionaryType.ToShortName()} does not have an Add method."));
                var dictionary = TypeActivator.CreateFast(myState.DictionaryType)!;
                var count = keys.Length;
                for (var i = 0; i < count; i++)
                {
                    var value = myState.ValueGetter!(values, i);
                    var key = myState.KeyGetter!(keys, i);
                    myState.DictionaryAddMethod.InvokeFast(dictionary, [key, value]);
                }
                return myState.Constructor is null ? dictionary : myState.Constructor.CreateFast([dictionary]);
            }
            default: throw new NotImplementedException($"Mode {myState.Data.Mode} is not implemented.");
        }
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState readerState, BlobConverterBundle bundle)
    {
        var reader = readerState.Reader;
        GetHandlingData(bundle.Type, out BlobDictionaryConverterData dictData);
        var keyArrayBundle = readerState.ReadConverter();
        var valueArrayBundle = readerState.ReadConverter();
        var keyElementType = keyArrayBundle.Type.GetElementType()!;
        if (!dictData.KeyType.IsAssignableFrom(keyElementType))
        {
            throw new InvalidOperationException($"Key type in stream {keyArrayBundle.Type.ToShortName()} is not compatible with expected type {dictData.KeyType.ToShortName()}.");
        }
        var valueElementType = valueArrayBundle.Type.GetElementType()!;
        if (!dictData.ValueType.IsAssignableFrom(valueElementType))
        {
            throw new InvalidOperationException($"Value type in stream {valueArrayBundle.Type.ToShortName()} is not compatible with expected type {dictData.ValueType.ToShortName()}.");
        }
        bundle.State = new BlobDictionaryConverterState(dictData, keyArrayBundle, valueArrayBundle)
        {
            KeyGetter = readerState.Serializer.ArrayGetterCache.Get(keyElementType),
            ValueGetter = readerState.Serializer.ArrayGetterCache.Get(valueElementType),
        };
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobDictionaryConverterState myState) throw new InvalidOperationException("Invalid state for dictionary converter.");
        var writer = state.Writer;

        if (myState.Data.Mode == BlobDictionaryConverterMode.UseIEnumerable && instance is not ICollection)
        {
            var list = (IList)Activator.CreateInstance(myState.ListType)!;
            foreach (var item in (IEnumerable)instance)
            {
                list.Add(item);
            }
            instance = list;
        }
        var args = new[] { instance, null, null };
        myState.ExplodeDelegate?.DynamicInvoke(args);
        var keys = args[1];
        var values = args[2];
        myState.KeyArrayBundle.Converter.WriteContent(state, myState.KeyArrayBundle, keys!);
        myState.ValueArrayBundle.Converter.WriteContent(state, myState.ValueArrayBundle, values!);
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobDictionaryConverterData data);
        var keyArrayBundle = state.WriteConverter(data.KeyType.MakeArrayType());
        var valueArrayBundle = state.WriteConverter(data.ValueType.MakeArrayType());
        bundle.State = new BlobDictionaryConverterState(data, keyArrayBundle, valueArrayBundle);
    }
}

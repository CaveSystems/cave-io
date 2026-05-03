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
        var reader = state.Reader;
        var count = reader.Read7BitEncodedInt32();
        switch (myState.Mode)
        {
            case BlobDictionaryConverterMode.UseIEnumerable:
            case BlobDictionaryConverterMode.UseArray:
            {
                var array = Array.CreateInstance(myState.KeyValuePairType, count);
                for (var i = 0; i < count; i++)
                {
                    var key = myState.KeyBundle.Converter.ReadContent(state, myState.KeyBundle);
                    var isNull = myState.ValueCanBeNull && reader.ReadBool();
                    var value = isNull ? null : myState.ValueBundle.Converter.ReadContent(state, myState.ValueBundle);
                    var keyValuePair = myState.KeyValuePairConstructor.CreateFast([key, value])!;
                    array.SetValue(keyValuePair, i);
                }
                return myState.Constructor is null ? array : myState.Constructor.CreateFast([array]);
            }
            case BlobDictionaryConverterMode.UseIDictionary:
            {
                myState.DictionaryType ??= myState.Constructor is null ? bundle.Type : typeof(Dictionary<,>).MakeGenericType(myState.KeyBundle.Type, myState.ValueBundle.Type);
                myState.DictionaryAddMethod ??= new MethodCache(myState.DictionaryType.GetMethod("Add") ??
                    throw new InvalidOperationException($"Dictionary type {myState.DictionaryType.ToShortName()} does not have an Add method."));
                var dictionary = TypeActivator.CreateFast(myState.DictionaryType)!;
                for (var i = 0; i < count; i++)
                {
                    var key = myState.KeyBundle.Converter.ReadContent(state, myState.KeyBundle);
                    var isNull = myState.ValueCanBeNull && reader.ReadBool();
                    var value = isNull ? null : myState.ValueBundle.Converter.ReadContent(state, myState.ValueBundle);
                    myState.DictionaryAddMethod.InvokeFast(dictionary, [key, value]);
                }
                return myState.Constructor is null ? dictionary : myState.Constructor.CreateFast([dictionary]);
            }
            default: throw new NotImplementedException($"Mode {myState.Mode} is not implemented.");
        }
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState readerState, BlobConverterBundle bundle)
    {
        var reader = readerState.Reader;
        GetHandlingData(bundle.Type, out BlobDictionaryConverterData dictData);
        var keyBundle = readerState.ReadConverter();
        var valueBundle = readerState.ReadConverter();
        if (!dictData.KeyType.IsAssignableFrom(keyBundle.Type))
        {
            throw new InvalidOperationException($"Key type in stream {keyBundle.Type.ToShortName()} is not compatible with expected type {dictData.KeyType.ToShortName()}.");
        }
        if (!dictData.ValueType.IsAssignableFrom(valueBundle.Type))
        {
            throw new InvalidOperationException($"Value type in stream {valueBundle.Type.ToShortName()} is not compatible with expected type {dictData.ValueType.ToShortName()}.");
        }
        bundle.State = new BlobDictionaryConverterState(dictData.Constructor, keyBundle, valueBundle, dictData.Mode);
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobDictionaryConverterState myState) throw new InvalidOperationException("Invalid state for dictionary converter.");
        var writer = state.Writer;
        switch (myState.Mode)
        {
            case BlobDictionaryConverterMode.UseIDictionary:
            {
                if (instance is not IDictionary dict) throw new InvalidOperationException($"Expected IDictionary instance for dictionary converter, got {instance.GetType().ToShortName()}.");
                writer.Write7BitEncoded32(dict.Count);
                state.Logger?.Verbose($"Write dictionary of {myState.KeyValuePairType.ToShortName()} with {dict.Count} items.");
                foreach (DictionaryEntry entry in dict)
                {
                    myState.KeyBundle.Converter.WriteContent(state, myState.KeyBundle, entry.Key);
                    if (myState.ValueCanBeNull)
                    {
                        var isNull = entry.Value == null;
                        writer.Write(isNull);
                        if (isNull) continue;
                    }
                    myState.ValueBundle.Converter.WriteContent(state, myState.ValueBundle, entry.Value!);
                }
                break;
            }
            case BlobDictionaryConverterMode.UseArray:
            {
                if (instance is not IList list) throw new InvalidOperationException($"Expected IList instance for dictionary converter, got {instance.GetType().ToShortName()}.");
                writer.Write7BitEncoded32(list.Count);
                state.Logger?.Verbose($"Write array of {myState.KeyValuePairType.ToShortName()} with {list.Count} items.");
                foreach (var entry in list)
                {
                    var key = myState.KeyProperty.GetValue(entry);
                    var value = myState.ValueProperty.GetValue(entry);
                    myState.KeyBundle.Converter.WriteContent(state, myState.KeyBundle, key!);
                    if (myState.ValueCanBeNull)
                    {
                        var isNull = value == null;
                        writer.Write(isNull);
                        if (isNull) continue;
                    }
                    myState.ValueBundle.Converter.WriteContent(state, myState.ValueBundle, value!);
                }
                break;
            }
            case BlobDictionaryConverterMode.UseIEnumerable:
            {
                if (instance is not IEnumerable enumerable) throw new InvalidOperationException($"Expected IEnumerable instance for dictionary converter, got {instance.GetType().ToShortName()}.");
                var list = enumerable.Cast<object>().ToList();
                state.Logger?.Verbose($"Warning: Slow write array of {myState.KeyValuePairType.ToShortName()} with {list.Count} items because IDictionary is not implemented!");
                writer.Write7BitEncoded32(list.Count);
                foreach (var entry in list)
                {
                    var key = myState.KeyProperty.GetValue(entry);
                    var value = myState.ValueProperty.GetValue(entry);
                    myState.KeyBundle.Converter.WriteContent(state, myState.KeyBundle, key!);
                    if (myState.ValueCanBeNull)
                    {
                        var isNull = value == null;
                        writer.Write(isNull);
                        if (isNull) continue;
                    }
                    myState.ValueBundle.Converter.WriteContent(state, myState.ValueBundle, value!);
                }
                break;
            }
            default: throw new NotImplementedException("Cannot handle this type of collection.");
        }
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobDictionaryConverterData data);
        var keyBundle = state.WriteConverter(data.KeyType);
        var valueBundle = state.WriteConverter(data.ValueType);
        bundle.State = new BlobDictionaryConverterState(data.Constructor, keyBundle, valueBundle, data.Mode);
    }
}

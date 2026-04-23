using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Provides a converter for dictionary-like types for blob serialization and deserialization.</summary>
public class BlobDictionaryConverter : IBlobConverter
{
    #region Private Methods

    /// <summary>Gets key and value types, suitable constructor, and mode for a dictionary-like type.</summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="keyType">Detected key type.</param>
    /// <param name="valueType">Detected value type.</param>
    /// <param name="constructor">Detected constructor.</param>
    /// <param name="mode">Detected converter mode.</param>
    /// <returns>True if suitable constructor and types found, otherwise false.</returns>
    static bool GetElementTypesAndConstructor(Type type, [MaybeNullWhen(false)] out Type keyType, [MaybeNullWhen(false)] out Type valueType, [MaybeNullWhen(false)] out ConstructorInfo constructor, out BlobDictionaryConverterMode mode)
    {
        foreach (var ctor in type.GetConstructors())
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length != 1) continue;

            var paramType = parameters[0].ParameterType;

            // KeyValuePair<TKey, TValue>[]
            if (paramType.IsArray)
            {
                var elemType = paramType.GetElementType();
                if (elemType?.IsGenericType == true &&
                    elemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = elemType.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    constructor = ctor;
                    mode = BlobDictionaryConverterMode.UseArray;
                    return true;
                }
            }

            // IDictionary<TKey, TValue> / IReadOnlyDictionary<TKey, TValue>
            var dictInterface = paramType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && (
#if NET45_OR_GREATER
                i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
#endif
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                ));

            if (dictInterface != null)
            {
                var args = dictInterface.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                constructor = ctor;
                mode = BlobDictionaryConverterMode.UseDictionary;
                return true;
            }

            // IEnumerable<KeyValuePair<TKey, TValue>>
            {
                var elemType = GetIEnumerableElementType(paramType);
                if (elemType != null &&
                    elemType.IsGenericType &&
                    elemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = elemType.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    constructor = ctor;
                    mode = BlobDictionaryConverterMode.UseDictionary;
                    return true;
                }
            }
        }

        mode = default;
        keyType = null;
        valueType = null;
        constructor = null;
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

    #region Public Methods

    /// <inheritdoc/>
    public virtual bool CanHandle(Type type)
    {
        var isDictionaryLike =
            typeof(IDictionary).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (
#if NET45_OR_GREATER
                i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
#endif
                i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));

        return isDictionaryLike && GetElementTypesAndConstructor(type, out _, out _, out _, out _);
    }

    /// <inheritdoc/>
    public virtual object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        if (bundle.State is not BlobDictionaryConverterState myState) throw new InvalidOperationException("Invalid state for dictionary converter.");
        var reader = state.Reader;
        var count = reader.Read7BitEncodedInt32();
        switch (myState.Mode)
        {
            case BlobDictionaryConverterMode.UseEnumerable:
            case BlobDictionaryConverterMode.UseArray:
            {
                var array = Array.CreateInstance(myState.KeyValuePairType, count);
                for (var i = 0; i < count; i++)
                {
                    var key = myState.KeyBundle.Converter.ReadContent(state, myState.KeyBundle);
                    var isNull = myState.ValueCanBeNull && reader.ReadBool();
                    var value = isNull ? null : myState.ValueBundle.Converter.ReadContent(state, myState.ValueBundle);
                    var keyValuePair = Activator.CreateInstance(myState.KeyValuePairType, key, value)!;
                    array.SetValue(keyValuePair, i);
                }
                return myState.Constructor.Invoke([array]);
            }
            case BlobDictionaryConverterMode.UseDictionary:
            {
                myState.DictionaryType ??= typeof(Dictionary<,>).MakeGenericType(myState.KeyBundle.Type, myState.ValueBundle.Type);
                var addMethod = myState.DictionaryAddMethod ??= myState.DictionaryType.GetMethod("Add") ?? throw new InvalidOperationException($"Dictionary type {myState.DictionaryType.FullName} does not have an Add method.");
                var dictionary = Activator.CreateInstance(myState.DictionaryType)!;
                for (var i = 0; i < count; i++)
                {
                    var key = myState.KeyBundle.Converter.ReadContent(state, myState.KeyBundle);
                    var isNull = myState.ValueCanBeNull && reader.ReadBool();
                    var value = isNull ? null : myState.ValueBundle.Converter.ReadContent(state, myState.ValueBundle);
                    addMethod.Invoke(dictionary, [key, value]);
                }
                return myState.Constructor.Invoke([dictionary]);
            }
            default: throw new NotImplementedException($"Mode {myState.Mode} is not implemented.");
        }
    }

    /// <inheritdoc/>
    public virtual void ReadInitialization(IBlobReaderState readerState, BlobConverterBundle bundle)
    {
        var reader = readerState.Reader;
        if (!GetElementTypesAndConstructor(bundle.Type, out var keyTypePresent, out var valueTypePresent, out var cctor, out var mode))
        {
            throw new InvalidOperationException($"Type {bundle.Type.FullName} does not have a suitable constructor for deserialization!");
        }
        var keyBundle = readerState.ReadConverter();
        var valueBundle = readerState.ReadConverter();
        if (!keyTypePresent.IsAssignableFrom(keyBundle.Type))
        {
            throw new InvalidOperationException($"Key type in stream {keyBundle.Type.FullName} is not compatible with expected type {keyTypePresent.FullName}.");
        }
        if (!valueTypePresent.IsAssignableFrom(valueBundle.Type))
        {
            throw new InvalidOperationException($"Value type in stream {valueBundle.Type.FullName} is not compatible with expected type {valueTypePresent.FullName}.");
        }
        bundle.State = new BlobDictionaryConverterState(cctor, keyBundle, valueBundle, mode);
    }

    /// <inheritdoc/>
    public virtual void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobDictionaryConverterState myState) throw new InvalidOperationException("Invalid state for dictionary converter.");
        var writer = state.Writer;
        if (instance is IDictionary dict)
        {
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
        }
        else if (instance is IEnumerable enumerable)
        {
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
        }
        else throw new NotImplementedException("Cannot handle this type of collection.");
    }

    /// <inheritdoc/>
    public virtual void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        if (!GetElementTypesAndConstructor(bundle.Type, out var keyType, out var valueType, out var cctor, out var mode))
        {
            throw new InvalidOperationException($"Type {bundle.Type.FullName} does not have a suitable constructor for deserialization!");
        }

        var keyBundle = state.WriteConverter(keyType);
        var valueBundle = state.WriteConverter(valueType);
        bundle.State = new BlobDictionaryConverterState(cctor, keyBundle, valueBundle, mode);
    }

    #endregion Public Methods
}

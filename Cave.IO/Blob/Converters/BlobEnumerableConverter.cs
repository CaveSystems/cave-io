using System;
using System.Collections;
using System.Collections.Generic;
using Cave.Collections;

namespace Cave.IO.Blob.Converters;

/// <summary>Converter for enumerable types supporting serialization and deserialization of collections.</summary>
public class BlobEnumerableConverter : BlobConverterBase
{
    #region Private Methods

    /// <summary>Gets the element type and suitable constructor for a collection type.</summary>
    /// <param name="type">Collection type.</param>
    /// <param name="data">Output data containing element type and constructor info if found.</param>
    /// <returns>True if a suitable constructor is found; otherwise, false.</returns>
    static bool GetElementTypeAndConstructor(Type type, out BlobEnumerableConverterData? data)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? throw new InvalidOperationException($"Array {type.ToShortName()} has no element type.");
            if (elementType != null && elementType != typeof(object))
            {
                data = new BlobEnumerableConverterData(elementType, null);
                return true;
            }
        }

        foreach (var ctor in type.GetConstructors())
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length != 1)
                continue;

            var param = parameters[0];
            var paramType = param.ParameterType;
            // params T[] (or regular T[])
            if (paramType.IsArray)
            {
                var elementType = paramType.GetElementType() ?? throw new InvalidOperationException($"Array {type.ToShortName()} has no element type.");
                data = new BlobEnumerableConverterData(elementType, ctor);
                return true;
            }

            // IEnumerable<T> or IList<T>
            if (paramType.IsGenericType)
            {
                var genericDef = paramType.GetGenericTypeDefinition();
                if (genericDef == typeof(IEnumerable<>) || genericDef == typeof(IList<>))
                {
                    var elementType = paramType.GetGenericArguments()[0];
                    data = new BlobEnumerableConverterData(elementType, ctor);
                    return true;
                }
            }
        }

        data = null;
        return false;
    }

    #endregion Private Methods

    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type) =>
        (typeof(IEnumerable).IsAssignableFrom(type) && GetElementTypeAndConstructor(type, out var data)) ? data : null;

    #endregion Protected Methods

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type)
    {
        GetHandlingData(type, out BlobEnumerableConverterData data);
        return [data.ElementType];
    }

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        if (bundle.State is not BlobEnumerableConverterState myState) throw new InvalidOperationException("Invalid state for enumerable converter.");
        var reader = state.Reader;
        var len = reader.Read7BitEncodedInt32();
        var array = Array.CreateInstance(myState.ElementConverterBundle.Type, len);
        for (var i = 0; i < len; i++)
        {
            var item = myState.ElementConverterBundle.Converter.ReadContent(state, myState.ElementConverterBundle);
            array.SetValue(item, i);
        }
        return myState.AcceptArray ? array : myState.Constructor?.CreateFast([array]) ?? throw new InvalidOperationException($"Type {bundle.Type.ToShortName()} does not accept an array and does not have a suitable constructor for deserialization!");
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobEnumerableConverterData data);
        var elementBundle = state.ReadConverter();
        if (!data.ElementType.IsAssignableFrom(elementBundle.Type)) throw new InvalidOperationException($"Element type in stream {elementBundle.Type.ToShortName()} is not compatible with the element type of the collection {data.ElementType.ToShortName()}.");
        bundle.State = new BlobEnumerableConverterState(bundle.Type, data.Constructor, elementBundle);
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobEnumerableConverterState myState) throw new InvalidOperationException("Invalid state for enumerable converter.");
        var writer = state.Writer;
        IEnumerable enumerable;
        if (instance is Array array)
        {
            state.Logger?.Verbose($"Write array of {myState.ElementConverterBundle.Type.ToShortName()} with {array.Length} items.");
            writer.Write7BitEncoded32(array.Length);
            enumerable = array;
        }
        else if (instance is ICollection list)
        {
            state.Logger?.Verbose($"Write list of {myState.ElementConverterBundle.Type.ToShortName()} with {list.Count} items.");
            writer.Write7BitEncoded32(list.Count);
            enumerable = list;
        }
        else if (instance is IEnumerable e)
        {
            var arrayList = e.ToArrayList();
            state.Logger?.Verbose($"Write objectlist of {myState.ElementConverterBundle.Type.ToShortName()} with {arrayList.Count} items.");
            writer.Write7BitEncoded32(arrayList.Count);
            enumerable = arrayList;
        }
        else throw new InvalidOperationException($"Type {instance.GetType().ToShortName()} is not an array or enumerable.");

        var converter = myState.ElementConverterBundle.Converter;
        foreach (var item in enumerable)
        {
            converter.WriteContent(state, myState.ElementConverterBundle, item);
        }
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobEnumerableConverterData data);
        //enforce element type converter to be written before writing the collection itself, so that it can be cached and reused for all items in the collection
        var elementBundle = state.WriteConverter(data.ElementType);
        bundle.State = new BlobEnumerableConverterState(bundle.Type, data.Constructor, elementBundle);
    }

    #endregion Public Methods
}

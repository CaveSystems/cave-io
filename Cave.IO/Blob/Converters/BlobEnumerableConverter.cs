using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    protected override object? GetCanHandleCache(Type type)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(type)) return null;
        if (!GetElementTypeAndConstructor(type, out var data)) return null;
        return data;
    }

    #endregion Protected Methods

    #region Public Methods

    static Array ReadPrimitive(DataReader reader, BlobPrimitiveType type)
    {
        switch (type)
        {
            case BlobPrimitiveType.ByteArray: return reader.ReadBytes()!;
            case BlobPrimitiveType.FloatArray: return reader.ReadFloatArray()!;
            case BlobPrimitiveType.DoubleArray: return reader.ReadDoubleArray()!;
            case BlobPrimitiveType.Int16Array: return reader.ReadInt16Array()!;
            case BlobPrimitiveType.UInt16Array: return reader.ReadUInt16Array()!;
            case BlobPrimitiveType.Int32Array: return reader.ReadInt32Array()!;
            case BlobPrimitiveType.UInt32Array: return reader.ReadUInt32Array()!;
            case BlobPrimitiveType.Int64Array: return reader.ReadInt64Array()!;
            case BlobPrimitiveType.UInt64Array: return reader.ReadUInt64Array()!;
            default: throw new InvalidOperationException($"Invalid primitive array {type}!");
        }
    }

    static void WritePrimitive(DataWriter writer, BlobPrimitiveType type, object instance)
    {
        switch (type)
        {
            case BlobPrimitiveType.ByteArray:
            {
                if (instance is byte[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<byte> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.FloatArray:
            {
                if (instance is float[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<float> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.DoubleArray:
            {
                if (instance is double[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<double> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.Int16Array:
            {
                if (instance is short[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<short> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.UInt16Array:
            {
                if (instance is ushort[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<ushort> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.Int32Array:
            {
                if (instance is int[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<int> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.UInt32Array:
            {
                if (instance is uint[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<uint> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.Int64Array:
            {
                if (instance is long[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<long> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            case BlobPrimitiveType.UInt64Array:
            {
                if (instance is ulong[] array) { writer.WritePrefixed(array); return; }
                else if (instance is IEnumerable<ulong> enumerable) { writer.WritePrefixed(enumerable.ToArray()); return; }
                break;
            }
            throw new InvalidOperationException($"Expected primitive {type}, but got {instance.GetType().ToShortName()}.");
        }
    }

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

        Array array;
        if (myState.Data.PrimitiveType != BlobPrimitiveType.Unsupported)
        {
            array = ReadPrimitive(reader, myState.Data.PrimitiveType);
        }
        else
        {
            var len = reader.Read7BitEncodedInt32();
            array = Array.CreateInstance(myState.Data.ElementType, len);
            for (var i = 0; i < len; i++)
            {
                var item = myState.ElementConverterBundle!.Converter.ReadContent(state, myState.ElementConverterBundle);
                array.SetValue(item, i);
            }
        }
        return myState.AcceptArray ? array : myState.Constructor?.CreateFast([array]) ?? throw new InvalidOperationException($"Type {bundle.Type.ToShortName()} does not accept an array and does not have a suitable constructor for deserialization!");
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobEnumerableConverterData data);
        BlobConverterBundle? elementBundle = null;
        if (data.PrimitiveType == default)
        {
            elementBundle = state.ReadConverter();
            if (!data.ElementType.IsAssignableFrom(elementBundle.Type)) throw new InvalidOperationException($"Element type in stream {elementBundle.Type.ToShortName()} is not compatible with the element type of the collection {data.ElementType.ToShortName()}.");
        }
        bundle.State = new BlobEnumerableConverterState(bundle.Type, elementBundle, data);
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobEnumerableConverterState myState) throw new InvalidOperationException("Invalid state for enumerable converter.");
        var writer = state.Writer;

        //fastpath possible ?
        if (myState.Data.PrimitiveType != default)
        {
            WritePrimitive(writer, myState.Data.PrimitiveType, instance);
            return;
        }

        IEnumerable enumerable;
        int itemCount;
        if (instance is Array array)
        {
            state.Logger?.Verbose($"Write array of {myState.Data.ElementType.ToShortName()} with {array.Length} items.");
            itemCount = array.Length;
            enumerable = array;
        }
        else if (instance is ICollection list)
        {
            state.Logger?.Verbose($"Write list of {myState.Data.ElementType.ToShortName()} with {list.Count} items.");
            itemCount = list.Count;
            enumerable = list;
        }
        else if (instance is IEnumerable e)
        {
            var arrayList = e.ToArrayList();
            state.Logger?.Verbose($"Write objectlist of {myState.Data.ElementType.ToShortName()} with {arrayList.Count} items.");
            itemCount = arrayList.Count;
            enumerable = arrayList;
        }
        else throw new InvalidOperationException($"Type {instance.GetType().ToShortName()} is not an array or enumerable.");

        writer.Write7BitEncoded32(itemCount);
        var converter = myState.ElementConverterBundle!.Converter;
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
        var elementBundle = data.PrimitiveType == default ? state.WriteConverter(data.ElementType) : null;
        bundle.State = new BlobEnumerableConverterState(bundle.Type, elementBundle, data);
    }

    #endregion Public Methods
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cave.IO
{
    /// <summary>Provides fast and efficient field and property de-/serialization.</summary>
    public class BinarySerializer
    {
        #region Private Methods

        BindingFlags GetBindingFlags(SerializerFlags flags)
        {
            var result = BindingFlags.Instance;
            if (flags.HasFlag(SerializerFlags.NonPublic)) result |= BindingFlags.NonPublic;
            if (flags.HasFlag(SerializerFlags.Public)) result |= BindingFlags.Public;
            return result;
        }

        #endregion Private Methods

        #region Public Properties

        /// <summary>Gets or sets flags for class field and property (de-)serialization.</summary>
        /// <remarks>By default this is set to (de-)serialize all non-&amp;public fields of classes.</remarks>
        public SerializerFlags ClassFlags { get; set; } = SerializerFlags.Fields | SerializerFlags.NonPublic | SerializerFlags.Public;

        /// <summary>Gets or sets flags for structure field and property (de-)serialization.</summary>
        /// <remarks>By default this is set to (de-)serialize all non-&amp;public fields of structures.</remarks>
        public SerializerFlags StructFlags { get; set; } = SerializerFlags.Fields | SerializerFlags.NonPublic | SerializerFlags.Public;

        #endregion Public Properties

        #region Private Enums

        enum TypeCode : byte
        {
            //START PRIMITIVES (VALUE TYPES)

            NULL = 0x00,

            BOOL = 0x10,
            BYTE = 0x11,
            SBYTE = 0x12,
            SHORT = 0x13,
            USHORT = 0x14,
            INT = 0x15,
            UINT = 0x16,
            LONG = 0x17,
            ULONG = 0x18,

            FLOAT = 0x30,
            DOUBLE = 0x31,
            DECIMAL = 0x32,

            CHAR = 0x40,
            STRING = 0x41,
            GUID = 0x42,

            DATETIME = 0x50,
            TIMESPAN = 0x51,

            //END PRIMITIVES 0x5F

            //START IMPLEMENTED

            ENUM = 0x60,
            STRUCT = 0x61,
            ARRAY = 0x62,
            CLASS = 0x63,
            PARSABLE = 0x64,

            //LAST = 0x7F !

            UNKNOWN = 0xFF
        }

        #endregion Private Enums

        #region Private Methods

        static TypeCode FromObject(object obj)
        {
            if (obj == null) return TypeCode.NULL;
            return FromType(obj.GetType());
        }

        static TypeCode FromType(Type type)
        {
            if (type == typeof(bool)) return TypeCode.BOOL;
            if (type == typeof(sbyte)) return TypeCode.SBYTE;
            if (type == typeof(byte)) return TypeCode.BYTE;
            if (type == typeof(short)) return TypeCode.SHORT;
            if (type == typeof(ushort)) return TypeCode.USHORT;
            if (type == typeof(int)) return TypeCode.INT;
            if (type == typeof(uint)) return TypeCode.UINT;
            if (type == typeof(long)) return TypeCode.LONG;
            if (type == typeof(ulong)) return TypeCode.ULONG;

            if (type == typeof(float)) return TypeCode.FLOAT;
            if (type == typeof(double)) return TypeCode.DOUBLE;
            if (type == typeof(decimal)) return TypeCode.DECIMAL;

            if (type == typeof(DateTime)) return TypeCode.DATETIME;
            if (type == typeof(TimeSpan)) return TypeCode.TIMESPAN;

            if (type == typeof(char)) return TypeCode.CHAR;
            if (type == typeof(string)) return TypeCode.STRING;

            if (type.IsEnum) return TypeCode.ENUM;
            if (type.IsValueType)
            {
                if (type.Name.StartsWith("Nullable"))
                {
#if NET20 || NET35 || NET40
                    var genericArguments = type.GetGenericArguments();
#else
                    var genericArguments = type.GenericTypeArguments;
#endif
                    if (genericArguments.Length == 1)
                    {
                        return FromType(genericArguments[0]);
                    }
                }
                else
                {
                    return TypeCode.STRUCT;
                }
                return TypeCode.UNKNOWN;
            }
            if (type.IsArray) return TypeCode.ARRAY;
            if (type.IsClass) return TypeCode.CLASS;

            return TypeCode.UNKNOWN;
        }

        static bool IsPrimitive(TypeCode objTypeCode) => ((int)objTypeCode) < 0x60;

        static object ReadPrimitive(TypeCode objTypeCode, DataReader reader) => objTypeCode switch
        {
            TypeCode.NULL => null,
            TypeCode.BOOL => reader.ReadBool(),
            TypeCode.SBYTE => reader.ReadInt8(),
            TypeCode.BYTE => reader.ReadByte(),
            TypeCode.SHORT => reader.ReadInt16(),
            TypeCode.USHORT => reader.ReadUInt16(),
            TypeCode.INT => reader.ReadInt32(),
            TypeCode.UINT => reader.ReadUInt32(),
            TypeCode.LONG => reader.ReadInt64(),
            TypeCode.ULONG => reader.ReadUInt64(),
            TypeCode.FLOAT => reader.ReadSingle(),
            TypeCode.DOUBLE => reader.ReadDouble(),
            TypeCode.DECIMAL => reader.ReadDecimal(),
            TypeCode.DATETIME => reader.ReadDateTime(),
            TypeCode.TIMESPAN => reader.ReadTimeSpan(),
            TypeCode.CHAR => reader.ReadChar(),
            TypeCode.STRING => reader.ReadString(),
            TypeCode.GUID => new Guid(reader.ReadBytes(16)),
            _ => throw new NotSupportedException(string.Format("Serialization of ObjectTypeCode {0} is not supported!", objTypeCode)),
        };

        static void WritePrimitive(bool writeTypeCode, TypeCode objTypeCode, object obj, DataWriter writer)
        {
            if (writeTypeCode) writer.Write((byte)objTypeCode);
            switch (objTypeCode)
            {
                case TypeCode.NULL: return;

                case TypeCode.BOOL: writer.Write((bool)obj); return;
                case TypeCode.BYTE: writer.Write((byte)obj); return;
                case TypeCode.SBYTE: writer.Write((sbyte)obj); return;
                case TypeCode.SHORT: writer.Write((short)obj); return;
                case TypeCode.USHORT: writer.Write((ushort)obj); return;
                case TypeCode.INT: writer.Write((int)obj); return;
                case TypeCode.UINT: writer.Write((uint)obj); return;
                case TypeCode.LONG: writer.Write((long)obj); return;
                case TypeCode.ULONG: writer.Write((ulong)obj); return;

                case TypeCode.FLOAT: writer.Write((float)obj); return;
                case TypeCode.DOUBLE: writer.Write((double)obj); return;
                case TypeCode.DECIMAL: writer.Write((decimal)obj); return;

                case TypeCode.CHAR: writer.Write((char)obj); return;
                case TypeCode.STRING: writer.WritePrefixed((string)obj); return;
                case TypeCode.GUID: writer.Write((Guid)obj); return;

                case TypeCode.DATETIME: writer.Write((DateTime)obj); return;
                case TypeCode.TIMESPAN: writer.Write((TimeSpan)obj); return;

                default: throw new NotSupportedException(string.Format("Serialization of Type {0} is not supported!", obj.GetType()));
            }
        }

        Array DeserializeArray(Type elementType, DataReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var elementTypeCode = (TypeCode)reader.ReadByte();
            var length = reader.Read7BitEncodedInt32();
            var array = Array.CreateInstance(elementType, length);

            if (IsPrimitive(elementTypeCode))
            {
                for (var i = 0; i < length; i++)
                {
                    array.SetValue(ReadPrimitive(elementTypeCode, reader), i);
                }
                return array;
            }

            if (elementTypeCode == TypeCode.ENUM)
            {
                for (var i = 0; i < length; i++)
                {
                    var value = Enum.ToObject(elementType, reader.Read7BitEncodedUInt64());
                    array.SetValue(value, i);
                }
                return array;
            }

            for (var i = 0; i < length; i++)
            {
                array.SetValue(Deserialize(elementType, reader), i);
            }
            return array;
        }

        #endregion Private Methods

        #region Public Properties

        /// <summary>Gets or sets the serializers used.</summary>
        public IList<IBinaryTypeSerializer> Serializers { get; set; }

        #endregion Public Properties

        #region Public Methods

        readonly Dictionary<Type, MethodBase> staticParseLookup = new();

        readonly Dictionary<string, Type> typeLookup = new();

        MethodBase GetParse(Type type)
        {
            staticParseLookup.TryGetValue(type, out var parse);
            return parse;
        }

        /// <summary>Deserializes the specified type from a <see cref="DataReader"/>.</summary>
        /// <param name="type">The type to deserialize</param>
        /// <param name="stream">The stream to deserialize from</param>
        /// <returns></returns>
        public object Deserialize(Type type, Stream stream) => Deserialize(type, new DataReader(stream));

        /// <summary>Deserializes the specified type from a byte block.</summary>
        /// <param name="block">The data block to deserialize from</param>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <returns></returns>
        public T Deserialize<T>(byte[] block) => (T)Deserialize(typeof(T), new MemoryStream(block));

        /// <summary>Deserializes the specified type from a <see cref="DataReader"/>.</summary>
        /// <param name="stream">The stream to deserialize from</param>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <returns></returns>
        public T Deserialize<T>(Stream stream) => (T)Deserialize(typeof(T), new DataReader(stream));

        /// <summary>Deserializes the specified type from a <see cref="DataReader"/>.</summary>
        /// <param name="reader">The reader to deserialize from</param>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <returns></returns>
        public T Deserialize<T>(DataReader reader) => (T)Deserialize(typeof(T), reader);

        /// <summary>Deserializes the specified type from a given data block</summary>
        /// <param name="type">The type to deserialize</param>
        /// <param name="block">The data block to deserialize from</param>
        /// <returns></returns>
        public object Deserialize(Type type, byte[] block) => Deserialize(type, new MemoryStream(block));

        /// <summary>Deserializes the specified type from a <see cref="DataReader"/>.</summary>
        /// <param name="type">The type to deserialize</param>
        /// <param name="reader">The reader to deserialize from</param>
        /// <returns></returns>
        public object Deserialize(Type type, DataReader reader)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var expectedTypeCode = FromType(type);
            var objTypeCode = (TypeCode)reader.ReadByte();

            if (expectedTypeCode != objTypeCode)
            {
                if (objTypeCode == TypeCode.NULL) return null;
                throw new FormatException(string.Format("Invalid ObjectTypeCode: {0} - expected: {1}!", objTypeCode, expectedTypeCode));
            }

            if (IsPrimitive(objTypeCode))
            {
                return ReadPrimitive(objTypeCode, reader);
            }

            if (objTypeCode == TypeCode.ENUM)
            {
                return Enum.ToObject(type, reader.Read7BitEncodedUInt64());
            }

            var serializer = Serializers?.FirstOrDefault(s => s.CanDeserialize(type));
            if (serializer != null)
            {
                return serializer.Deserialize(reader, type);
            }
            if (objTypeCode == TypeCode.UNKNOWN)
            {
                throw new Exception($"No serializer registered for type {type}!");
            }

            switch (objTypeCode)
            {
                case TypeCode.ARRAY:
                {
                    var elementType = type.GetElementType();
                    return DeserializeArray(elementType, reader);
                }

                case TypeCode.STRUCT:
                {
                    if (StructFlags.HasFlag(SerializerFlags.TypeName) || StructFlags.HasFlag(SerializerFlags.AssemblyName))
                    {
                        var className = reader.ReadString();
                        if (!typeLookup.TryGetValue(className, out type))
                        {
                            typeLookup[className] = type = AppDom.FindType(className, AppDom.LoadFlags.None);
                        }
                    }

                    var result = Activator.CreateInstance(type);
                    if (StructFlags.HasFlag(SerializerFlags.Fields))
                    {
                        var fields = type.GetFields(GetBindingFlags(StructFlags));
                        foreach (var field in fields)
                        {
                            var value = Deserialize(field.FieldType, reader);
                            field.SetValue(result, value);
                        }
                    }
                    if (StructFlags.HasFlag(SerializerFlags.Properties))
                    {
                        var properties = type.GetProperties(GetBindingFlags(StructFlags));
                        foreach (var property in properties)
                        {
                            var value = Deserialize(property.PropertyType, reader);
                            property.SetValue(result, value, null);
                        }
                    }
                    return result;
                }

                case TypeCode.CLASS:
                {
                    if (ClassFlags.HasFlag(SerializerFlags.TypeName) || ClassFlags.HasFlag(SerializerFlags.AssemblyName))
                    {
                        var className = reader.ReadString();
                        if (!typeLookup.TryGetValue(className, out type))
                        {
                            typeLookup[className] = type = AppDom.FindType(className, AppDom.LoadFlags.None);
                        }
                    }

                    var parse = GetParse(type);
                    if (parse != null)
                    {
                        var str = reader.ReadString();
                        if (parse.IsConstructor)
                        {
                            return ((ConstructorInfo)parse).Invoke(new[] { str });
                        }
                        else
                        {
                            return parse.Invoke(null, new[] { str });
                        }
                    }

                    var result = Activator.CreateInstance(type);
                    if (ClassFlags.HasFlag(SerializerFlags.Fields))
                    {
                        var fields = type.GetFields(GetBindingFlags(StructFlags));
                        foreach (var field in fields)
                        {
                            var value = Deserialize(field.FieldType, reader);
                            field.SetValue(result, value);
                        }
                    }
                    if (ClassFlags.HasFlag(SerializerFlags.Properties))
                    {
                        var properties = type.GetProperties(GetBindingFlags(StructFlags));
                        foreach (var property in properties)
                        {
                            var value = Deserialize(property.PropertyType, reader);
                            property.SetValue(result, value, null);
                        }
                    }
                    return result;
                }
            }
            throw new NotImplementedException($"Unknown TypeCode {objTypeCode} at element type {type}!");
        }

        /// <summary>Serializes the specified object</summary>
        /// <param name="value">The object to serialize</param>
        /// <param name="stream">The stream to serialize to</param>
        public void Serialize(object value, Stream stream)
        {
            var writer = new DataWriter(stream);
            Serialize(value, writer);
            writer.Flush();
        }

        /// <summary>Serializes the specified object</summary>
        /// <param name="value">The object to serialize</param>
        /// <param name="data">Returns the serialized data.</param>
        public void Serialize(object value, out byte[] data)
        {
            using var result = new MemoryStream();
            Serialize(value, result);
            data = result.ToArray();
        }

        /// <summary>Serializes the specified object to a <see cref="DataWriter"/></summary>
        /// <param name="value">The object to serialize</param>
        /// <param name="writer">The writer to serialize to</param>
        public void Serialize(object value, DataWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var objTypeCode = FromObject(value);
            writer.Write((byte)objTypeCode);

            if (IsPrimitive(objTypeCode))
            {
                WritePrimitive(false, objTypeCode, value, writer);
                return;
            }

            if (objTypeCode == TypeCode.ENUM)
            {
                writer.Write7BitEncoded64(Convert.ToInt64(value));
                return;
            }

            var serializer = Serializers?.FirstOrDefault(s => s.CanSerialize(value));
            if (serializer != null)
            {
                serializer.Serialize(writer, value);
                return;
            }
            if (objTypeCode == TypeCode.UNKNOWN)
            {
                throw new Exception($"No serializer registered for value {value} type {value?.GetType()}!");
            }

            switch (objTypeCode)
            {
                case TypeCode.ARRAY:
                {
                    var array = (Array)value;
                    var elementTypeCode = FromType(value.GetType().GetElementType());
                    writer.Write((byte)elementTypeCode);
                    writer.Write7BitEncoded32(array.Length);

                    if (IsPrimitive(elementTypeCode))
                    {
                        foreach (var item in array)
                        {
                            WritePrimitive(false, elementTypeCode, item, writer);
                        }
                        return;
                    }

                    if (elementTypeCode == TypeCode.ENUM)
                    {
                        foreach (var item in array)
                        {
                            writer.Write7BitEncoded64(Convert.ToUInt64(item));
                        }
                        return;
                    }

                    foreach (var element in array)
                    {
                        Serialize(element, writer);
                    }
                    return;
                }

                case TypeCode.STRUCT:
                {
                    var type = value.GetType();
                    if (StructFlags.HasFlag(SerializerFlags.AssemblyName))
                    {
                        writer.WritePrefixed(type.AssemblyQualifiedName);
                    }
                    else if (StructFlags.HasFlag(SerializerFlags.TypeName))
                    {
                        writer.WritePrefixed(type.FullName);
                    }
                    if (StructFlags.HasFlag(SerializerFlags.Fields))
                    {
                        var fields = type.GetFields(GetBindingFlags(StructFlags));
                        foreach (var field in fields)
                        {
                            var fieldValue = field.GetValue(value);
                            Serialize(fieldValue, writer);
                        }
                    }
                    if (StructFlags.HasFlag(SerializerFlags.Properties))
                    {
                        var properties = type.GetProperties(GetBindingFlags(StructFlags));
                        foreach (var property in properties)
                        {
                            var fieldValue = property.GetValue(value, null);
                            Serialize(fieldValue, writer);
                        }
                    }
                    return;
                }

                case TypeCode.CLASS:
                {
                    var type = value.GetType();
                    if (ClassFlags.HasFlag(SerializerFlags.AssemblyName))
                    {
                        writer.WritePrefixed(type.AssemblyQualifiedName);
                    }
                    else if (ClassFlags.HasFlag(SerializerFlags.TypeName))
                    {
                        writer.WritePrefixed(type.FullName);
                    }

                    var parse = GetParse(type);
                    if (parse != null)
                    {
                        writer.WritePrefixed(value.ToString());
                        return;
                    }

                    if (ClassFlags.HasFlag(SerializerFlags.Fields))
                    {
                        var fields = type.GetFields(GetBindingFlags(StructFlags));
                        foreach (var field in fields)
                        {
                            var fieldValue = field.GetValue(value);
                            Serialize(fieldValue, writer);
                        }
                    }
                    if (ClassFlags.HasFlag(SerializerFlags.Properties))
                    {
                        var properties = type.GetProperties(GetBindingFlags(StructFlags));
                        foreach (var property in properties)
                        {
                            var fieldValue = property.GetValue(value, null);
                            Serialize(fieldValue, writer);
                        }
                    }
                    return;
                }
            }

            throw new NotImplementedException($"Unknown TypeCode {objTypeCode} at element type {value?.GetType()}!");
        }

        /// <summary>Registers the specified type for ToString() serialization and Cctor(string) deserialization.</summary>
        /// <param name="type">The type to register.</param>
        public void UseToStringAndCctor(Type type)
        {
            var parse = type.GetConstructor(new[] { typeof(string) });
            if (parse == null) throw new ArgumentException("Could not find a matching Cctor(string) method!");
            staticParseLookup.Add(type, parse);
        }

        /// <summary>Registers the specified type for ToString() serialization and static Parse(string) deserialization.</summary>
        /// <param name="type">The type to register.</param>
        public void UseToStringAndParse(Type type)
        {
            var parse = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(string) }, null);
            if (parse == null) throw new ArgumentException("Could not find a matching static Parse(string) method!");
            staticParseLookup.Add(type, parse);
        }

        #endregion Public Methods
    }
}

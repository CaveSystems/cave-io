using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Reflection-based <see cref="IBlobConverter"/> for serializing and deserializing types by inspecting fields and properties at runtime.</summary>
/// <remarks>
/// Member inclusion is controlled by <see cref="BlobConverterFlags"/>. Default: value types use fields, reference types use properties; both public and
/// non-public members are included. Each member gets a stable numeric ID in the binary stream, supporting fuzzy name matching for minor renames.
/// </remarks>
public class BlobReflectionConverter : IBlobConverter
{
    #region Private Methods

    /// <summary>Normalizes a member name for fuzzy comparisons (letters/digits, lower-case).</summary>
    /// <param name="name">Input member name.</param>
    /// <returns>Normalized name for fuzzy matching.</returns>
    static string FuzzyName(string name) => name.GetValidChars(ASCII.Strings.Letters + ASCII.Strings.Digits).ToLowerInvariant();

    #endregion Private Methods

    #region Public Methods

    /// <inheritdoc/>
    /// <summary>Checks if the converter can handle the given <paramref name="type"/>.</summary>
    /// <param name="type">Type to check.</param>
    /// <returns><c>true</c> if serializable members exist; otherwise <c>false</c>.</returns>
    public virtual bool CanHandle(Type type) =>
        (
        type.IsValueType ||
        type.GetConstructor([]) != null
        ) && (
        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any() ||
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(p => p.CanRead && p.CanWrite)
        );

    /// <inheritdoc/>
    /// <summary>Reads instance content from <paramref name="state"/> using <paramref name="bundle"/>.</summary>
    /// <param name="state">Reader state.</param>
    /// <param name="bundle">Converter bundle.</param>
    /// <returns>Populated instance of the target type.</returns>
    /// <exception cref="InvalidOperationException">Invalid bundle state or instance creation failure.</exception>
    /// <exception cref="InvalidDataException">Malformed binary data.</exception>
    public virtual object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var type = bundle.Type;
        state.Logger?.Debug($"Read content of {type.Name}");
        if (bundle.State is not BlobReflectionConverterState myState) throw new InvalidOperationException("Invalid state for reflection converter.");
        var result = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Could not create instance of type {type.Name}.");
        var reader = state.Reader;
        for (var index = 0; index < myState.MemberCount;)
        {
            // read member index and allow to skip not serialized ones
            var next = reader.Read7BitEncodedInt32();
            if (next > myState.MemberCount) throw new InvalidDataException($"Invalid binary format (member number {next} exceeds member count {myState.MemberCount}).");
            if (next == myState.MemberCount) break;
            if (next < index) throw new InvalidDataException($"Invalid binary format (expected member number > {index} but got {next}).");
            index = next;
            var member = myState.Members[index];
            var converter = member.Bundle.Converter;
            var content = converter.ReadContent(state, member.Bundle) ?? throw new InvalidDataException($"Invalid binary format (member with id {index} has null value).");
            state.Logger?.Verbose($"Set Value {member.Member.Name} = {content}");
            member.Setter.Invoke(result, content);
        }
        return result;
    }

    /// <inheritdoc/>
    /// <summary>Reads initialization metadata and prepares cached state.</summary>
    /// <param name="state">Reader state.</param>
    /// <param name="bundle">Converter bundle to populate.</param>
    /// <exception cref="InvalidOperationException">No matching field or property found.</exception>
    public virtual void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var count = (int)reader.Read7BitEncodedUInt32();
        var myState = new BlobReflectionConverterState(bundle.Type, count);

        for (var memberIndex = 0; memberIndex < myState.MemberCount; memberIndex++)
        {
            var memberName = reader.ReadPrefixedString() ?? throw new InvalidDataException("Invalid binary format (missing member name).");
            var memberBundle = state.ReadConverter();
            //match field or property
            {
                if (myState.Fields.FirstOrDefault(f => f.Name == memberName && f.FieldType.IsAssignableFrom(memberBundle.Type)) is FieldInfo field)
                {
                    myState.Members[memberIndex] = new BlobReflectionConverterMember(field, field.SetValue, memberBundle);
                    continue;
                }
                if (myState.Properties.FirstOrDefault(p => p.Name == memberName && p.PropertyType.IsAssignableFrom(memberBundle.Type)) is PropertyInfo property)
                {
                    myState.Members[memberIndex] = new BlobReflectionConverterMember(property, property.SetValue, memberBundle);
                    continue;
                }
            }

            // try to match names a little bit more loosely, to allow for some renaming or type changes (e.g. from int to long)
            {
                if (myState.Fields.FirstOrDefault(f => FuzzyName(f.Name) == FuzzyName(memberName) && f.FieldType.IsAssignableFrom(memberBundle.Type)) is FieldInfo field)
                {
                    myState.Members[memberIndex] = new BlobReflectionConverterMember(field, field.SetValue, memberBundle);
                    continue;
                }
                if (myState.Properties.FirstOrDefault(p => FuzzyName(p.Name) == FuzzyName(memberName) && p.PropertyType.IsAssignableFrom(memberBundle.Type)) is PropertyInfo property)
                {
                    myState.Members[memberIndex] = new BlobReflectionConverterMember(property, property.SetValue, memberBundle);
                    continue;
                }
            }

            throw new InvalidOperationException($"Could not find matching field or property for member {memberName} of type {memberBundle.Type} in type {bundle.Type}.");
        }
        bundle.State = myState;
    }

    /// <inheritdoc/>
    /// <summary>Writes non-null member values of <paramref name="instance"/> to the writer.</summary>
    /// <param name="state">Writer state.</param>
    /// <param name="bundle">Converter bundle with cached state.</param>
    /// <param name="instance">Instance to serialize.</param>
    /// <exception cref="InvalidOperationException">Invalid bundle state or missing initialization.</exception>
    public virtual void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobReflectionConverterState myState) throw new InvalidOperationException("Invalid state for reflection converter.");
        if (myState.MemberCount == 0) throw new InvalidOperationException("Initialization has not been written yet.");
        var writer = state.Writer;
        var index = 0;

        state.Logger?.Verbose($"Write {myState.Fields.Length} fields and {myState.Properties.Length} properties.");
        // iterate fields and write only non-null values, writing the member index before each value so that deserialization can skip missing members
        foreach (var field in myState.Fields)
        {
            var i = index++;
            var value = field.GetValue(instance);
            if (value is null) continue;
            var member = myState.Members[i];
            writer.Write7BitEncoded32(i);
            member.Bundle.Converter.WriteContent(state, member.Bundle, value);
        }
        // iterate properties in the same way
        foreach (var property in myState.Properties)
        {
            var i = index++;
            var value = property.GetValue(instance);
            if (value is null) continue;
            var member = myState.Members[i];
            writer.Write7BitEncoded32(i);
            member.Bundle.Converter.WriteContent(state, member.Bundle, value);
        }
        //write end mark
        writer.Write7BitEncoded32(index);
        if (index != myState.MemberCount) throw new InvalidOperationException("Count of written members does not match count of initialized members.");
    }

    /// <inheritdoc/>
    /// <summary>Writes initialization metadata and registers converters for each member type.</summary>
    /// <param name="state">Writer state.</param>
    /// <param name="bundle">Converter bundle to populate.</param>
    public virtual void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        var myState = new BlobReflectionConverterState(bundle.Type);
        var writer = state.Writer;
        writer.Write7BitEncoded32(myState.MemberCount);
        var currentIndex = 0;
        foreach (var field in myState.Fields)
        {
            try
            {
                writer.WritePrefixed(field.Name);
                var fieldBundle = state.WriteConverter(field.FieldType);
                myState.Members[currentIndex++] = new(field, field.SetValue, fieldBundle);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error initialize {field.FieldType.ToShortName()} {field.Name} at type {bundle.Type.ToShortName()}.", ex);
            }
        }
        foreach (var property in myState.Properties)
        {
            try
            {
                writer.WritePrefixed(property.Name);
                var propertyBundle = state.WriteConverter(property.PropertyType);
                myState.Members[currentIndex++] = new(property, property.SetValue, propertyBundle);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error initialize {property.PropertyType.ToShortName()} {property.Name} at type {bundle.Type.ToShortName()}.", ex);
            }
        }
        bundle.State = myState;
        state.Logger?.Debug($"BlobReflectionConverter {bundle} initialized with {myState.Fields.Length} fields and {myState.Properties.Length} properties.");
        return;
    }

    #endregion Public Methods
}

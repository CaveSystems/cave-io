using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

/// <summary>Blob converter for positional records.</summary>
/// <remarks>Uses constructor parameters and matching readable properties.</remarks>
public class BlobPositionalRecordConverter : BlobConverterBase
{
    #region Protected Methods

    /// <inheritdoc/>
    protected override object? GetCanHandleCache(Type type) => CreateData(type);

    #endregion Protected Methods

    #region Internal Methods

    /// <summary>Creates converter data for a supported type.</summary>
    /// <param name="type">Target type.</param>
    /// <returns>Converter data or null.</returns>
    internal static BlobPositionalRecordConverterData? CreateData(Type type)
    {
        if (type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic)?.PropertyType != typeof(Type))
        {
            return null;
        }
        var allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead).ToArray();
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => c.GetParameters().Length > 0)
            .Where(c =>
            {
                var parameters = c.GetParameters();
                return !(parameters.Length == 1 && parameters[0].ParameterType == type);
            })
            .OrderByDescending(c => c.GetParameters().Length)
            .ThenBy(c => c.IsPublic ? 0 : 1);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var properties = new PropertyInfo[parameters.Length];
            var valid = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var property = allProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                    p.PropertyType == parameter.ParameterType);
                if (property is null)
                {
                    valid = false;
                    break;
                }
                properties[i] = property;
            }

            if (!valid) continue;
            if (!IsRecordType(type, parameters)) continue;

            return new BlobPositionalRecordConverterData(type, constructor, parameters, properties);
        }

        return null;
    }

    /// <summary>Normalizes a member name for fuzzy comparisons.</summary>
    /// <param name="name">Input name.</param>
    /// <returns>Normalized name.</returns>
    internal static string FuzzyName(string name) => name.GetValidChars(ASCII.Strings.Letters + ASCII.Strings.Digits).ToLowerInvariant();

    /// <summary>Gets a default value for a constructor parameter.</summary>
    /// <param name="parameter">Constructor parameter.</param>
    /// <returns>Default value.</returns>
    internal static object? GetDefaultValue(ParameterInfo parameter)
    {
#if NET20 || NET35 || NET40
        if (parameter.IsOptional && parameter.DefaultValue != DBNull.Value) return parameter.DefaultValue;
#else
        if (parameter.HasDefaultValue) return parameter.DefaultValue;
#endif
        var parameterType = parameter.ParameterType;
        return parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
    }

    /// <summary>Checks for a matching Deconstruct method.</summary>
    /// <param name="type">Target type.</param>
    /// <param name="parameters">Constructor parameters.</param>
    /// <returns>True if a matching method exists.</returns>
    internal static bool HasMatchingDeconstruct(Type type, ParameterInfo[] parameters)
    {
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == "Deconstruct");
        foreach (var method in methods)
        {
            var methodParameters = method.GetParameters();
            if (methodParameters.Length != parameters.Length) continue;

            var valid = true;
            for (var i = 0; i < methodParameters.Length; i++)
            {
                var methodParameter = methodParameters[i];
                if (!methodParameter.ParameterType.IsByRef)
                {
                    valid = false;
                    break;
                }

                var elementType = methodParameter.ParameterType.GetElementType();
                if (elementType != parameters[i].ParameterType)
                {
                    valid = false;
                    break;
                }
            }

            if (valid) return true;
        }

        return false;
    }

    /// <summary>Checks whether the type looks like a record.</summary>
    /// <param name="type">Target type.</param>
    /// <param name="parameters">Constructor parameters.</param>
    /// <returns>True for supported record types.</returns>
    internal static bool IsRecordType(Type type, ParameterInfo[] parameters)
    {
        if (type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic)?.PropertyType == typeof(Type))
        {
            return true;
        }

        if (!HasMatchingDeconstruct(type, parameters))
        {
            return false;
        }

        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(m => m.Name == "PrintMembers");
    }

    /// <summary>Tries to resolve a serialized member.</summary>
    /// <param name="data">Converter data.</param>
    /// <param name="memberName">Serialized member name.</param>
    /// <param name="memberType">Serialized member type.</param>
    /// <param name="useFuzzyMatch">Use fuzzy name matching.</param>
    /// <param name="member">Resolved member.</param>
    /// <returns>True if resolved.</returns>
    internal static bool TryFindMember(
        BlobPositionalRecordConverterData data,
        string memberName,
        Type memberType,
        bool useFuzzyMatch,
        out BlobPositionalRecordConverterMember? member)
    {
        for (var i = 0; i < data.Parameters.Length; i++)
        {
            var parameter = data.Parameters[i];
            var property = data.Properties[i];

            var nameMatches = useFuzzyMatch
                ? FuzzyName(property.Name) == FuzzyName(memberName) || FuzzyName(parameter.Name ?? string.Empty) == FuzzyName(memberName)
                : property.Name == memberName || parameter.Name == memberName;

            if (!nameMatches) continue;
            if (!parameter.ParameterType.IsAssignableFrom(memberType)) continue;

            member = new BlobPositionalRecordConverterMember(parameter, i, property, null!);
            return true;
        }

        member = null;
        return false;
    }

    #endregion Internal Methods

    #region Public Methods

    /// <inheritdoc/>
    public override IList<Type> GetContentTypes(Type type)
    {
        GetHandlingData(type, out BlobPositionalRecordConverterData data);
        return data.ElementTypes.ToList();
    }

    /// <inheritdoc/>
    public override object ReadContent(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var type = bundle.Type;
        state.Logger?.Debug($"Read content of {type.Name}");

        if (bundle.State is not BlobPositionalRecordConverterData myState)
        {
            throw new InvalidOperationException("Invalid state for positional record converter.");
        }

        var reader = state.Reader;
        var args = new object?[myState.ParameterCount];
        for (var i = 0; i < args.Length; i++)
        {
            args[i] = GetDefaultValue(myState.Parameters[i]);
        }

        var minimumIndex = 0;
        while (true)
        {
            var next = reader.Read7BitEncodedInt32();
            if (next > myState.SerializedMemberCount)
            {
                throw new InvalidDataException($"Invalid binary format (member number {next} exceeds member count {myState.SerializedMemberCount}).");
            }

            if (next == myState.SerializedMemberCount)
            {
                break;
            }

            if (next < minimumIndex)
            {
                throw new InvalidDataException($"Invalid binary format (expected member number >= {minimumIndex} but got {next}).");
            }

            var member = myState.Members[next];
            var content = member.Bundle.Converter.ReadContent(state, member.Bundle);
            state.Logger?.Verbose($"Set ctor arg {member.Parameter.Name} = {content}");
            args[member.ParameterIndex] = content;
            minimumIndex = next + 1;
        }

        try
        {
            return myState.Constructor.Invoke(args);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not create record instance of type {type.ToShortName()}.", ex);
        }
    }

    /// <inheritdoc/>
    public override void ReadInitialization(IBlobReaderState state, BlobConverterBundle bundle)
    {
        var reader = state.Reader;
        var count = (int)reader.Read7BitEncodedUInt32();

        GetHandlingData(bundle.Type, out BlobPositionalRecordConverterData data);
        var myState = data with
        {
            Members = new BlobPositionalRecordConverterMember[count],
            SerializedMemberCount = (uint)count
        };

        for (var memberIndex = 0; memberIndex < count; memberIndex++)
        {
            var memberName = reader.ReadPrefixedString() ?? throw new InvalidDataException("Invalid binary format (missing member name).");
            var memberBundle = state.ReadConverter();

            if (TryFindMember(data, memberName, memberBundle.Type, false, out var exactMatch))
            {
                myState.Members[memberIndex] = new BlobPositionalRecordConverterMember(
                    exactMatch!.Parameter,
                    exactMatch.ParameterIndex,
                    exactMatch.Property,
                    memberBundle);
                continue;
            }

            if (TryFindMember(data, memberName, memberBundle.Type, true, out var fuzzyMatch))
            {
                myState.Members[memberIndex] = new BlobPositionalRecordConverterMember(
                    fuzzyMatch!.Parameter,
                    fuzzyMatch.ParameterIndex,
                    fuzzyMatch.Property,
                    memberBundle);
                continue;
            }

            throw new InvalidOperationException(
                $"Could not find matching positional record member for {memberName} of type {memberBundle.Type} in type {bundle.Type}.");
        }

        bundle.State = myState;
    }

    /// <inheritdoc/>
    public override void WriteContent(IBlobWriterState state, BlobConverterBundle bundle, object instance)
    {
        if (bundle.State is not BlobPositionalRecordConverterData myState)
        {
            throw new InvalidOperationException("Invalid state for positional record converter.");
        }

        if (myState.Members.Count == 0)
        {
            throw new InvalidOperationException("Initialization has not been written yet.");
        }

        var writer = state.Writer;
        state.Logger?.Verbose($"Write {myState.Properties.Length} positional properties.");

        for (var i = 0; i < myState.Properties.Length; i++)
        {
            var property = myState.Properties[i];
            var value = property.GetValue(instance);
            if (value is null) continue;

            var member = myState.Members[i];
            writer.Write7BitEncoded32(i);
            member.Bundle.Converter.WriteContent(state, member.Bundle, value);
        }

        writer.Write7BitEncoded32((int)myState.ParameterCount);

        if (myState.Members.Count != myState.ParameterCount)
        {
            throw new InvalidOperationException("Count of written members does not match count of initialized members.");
        }
    }

    /// <inheritdoc/>
    public override void WriteInitialization(IBlobWriterState state, BlobConverterBundle bundle)
    {
        GetHandlingData(bundle.Type, out BlobPositionalRecordConverterData data);
        var myState = data with
        {
            Members = new BlobPositionalRecordConverterMember[data.ParameterCount],
            SerializedMemberCount = data.ParameterCount
        };

        var writer = state.Writer;
        writer.Write7BitEncoded32((int)myState.ParameterCount);

        for (var i = 0; i < myState.Properties.Length; i++)
        {
            var property = myState.Properties[i];
            var parameter = myState.Parameters[i];
            try
            {
                writer.WritePrefixed(property.Name);
                var propertyBundle = state.WriteConverter(property.PropertyType);
                myState.Members[i] = new BlobPositionalRecordConverterMember(parameter, i, property, propertyBundle);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error initialize {property.PropertyType.ToShortName()} {property.Name} at type {bundle.Type.ToShortName()}.",
                    ex);
            }
        }

        bundle.State = myState;
        state.Logger?.Debug($"BlobPositionalRecordConverter {bundle} initialized with {myState.Properties.Length} positional properties.");
    }

    #endregion Public Methods
}

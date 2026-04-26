using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cave;

namespace Cave.IO.Blob.Converters;

/// <summary>Holds the per-operation state for a single type handled by <see cref="BlobReflectionConverter"/>.</summary>
/// <remarks>
/// This state caches the reflected fields and properties, the converter flags used to select members, the element types encountered and the per-member
/// converter bundles required for (de)serialization.
/// </remarks>
[DebuggerDisplay("{MemberCount} members")]
internal sealed class BlobReflectionConverterState
{
    #region Fields

    /// <summary>Reflected fields for serialization/deserialization.</summary>
    internal readonly FieldInfo[] Fields = [];

    /// <summary>Effective <see cref="BlobConverterFlags"/> for member selection.</summary>
    internal readonly BlobConverterFlags Flags;

    /// <summary>Total number of members (fields + properties).</summary>
    internal readonly uint MemberCount;

    /// <summary>Ordered list of per-member metadata.</summary>
    internal readonly IList<BlobReflectionConverterMember> Members = [];

    /// <summary>Reflected properties for serialization/deserialization.</summary>
    internal readonly PropertyInfo[] Properties = [];

    #endregion Fields

    #region Public Constructors

    /// <summary>Read-only list of distinct element types used by this type.</summary>
    internal readonly IList<Type> ElementTypes;

    /// <summary>Initializes a new <see cref="BlobReflectionConverterState"/> for the given <paramref name="type"/>.</summary>
    /// <param name="type">CLR type for this state.</param>
    /// <param name="count">
    /// Optional expected member count. If zero, derived from fields/properties. Passing a specific count can be used to reserve a different number of member slots.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type defines no serializable fields or properties according to the resolved flags, or when the resolved element type list is empty.
    /// </exception>
    public BlobReflectionConverterState(Type type, int count = 0)
    {
        var flags = Flags;
        type.GetCustomAttributes(true).OfType<BlobConverterAttribute>().ForEach(a => flags |= a.Source);
        if (flags == default)
        {
            Flags = flags = (type.IsValueType ? BlobConverterFlags.Fields : BlobConverterFlags.Properties) | BlobConverterFlags.Public | BlobConverterFlags.Private;
        }
        var visibilitySet = false;
        var bindingFlags = BindingFlags.Instance;
        if (flags.HasFlag(BlobConverterFlags.Public)) { bindingFlags |= BindingFlags.Public; visibilitySet = true; }
        if (flags.HasFlag(BlobConverterFlags.Private)) { bindingFlags |= BindingFlags.NonPublic; visibilitySet = true; }
        if (!visibilitySet) { bindingFlags |= BindingFlags.Public | BindingFlags.NonPublic; }
        if (flags.HasFlag(BlobConverterFlags.Properties))
        {
            Properties = type.GetProperties(bindingFlags).Where(p => p.CanWrite && p.CanRead).ToArray();
        }
        if (flags.HasFlag(BlobConverterFlags.Fields))
        {
            Fields = type.GetFields(bindingFlags);
        }
        ElementTypes = Fields.Select(f => f.FieldType).Concat(Properties.Select(p => p.PropertyType)).Distinct().AsReadOnly();

        if (count <= 0)
        {
            count = (Fields.Length + Properties.Length);
        }
        Members = new BlobReflectionConverterMember[count];
        MemberCount = (uint)count;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override string ToString() => $"{MemberCount} members, {ElementTypes.Count} element types";

    #endregion Public Methods
}

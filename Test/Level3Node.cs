using Cave;
using Cave.Collections;
using Cave.IO;
using Cave.IO.Blob;
using Cave.IO.Blob.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tests.Cave.IO;

public sealed record Level3Node(string Name, List<string> Tags, Level4Node Child) : BaseRecord
{
    Level3Node() : this(null, null, null) { }

    public bool Equals(Level3Node other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        return Name == other.Name && Child.Equals(other.Child) && DeepEquals.ListEqual(Tags, other.Tags);
    }

    public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ Tags.Count;
}

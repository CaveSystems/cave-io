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

internal static class DeepEquals
{
    public static bool ListEqual<T>(IList<T> a, IList<T> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        var cmp = EqualityComparer<T>.Default;
        for (int i = 0; i < a.Count; i++)
        {
            if (!cmp.Equals(a[i], b[i])) return false;
        }
        return true;
    }

    public static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        var cmp = EqualityComparer<TValue>.Default;
        foreach (var kv in a)
        {
            TValue other;
            if (!b.TryGetValue(kv.Key, out other)) return false;
            if (!cmp.Equals(kv.Value, other)) return false;
        }
        return true;
    }
}

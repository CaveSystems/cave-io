using System;
using System.Collections.Generic;

namespace Cave.IO.Blob;

static class TypeExtensions
{
    public static string ToShortName(this Type type)
    {
        var nullableMark = string.Empty;
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
            nullableMark = "?";
        }
        var genericArgs = type.GetGenericArguments();
        if (genericArgs.Length == 0)
        {
            return type.Name + nullableMark;
        }
        var genericArgIds = new List<string>(genericArgs.Length);
        foreach (var arg in genericArgs)
        {
            genericArgIds.Add(arg.ToShortName());
        }
        return $"{type.Name.BeforeFirst('`')}{nullableMark}<{genericArgIds.Join(",")}>";
    }
}

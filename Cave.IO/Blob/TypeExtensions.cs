using System;
using System.Collections.Generic;
using System.Linq;

namespace Cave.IO.Blob;

//todo: move this to cave.extensions
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

    public static string GetPortableTypeName(this Type type)
    {
        if (type.IsArray) return $"{GetPortableTypeName(type.GetElementType()!)}[]";
        if (type.IsGenericParameter) return type.Name;
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var baseName = GetPortableDeclaringName(def);
            var args = type.GetGenericArguments().Select(GetPortableTypeName).Join(',');
            return $"{baseName}[{args}]";
        }
        return GetPortableDeclaringName(type);
    }

    public static string GetPortableDeclaringName(this Type type)
    {
        if (type.DeclaringType != null) return $"{GetPortableDeclaringName(type.DeclaringType)}+{type.Name}";
        return $"{type.Namespace}.{type.Name}";
    }
}

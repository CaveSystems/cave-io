using System;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobStringParseConverterState
{
    #region Fields

    internal readonly MethodInfo? ParseMethod;

    internal readonly ConstructorInfo? Constructor;

    internal readonly bool UseCulture;

    internal BlobStringParseConverterMode Mode;

    #endregion Fields

    #region Public Constructors

    public BlobStringParseConverterState(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }

        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == "Parse").OrderBy(m => (m.IsStatic ? 100 : 200) * m.GetParameters().Length);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(IFormatProvider))
            {
                //best variant, break instantly
                ParseMethod = method;
                UseCulture = true;
                break;
            }
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                //second best variant, keep looking for culture-aware overloads but remember this one
                ParseMethod = method;
                UseCulture = false;
            }
        }

        // use constructor
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1) continue;
            if (parameters[0].ParameterType == typeof(string))
            {
                Constructor = constructor;
            }
        }

        if (ParseMethod is null && Constructor is null)
        {
            throw new InvalidOperationException("Could not find matching parse function or constructor!");
        }
    }

    #endregion Public Constructors
}

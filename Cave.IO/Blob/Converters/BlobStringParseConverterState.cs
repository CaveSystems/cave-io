using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

sealed class BlobStringParseConverterState
{
    #region Fields

    internal readonly ConstructorInfo? Constructor;
    internal readonly MethodInfo? ParseMethod;
    internal readonly Type Type;
    internal readonly bool UseCulture;
    internal bool RoundtripTest = true;
    internal BlobStringParseConverterMode Mode;

    #endregion Fields

    #region Public Constructors

    public BlobStringParseConverterState(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            type = underlying;
        }
        Type = type;

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

    #region Internal Methods

    /// <summary>Parses the specified text into an object of the target type using the configured constructor or parse method.</summary>
    /// <remarks>
    /// If a constructor is configured, it is used to create the object. Otherwise, a static or instance parse method is invoked. The current culture may be
    /// used depending on configuration.
    /// </remarks>
    /// <param name="text">The text representation to parse into an object. Cannot be null.</param>
    /// <returns>An object created by parsing the specified text.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the constructor or parse method returns null.</exception>
    internal object Parse(string text)
    {
        var useCulture = UseCulture;
        if (Constructor is not null)
        {
            return Constructor.Invoke([text]) ?? throw new InvalidOperationException("Constructor returned null.");
        }
        if (ParseMethod!.IsStatic)
        {
            return ParseMethod.Invoke(null, useCulture ? [text, CultureInfo.InvariantCulture] : [text]) ??
                throw new InvalidOperationException("Parse method returned null.");
        }
        else
        {
            var instance = Activator.CreateInstance(Type);
            return ParseMethod.Invoke(instance, useCulture ? [text, CultureInfo.InvariantCulture] : [text]) ??
                throw new InvalidOperationException("Parse method returned null.");
        }
    }

    #endregion Internal Methods
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Cave.IO
{
    /// <summary>Provides a fast and simple initialization data reader class.</summary>
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public class IniReader
    {
        #region Private Fields

        /// <summary>Holds all lines of the configuration.</summary>
        IList<string> lines;

        #endregion Private Fields

        #region Internal Methods

        internal static object ConvertValue(Type targetType, string value, IniProperties properties)
        {
            if (targetType == typeof(DateTime))
            {
                var style = DateTimeStyles.AllowWhiteSpaces | properties.DateTimeKind switch
                {
                    DateTimeKind.Utc => DateTimeStyles.AssumeUniversal,
                    DateTimeKind.Local => DateTimeStyles.AssumeLocal,
                    _ => throw new NotSupportedException($"Properties.DateTimeKind {properties.DateTimeKind} is not supported!"),
                };
                return DateTime.ParseExact(value, properties.DateTimeFormat, properties.Culture, style);
            }
            return TypeExtension.ConvertValue(targetType, value, properties.Culture);
        }

        #endregion Internal Methods

        #region Private Constructors

        /// <summary>Initializes a new instance of the <see cref="IniReader"/> class.</summary>
        /// <param name="name">The (file)name.</param>
        /// <param name="lines">The lines.</param>
        /// <param name="properties">Properties of the initialization data.</param>
        IniReader(string name, IList<string> lines, IniProperties properties = default)
        {
            FileName = name ?? throw new ArgumentNullException(nameof(name));
            Properties = properties.Valid ? properties : IniProperties.Default;
            if (Properties.Culture.Calendar is not GregorianCalendar) throw new NotSupportedException($"Calendar {Properties.Culture.Calendar} is not supported!");
            this.lines = lines;
        }

        #endregion Private Constructors

        #region Private Methods

        string[] Parse(byte[] data)
        {
            if (data.Length == 0)
            {
#if NETSTANDARD20
                return Array.Empty<string>();
#else
                return new string[0];
#endif
            }

            if ((Properties.Encryption == null) && (Properties.Compression == IniCompressionType.None))
            {
                return Properties.Encoding.GetString(data).SplitNewLine();
            }
            Stream stream = new MemoryStream(data);
            try
            {
                if (Properties.Encryption != null)
                {
                    stream = new CryptoStream(stream, Properties.Encryption.CreateDecryptor(), CryptoStreamMode.Read);
                }
                switch (Properties.Compression)
                {
                    case IniCompressionType.Deflate:
                        stream = new DeflateStream(stream, CompressionMode.Decompress, true);
                        break;

                    case IniCompressionType.GZip:
                        stream = new GZipStream(stream, CompressionMode.Decompress, true);
                        break;

                    case IniCompressionType.None: break;
                    default: throw new InvalidDataException($"Unknown compression {nameof(IniCompressionType)}.{Properties.Compression}");
                }
                var reader = new StreamReader(stream, Properties.Encoding);
                var result = reader.ReadToEnd().SplitNewLine();
                reader.Close();
                return result;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>Obtains the index (linenumber) the specified section starts.</summary>
        /// <param name="section">Section to search.</param>
        /// <returns>Returns the index the section starts at.</returns>
        int SectionStart(string section)
        {
            if (section == null)
            {
                return 0;
            }
            Ini.CheckName(section, nameof(section));

            section = "[" + section + "]";
            var i = 0;
            while (i < lines.Count)
            {
                var line = lines[i].Trim();
                if (string.Compare(line, section, !Properties.CaseSensitive, Properties.Culture) == 0)
                {
                    return i;
                }

                i++;
            }
            return -1;
        }

        #endregion Private Methods

        #region Public Properties

        /// <summary>Gets a value indicating whether the config can be reloaded.</summary>
        public bool CanReload
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                {
                    return false;
                }

                if (FileName.IndexOfAny(Path.GetInvalidPathChars()) > -1)
                {
                    return false;
                }

                try
                {
                    return File.Exists(FileName);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Error at {nameof(IniReader)}.{nameof(CanReload)} checking File.Exists(FileName):");
                    Trace.TraceError($"{ex}");
                    return false;
                }
            }
        }

        /// <summary>Gets the culture used to decode values.</summary>
        public CultureInfo Culture => Properties.Culture;

        /// <summary>Gets the name of the settings.</summary>
        public string FileName { get; }

        /// <summary>Gets or sets the properties.</summary>
        public IniProperties Properties { get; set; }

        #endregion Public Properties

        #region static constructors

        /// <summary>Loads initialization data from file.</summary>
        /// <param name="fileName">File name to read.</param>
        /// <param name="properties">The content properties.</param>
        /// <returns>Returns a new <see cref="IniReader"/> instance.</returns>
        public static IniReader FromFile(string fileName, IniProperties properties = default) => File.Exists(fileName)
                ? Parse(fileName, File.ReadAllBytes(fileName), properties)
#if NETSTANDARD20
                : new IniReader(fileName, Array.Empty<string>(), properties);

#else
                : new IniReader(fileName, new string[0], properties);

#endif

        /// <summary>Loads initialization data from stream.</summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The stream to read.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="properties">The content properties.</param>
        /// <returns>Returns a new <see cref="IniReader"/> instance.</returns>
        public static IniReader FromStream(string name, Stream stream, int count, IniProperties properties = default)
        {
            var data = stream.ReadBlock(count);
            return Parse(name, data, properties);
        }

        /// <summary>Parses initialization data.</summary>
        /// <param name="name">The (file)name.</param>
        /// <param name="data">Content to parse.</param>
        /// <param name="properties">The data properties.</param>
        /// <returns>Returns a new <see cref="IniReader"/> instance.</returns>
        public static IniReader Parse(string name, string data, IniProperties properties = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new IniReader(name, data.SplitNewLine(), properties);
        }

        /// <summary>Parses initialization data.</summary>
        /// <param name="name">The name.</param>
        /// <param name="data">Content to parse.</param>
        /// <param name="properties">The data properties.</param>
        /// <returns>Returns a new <see cref="IniReader"/> instance.</returns>
        public static IniReader Parse(string name, byte[] data, IniProperties properties = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Parse(name, Encoding.UTF8.GetString(data), properties);
        }

        /// <summary>Loads initialization data from strings.</summary>
        /// <param name="name">The name.</param>
        /// <param name="lines">Content to parse.</param>
        /// <param name="properties">The content properties.</param>
        /// <returns>Returns a new <see cref="IniReader"/> instance.</returns>
        public static IniReader Parse(string name, string[] lines, IniProperties properties = default)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            return new IniReader(name, (string[])lines.Clone(), properties);
        }

        #endregion static constructors

        #region Public Methods

        /// <summary>Obtains all section names present at the file.</summary>
        /// <returns>Returns an array of all section names.</returns>
        public string[] GetSectionNames()
        {
            var result = new List<string>();
            foreach (var line in lines)
            {
                var trimed = line.Trim();
                if (trimed.StartsWith("[", StringComparison.OrdinalIgnoreCase) && trimed.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                {
                    var section = trimed.Substring(1, trimed.Length - 2).Trim();
                    Ini.CheckName(section, nameof(section));
                    result.Add(section);
                }
            }
            return result.ToArray();
        }

        /// <summary>Obtains whether a specified section exists or not.</summary>
        /// <param name="section">Section to search.</param>
        /// <returns>Returns true if the sections exists false otherwise.</returns>
        public bool HasSection(string section) => SectionStart(section) > -1;

        /// <summary>Reads a whole section from the ini (automatically removes empty lines and comments).</summary>
        /// <param name="section">Name of the section.</param>
        /// <returns>Returns an array of string containing all section lines.</returns>
        public string[] ReadSection(string section) => ReadSection(section, true);

        /// <summary>Reads a whole section from the ini.</summary>
        /// <param name="section">Name of the section.</param>
        /// <param name="remove">Remove comments and empty lines.</param>
        /// <returns>Returns the whole section as string array.</returns>
        public string[] ReadSection(string section, bool remove)
        {
            // find section
            int i;
            if (section == null)
            {
                i = -1;
            }
            else
            {
                i = SectionStart(section);
                if (i < 0)
                {
                    // empty or not present
#if NETSTANDARD20
                    return Array.Empty<string>();
#else
                    return new string[0];
#endif
                }
            }

            // got it, add lines to result
            var result = new List<string>();
            for (; ++i < lines.Count;)
            {
                var line = lines[i];
                if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (remove)
                {
                    // remove comments and empty lines
                    var comment = line.IndexOfAny(new char[] { '#', ';' });
                    if (comment > -1)
                    {
                        // only remove if comment marker is the first character
                        var whiteSpace = line.Substring(0, comment);
                        if (string.IsNullOrEmpty(whiteSpace) || (whiteSpace.Trim().Length == 0))
                        {
                            continue;
                        }
                    }
                    if (line.Trim().Length == 0)
                    {
                        continue;
                    }
                }
                result.Add(line);
            }
            return result.ToArray();
        }

        /// <summary>Reads a setting from the ini.</summary>
        /// <param name="section">Sectionname of the setting.</param>
        /// <param name="settingName">Name of the setting.</param>
        /// <returns>Returns null if the setting is not present a string otherwise.</returns>
        public string ReadSetting(string section, string settingName)
        {
            // find section
            var i = SectionStart(section);
            if (i < 0)
            {
                return null;
            }

            // iterate all lines
            for (++i; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase) && line.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // ignore comments
                if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (line.StartsWith(";", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // find equal sign
                var sign = line.IndexOf('=');
                if (sign > -1)
                {
                    // got a setting, check name
                    var name = line.Substring(0, sign).Trim();
                    if (string.Compare(settingName, name, !Properties.CaseSensitive, Properties.Culture) == 0)
                    {
                        var value = line.Substring(sign + 1).Trim();
                        if (value.Length < 1)
                        {
                            return string.Empty;
                        }

                        if (value.IndexOf(Properties.BoxCharacter) > -1)
                        {
                            return Ini.Unescape(value, Properties);
                        }
                        var comment = value.IndexOf('#');
                        if (comment > -1)
                        {
                            value = value.Substring(0, comment).Trim();
                        }

                        return Ini.Unescape(value, Properties);
                    }
                }
            }

            // no setting with the specified name found
            return null;
        }

        /// <summary>Reload the whole config.</summary>
        public void Reload()
        {
            if (!CanReload)
            {
                throw new InvalidOperationException($"Reloading not possible. Check File.Exists({nameof(FileName)});");
            }

            lines = Parse(File.ReadAllBytes(FileName));
        }

        #endregion Public Methods

        #region ReadEnums

        /// <summary>Reads a whole section as values of an enum and returns them as array.</summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="section">The Section to read.</param>
        /// <param name="throwEx">Throw an error for any unknown value in the section.</param>
        /// <returns>Returns an array of values.</returns>
        public T[] ReadEnums<T>(string section, bool throwEx = true)
            where T : struct
        {
            // iterate all lines of the section
            var result = new List<T>();
            foreach (var value in ReadSection(section, true))
            {
                // try to parse enum value
                try
                {
                    result.Add((T)Enum.Parse(typeof(T), value.Trim(), true));
                }
                catch (Exception ex)
                {
                    if (throwEx)
                    {
                        throw;
                    }

                    Trace.TraceWarning($"Ignoring Invalid Enum Value: {value}, Section: {section}, {ex}");
                }
            }
            return result.ToArray();
        }

        #endregion ReadEnums

        #region ReadStruct

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        [Obsolete("Use ReadStructFields instead")]
        public T ReadStruct<T>(string section, bool throwEx = true)
            where T : struct
            => ReadStructFields<T>(section, throwEx);

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="item">The structure.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        [Obsolete("Use ReadStructFields instead")]
        public bool ReadStruct<T>(string section, ref T item, bool throwEx = true)
            where T : struct
            => ReadStructFields<T>(section, ref item, throwEx);

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        public T ReadStructFields<T>(string section, bool throwEx = true)
            where T : struct
        {
            object result = default(T);
            ReadObjectFields(section, result, throwEx);
            return (T)result;
        }

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="item">The structure.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        public bool ReadStructFields<T>(string section, ref T item, bool throwEx = true)
            where T : struct
        {
            object box = item;
            var result = ReadObjectFields(section, box, throwEx);
            item = (T)box;
            return result;
        }

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        public T ReadStructProperties<T>(string section, bool throwEx = true)
            where T : struct
        {
            object result = default(T);
            ReadObjectProperties(section, result, throwEx);
            return (T)result;
        }

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="item">The structure.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        public bool ReadStructProperties<T>(string section, ref T item, bool throwEx = true)
            where T : struct
        {
            object box = item;
            var result = ReadObjectProperties(section, box, throwEx);
            item = (T)box;
            return result;
        }

        #endregion ReadStruct

        #region ReadObject

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any invalid value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        [Obsolete("Use ReadObjectFields instead")]
        public T ReadObject<T>(string section, bool throwEx = false)
            where T : class, new()
            => ReadObjectFields<T>(section, throwEx);

        /// <summary>Reads a whole section as values of an object (this does not work with structs).</summary>
        /// <param name="section">Section to read.</param>
        /// <param name="container">Container to set the field at.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        [Obsolete("Use ReadObjectFields instead")]
        public bool ReadObject(string section, object container, bool throwEx = false) => ReadObjectFields(section, container, throwEx);

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any invalid value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        public T ReadObjectFields<T>(string section, bool throwEx = false)
            where T : class, new()
        {
            object result = new T();
            ReadObjectFields(section, result, throwEx);
            return (T)result;
        }

        /// <summary>Reads a whole section as values of an object (this does not work with structs).</summary>
        /// <param name="section">Section to read.</param>
        /// <param name="container">Container to set the field at.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        public bool ReadObjectFields(string section, object container, bool throwEx = false)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            // iterate all fields of the struct
            var type = container.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(f => !f.HasAttribute<IniIgnoreAttribute>());
            if (!fields.Any())
            {
                if (!throwEx)
                {
                    return false;
                }

                throw new ArgumentException($"Container type {container.GetType()} does not have any {nameof(fields)}!", nameof(container));
            }
            var result = true;
            var i = 0;
            foreach (var field in fields)
            {
                i++;

                if (field.HasAttribute<IniSectionAttribute>())
                {
                    var sectionAttribute = field.GetAttribute<IniSectionAttribute>();
                    var fieldObject = Activator.CreateInstance(field.FieldType);
                    switch (sectionAttribute.SettingsType)
                    {
                        case IniSettingsType.Fields: ReadObjectFields(sectionAttribute.Name ?? field.Name, fieldObject); break;
                        case IniSettingsType.Properties: ReadObjectProperties(sectionAttribute.Name ?? field.Name, fieldObject); break;
                        default: throw new NotImplementedException($"IniSettingsType.{sectionAttribute.SettingsType} not implemented at {GetType()}!");
                    }
                    field.SetValue(container, fieldObject);
                    continue;
                }

                // yes, can we read a value from the config for this field ?
                var value = ReadSetting(section, field.Name);
                if (value is null)
                {
                    Trace.TraceError($"Field is not set, using default value: {field.FieldType.Name} {field.Name}");
                    continue;
                }

                // yes, try to set value to field
                try
                {
                    var obj = ConvertValue(field.FieldType, value, Properties);
                    field.SetValue(container, obj);
                }
                catch (Exception ex)
                {
                    var message = $"Invalid field value {value} for field {field.FieldType.Name} {field.Name}";
                    if (throwEx)
                    {
                        throw new InvalidDataException(message, ex);
                    }
                    else
                    {
                        Trace.TraceWarning(message);
                    }

                    result = false;
                }
            }
            if (i == 0)
            {
                var message = $"No field in section {section}!";
                if (throwEx)
                {
                    throw new ArgumentException(message, nameof(container));
                }
                else
                {
                    Trace.TraceWarning(message);
                }

                result = false;
            }
            return result;
        }

        /// <summary>Reads a whole section as values of a struct.</summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="section">Section to read.</param>
        /// <param name="throwEx">Throw an error for any invalid value in the section.</param>
        /// <returns>Returns a new struct instance.</returns>
        public T ReadObjectProperties<T>(string section, bool throwEx = false)
            where T : class, new()
        {
            object result = new T();
            ReadObjectProperties(section, result, throwEx);
            return (T)result;
        }

        /// <summary>Reads a whole section as values of an object (this does not work with structs).</summary>
        /// <param name="section">Section to read.</param>
        /// <param name="container">Container to set the field at.</param>
        /// <param name="throwEx">Throw an error for any unset value in the section.</param>
        /// <returns>Returns true if all fields could be read. Throws an exception or returns false otherwise.</returns>
        public bool ReadObjectProperties(string section, object container, bool throwEx = false)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            // iterate all fields of the struct
            var type = container.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.HasAttribute<IniIgnoreAttribute>());
            if (!properties.Any())
            {
                if (!throwEx)
                {
                    return false;
                }

                throw new ArgumentException($"Container type {container.GetType()} does not have any {nameof(properties)}!", nameof(container));
            }
            var result = true;
            var i = 0;
            foreach (var property in properties)
            {
                i++;

                if (property.HasAttribute<IniSectionAttribute>())
                {
                    var sectionAttribute = property.GetAttribute<IniSectionAttribute>();
                    var fieldObject = Activator.CreateInstance(property.PropertyType);
                    switch (sectionAttribute.SettingsType)
                    {
                        case IniSettingsType.Fields: ReadObjectFields(sectionAttribute.Name ?? property.Name, fieldObject); break;
                        case IniSettingsType.Properties: ReadObjectProperties(sectionAttribute.Name ?? property.Name, fieldObject); break;
                        default: throw new NotImplementedException($"IniSettingsType.{sectionAttribute.SettingsType} not implemented at {GetType()}!");
                    }
                    property.SetValue(container, fieldObject, null);
                    continue;
                }

                // yes, can we read a value from the config for this field ?
                var value = ReadSetting(section, property.Name);
                if (value is null)
                {
                    Trace.TraceError($"Field is not set, using default value: {property.PropertyType.Name} {property.Name}");
                    continue;
                }

                // yes, try to set value to field
                try
                {
                    var obj = ConvertValue(property.PropertyType, value, Properties);
                    property.SetValue(container, obj, null);
                }
                catch (Exception ex)
                {
                    var message = $"Invalid field value {value} for field {property.PropertyType.Name} {property.Name}";
                    if (throwEx)
                    {
                        throw new InvalidDataException(message, ex);
                    }
                    else
                    {
                        Trace.TraceWarning(message);
                    }

                    result = false;
                }
            }
            if (i == 0)
            {
                var message = $"No field in section {section}!";
                if (throwEx)
                {
                    throw new ArgumentException(message, nameof(container));
                }
                else
                {
                    Trace.TraceWarning(message);
                }

                result = false;
            }
            return result;
        }

        #endregion ReadObject

        #region Read Value Members

        /// <summary>Reads a bool value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public bool ReadBool(string section, string name, bool? defaultValue = null)
        {
            var result = false;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a date time value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public DateTime ReadDateTime(string section, string name, DateTime? defaultValue = null)
        {
            var result = DateTime.MinValue;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a decimal value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public decimal ReadDecimal(string section, string name, decimal? defaultValue = null)
        {
            decimal result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a double value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public double ReadDouble(string section, string name, double? defaultValue = null)
        {
            double result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads the enum.</summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public T ReadEnum<T>(string section, string name, T? defaultValue = null)
            where T : struct, IConvertible
        {
            var result = default(T);
            if (!GetEnum(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a float value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public float ReadFloat(string section, string name, float? defaultValue = null)
        {
            float result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a int32 value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public int ReadInt32(string section, string name, int? defaultValue = null)
        {
            var result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a int64 value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public long ReadInt64(string section, string name, long? defaultValue = null)
        {
            long result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a string value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public string ReadString(string section, string name, string defaultValue = null)
        {
            string result = null;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a time span value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public TimeSpan ReadTimeSpan(string section, string name, TimeSpan? defaultValue = null)
        {
            var result = TimeSpan.Zero;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a uint32 value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public uint ReadUInt32(string section, string name, uint? defaultValue = null)
        {
            uint result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        /// <summary>Reads a uint64 value.</summary>
        /// <param name="section">The section.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns the (converted) value if a value is present or the default value if not.</returns>
        public ulong ReadUInt64(string section, string name, ulong? defaultValue = null)
        {
            ulong result = 0;
            if (!GetValue(section, name, ref result))
            {
                result = defaultValue ?? throw new InvalidDataException($"Section [{section}] Setting {name} is unset! You can set {nameof(defaultValue)} to define a implicit result and disable this exception.");
            }
            return result;
        }

        #endregion Read Value Members

        #region GetValue Members

        /// <summary>Directly obtains a (enum) value from the specified subsection(s) with the specified name.</summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetEnum<T>(string section, string name, ref T value)
            where T : struct, IConvertible
        {
            var data = value.ToString();
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            var result = data.TryParse(out T resultValue);
            if (result)
            {
                value = resultValue;
            }

            return result;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref bool value)
        {
            var v = ReadSetting(section, name);
            if (!string.IsNullOrEmpty(v))
            {
                if (bool.TryParse(v, out var b))
                {
                    value = b;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref string value)
        {
            var v = ReadSetting(section, name);
            if (string.IsNullOrEmpty(v))
            {
                return false;
            }

            value = v;
            return true;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref int value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (int.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref uint value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (uint.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref long value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (long.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref ulong value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (ulong.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref float value)
        {
            var data = value.ToString("R", Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (float.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref double value)
        {
            var data = value.ToString("R", Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (double.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref decimal value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (decimal.TryParse(data, NumberStyles.Any, Culture, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref DateTime value)
        {
            var data = value.ToString(Culture);
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            var style = DateTimeStyles.AllowWhiteSpaces | Properties.DateTimeKind switch
            {
                DateTimeKind.Utc => DateTimeStyles.AssumeUniversal,
                DateTimeKind.Local => DateTimeStyles.AssumeLocal,
                _ => throw new NotSupportedException($"Properties.DateTimeKind {Properties.DateTimeKind} is not supported!"),
            };
            if (DateTime.TryParseExact(data, Properties.DateTimeFormat, Properties.Culture, style, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>Directly obtains a value from the specified subsection(s) with the specified name.</summary>
        /// <param name="section">The subsection(s).</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The default value.</param>
        /// <returns>Returns true if the setting exist and the read value was returned, true otherwise (default value returned).</returns>
        public bool GetValue(string section, string name, ref TimeSpan value)
        {
            var data = value.ToString();
            if (!GetValue(section, name, ref data))
            {
                return false;
            }

            if (TimeSpan.TryParse(data, out var result))
            {
                value = result;
                return true;
            }
            return false;
        }

        #endregion GetValue Members

        /// <summary>Obtains a string array with the whole configuration.</summary>
        /// <returns>Returns an array containing all strings (lines) of the configuration.</returns>
        public string[] ToArray()
        {
            var result = new string[lines.Count];
            for (var i = 0; i < lines.Count; i++)
            {
                result[i] = Ini.Escape(lines[i], Properties);
            }
            lines.CopyTo(result, 0);
            return result;
        }

        /// <summary>Retrieves the whole data as string.</summary>
        /// <returns>Returns a new string.</returns>
        public override string ToString() => StringExtensions.JoinNewLine(ToArray());
    }
}

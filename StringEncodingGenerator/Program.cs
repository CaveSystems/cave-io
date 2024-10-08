﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cave;
using Cave.IO;

namespace StringEncodingGenerator;

sealed class Program
{
    static StreamWriter? writer;

    static void WriteLine(string? text = null, params object[] args)
    {
        if (text == null)
        {
            Console.WriteLine();
            writer?.WriteLine();
        }
        else if (args.Length == 0)
        {
            Console.WriteLine(text);
            writer?.WriteLine(text);
        }
        else
        {
            Console.WriteLine(text, args);
            writer?.WriteLine(text, args);
        }
    }

    static void Header()
    {
        WriteLine("using System;");
        WriteLine("using System.ComponentModel;");
        WriteLine();
        WriteLine("#pragma warning disable CA1707");
        WriteLine();
        WriteLine("namespace Cave.IO;");
        WriteLine();
        WriteLine("/// <summary>Provides supported string encodings.</summary>");
        WriteLine("public enum StringEncoding");
        WriteLine("{");
        WriteLine("\t/// <summary>Character set not defined.</summary>");
        WriteLine("\tUndefined = 0,");
        WriteLine();
        WriteLine("\t#region internally handled fast encodings");
        WriteLine();
        WriteLine("\t/// <summary>7 Bit per character.</summary>");
        WriteLine("\t[Description(\"{0} | {1}\")]", Encoding.ASCII.EncodingName, Encoding.ASCII.WebName);
        WriteLine($"\t[Obsolete(\"Use {nameof(StringEncoding.US_ASCII)}\")]");
        WriteLine("\tASCII = 1,");
        WriteLine();
        WriteLine("\t/// <summary>8 Bit per character Unicode</summary>");
        WriteLine("\t[Description(\"{0} | {1}\")]", Encoding.UTF8.EncodingName, Encoding.UTF8.WebName);
        WriteLine($"\t[Obsolete(\"Use {nameof(StringEncoding.UTF_8)}\")]");
        WriteLine("\tUTF8 = 2,");
        WriteLine();
        WriteLine("\t/// <summary>Little endian 16 Bit per character unicode.</summary>");
        WriteLine("\t[Description(\"{0} | {1}\")]", Encoding.Unicode.EncodingName, Encoding.Unicode.WebName);
        WriteLine($"\t[Obsolete(\"Use {nameof(StringEncoding.UTF_16)}\")]");
        WriteLine("\tUTF16 = 3,");
        WriteLine();
        WriteLine("\t/// <summary>Little endian 32 Bit per character unicode.</summary>");
        WriteLine("\t[Description(\"{0} | {1}\")]", Encoding.UTF32.EncodingName, Encoding.UTF32.WebName);
        WriteLine($"\t[Obsolete(\"Use {nameof(StringEncoding.UTF_32)}\")]");
        WriteLine("\tUTF32 = 4,");
        WriteLine();
        WriteLine("\t#endregion internally handled fast encodings");
        WriteLine();
    }

    static void Main()
    {
#if NET5_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        Thread.CurrentThread.CurrentUICulture =
            Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

        writer = File.CreateText("StringEncoding.cs");
        Header();
        WriteLine("#region autogenerated enum values");
        Dictionary<string, int> names = [];
        foreach (var item in Encoding.GetEncodings().ToDictionary(e => e.CodePage).OrderBy(e => e.Key))
        {
            var encodingInfo = item.Value;
            var encoding = item.Value.GetEncoding();
            var windowsCodePage = TryGetCodePage(encoding);
            WriteLine("/// <summary>{0}</summary>", encodingInfo.DisplayName);
            WriteLine("/// <remarks>Codepage: {0}, Windows Codepage: {1}</remarks>", encoding.CodePage, windowsCodePage);
            WriteLine("[Description(\"{0} | {1}\")]", encoding.EncodingName, encoding.WebName);
            var name = encodingInfo.Name.ReplaceInvalidChars(ASCII.Strings.Letters + ASCII.Strings.Digits, "_").ToUpper(CultureInfo.InvariantCulture);
            names.TryGetValue(name, out var number);
            names[name] = ++number;
            if (number > 1) name += "_" + number;
            WriteLine("{0} = {1},", name, item.Key);
            WriteLine();
        }
        WriteLine("#endregion autogenerated enum values");
        WriteLine("}");
        writer.Close();
    }

    private static int TryGetCodePage(Encoding encoding) { try { return encoding.WindowsCodePage; } catch { return 0; } }
}

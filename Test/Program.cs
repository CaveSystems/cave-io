#if NETCOREAPP1_0_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
#define ALTERNATE_CODE
#endif

using NUnit.Framework;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Tests.Cave.IO;
using Tests.Cave.IO.IniFile;

namespace Test;

[ExcludeFromCodeCoverage]
class Program
{
    static bool initialized;

    public static void Init()
    {
        if (!initialized)
        {
#if NET5_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            initialized = true;
        }
    }

    public const bool Verbose = false;

    static int Main(string[] args)
    {
        try
        {
            Init();
            var errors = 0;

#if ALTERNATE_CODE
            var asm = typeof(Program).GetTypeInfo().Assembly;
            var targetFramework = asm.GetCustomAttributes<TargetFrameworkAttribute>().FirstOrDefault();
            var frameworkVersion = targetFramework.FrameworkDisplayName;
            var types = asm.DefinedTypes.Select(t => t.AsType());
#else
            var types = typeof(Program).Assembly.GetTypes().ToArray();
            var frameworkVersion = "net " + Environment.Version;
#endif
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(frameworkVersion);
            Console.WriteLine();
            foreach (var type in types)
            {
                Console.ResetColor();
#if ALTERNATE_CODE
                var attrib = type.GetTypeInfo().GetCustomAttribute<TestFixtureAttribute>();
                if (attrib is not TestFixtureAttribute) continue;
#else
                var typeAttributes = type.GetCustomAttributes(typeof(TestFixtureAttribute), false).ToArray();
                var typeAttributesCount = typeAttributes.Length;
                if (typeAttributesCount == 0)
                {
                    continue;
                }
#endif

                Console.WriteLine("---");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Create " + type);
                Console.ResetColor();
                Console.WriteLine("---");
                var instance = Activator.CreateInstance(type);
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    var methodAttributes = method.GetCustomAttributes(typeof(TestAttribute), false).ToArray();

#if ALTERNATE_CODE
                var methodAttributesCount = methodAttributes.Count();
#else
                    var methodAttributesCount = methodAttributes.Length;
#endif
                    if (methodAttributesCount == 0)
                    {
                        continue;
                    }

                    GC.Collect(999, GCCollectionMode.Forced);

                    Console.ResetColor();
                    Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0001: Start {method.Name}: {frameworkVersion}");
                    try
                    {
                        method.Invoke(instance, null);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0002: Success {method.Name}: {frameworkVersion}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"{method.DeclaringType.Name}.cs: error TE0001: {ex.Message}: {frameworkVersion}");
                        Console.Error.WriteLine(ex.ToString());
                        Console.Error.WriteLine(ex.StackTrace);
                        errors++;
                    }
                }
            }
            if (errors == 0)
            {
                Console.ResetColor();
                Console.WriteLine("---");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"All tests successfully completed: {frameworkVersion}");
                Console.ResetColor();
                Console.WriteLine("---");
            }
            else
            {
                Console.ResetColor();
                Console.WriteLine("---");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"{errors} tests failed: {frameworkVersion}");
                Console.ResetColor();
                Console.WriteLine("---");
            }

            if (Debugger.IsAttached)
            {
                WaitExit();
            }

            return errors;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Fatal error on test runner startup!");
            Console.Error.WriteLine(ex.ToString());
            return -1;
        }
    }

#if ALTERNATE_CODE
    static void WaitExit() { }
#else

    static void WaitExit()
    {
        Console.Write("--- press enter to exit ---");
        while (Console.ReadKey(true).Key != ConsoleKey.Enter)
        {
            ;
        }
    }

#endif
}

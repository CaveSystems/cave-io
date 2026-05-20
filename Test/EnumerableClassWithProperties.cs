using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cave;
using Cave.IO.Blob;
using Cave.Security;

namespace Tests.Cave.IO;

public class EnumerableClassWithProperties : IEnumerable<string>
{
    public EnumerableClassWithProperties()
    {
        myStrings = new[] { "Hello", "World", RNG.GetAscii(12) };
    }

    string[] myStrings;

    public int SomeValue { get; set; } = RNG.Int16;

    public Uri Uri { get; set; } = new Uri("https://example.com/" + RNG.GetAscii(8));

    public TestClass SomeObject { get; set; } = TestClass.Create(RNG.UInt16);

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)myStrings).GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => myStrings.GetEnumerator();
}

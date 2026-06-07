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

public sealed record Level4Node(Guid Id, Level5Payload Payload, double Min, double Max) : BaseRecord
{
    Level4Node() : this(Guid.Empty, null, 0.0, 0.0) { }
}

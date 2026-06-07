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

public sealed record RootRecord(Guid Id, DateTime CreatedUtc, Level2Node A, Level2Node B) : BaseRecord
{
    RootRecord() : this(Guid.Empty, default, null, null) { }
}

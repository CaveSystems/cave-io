using System;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

record BlobEnumerableConverterData(Type ElementType, ConstructorInfo? Constructor) : BaseRecord;

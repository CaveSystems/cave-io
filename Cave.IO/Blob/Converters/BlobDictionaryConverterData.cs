using System;
using System.Reflection;

namespace Cave.IO.Blob.Converters;

record BlobDictionaryConverterData(ConstructorInfo? Constructor, Type KeyType, Type ValueType, BlobDictionaryConverterMode Mode) : BaseRecord;

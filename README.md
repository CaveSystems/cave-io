# Cave.IO

High-performance .NET I/O utilities for binary data processing, endian conversion, buffering primitives, bit-level stream access, INI files, and lightweight serialization.

> Repository: https://github.com/CaveSystems/cave-io  
> License: MIT

---

## Overview

`Cave.IO` is a multi-targeted .NET library focused on fast, low-overhead data handling.  
It provides tools commonly needed in protocol stacks, file formats, embedded systems, and performance-sensitive backend systems.

The package includes:

- Fast low-overhead data readers and writers
- Endian conversion utilities
- Bit-level stream readers and writers
- Ring buffer utilities
- High-performance struct marshalling
- INI readers and writers
- `BinarySerializer` for lightweight object serialization
- `BlobSerializer` for version-resilient binary serialization
  - reflection-based converters
  - custom converters
  - dictionary and enumerable support

---

## Features

- **Performance-oriented I/O** with minimal overhead
- **Broad framework support** from legacy .NET Framework to modern .NET
- **Binary-first design** for protocol and storage scenarios
- **Flexible serialization** for evolving data contracts
- **Simple integration** into existing stream-based workflows

---

## Supported Target Frameworks

- `.NET Framework 2.0`
- `.NET Framework 3.5`
- `.NET Framework 4.0`
- `.NET Framework 4.5`
- `.NET Framework 4.6`
- `.NET Framework 4.7`
- `.NET Framework 4.8`
- `.NET Standard 2.0`
- `.NET Standard 2.1`
- `.NET 8`

---

## Installation

### Package Manager
```
Install-Package Cave.IO
```

### .NET CLI
```
dotnet add package Cave.IO
```

## Typical Use Cases

### 1) Fast Binary Read/Write

Use `Cave.IO` readers and writers for efficient primitive and structured binary access in custom file and protocol formats.

- Use `DataReader` and `DataWriter` for non-buffering, low-overhead access to binary data.
- Use `BitStreamReader` and `BitStreamWriter` for bit-level manipulation in compact binary formats.

### 2) Endian-Safe Processing

Use endian conversion helpers when working with network protocols, device data, or cross-platform binary formats.

### 3) Buffering and Ring Buffers

Use ring buffers for producer-consumer pipelines and other streaming scenarios with controlled memory usage.

- Use `RingBuffer<TValue>` for lock-free buffering with fixed capacity.
- Use `CircularBuffer<TValue>` for fixed-capacity ring buffering with overflow prevention.

### 4) INI Configuration

Read and write INI-style configuration files with lightweight parsing and serialization utilities.

### 5) Object Serialization

- Use `BinarySerializer` for straightforward binary object serialization.
- Use `BlobSerializer` when schema evolution and converter extensibility are required.
- Use `LittleEndian`, `BigEndian`, `BitConverterLE`, and `BitConverterBE` for endian-aware struct marshalling and byte-array conversions.

---

## Serialization Notes

`BlobSerializer` is designed for robust binary serialization in long-lived systems:

- Custom converters for domain-specific types
- Reflection-based conversion support
- Support for collections such as dictionaries and enumerables
- Better adaptability to changing object structures

This is useful when backward and forward compatibility matter.

---

## Repository Structure

- `Cave.IO` - main library project
- `StringEncodingGenerator` - tooling and generator project
- `Test` - framework-specific test projects and validation

---

## Versioning and Compatibility

- Designed to support both legacy and current .NET environments
- Prefer the latest package version for fixes and performance improvements
- Validate serialization format compatibility in integration tests when upgrading

---

## Contributing

Contributions are welcome.

1. Fork the repository
2. Create a feature or fix branch
3. Add or update tests
4. Open a pull request with clear change notes

Please keep changes focused and maintain backward compatibility where possible.

---

## License

This project is licensed under the MIT License.  
See [`LICENSE`](LICENSE) for details.

---

## Links

- GitHub: https://github.com/CaveSystems/cave-io
- Issues: https://github.com/CaveSystems/cave-io/issues

# IBinaryStream

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** defines the byte-oriented operations used by packers and unpackers.

## UML class card

```mermaid
classDiagram
    direction TB
    class IBinaryPackable
    class IBinaryUnpackable
    class IBinaryStream {
      <<interface>>
      +CanRead bool
      +CanWrite bool
      +Position long
      +Length long
      +Read(buffer: Span~byte~) int
      +Write(buffer: ReadOnlySpan~byte~) void
      +Flush() void
      +Dispose() void
    }
    class MemoryBinaryStream
    class StreamBinaryStream
    IBinaryPackable ..> IBinaryStream : writes
    IBinaryUnpackable ..> IBinaryStream : reads
    MemoryBinaryStream ..|> IBinaryStream
    StreamBinaryStream ..|> IBinaryStream
```

## Use cases

- Share one byte-oriented contract across domain packers and unpackers.
- Use the in-memory adapter for tests or wrap a .NET Stream for integration.

## Contract boundary

Read returns the actual byte count; Position and Length can fail if the underlying stream does not support them. See [full UML card](../docs/UML/IBinaryStreamUML.md) and [source](IBinaryStream.cs).

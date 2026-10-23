# MemoryBinaryStream

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** supplies a memory-backed IBinaryStream.

## UML class card

```mermaid
classDiagram
    direction TB
    class IBinaryPackable
    class IBinaryUnpackable
    class IBinaryStream {
      <<interface>>
    }
    class MemoryBinaryStream {
      +MemoryBinaryStream()
      +MemoryBinaryStream(data: byte[])
      +ToArray() byte[]
      +Read(buffer: Span~byte~) int
      +Write(buffer: ReadOnlySpan~byte~) void
      +Dispose() void
    }
    IBinaryPackable ..> IBinaryStream : writes
    IBinaryUnpackable ..> IBinaryStream : reads
    MemoryBinaryStream ..|> IBinaryStream
```

## Use cases

- Test pack/unpack implementations without filesystem or network dependencies.
- Inspect the resulting byte sequence using ToArray.
- Initialize from a byte array and read from position zero.

## Ownership and behavior

The constructor copies initial data into its internal MemoryStream. The instance supports reading and writing and disposes its internal stream when disposed. See [source](MemoryBinaryStream.cs).

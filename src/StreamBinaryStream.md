# StreamBinaryStream

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** adapts a .NET Stream to IBinaryStream without adding a format layer.

## UML class card

```mermaid
classDiagram
    direction TB
    class IBinaryStream {
      <<interface>>
    }
    class StreamBinaryStream {
      +StreamBinaryStream(stream: Stream)
      +CanRead bool
      +CanWrite bool
      +Position long
      +Length long
      +Read(buffer: Span~byte~) int
      +Write(buffer: ReadOnlySpan~byte~) void
      +Flush() void
      +Dispose() void
    }
    StreamBinaryStream ..|> IBinaryStream
    StreamBinaryStream --> Stream : delegates to
```

## Use cases

- Integrate an existing .NET stream with the package's byte-oriented contracts.
- Reuse the underlying stream's read/write capabilities and position semantics.

## Ownership and behavior

The adapter delegates operations and capability properties to the supplied stream. Disposing the adapter disposes that underlying stream. Position and Length may throw if the underlying stream does not support them. See [source](StreamBinaryStream.cs).

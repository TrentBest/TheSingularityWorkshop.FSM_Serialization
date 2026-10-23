# IBinaryPackable

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** declares the write direction of a domain-owned binary representation.

## UML class card

```mermaid
classDiagram
    direction TB
    class DomainType {
      +Pack(stream: IBinaryStream) void
    }
    class IBinaryPackable {
      <<interface>>
      +Pack(stream: IBinaryStream) void
    }
    DomainType ..|> IBinaryPackable : implements
    IBinaryPackable ..> IBinaryStream : writes bytes to
```

## Use cases

- A domain type emits a representation without committing to a package-wide object graph format.
- A producer exposes only the write capability it needs.

## Contract boundary

Pack defines no universal field order or encoding. The implementing type owns the bytes' meaning and must define its format. See [Byte Operations](../docs/BYTE_OPERATIONS.md#packing-forward) and [source](IBinaryPackable.cs).

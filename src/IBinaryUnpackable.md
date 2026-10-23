# IBinaryUnpackable

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** declares the read direction of a domain-owned binary representation.

## UML class card

```mermaid
classDiagram
    direction TB
    class IBinaryUnpackable {
      <<interface>>
      +Unpack(stream: IBinaryStream) void
    }
    class DomainType {
      +Unpack(stream: IBinaryStream) void
    }
    DomainType ..|> IBinaryUnpackable : implements
    IBinaryUnpackable ..> IBinaryStream : reads bytes from
```

## Use cases

- A type reconstructs its state from a representation defined by its domain contract.
- A read-only consumer avoids advertising a write capability it does not need.

## Contract boundary

Read may return fewer bytes than requested. The implementation must handle short reads, malformed input, and format-specific validation. See [Byte Operations](../docs/BYTE_OPERATIONS.md#unpacking-reverse) and [source](IBinaryUnpackable.cs).

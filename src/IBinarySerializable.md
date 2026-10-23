# IBinarySerializable

![The Singularity Workshop animated moniker](https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Renderer/development/docs/assets/moniker.svg)

**Purpose:** combines the pack and unpack contracts for a type that supports both directions.

## UML class card

```mermaid
classDiagram
    direction TB
    class IBinarySerializable {
      <<interface>>
    }
    class IBinaryPackable {
      <<interface>>
      +Pack(stream: IBinaryStream) void
    }
    class IBinaryUnpackable {
      <<interface>>
      +Unpack(stream: IBinaryStream) void
    }
    IBinarySerializable --|> IBinaryPackable
    IBinarySerializable --|> IBinaryUnpackable
```

## Use cases

- A domain-owned value is both written and reconstructed.
- A test exercises a full round trip through IBinaryStream.

## Contract boundary

The interface adds no members and does not define a universal format. It simply combines two capabilities. See [source](IBinarySerializable.cs).

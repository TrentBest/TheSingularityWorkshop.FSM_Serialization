# TheSingularityWorkshop.FSM_Serialization

![FSM Serialization — The Binary Boundary](docs/images/fsm-serialization-01-boundary.jpg)

**Deterministic binary representation for the FSM ecosystem.**

FSM_Serialization defines the boundary between an in-memory semantic model and a byte representation that can be persisted, transported, cached, versioned, compared, or reconstructed.

The package is deliberately small: it provides the binary stream and pack/unpack contracts without becoming the semantic owner of the data.

## The boundary

Serialization is a representation boundary, not a second domain model.

![Representation Boundary](docs/images/fsm-serialization-02-representation-boundary.jpg)

~~~text
Semantic Model
      |
      v
FSM_Serialization
      |
      v
Binary Representation
      |
      v
FSM_Serialization
      |
      v
Semantic Model
~~~

The central contract is explicit:

~~~text
IBinaryPackable
    Pack(IBinaryStream)

IBinaryUnpackable
    Unpack(IBinaryStream)

IBinarySerializable
    = IBinaryPackable + IBinaryUnpackable
~~~

A type owns the meaning of its state. FSM_Serialization only provides the byte-oriented boundary through which that state can be represented and reconstructed.

## Binary first

The original binary IO implementation was proven inside the WebPage/Workshop proving ground before being extracted into this package.

The reusable foundation consists of:

- IBinaryStream — the platform-neutral byte stream contract;
- IBinaryPackable — writes an object's representation;
- IBinaryUnpackable — reconstructs an object's state;
- IBinarySerializable — combines packing and unpacking;
- MemoryBinaryStream — in-memory stream implementation for buffering and tests;
- StreamBinaryStream — adapter over a standard .NET Stream.

This package does not introduce a competing JSON serializer or force a particular wire format.

![IBinaryStream Contract Blueprint](docs/images/fsm-serialization-03-binary-stream-blueprint.jpg)

## What belongs here

FSM_Serialization provides neutral representation infrastructure for:

- binary state and composition data;
- MicroBundle descriptors;
- Experience manifests;
- dependency declarations;
- identity and version metadata;
- lineage metadata;
- runtime configuration;
- persistence and transport representations.

The domain package that owns a type remains responsible for deciding what its fields mean and how those fields are packed.

## What does not belong here

Serialization is not a domain database, interpreter, or application framework.

It should not contain:

- FSM domain rules;
- game rules;
- AEC rules;
- GUI definitions;
- WebPage-specific models;
- domain physics;
- MicroBundle-family-specific semantics;
- storage policy;
- filesystem policy.

A physical filesystem can consume IBinaryStream, but filesystem ownership belongs to the host/storage layer rather than the serialization contract.

## Deterministic representation

Where a domain contract defines an ordering, the implementation should make that ordering explicit rather than relying on incidental collection or runtime ordering.

![Deterministic vs. Non-Deterministic Representation](docs/images/fsm-serialization-04-deterministic-representation.jpg)

Deterministic binary representations are useful for:

- reproducible persistence;
- content identity;
- caching;
- synchronization;
- version comparison;
- debugging;
- lineage records.

Determinism is therefore a property of the representation contract and the type implementing it, not a hidden promise that every arbitrary object graph will serialize identically.

## Round-tripping

The fundamental invariant is semantic round-tripping:

~~~text
A
  |
  | Pack
  v
bytes
  |
  | Unpack
  v
A'
~~~

A' need not be the same object instance. It must preserve the information required by the owning semantic contract.

![Semantic Round-Trip](docs/images/fsm-serialization-05-semantic-round-trip.jpg)

The test suite proves this at the binary contract level before higher-level FSM objects are introduced.

## WebPage relationship

WebPage was the proving ground for this serialization boundary.

The extraction direction is intentional:

~~~text
WebPage / Workshop
        |
        | proved binary IO
        v
FSM_Serialization
        |
        | reusable package
        v
WebPage / Workshop
~~~

WebPage should consume the package rather than continue owning a parallel copy of the binary serialization contracts.

That keeps serialization reusable by server, desktop, Unity, Forge, and other compatible hosts.

## Architecture

The broader FSM ecosystem separates responsibilities:

~~~text
FSM_API
  |
FSM_COS / MicroBundle composition
  |
FSM_Serialization ---- representation boundary
  |
FSM_Memory ---------- persistence/cache
  |
FSM_REST ------------ transport
  |
WebPage / AnyApp ----- manifestation
~~~

The package knows how to cross the representation boundary. It does not decide what an Experience means.

![FSM Ecosystem Representation Boundary](docs/images/fsm-serialization-06-ecosystem.jpg)

## Current implementation status

The first implementation is intentionally small and binary-first.

The package currently provides:

1. binary stream abstraction;
2. pack/unpack contracts;
3. combined binary serialization contract;
4. memory and standard-stream adapters;
5. unit tests for round-tripping, piping, delegation, and incomplete input.

Higher-level canonical manifest formats can be added later without replacing these primitive contracts.

## Packaging

NuGet package:

~~~text
TheSingularityWorkshop.FSM_Serialization
~~~

Repository:

https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization

The package targets .NET 8 and is intended to remain independent of WebPage.

## License

MIT. See LICENSE.txt.

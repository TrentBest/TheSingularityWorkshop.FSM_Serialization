# FSM_Serialization Theory

## Representation is not reality

The FSM ecosystem distinguishes between a semantic object and a representation of that object.

Serialization crosses boundaries while preserving enough information to reconstruct the semantic object.

~~~text
Meaning        -> domain package
Composition    -> FSM_COS / MicroBundle descriptors
Representation -> FSM_Serialization
Storage        -> FSM_Memory / host
Transport      -> FSM_REST
Manifestation  -> WebPage / AnyApp / GUI
~~~

FSM_Serialization therefore owns representation mechanics, not the meaning represented.

## Binary representation as a boundary

The first proven representation boundary is binary.

A serializable object does not hand its semantics to a serializer that knows the domain. Instead, the object exposes the operations required to write and reconstruct its own representation:

~~~text
Domain Object
    |
    +---- Pack ----> IBinaryStream
    |
    +---- Unpack <---- IBinaryStream
~~~

This keeps the serialization package domain-neutral.

IBinaryPackable describes the outbound operation.

IBinaryUnpackable describes the inbound operation.

IBinarySerializable is simply the composition of those two contracts.

The package does not require every domain type to implement the combined interface. A type may be intentionally one-way when that is the owning contract.

## The stream boundary

IBinaryStream is the narrow byte-oriented boundary between representation code and its destination.

~~~text
Packable
   |
   v
IBinaryStream
   |
   +--> memory
   +--> .NET Stream
   +--> file
   +--> pipe
   +--> network
   +--> host-specific resource
~~~

The serialization package therefore does not need to know whether bytes are destined for memory, storage, transport, or another host resource.

MemoryBinaryStream exists as a concrete buffer and test primitive.

StreamBinaryStream adapts the standard .NET stream abstraction without making domain code depend directly on it.

## Representation is not storage

A binary stream is not a filesystem.

The distinction matters:

~~~text
Representation
    |
    v
IBinaryStream
    |
    +--> storage adapter
    +--> transport adapter
    +--> memory
~~~

Filesystem abstractions, physical paths, directory policy, and persistence policy belong to a storage/host layer.

This is why the WebPage IFileSystem, PhysicalFileSystem, and InMemoryFileSystem are not being moved wholesale into FSM_Serialization.

The reusable serialization boundary is extracted; storage remains an application/platform concern.

## Canonical form

A canonical representation is intentionally normalized.

Canonicalization may include stable property ordering, stable ordering of semantically unordered collections, explicit version fields, explicit identifiers, explicit dependency declarations, and explicit lineage.

Canonicalization must never erase information that is semantically meaningful.

The binary contracts do not invent canonical ordering for domain objects. The owning type must define the ordering required by its semantic contract.

## Composition manifests

An Experience should be representable as a composition manifest rather than a giant application-specific object graph.

~~~text
Experience
  ├─ identity
  ├─ version
  ├─ ontology
  ├─ MicroBundles
  │    ├─ id
  │    ├─ version
  │    ├─ configuration
  │    └─ dependencies
  └─ lineage
       ├─ source
       ├─ contributors
       └─ ancestry
~~~

The exact wire format is an implementation decision. The conceptual structure is the invariant.

The binary contracts are the primitive boundary through which such a manifest can eventually be represented.

## Demand-driven reconstruction

Serialization should not imply that every possible capability must be loaded.

A manifest may identify optional MicroBundles without forcing the runtime to materialize their full data.

~~~text
manifested capability != loaded capability
~~~

The runtime loads only what the Experience requires.

## Versioning

A representation should distinguish:

- identity of a thing;
- version of that thing;
- versions of dependencies;
- version of the containing Experience.

This allows a host to reason about compatibility without interpreting the domain itself.

## Lineage

Experiences can be derived from existing Experiences.

Serialization must preserve structural ancestry so the ecosystem can identify the source composition, source version, contributors, and ancestry of a published Experience.

Lineage is metadata about composition history. It is not a string appended to an author field.

## Format neutrality

Binary is the first concrete representation boundary, not a declaration that binary is the only possible format.

JSON may be convenient for WebPage and REST. Other representations may be useful for compact transport, high-performance runtime loading, or archival.

The semantic contract should remain independent of any one wire format.

The package should expose serialization concepts rather than make the ecosystem dependent on one format.

## Extraction principle

The WebPage implementation served as the proving ground for these contracts.

The architecture now moves the reusable abstraction outward:

~~~text
WebPage proving ground
        |
        v
FSM_Serialization
        |
        v
WebPage consumer
~~~

This is the intended direction for reusable FSM infrastructure: prove the boundary in a working application, extract the stable contract, then return the application to consuming the package.

The package should remain small enough that domain packages can depend on it without inheriting WebPage's application concerns.

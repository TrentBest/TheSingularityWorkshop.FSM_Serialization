# FSM_Serialization Theory

## 1. The central distinction

The architecture begins with a distinction that is easy to lose once software becomes large:

> **Representation is not reality.**

A semantic object has meaning inside a domain.

A serialized value is a representation of enough of that meaning to cross a boundary.

FSM_Serialization exists between those two concerns.

~~~text
semantic meaning
       │
       ▼
   domain type
       │
       │ representation rules
       ▼
FSM_Serialization
       │
       ▼
      bytes
~~~

The package therefore should not become the owner of the semantics it represents.

That is the first architectural invariant.

---

## 2. Why extract serialization?

The reusable infrastructure in a system is often discovered inside an application before it is recognized as infrastructure.

The WebPage/Workshop environment served as the proving ground for binary IO.

The extraction pattern is:

~~~text
application
    │
    │ prove a useful boundary
    ▼
stable abstraction
    │
    │ extract reusable contract
    ▼
package
    │
    │ consume the package again
    ▼
application
~~~

The goal is not to move an application wholesale into a library.

The goal is to identify the smallest boundary that multiple applications can share.

FSM_Serialization is that extracted boundary.

---

## 3. Representation versus storage

Serialization and storage are related but not equivalent.

Serialization asks:

> How are semantic values represented as bytes?

Storage asks:

> Where are those bytes kept?

Transport asks:

> How do those bytes cross an external boundary?

These can be composed:

~~~text
                 representation
                       │
                       ▼
                IBinaryStream
                       │
             ┌─────────┼─────────┐
             ▼         ▼         ▼
           memory    storage   transport
~~~

A filesystem is therefore not a serializer.

A database is not a serializer.

An HTTP endpoint is not a serializer.

A cache is not a serializer.

Those systems may consume serialized bytes, but their policies belong elsewhere.

---

## 4. The binary boundary

The first concrete representation boundary is binary.

The fundamental relationship is:

~~~text
Domain Object
    │
    ├── Pack ──────► IBinaryStream
    │
    └── Unpack ◄──── IBinaryStream
~~~

The three core contracts describe separate responsibilities:

- IBinaryPackable — outbound representation.
- IBinaryUnpackable — inbound reconstruction.
- IBinarySerializable — both directions.

This separation matters.

A domain may intentionally be able to emit a representation without accepting one.

Another domain may be able to consume a representation it did not produce.

The package therefore does not force symmetry where the domain does not require it.

---

## 5. Why the stream is an interface

IBinaryStream is intentionally smaller than System.IO.Stream.

That is not an attempt to replace .NET's stream abstraction.

It establishes a package-owned boundary around the operations the serialization contracts currently require:

~~~text
CanRead
CanWrite
Position
Length
Read
Write
Flush
Dispose
~~~

The implementation can then be:

~~~text
IBinaryStream
   │
   ├── MemoryBinaryStream
   │
   ├── StreamBinaryStream
   │
   ├── future host adapter
   │
   ├── future storage adapter
   │
   └── future transport adapter
~~~

The serializer does not need to know which one it received.

That is the abstraction's purpose.

---

## 6. Partial reads are real

A byte stream is not a magical array with an infinite guarantee that every requested read will be complete.

Therefore:

~~~csharp
stream.Read(buffer)
~~~

returns an actual count.

A fixed-width decoder should validate that count before interpreting the bytes.

This produces an important rule:

> **Never interpret an incomplete representation as a complete value.**

The package does not silently pad missing bytes or invent values.

The domain implementation must decide how malformed or incomplete representations should be handled.

---

## 7. Domain ownership of format

The package intentionally does not define a universal binary schema.

Suppose a domain value contains:

~~~text
identity
version
flags
payload
~~~

The domain might choose:

~~~text
[identity][version][flags][payload-length][payload]
~~~

Another domain might choose:

~~~text
[format-version][identity][payload]
~~~

Both are valid representations if their owners define and enforce their contracts.

FSM_Serialization provides the boundary.

It does not choose the fields.

---

## 8. Determinism

Deterministic bytes are valuable.

They can support:

- reproducible representations;
- content identity;
- caching;
- synchronization;
- comparison;
- lineage;
- integrity workflows.

But determinism does not appear merely because a type implements IBinaryPackable.

The implementation must define deterministic rules.

For example:

~~~text
semantic set
     │
     ▼
canonical order
     │
     ▼
fixed representation rules
     │
     ▼
deterministic bytes
~~~

If a collection is semantically unordered, the implementation may need to establish a canonical ordering before writing it.

If two equivalent semantic values can be emitted in different byte orders, the representation is not canonical.

The package provides no hidden canonicalization because canonicalization is domain-specific.

---

## 9. Canonical representation

Canonicalization is the deliberate reduction of multiple equivalent representations to one representation.

Potential canonical rules include:

- stable field ordering;
- stable collection ordering;
- explicit numeric widths;
- explicit byte ordering;
- explicit string encoding;
- explicit version fields;
- explicit identifiers;
- explicit dependency declarations;
- explicit lineage.

Canonicalization must not discard information that the domain considers meaningful.

This is why canonicalization belongs to the representation owner rather than to a generic serializer.

---

## 10. Round-trip semantics

The basic invariant is:

~~~text
        A
        │
      Pack
        ▼
      bytes
        │
     Unpack
        ▼
       A'
~~~

The correct question is not:

> Is A' the same object?

The correct question is:

> Does A' preserve the semantic information required by the domain contract?

Runtime identity, object references, caches, handles, open resources, and other incidental runtime state may not belong in the persistent representation at all.

Serialization should represent the state that the domain says is meaningful across the boundary.

---

## 11. Versioning is part of the boundary contract

Once bytes leave the process, the producing code and consuming code may no longer be identical.

That creates at least three useful version concepts:

~~~text
semantic object version
format version
dependency version
~~~

These should not be conflated.

A semantic package can change while a representation remains compatible.

A representation can change while the semantic concept remains the same.

A dependency can change independently of both.

The binary boundary therefore needs an explicit compatibility story whenever representations are expected to survive upgrades.

FSM_Serialization deliberately does not impose one universal versioning model.

---

## 12. Compatibility is an active policy

There are several legitimate compatibility strategies:

~~~text
old reader + new writer
new reader + old writer
strict rejection
migration
version dispatch
compatibility window
~~~

The correct strategy depends on the domain.

What should not happen is accidental compatibility.

A representation that changes because a developer reordered fields without realizing that the bytes were persisted has an implicit breaking change.

The package's smallness makes this visible.

---

## 13. Composition manifests

The broader FSM architecture treats an Experience as something that can be described as a composition.

Conceptually:

~~~text
Experience
 ├── identity
 ├── version
 ├── ontology
 ├── MicroBundles
 │    ├── identity
 │    ├── version
 │    ├── configuration
 │    └── dependencies
 └── lineage
      ├── source
      ├── contributors
      └── ancestry
~~~

A manifest can therefore become a compact representation of a larger runtime request.

But FSM_Serialization does **not** own this manifest model.

That belongs to the composition layer.

FSM_Serialization provides the primitive representation boundary through which such a model can eventually be encoded.

This distinction prevents the serializer from becoming a hidden dependency on every domain model in the ecosystem.

---

## 14. Demand-driven reconstruction

A representation can describe capability without requiring immediate instantiation.

This distinction is important:

~~~text
manifested capability
        ≠
loaded capability
~~~

A composition system can read a manifest, determine what is required, and load only the necessary components.

Serialization therefore participates in reconstruction without becoming the reconstruction policy itself.

The serializer turns bytes into the representation's semantic values.

The composition/runtime layer decides what those values mean operationally.

---

## 15. Lineage

Published or derived Experiences may have ancestry.

A representation can therefore need to preserve information such as:

~~~text
source identity
source version
contributors
parent composition
derivation
~~~

Lineage is metadata about composition history.

It should not be confused with author display text or a free-form note.

If lineage becomes part of a durable representation, its field ordering, identity rules, and versioning should be explicitly defined by the owning domain.

---

## 16. Format neutrality

Binary is the first concrete representation boundary.

It is not a claim that binary is the only useful representation.

Different boundaries may prefer different formats:

~~~text
semantic model
    │
    ├── binary ── runtime / compact persistence
    ├── JSON ──── APIs / human inspection
    ├── text ──── diagnostics / authoring
    └── other ─── specialized boundaries
~~~

The semantic model should not have to become identical to any one wire format.

FSM_Serialization therefore concentrates on serialization concepts and binary contracts rather than declaring one universal representation technology for the entire ecosystem.

---

## 17. What the package must resist becoming

A small infrastructure layer tends to accumulate requests.

Many of those requests can be useful somewhere.

That does not mean they belong here.

The package should resist absorbing:

- database access;
- filesystem policy;
- network clients;
- transport protocols;
- GUI state;
- browser lifecycle;
- Unity lifecycle;
- application configuration;
- MicroBundle semantics;
- Experience execution;
- domain-specific schemas;
- global serializer registries;
- reflection-driven object graphs.

The test for inclusion is architectural:

> **Does this feature strengthen the reusable representation boundary without requiring the package to understand the domain?**

If not, it belongs above the primitive.

---

## 18. Dependency direction

The intended architectural direction is:

~~~text
FSM_API
   │
   ▼
higher-level domain/composition systems
   │
   ▼
FSM_Serialization
   │
   ▼
storage / transport / host
~~~

The exact graph can evolve as the ecosystem grows.

The important rule is that the low-level serialization package should not pull application-specific architecture downward into itself.

A representation primitive should remain easier to depend on than the systems that depend on it.

---

## 19. Proving-ground extraction

The WebPage/Workshop environment demonstrated a useful development pattern:

~~~text
working application
      │
      │ discover repeated infrastructure
      ▼
explicit contract
      │
      │ extract
      ▼
reusable package
      │
      │ consume
      ▼
working application
~~~

This is preferable to designing an abstract package without a concrete proving ground.

The application supplies evidence.

The package captures the stable boundary.

The application then becomes a consumer instead of the owner of duplicated infrastructure.

---

## 20. Testing as a boundary definition

Tests are not merely regression protection.

For infrastructure like this, tests are executable statements of the boundary.

The current suite establishes observable behavior for:

- memory stream creation;
- stream capabilities;
- byte preservation;
- positioning;
- overwriting;
- end-of-input behavior;
- standard .NET stream adaptation;
- null-input rejection;
- pack/unpack round trips;
- combined serialization contracts;
- incomplete representations.

A useful invariant is:

~~~text
implementation
     │
     ▼
observable behavior
     │
     ▼
test
     │
     ▼
documented contract
~~~

As the package grows, the tests should remain the pressure against accidental semantic expansion.

---

## 21. CI and package publication

The repository separates verification from publication.

Verification establishes:

~~~text
restore
  ↓
build
  ↓
test
  ↓
coverage
  ↓
pack
~~~

Publication is a separate deliberate operation:

~~~text
verified package artifact
        │
        ▼
manual publication
~~~

This separation is especially appropriate for an alpha infrastructure package.

A passing build means the repository is internally coherent.

It does not mean a new public package version should automatically be released.

---

## 22. The architectural equation

The package can be reduced to one equation:

~~~text
semantic state
      +
explicit representation rules
      +
byte boundary
      =
portable representation
~~~

Not:

~~~text
object
  +
magic serializer
  =
architecture
~~~

The first equation keeps ownership visible.

The second hides it.

FSM_Serialization is designed around the first.

---

## 23. The boundary we want to preserve

The package should remain recognizable even after the ecosystem becomes much larger.

~~~text
                 DOMAIN
                   │
          owns meaning and rules
                   │
                   ▼
          Pack / Unpack contract
                   │
                   ▼
          FSM_Serialization
                   │
             byte boundary
                   │
        ┌──────────┴──────────┐
        ▼                     ▼
     storage               transport
        │                     │
        └──────────┬──────────┘
                   ▼
                  HOST
                   │
             manifestation
~~~

The package does not need to own the whole journey.

It needs to make **one boundary exceptionally clear**.

That is the theory.

---

## 24. Final invariant

The most important invariant is:

> **The serializer owns the boundary. The domain owns the meaning. The host owns the destination.**

If a future feature makes those responsibilities less clear, the feature belongs somewhere else or the boundary needs to be reconsidered before the feature is added.

That is how a small package remains infrastructure rather than becoming another application framework.


---

## 🔗 The Singularity Workshop

This project is part of a deliberately troublesome ecosystem:

- **[FSM_API](https://github.com/TrentBest/FSM_API)** — behavior and state.
- **[FSM_COS](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS)** — composition and runtime assembly.
- **[FSM_Serialization](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization)** — representation and the byte boundary.
- **[WebPage](https://github.com/TrentBest/WebPage)** — browser manifestation and proving ground.
- **[FSM_API_Unity](https://github.com/TrentBest/FSM_API_Unity)** — Unity manifestation.

<p align="center"><em>The Singularity Workshop — Tools for the curious, the bold, and the systemically inclined.</em><br><strong>Because state shouldn't be a mess.</strong><br><em>And because static boundaries are invitations to cause trouble.</em></p>

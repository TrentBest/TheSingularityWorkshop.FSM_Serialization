# TheSingularityWorkshop.FSM_Serialization

**Deterministic representation for FSM ecosystem state and composition.**

FSM_Serialization defines the boundary between the in-memory semantic model and representations that can be persisted, transported, cached, versioned, compared, or reconstructed.

## Why serialization exists

The FSM ecosystem needs a representation that can survive process boundaries, application restarts, network transport, storage backends, package and Experience versions, and sharing between users.

Serialization provides that representation without becoming the semantic owner of the data.

## What belongs here

The package is intended to provide neutral serialization infrastructure for:

- MicroBundle descriptors;
- bundle composition;
- dependency declarations;
- Experience manifests;
- runtime configuration;
- identity and version metadata;
- lineage metadata;
- deterministic representations suitable for persistence and transport.

## What does not belong here

Serialization is not a domain database or domain interpreter.

It should not contain atomic-element semantics, game rules, AEC rules, GUI definitions, WebPage-specific models, domain physics, or assumptions about a particular MicroBundle family.

The data is represented here; its meaning remains with the package that owns it.

## Core theory

A serialized Experience is not a second implementation of an Experience. It is a representation from which the runtime can reconstruct the same semantic composition.

~~~text
Semantic Model
      |
      v
Serialization
      |
      v
Persistent / Transport Representation
      |
      v
Deserialization
      |
      v
Semantic Model
~~~

The goal is semantic round-tripping:

~~~text
A -> serialize -> representation -> deserialize -> A'
~~~

where A' preserves the information required by the runtime contract.

## Determinism

Where practical, equivalent semantic inputs should produce equivalent serialized output.

Deterministic serialization matters for content identity, caching, version comparison, debugging, reproducible builds, lineage records, and synchronization.

Ordering and canonical representation should therefore be explicit rather than accidental.

## Lineage

Experiences can be derived from existing Experiences.

Serialization must preserve structural ancestry so the ecosystem can identify the source composition, source version, contributors, and ancestry of a published Experience.

Lineage is metadata about composition history. It is not a string appended to an author field.

## Packaging

This repository produces a reusable NuGet package:

~~~text
TheSingularityWorkshop.FSM_Serialization
~~~

The package should remain independent of WebPage and usable by server, desktop, Unity, and other compatible runtimes.

## Current status

The project is at the contract/theory stage. The first implementation should be deliberately small and prove deterministic round-tripping before format-specific complexity is introduced.

## License

MIT. See LICENSE.txt.

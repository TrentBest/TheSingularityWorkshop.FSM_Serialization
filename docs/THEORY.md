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

## Canonical form

A canonical representation is intentionally normalized.

Canonicalization may include stable property ordering, stable ordering of semantically unordered collections, explicit version fields, explicit identifiers, explicit dependency declarations, and explicit lineage.

Canonicalization must never erase information that is semantically meaningful.

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

## Format neutrality

JSON may be convenient for WebPage and REST, but the semantic contract should not be reduced to JSON.

Other representations may be useful for compact transport, binary storage, high-performance runtime loading, or archival.

The package should expose serialization concepts rather than make the ecosystem dependent on one format.

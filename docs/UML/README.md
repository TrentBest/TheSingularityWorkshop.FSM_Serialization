# Source-derived UML and design documentation

This folder is the navigation root for the Workshop UML standard. The existing focused UML pages are useful human-maintained documentation, but **the repository does not yet have a Roslyn-backed AST generator, complete source-derived neighborhoods, generated sequence traces, or automated complexity analysis**. Those are tracked in [issue #7](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/issues/7).

## Canonical view folders

- [Class Diagrams](Class%20Diagrams/README.md) — one canonical diagram per declared type.
- [Class Neighborhoods](Class%20Neighborhoods/README.md) — focus-type dependencies and usage evidence.
- [Sequence Diagrams](Sequence%20Diagrams/README.md) — interaction traces and method complexity/performance review.
- [Use Cases](Use%20Cases/README.md) — one use case per file, organized by category.
- [Architecture](Architecture/README.md) — package, component, and deployment boundaries.
- [Data Models](Data%20Models/README.md) — expandable UML data cards, shapes, invariants, and transformations.
- [Indexes](Indexes/README.md) — catalogs, traceability, source coverage, and generator metadata.

## Existing hand-maintained class cards

These current documents are retained until generated replacements can be compared and reviewed:

### Contracts
- [IBinaryStream](IBinaryStreamUML.md) — byte operations, stream adapters, and users.
- [IBinaryPackable](IBinaryPackableUML.md) — writing a domain-owned representation.
- [IBinaryUnpackable](IBinaryUnpackableUML.md) — reconstructing domain-owned state.
- [IBinarySerializable](IBinarySerializableUML.md) — combined pack/unpack capability.
- [Combined contract overview](BinaryContractsUML.md) — capability relationships.

### Stream implementations
- [MemoryBinaryStream](MemoryBinaryStreamUML.md) — in-memory byte storage and ownership.
- [StreamBinaryStream](StreamBinaryStreamUML.md) — adapter around a supplied .NET Stream.

Companion cards beside source files remain explanatory documents, not generated API claims.

## Extraction architecture: important distinction

**Roslyn is the source-code AST and semantic-analysis layer. Reflection is the compiled/runtime metadata layer.** Reflection can enumerate runtime types and members, but does not generally expose method-body syntax, branch structure, or a complete static call graph. Use Roslyn syntax trees and semantic models for those source facts; use reflection to validate compiled API shape and supplement runtime observations.

Normalize both into one renderer-neutral model with stable identities, source spans, provenance, and confidence. Render Markdown, Mermaid, SVG, and future GUI.SVG views from that shared model rather than hand-maintaining competing graph data.

## Complexity is an estimate, not a promise

Sequence traces should tabulate Big-O time and space per method, state assumptions and input variables, and flag likely O(n²) or worse patterns, nested scans, recursion, repeated searches, allocations, and copies. Static analysis is heuristic where dynamic dispatch, library calls, I/O, concurrency, or data-dependent behavior are involved. Practical performance estimates require reproducible benchmark/profiler evidence and recorded workload assumptions; Big-O alone cannot establish a universal upper bound.

## Use-case taxonomy

Use one primary category per use-case file, with links/tags for secondary concerns:
- User Goals
- API and Contract
- Data and Serialization
- Operational
- Integration
- Performance
- Security and Abuse
- Maintenance and Evolution

Each use case should state actors, preconditions, trigger, main flow, alternate/error flows, postconditions, and links to relevant source declarations and tests.

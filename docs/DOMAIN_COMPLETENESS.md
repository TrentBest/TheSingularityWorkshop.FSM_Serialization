<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

# FSM_Serialization Domain Completeness Review

**Review target:** the current `development` implementation and its documented consumer boundary.  
**Purpose:** distinguish implemented capability, domain obligations, and missing evidence without expanding the primitive library into a generic serializer.

## Executive assessment

**The primitive boundary is coherent; the broader serialization domain is not yet demonstrated complete.** The implementation remains intentionally small: three pack/unpack contracts, two stream adapters, and a reusable exact-read extension. Current tests give useful evidence for byte fidelity, stream delegation, positioning, short reads, disposal, and one little-endian Int32 example.

That proves the mechanics of the boundary, including reusable exact reads, not the completeness of real Workshop formats. No production FSM definition, FSM_COS manifest, or runtime snapshot codec is established by the current primitive API and tests. Those are not automatically defects in this package: their schemas belong to their domain owners. They are, however, important missing consumer-level proof.

The right target is **complete responsibility coverage across the owning layers**, not maximal API surface inside FSM_Serialization.

## Domain map

| Domain responsibility | Current coverage | Assessment |
|---|---|---|
| Explicit outbound representation | `IBinaryPackable.Pack` | Implemented contract; field semantics remain with each domain type. |
| Explicit inbound reconstruction | `IBinaryUnpackable.Unpack` | Implemented contract; parsing, validation, and mutation semantics remain with each format owner. |
| Combined pack/unpack capability | `IBinarySerializable` | Implemented; correctly adds no extra format policy. |
| Byte stream contract | `IBinaryStream` | Implemented: capabilities, position, length, span read/write, flush, disposal. |
| In-memory stream | `MemoryBinaryStream` | Implemented; initial data is copied and the position starts at zero. |
| .NET stream adapter | `StreamBinaryStream` | Implemented; delegates operations and owns/disposes the wrapped stream. |
| Short reads and truncated fixed-width fields | `BinaryStreamExtensions.ReadExactly` plus contract tests | Implemented as a reusable loop that fills a caller-provided span across short reads and throws `EndOfStreamException` on premature end. It allocates no buffer and defines no field encoding. |
| Primitive encodings beyond the example | Domain-owned code only | Not supplied as reusable codecs. Endianness, signedness, varints, strings, and length prefixes must be implemented and tested by each format owner. |
| Structured collection and graph representation | No production codec evidenced | Missing consumer-level implementation/evidence: ordering, references, cycles where allowed, sparse IDs, forward references, and canonical output. |
| FSM definition / blueprint format | No production codec evidenced | Missing. Define the semantic scope and byte contract in the owning FSM domain before implementing it. |
| FSM_COS manifest / MicroBundle composition | No production codec evidenced here | Missing consumer integration evidence. FSM_COS owns composition meaning; this package should only supply the byte boundary. |
| Runtime snapshot, history, and replay | Not defined by this primitive | Unresolved domain decisions. Do not serialize live runtime state until consistency, identity, and replay semantics are specified. |
| Format identity, versioning, compatibility | Guidance only | No universal mechanism implemented. A durable format owner must define version markers, rejection/migration behavior, and compatibility tests. |
| Malformed/untrusted input limits | Format-owner obligation; limited example tests | Missing broad evidence: maximum lengths/counts, overflow checks, malformed encodings, bounded allocation/work, and failure behavior. |
| Atomic reconstruction / partial mutation | Not specified by interfaces | Unresolved. Each format must define whether failed unpack leaves a target unchanged, partially changed, or discarded. |
| Canonical/deterministic bytes | Theory and guidance only | Not guaranteed by `Pack`. Each durable or hashable format needs canonical ordering and golden fixtures. |
| Ownership and lifetime | Partially specified | The .NET adapter disposes its wrapped stream. Callers need explicit lifetime guidance; stream capability exceptions remain delegated. |
| Cross-version compatibility | No format fixtures evidenced | Missing. Need historical golden payloads and old-reader/new-reader policy for any format that will persist or travel between versions. |
| Benchmark domain realism | Primitive microbenchmarks and stream baselines | Incomplete. Primitive performance is useful but does not predict FSM graph or manifest encode/decode cost. |

## What is actually missing?

### 1. A concrete, consumer-owned format

The highest-value missing proof is not another generic interface. It is one real representation contract used by an owning domain. Start with an FSM **definition/blueprint**, not a live runtime snapshot: define which semantic fields survive the boundary, which runtime-only values are excluded, and how nested/reference relationships are represented.

The format owner should supply:
- a written byte-layout specification;
- deterministic encoding rules;
- golden byte fixtures;
- semantic round-trip tests;
- malformed and truncated input tests;
- format version and compatibility policy;
- bounded-input/resource limits.

FSM_Serialization should provide the byte boundary and stream mechanics, not import FSM_API or FSM_COS types to achieve this.

### 2. A clear choice between definition, snapshot, and history

These are different domains and must not be treated as interchangeable payloads.

- **Definition/blueprint:** the authored structure and configuration needed to describe a machine.
- **Runtime snapshot:** a consistent capture of selected live state at a defined logical instant, with explicit rules for active states, timers, handles, and external resources.
- **History/replay:** an ordered event or transition record with ordering, causality, and replay guarantees.

The immediate decision is to specify and test the definition format first. Snapshot and replay formats should remain deferred until their owning runtime contracts are explicit.

### 3. Reusable read/write safety patterns—or documented reasons not to provide them

The current contracts expose raw spans and actual read counts, which is a sound minimal boundary. But format authors repeatedly need to handle exact reads, lengths, overflow, and malformed input. We should decide whether to add a small, format-neutral helper layer for exact reads and bounded length handling, or deliberately keep these patterns in consumer-owned codecs.

Do not add helpers merely for convenience. Require multiple real consumers, consistent exception semantics, and benchmark evidence before making them public API.

### 4. Compatibility evidence

The existing historical benchmark run targets an older package version and is performance evidence only; it is not a compatibility test. Any durable format needs immutable golden payloads tied to a documented format version, tests that decode prior fixtures, and explicit rules for unknown versions or fields. Package version and format version must remain separate concepts.

### 5. Security and resource behavior

Before any decoder accepts untrusted or externally supplied bytes, the owning format must define maximum payload size, collection counts, string lengths, nesting depth, reference counts, and work limits. Tests should cover integer overflow, impossible lengths, truncated fields, invalid encodings, duplicate or dangling references, and deterministic rejection. A stream abstraction alone does not make a decoder safe.

### 6. Domain-level benchmarks

The current benchmark project has useful stream, buffer, and integer microbenchmarks. It does not yet establish the cost of real FSM definition serialization. Once the definition schema exists, add equivalent encode-only, decode-only, and round-trip cases over deterministic tiny, typical, large, sparse, branching, and reference-heavy fixtures. Keep correctness validation outside timed methods, but require it before a result can be considered valid.

## Recommended order of work

1. **Lock the ownership map.** Keep the primitive package domain-neutral; assign definition/manifest/snapshot semantics to their actual owners.
2. **Choose the first real consumer format.** Prefer FSM definition/blueprint over live snapshots.
3. **Write the format contract before the codec.** Define field order, numeric encodings, strings, collections, IDs/references, canonical order, versioning, and failure behavior.
4. **Build fixtures and tests first.** Golden bytes, round trips, truncation at every boundary, malformed input, overflow, resource limits, and historical compatibility.
5. **Implement the smallest codec at the owning layer.** Add a primitive helper to FSM_Serialization only if multiple consumers demonstrate a stable shared need.
6. **Add semantic benchmarks.** Preserve stable IDs and raw evidence; compare only equivalent schemas and workloads.
7. **Document verified behavior.** Link each claim to source, tests, fixtures, or benchmark evidence. Keep proposed capability visibly separate from implemented capability.

## Completion criteria

The serialization domain is adequately covered when each in-scope responsibility has one of three explicit outcomes:

- **Implemented and evidenced** by source plus tests/fixtures;
- **Owned by a named higher layer** with a documented contract and integration test;
- **Deliberately deferred** with the missing decision or consumer requirement stated.

A checkbox matrix or 100% line coverage is not, by itself, domain completeness.

## Scope guardrails

Do not pull the following into this package merely to close a matrix gap:

- FSM execution semantics, transitions, timers, or runtime lifecycle;
- FSM_COS composition/arbitration or MicroBundle policy;
- Experience manifests as a package-owned schema;
- storage, database, filesystem, or transport policy;
- generic reflection-driven object graphs or global serializer registries;
- automatic migration/version negotiation without a concrete consumer contract.

**Guiding invariant:** the domain owns meaning and format rules; FSM_Serialization owns the explicit byte boundary; the host owns storage, transport, and lifecycle.

## Related documentation

- [README and quick start](../README.md)
- [Serialization domain inventory](SERIALIZATION_DOMAIN.md)
- [Test matrix and roadmap](TEST_MATRIX.md)
- [Byte operations](BYTE_OPERATIONS.md)
- [Theory](THEORY.md)
- [UML index](UML/README.md)
- [Benchmark methodology](BENCHMARKING.md)
- [FSM_COS documentation standard](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/blob/master/DOCUMENTATION_STANDARD.md)

---

<p align="center">
  <a href="https://github.com/TrentBest/FSM_API">
    <img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="160" />
  </a>
</p>

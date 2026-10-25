# Serialization Domain Inventory

**Status:** source-backed inventory for the next package review. This document distinguishes implemented primitives from responsibilities that remain with the domain or format owner.

## Responsibility map

| Concern | Current package responsibility | Evidence / boundary |
|---|---|---|
| Write a domain value's bytes | `IBinaryPackable.Pack(IBinaryStream)` | The domain type chooses fields, order, widths, and encoding. |
| Reconstruct a domain value | `IBinaryUnpackable.Unpack(IBinaryStream)` | The domain type parses and validates its own format. |
| Declare both directions | `IBinarySerializable` | Convenience composition of pack and unpack contracts; it adds no methods. |
| Read/write byte boundary | `IBinaryStream` | Reports capabilities, position and length; exposes span-based read/write and flush; is disposable. |
| In-memory byte storage | `MemoryBinaryStream` | Owns a `.NET MemoryStream`; copies initial bytes and starts at position zero. |
| Adapt an existing stream | `StreamBinaryStream` | Delegates capabilities and operations to the supplied `.NET Stream`; disposing the adapter disposes that stream. |

## Format responsibilities owned by the domain

The package does **not** impose one universal binary format. A concrete type or protocol must specify and test:

- field order, numeric widths, signedness, and endianness;
- string and text encoding, length prefixes, and framing;
- optional, missing, unknown, and duplicate fields;
- malformed, truncated, oversized, and trailing input;
- allocation and length limits for untrusted data;
- schema/format version identifiers and compatibility/evolution rules;
- validation order and whether failed unpacking can leave partially mutated state;
- stream ownership and lifetime at the call site.

The current tests prove an integer example using little-endian Int32, and verify that truncated input is rejected by that example. That is an example contract, not a package-wide imposed format.

## Stream behavior and edge conditions

- A read returns the number of bytes actually read; it may be shorter than the requested buffer. Fixed-size parsers must loop until complete or explicitly fail at end-of-stream.
- End-of-input is represented by a zero-byte read. The package does not currently provide a separate `ReadExactly` helper.
- `CanRead` and `CanWrite` report the underlying stream's capabilities; callers must respect them. Unsupported operations, including position/length on some streams, may throw from the underlying stream.
- `StreamBinaryStream` delegates rather than buffering, filling short reads, converting byte order, or emulating unsupported capabilities.
- `MemoryBinaryStream` is seekable in practice because its internal `.NET MemoryStream` is seekable.
- Both stream implementations are disposable. `StreamBinaryStream.Dispose()` also disposes the wrapped stream; callers should treat the adapter as owning that stream for its lifetime.
- Flush delegates to the underlying stream. The in-memory implementation delegates to `.NET MemoryStream.Flush()`, which does not create a persistence guarantee.

## Explicit non-goals of this package

These are intentionally not implied by the current API:

- automatic object-graph or reflection-driven serialization;
- a global serializer registry or implicit type discovery;
- universal schema/manifest definitions or migration engine;
- file, database, cloud-blob, or network transport policy;
- compression, encryption, checksums, signing, or framing;
- cross-version compatibility guarantees for arbitrary domain formats;
- transactional rollback if a domain type mutates itself while unpacking.

Those concerns may be implemented by a higher layer or a future separately designed component if a concrete use case justifies it. They should not be inferred from the small byte contracts.

## Domain coverage matrix

The inventory above is the package's current boundary, not a claim that every serialization feature should be implemented here. The wider FSM serialization domain should be explored explicitly and assigned to the correct architectural owner.

| Domain area | Questions that must be answered | Current disposition |
|---|---|---|
| Byte-stream contract | What do read counts, empty buffers, position, length, flush, and disposal mean? | Implemented by the two stream adapters; exercise edge cases in unit tests. |
| Partial I/O | Can one read return fewer bytes than requested without reaching EOF? | Contract permits short reads; each fixed-size parser must loop or deliberately reject the input. The quick-start example demonstrates the loop. |
| Format definition | Field order, numeric width, signedness, byte order, text encoding, framing, and lengths? | Owned by the domain type/protocol; no universal format is imposed here. |
| Round-trip semantics | Which semantic fields must survive Pack → bytes → Unpack? Which runtime-only values must not? | Example integer round trip exists; real FSM snapshot contracts remain to be defined by a consuming domain. |
| Determinism/canonicalization | Do equivalent values produce identical bytes? How are unordered collections sorted? | Not guaranteed by the primitive; format-specific invariant and tests required. |
| Malformed input | How are truncation, invalid tags, invalid enums, impossible lengths, duplicates, unknown fields, and trailing bytes handled? | Truncation is demonstrated in the example type; broader parser behavior is domain-owned and needs representative format tests. |
| Resource/abuse limits | What maximum lengths, nesting, counts, and allocations are permitted for untrusted bytes? | No format parser or size-limit policy is provided by this package. A concrete format must define limits before consuming untrusted data. |
| Versioning/evolution | How are format version, semantic version, and dependency version distinguished? What are the old-reader/new-writer rules? | Theory documented; no schema registry or migration engine implemented. |
| Mutation/atomicity | Can failed Unpack leave partially modified state? Is parse-then-apply required? | Domain-owned decision; transactional rollback is not provided by the primitive. |
| FSM state vs. definition | Are we persisting a live runtime snapshot, a blueprint/definition, transition history, or a composition manifest? Which runtime handles and external resources are intentionally excluded? | Open domain-design question for FSM_API/FSM_COS consumers; must not be guessed by the primitive library. |
| Stream ownership | Who disposes the wrapped stream? Is the adapter owning or non-owning? | Current `StreamBinaryStream` owns and disposes the supplied stream; covered by a test. |
| Concurrency | May one stream be read/written concurrently? Is snapshot capture consistent with a running FSM? | No thread-safety or snapshot synchronization guarantee is provided. Higher-level coordination is required. |
| Storage and transport | File, blob, database, network, compression, encryption, checksums, signatures? | Explicit non-goals here; separate layers may compose these concerns around the byte boundary. |
| Performance | Where are time, allocation, copying, and buffer growth spent at representative FSM sizes? | Separate BenchmarkDotNet evidence exists for stream primitives; FSM workload comparisons must be reproducible and tied to exact workload/commit. |
| Compatibility/interoperability | Can other runtimes/languages read the format? Are golden byte fixtures stable? | No cross-language wire-format guarantee yet; add golden fixtures when a concrete format is selected. |
| Property/fuzz testing | Do generated valid values round-trip? Do arbitrary malformed byte sequences fail safely and within resource limits? | Not yet established as a systematic suite; high-priority follow-up for concrete parsers. |

## Test strategy and acceptance evidence

The next test expansion should be driven by this matrix rather than by a target test count. For each concrete format, require:

1. **Golden bytes** for a small, human-auditable value, so byte order and field layout cannot drift silently.
2. **Round-trip properties** across boundary values, empty collections, optional fields, and representative FSM graph sizes.
3. **Malformed-input cases** for truncation at every field boundary, invalid tags/versions, oversized lengths, and trailing bytes according to the format policy.
4. **Compatibility fixtures** produced by earlier versions when durable bytes are expected to survive upgrades.
5. **Allocation/performance measurements** for empty, typical, and stress-sized payloads; report throughput and allocations as well as elapsed time.
6. **Security/resource assertions** that malformed or hostile input cannot request unbounded allocation or excessive work.
7. **Domain ownership tests** that distinguish FSM definition/blueprint data from live runtime state, handles, caches, and external resources.

Coverage is a diagnostic, not a completeness score. A small primitive can reach 100% executable line/branch coverage while leaving most format-level questions unimplemented. Do not add a generic schema, migration, compression, or persistence API merely to fill a matrix cell; first identify a real consumer and place the responsibility at the correct layer.

## Current verification evidence

The last inspected baseline artifact, from commit `bda04a33acac3717f326a82ddb31596ae5bfc908` in [CI run 40163849434](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/40163849434), reports **17 tests passed, 0 failed**, with 100% line coverage (31/31 lines) and 100% branch coverage (4/4 branches) for the then-current assembly. That is full executable coverage of the small baseline, not exhaustive proof of the domain matrix above.

The current development branch is expanding tests around empty-buffer behavior, seeking beyond the current end, invalid positions, disposal, flush delegation, and parsing fixed-size fields across multiple short reads. These changes require a fresh CI run before their test count or coverage is reported as verified.

<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

For the current source-backed coverage assessment and prioritized missing evidence, see the [domain completeness review](DOMAIN_COMPLETENESS.md).\n\n# FSM-Aware Serialization Architecture and Proof Plan

**Status:** architecture proposal and acceptance plan. This document does not claim the FSM-specific codec is implemented or benchmark-proven.

## Objective

FSM_Serialization should grow beyond generic byte-stream wrappers into a deliberate serialization system for finite-state-machine data. The hypothesis is that an FSM's structured, reference-heavy definition can be represented more compactly and traversed more efficiently than a naive per-object/per-string encoding. That hypothesis must be measured, not assumed.

Keep the existing stream contracts as the byte boundary. Add FSM-aware formats as explicit, versioned layers with clear ownership. Do not turn primitive stream adapters into a reflection serializer or couple them to application hosting, storage, transport, or UI.

## First rule: serialize a named semantic target

These are different formats and must not be conflated:

1. **Definition / blueprint** — reusable FSM structure: identity/name, declared states, transition rules and their references, processing-group declarations, and other stable configuration actually exposed by FSM_API.
2. **Runtime snapshot** — a definition plus an explicitly selected subset of live runtime state, such as current state and runtime values that have a stable public representation. Snapshot consistency must be defined when the FSM can be mutating.
3. **Event / transition history** — ordered events required for audit or replay. This is not a snapshot and has different ordering, retention, and compatibility requirements.
4. **Composition manifest** — FSM_COS/MicroBundle composition, dependency identity, versioning, and lineage. This belongs to the composition/manifest format owner; it may reuse this package's byte codec.
5. **Application context payload** — user-owned POCO data. It needs an explicit caller-provided codec or contract. Never serialize arbitrary objects, delegates, callbacks, locks, caches, handles, or external resources by reflection as an implicit side effect.

Start with **definition serialization**. It is deterministic, reusable, testable without a running host, and does not require inventing a universal encoding for arbitrary application context.

## Proposed layered design

| Layer | Responsibility | Required property |
|---|---|---|
| L0 — Byte boundary | Existing IBinaryStream, MemoryBinaryStream, StreamBinaryStream | Correct stream contract; partial reads handled by codecs |
| L1 — Primitive codec | Fixed-width integers, bounded lengths, UTF-8 strings, byte spans, version/header helpers | Explicit byte order; no hidden allocations where avoidable |
| L2 — FSM definition model | A stable DTO/model derived only from confirmed public FSM_API concepts | Semantic equality independent of object identity |
| L3 — FSM definition codec | Encode/decode the definition model | Deterministic bytes, strict validation, bounded input |
| L4 — Snapshot/context extensions | Opt-in state and caller-supplied context codecs | Explicit consistency and failure semantics |
| L5 — History / manifest integrations | Separate formats or packages with independent evolution rules | No accidental coupling to the definition wire format |
| L6 — Storage/transport adapters | File, blob, network, compression, encryption, checksums | Outside the core semantic codec |

A dependency on FSM_API may be appropriate for an optional integration layer, but only after inspecting the public API and establishing a stable mapping. Keep the generic byte boundary usable without an FSM_API reference. Do not add ProtocolAi or another ontology/symbol package as a dependency; internal name-to-index mapping belongs inside the codec when needed.

## Candidate compact definition layout

This is a candidate wire layout to validate against the actual FSM_API model, not yet a compatibility promise.

1. **Header:** magic bytes, format major/minor version, feature flags, and declared payload length where applicable.
2. **String table:** unique UTF-8 names/labels used by states, transitions, groups, and other named records.
3. **State table:** dense state indices plus string-table references and only the stable definition attributes exposed by FSM_API.
4. **Transition table:** source and destination state indices, condition/rule identifiers, and stable transition metadata. Delegate implementations are not serialized.
5. **Processing-group table:** compact references to declared groups and their membership/association data, only where part of the actual definition contract.
6. **Optional extension sections:** length-delimited, versioned sections with explicitly defined unknown-section behavior.

Prefer dense local indices for references within a single encoded definition, even if the public API uses names or sparse IDs. Preserve semantic IDs where the contract requires them; do not assume array order equals identity. Build internal mapping tables without external dependencies. For small definitions, measure whether a string table and index map pay for their own headers and lookup work; a simpler representation may win at that scale.

Potential optimizations to benchmark rather than presume:
- intern repeated strings once per payload;
- encode references as compact integer indices;
- pre-size buffers from measured encoded-size estimates;
- write directly to spans where safe;
- avoid ToArray and intermediate per-record allocations;
- use fixed-width fields first for simplicity, then compare varints for large/sparse numeric domains;
- decode in two passes if necessary to resolve forward references, and compare its cost against fix-up tables;
- offer caller-owned destination buffers only if they measurably reduce allocations without compromising safety.

## Correctness and safety contract

Every concrete format must specify:

- canonical ordering and whether semantically equivalent definitions produce byte-identical output;
- encoding, numeric widths, byte order, and length representation;
- duplicate names/IDs and invalid or unresolved references;
- empty definitions and maximum state/transition/group/string counts;
- maximum string and section sizes, total payload size, and bounded allocation/work;
- truncation, malformed UTF-8, invalid versions/flags, unknown extension sections, and trailing bytes;
- whether decode consumes exactly one value or may be used in a framed stream;
- whether a failed decode returns no object or can expose a partially constructed object;
- old-reader/new-writer and new-reader/old-writer compatibility policy;
- snapshot consistency under concurrent mutation, if runtime snapshots are later supported.

Reject invalid lengths before allocating. Never trust counts from the input without checking limits and remaining bytes. Do not silently map unresolved references to a default state. Use golden fixtures to lock down intentional wire-format decisions.

## Domain acceptance matrix

| Area | Required tests / evidence |
|---|---|
| Definition semantics | Empty/minimal definitions; representative state/transition graphs; preserve all supported semantic fields |
| Graph references | Forward references, repeated references, invalid source/destination, duplicate IDs/names, sparse external identifiers |
| Determinism | Same semantic definition built in different insertion orders produces canonical bytes where promised |
| Round trip | Model → bytes → model semantic equality; compare by meaning, not object reference |
| Golden format | Human-auditable byte fixtures for a minimal graph and a graph with repeated names/references |
| Malformed input | Every truncated prefix for small fixtures; invalid header/version/flags; invalid indices; duplicate records; trailing data policy |
| Resource limits | Oversized counts/strings/sections and adversarial payloads fail before dangerous allocation or unbounded work |
| Evolution | Versioned fixture corpus; supported and rejected version combinations documented |
| Failure behavior | Decode failure does not leak a usable partial definition; caller's existing object is not mutated unexpectedly |
| Context/snapshot | Explicit codec contract; unsupported delegates/handles/resources excluded; consistent snapshot policy |
| History/replay | Ordered event fixtures, sequence semantics, missing-event policy; separate from definition tests |
| Interop | Golden byte fixtures can be decoded independently of runtime object identity; document wire contract |
| Performance | Latency distributions, throughput, allocated bytes, bytes copied, output size, and scaling across graph shapes |

## Benchmark plan

The companion repository is [FSM_Serialization_Benchmarks](https://github.com/TrentBest/FSM_Serialization_Benchmarks). Its initial BenchmarkDotNet harness (commit a820c0c0ddcb7067c41946c43af655303bec4a99) measures generic stream primitives and a synthetic IBinarySerializable payload at 16 B, 1 KiB, 10 KiB, and 100 KiB. That is useful baseline evidence, but it does not yet measure an FSM graph codec.

Extend the harness in separate benchmark classes so results do not mix unrelated costs:

### A. Primitive baselines
- MemoryStream vs MemoryBinaryStream vs StreamBinaryStream;
- construction, existing-data setup, read, write, overwrite, rewind, and output extraction;
- separate input setup/copying from the timed operation wherever possible.

### B. FSM definition codec
- compact codec vs a simple reference implementation over the same semantic model;
- encode only, decode only, and encode + decode;
- no-op in-memory traversal baseline to expose codec overhead;
- payload sizes by graph shape, not just raw byte count: tiny, typical, large, dense-transition, repeated-name-heavy, and sparse-external-ID definitions;
- output bytes, allocated bytes/op, Gen0 collections, throughput, and latency distribution.

### C. Snapshot/context and history
- definition-only vs definition + runtime snapshot;
- context codec time measured separately from graph structure;
- event append/encode/decode/replay measured separately from snapshot encoding.

### D. End-to-end workload
- preserve the original SimpleDemos stress test as a historical baseline;
- measure no serialization, encode-only, decode-only, round trip, and snapshot capture + encode as distinct modes;
- use identical worker counts, update intervals, warmup, trial counts, runtime, and machine settings for each paired comparison;
- report medians and dispersion, paired per-trial deltas, allocations, and serialized bytes. Do not attribute FSM_COS gains to serialization unless the isolated serialization mode demonstrates it.

The existing SimpleDemos FSM_API vs FSM_COS comparison showed a roughly 20.6% reduction when comparing the medians of all tick rows, but the median paired per-trial improvement was only about 2.24%, with noisy trials. It is not evidence for FSM_Serialization performance. Treat it as motivation to isolate workloads, not as a serialization claim.

### Benchmark acceptance rules
- Pin and record commit, package version, runtime, OS, CPU, BenchmarkDotNet version, parameters, and exact commands.
- Use Release builds and BenchmarkDotNet warmup/iteration discipline; avoid timing a one-off run as proof.
- Keep benchmark output as CI artifacts or checked-in compact CSV/Markdown summaries with source links.
- Compare identical semantic inputs and validate decoded output before reporting timings.
- State which baseline is being compared and whether copying, allocation, construction, and setup are inside the timed region.
- Claim a speedup only when repeated runs show a stable improvement with allocation and output-size trade-offs disclosed.

## Implementation sequence

1. **Inventory public FSM_API definition concepts** and confirm what can be read without private reflection or runtime mutation. Write a mapping table from public concepts to candidate model fields.
2. **Define the semantic definition DTO and exclusions.** Add semantic equality tests before implementing a wire format.
3. **Implement L1 primitives** with bounds checking and exact-read helpers where justified by repeated use.
4. **Implement the smallest valid definition codec** with header, string table if measurements justify it, state records, and transition references.
5. **Add golden, property, malformed-input, and resource-limit tests** before optimizing.
6. **Add BenchmarkDotNet FSM graph cases** to the existing benchmark repository; preserve old primitive benchmarks as a separate historical suite.
7. **Establish baseline results**, then optimize one variable at a time (table layout, indexing, allocation, width encoding, buffering).
8. **Add snapshot/context/history only as separately specified capabilities**, not by silently expanding the definition format.
9. **Document format compatibility and versioning** before declaring the codec stable or publishing a package.

## Claims we must not make yet

- That FSM-aware serialization is implemented end-to-end.
- That an FSM-specific compact layout is faster than a naive layout.
- That the earlier approximately 21% FSM_COS stress result is caused by serialization.
- That 100% primitive line/branch coverage means the full FSM serialization domain is covered.
- That a wire format is stable before golden fixtures and compatibility policy are approved.

The proof target is not “more tests” or “a clever binary layout.” It is a deterministic, safe FSM definition codec with semantic tests and reproducible benchmarks showing where it wins, where it loses, and why.


---

<p align="center">
  <a href="https://github.com/TrentBest/FSM_API">
    <img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="160" />
  </a>
</p>

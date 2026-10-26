<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

# FSM_Serialization Test Matrix and Domain Roadmap

**Purpose:** turn the serialization domain inventory into an evidence plan. This matrix separates tests that belong in the primitive package from tests that require a concrete FSM or FSM_COS format owner.

## Reading this matrix

Use the [domain completeness review](DOMAIN_COMPLETENESS.md) for the current coverage assessment and prioritized gaps. This matrix is the evidence plan: it identifies what must be proven at each layer and where the responsibility belongs.

## Current package boundary

The implementation currently contains three contracts and two stream adapters:

- `IBinaryPackable` — write a domain-owned representation.
- `IBinaryUnpackable` — reconstruct a domain-owned value.
- `IBinarySerializable` — compose both capabilities.
- `IBinaryStream` — expose read/write, position, length, flush, capabilities, and disposal.
- `MemoryBinaryStream` — mutable in-memory stream with copied initial data.
- `StreamBinaryStream` — adapter that delegates to and disposes a supplied .NET `Stream`.

The primitive package should stay small. It should not acquire a generic schema engine, object-graph reflection, persistence policy, FSM runtime knowledge, or transport policy merely to make this matrix look complete.

## Test layers

| Layer | What it proves | Owner | Acceptance evidence |
|---|---|---|---|
| L0 — Stream contract | Byte fidelity, position, length, capability delegation, empty buffers, short reads, flush, disposal, exceptions | FSM_Serialization | Unit tests and Cobertura |
| L1 — Concrete format | Golden byte layout, endianness, lengths, strings, optional fields, malformed data, truncation, trailing bytes | Format/domain owner | Golden fixtures plus round-trip and negative tests |
| L2 — FSM semantic snapshot | Which FSM data survives; which handles, caches, delegates, live resources, and runtime-only state are omitted or reconstructed | FSM_API/FSM_COS consumer with format owner | Semantic equality assertions and explicit exclusions |
| L3 — Compatibility | Read old fixtures with new code; define new-reader/old-writer and old-reader/new-writer policy | Versioned format owner | Versioned fixture corpus and compatibility matrix |
| L4 — Abuse/resource safety | Oversized lengths, excessive counts/depth, invalid tags, adversarial inputs, bounded allocation/work | Parser/format owner | Fuzz/property tests, resource assertions, safe rejection |
| L5 — Performance | Throughput, latency distributions, allocations, bytes copied, peak memory, payload scaling | Benchmark owner | Reproducible BenchmarkDotNet or workload harness results |
| L6 — Integration | Storage/transport wrapper, cancellation, failures, partial writes, retry boundaries, snapshot consistency | Higher-level integration | End-to-end tests with a stated failure model |

## Immediate package tests

The existing suite and current development additions target observable behavior of the two stream adapters and an illustrative Int32 format:

- constructor behavior, copied initial bytes, and null rejection;
- empty-buffer operations, EOF, position, overwriting, seeking beyond end, and invalid negative position;
- read/write capability and position/length delegation;
- unsupported operations and short reads;
- disposal and flush delegation;
- golden little-endian bytes and Int32 boundary values;
- complete-field reads across repeated short reads;
- truncation at every prefix length for the example Int32;
- round-trip semantics.

These are contract tests, not proof that a universal FSM format exists. The current suite's latest count and coverage must always be read from a successful CI artifact; never infer it from this list alone.

## Concrete FSM format decisions — required before implementing an FSM serializer

Before designing a snapshot or definition format, answer these questions with the consuming repository:

1. **What is being serialized?** FSM blueprint/definition, runtime snapshot, transition/event history, FSM_COS manifest, or a deliberately composed subset?
2. **What is identity?** Stable machine IDs, state IDs, transition IDs, context identity, and references between objects.
3. **What is excluded?** Delegates, callbacks, open resources, process-local handles, caches, locks, and transient scheduling state.
4. **What is reconstructed?** Runtime handles, callbacks, services, external references, and derived indexes.
5. **What is canonical?** Ordering of states/transitions/collections, numeric widths, byte order, text encoding, and stable IDs.
6. **What is the evolution policy?** Format version, semantic version, optional fields, unknown fields, migrations, and compatibility windows.
7. **What is the failure contract?** Strict rejection versus defaults, parse-before-mutate versus partial mutation, and rollback guarantees.
8. **What are the resource limits?** Maximum bytes, strings, states, transitions, nested structures, and allocations.
9. **What is the trust boundary?** Are bytes local and trusted, persisted across releases, received from another process, or user-controlled?
10. **What is the consistency boundary?** Can a snapshot be taken while the FSM is changing, or must the runtime provide a stable/quiescent snapshot?

These decisions belong in a consumer-backed format specification, not in the primitive interfaces by assumption.

## Performance investigation plan

The reported roughly 21% improvement in an FSM_COS stress test is an end-to-end observation. It should not be attributed to serialization without a controlled comparison.

Use two related experiments:

### A. Preserve the Simple Demos baseline

Record the original stress-test source and commit, machine/runtime details, scenario size, state and transition counts, active FSM count, tick/update cadence, workload duration, warm-up, measured iterations, and output metrics. Preserve the baseline rather than replacing it with a new test.

### B. Isolate serialization cost

Measure these separately:

- direct FSM operation with no serialization;
- pack to memory;
- unpack from an existing byte buffer;
- full pack + unpack round trip;
- snapshot capture plus serialization, if snapshot capture is part of the consumer;
- allocation and bytes copied per operation.

Vary payload size and FSM shape. Include typical and stress-sized cases, and report throughput/latency plus allocation and payload bytes. Keep correctness assertions enabled outside the timed region. Use identical semantic inputs and compare the same commit/runtime where possible.

The decision output should distinguish: end-to-end FSM_COS improvement, serializer overhead, copying cost, allocation cost, and any snapshot-consistency cost. These are related but not interchangeable measurements.

## Completion gates

- [ ] L0: current primitive tests pass and CI enforces a minimum line/branch coverage threshold.
- [ ] L1: a real consuming format has a written byte-layout specification and golden fixtures.
- [ ] L2: FSM definition versus runtime snapshot scope is explicitly chosen.
- [ ] L3: format versioning and compatibility policy are documented before durable bytes ship.
- [ ] L4: untrusted-input limits and malformed-input tests exist for any parser exposed to untrusted data.
- [ ] L5: benchmark source, commit, environment, workload, allocation, and result are reproducible.
- [ ] L6: integration failure behavior is tested at the layer that owns storage/transport.
- [ ] Docs and diagrams distinguish implemented behavior from future domain capabilities.

## Governing rule

**Explore the whole domain; implement only the responsibility that belongs at this layer.** A domain gap should produce an explicit design decision, test, consumer requirement, or deferred item—not an accidental expansion of the primitive package.


---

<p align="center">
  <a href="https://github.com/TrentBest/FSM_API">
    <img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="160" />
  </a>
</p>

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

## Current verification evidence

The CI test artifact for commit `0669623b83a7534513385afe3a2a47fd70c1b9d6` reports **17 tests passed, 0 failed**, with 100% line coverage (31/31 lines) and 100% branch coverage (4/4 branches) for the current assembly. Tests cover in-memory stream basics, initial-data copying, seeking/overwriting, adapter capability and byte/position delegation, unsupported underlying operations, null rejection, wrapped-stream disposal, short-read propagation, a little-endian integer round trip, and truncated input. This is strong coverage of the current small API, not exhaustive proof of every format concern listed above.

The latest package audit also found that source-adjacent companion Markdown was not yet included in the packed package and that the internal branch-reconciliation ledger was included unintentionally. The packaging correction is now in `development`; the next CI artifact must be inspected to confirm both outcomes.

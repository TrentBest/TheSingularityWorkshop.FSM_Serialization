# Byte Operations: From Meaning to Bytes and Back

This page follows the two directions independently. Packing and unpacking are separate capabilities; the combined interface is only a convenience for a type that supports both.

## Packing forward

![Animated packing flow](images/packing-flow.svg)

1. **Domain state** — the owning type decides which values are part of its representation.
2. **Pack contract** — an implementation of IBinaryPackable writes those values in a documented order.
3. **Byte stream** — IBinaryStream accepts bytes through Write. The stream does not decide what those bytes mean.
4. **Destination** — the selected stream adapter determines where bytes are held. MemoryBinaryStream stores them in memory; StreamBinaryStream delegates to a supplied .NET Stream.

The package does not impose a universal field order, numeric encoding, string format, length prefix, schema identifier, or version header. Those are format decisions and must be defined by the type or protocol that owns the data.

## Unpacking reverse

![Animated unpacking flow](images/unpacking-flow.svg)

1. **Byte source** — the stream exposes bytes at its current position.
2. **Read contract** — Read returns the number of bytes actually read. A read is not a promise to fill the entire destination buffer.
3. **Domain parser** — an IBinaryUnpackable implementation interprets bytes according to its own format.
4. **Reconstructed state** — the domain type validates and applies the values it owns.

If a fixed-size field requires N bytes, the unpacker must account for short reads and end-of-input. The example tests throw EndOfStreamException when a four-byte integer cannot be read completely. Do not assume every implementation of IBinaryStream will fill a requested buffer in one call.

## Two directions, three contracts

- **IBinaryPackable**: writes a representation.
- **IBinaryUnpackable**: reconstructs from a representation.
- **IBinarySerializable**: inherits both contracts; it adds no methods of its own.

This separation lets a write-only producer, read-only consumer, or round-trip value express only the capability it needs.

## Stream behavior and ownership

| Type | Read/write | Position and length | Disposal |
|---|---|---|---|
| MemoryBinaryStream | Both | Backed by a MemoryStream; starts at position zero | Disposes its internal MemoryStream |
| StreamBinaryStream | Reflects the supplied .NET Stream capabilities | Delegates to the supplied stream; the underlying stream may reject unsupported operations | Disposes the supplied stream |

The adapter does not add buffering, byte-order conversion, schema interpretation, or capability emulation. Operations such as Position and Length can fail when the underlying .NET Stream does not support them. Check CanRead and CanWrite before attempting the corresponding operation.

## Review checklist for a concrete format

Before bytes become a long-lived contract, specify and test:

- field order and integer endianness;
- string/text encoding and length representation;
- optional, missing, and unknown fields;
- malformed, truncated, oversized, and trailing input;
- limits on lengths and allocations;
- compatibility/version policy;
- whether parsing mutates the target incrementally or validates before applying;
- ownership and lifetime of the stream.

These are not universal features automatically supplied by FSM_Serialization. They are the concrete format's responsibilities unless a future, explicitly designed package API says otherwise.

## Source-backed proof

The current test suite exercises memory-stream initialization and copying, read/write round trips, seeking and overwriting, adapter capability/byte/position delegation, short reads, wrapped-stream disposal, null arguments, a successful integer round trip, and truncated input. CI currently reports 17 tests passing with no failures; verify the latest workflow artifact before relying on that count. This is evidence for the current API—not a claim that every listed format concern is implemented or tested.

# UML and API class cards

These diagrams are intentionally focused: public contracts first, adapters second, and the domain type that owns byte meaning above the package boundary. Each focused diagram includes a Use cases section.

## Contracts

- [IBinaryStream](IBinaryStreamUML.md) — byte operations, stream adapters, and users.
- [IBinaryPackable](IBinaryPackableUML.md) — writing a domain-owned representation.
- [IBinaryUnpackable](IBinaryUnpackableUML.md) — reconstructing domain-owned state.
- [IBinarySerializable](IBinarySerializableUML.md) — the combined pack/unpack capability.
- [Combined contract overview](BinaryContractsUML.md) — capability relationships at a glance.

## Stream implementations

- [MemoryBinaryStream](MemoryBinaryStreamUML.md) — in-memory byte storage and ownership.
- [StreamBinaryStream](StreamBinaryStreamUML.md) — adapter around a supplied .NET Stream.

Companion class cards live beside each source file in `src/*.md`. These are human-maintained explanations of the current source contract; they are not generated API claims.

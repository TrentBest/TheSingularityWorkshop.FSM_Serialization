<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

# Experience Container Format — Version 1 Proposal

<badge>Proposal — not an implemented or frozen wire contract</badge>

**Status:** cross-repository design proposal for review.  
**Canonical file extension:** `.experience`  
**Reserved extension:** `.fsm` remains for finite-state-machine definitions.  
**Proposed container format version:** 1.0.

This document defines a candidate byte-level envelope for a single-file Experience container. It is deliberately separate from the semantic schemas for Experience manifests, MicroBundle configuration, resources, and executable artifacts.

The format is intended to support bounded inspection, lazy section access, deterministic production, independent section compression, and integrity verification without executing any content.

## 1. Ownership and layering

| Concern | Owner |
|---|---|
| Fixed header, directory, byte order, section framing, safe reader/writer primitives, container conformance fixtures | FSM_Serialization |
| Experience identity, manifest fields, compatibility meaning, MicroBundle configuration defaults/schema/validation/override semantics | Experience and MicroBundle domain owners; MicroBundleDomain owns bundle parameter contracts |
| Immutable artifact identity, retrieval, storage, and repository verification | MicroBundleRepository |
| Dependency resolution, effective configuration, composition, and RuntimeAssembly creation | FSM_COS |
| Runtime execution, lifecycle, cancellation, and cleanup | FSM_Foundry |
| Install/open/inspect UX, publisher status display, consent, capability policy, and presentation | AnyApp host and its Internal Module |

FSM_Serialization owns the envelope, not the meaning of section payloads. It must not reference Experience, MicroBundleDomain, FSM_COS, or Foundry domain types to parse the generic container.

The container layer may expose opaque section descriptors and bounded byte/stream access. Domain-specific codecs interpret payload bytes above it.

## 2. File layout

A version 1 file has this physical layout:

```text
+-------------------------------+
| Fixed header (64 bytes)       |
+-------------------------------+
| Padding to 8-byte boundary    |
+-------------------------------+
| Section payloads              |
| (each independently framed)   |
+-------------------------------+
| Padding as required           |
+-------------------------------+
| Section directory             |
| (fixed-size entries)          |
+-------------------------------+
| End of file                   |
```

The directory is placed after the payloads so a writer can stream section data before emitting the final directory. A reader locates it from the fixed header. The header is always at offset zero. The directory offset MUST be 8-byte aligned, and `directoryOffset + directoryLength` MUST equal the declared file length; trailing bytes are forbidden. Every payload range MUST end at or before the directory offset.

All integer fields are **unsigned little-endian** unless explicitly stated otherwise. All offsets and lengths are byte counts from the beginning of the file. No native struct layout, compiler packing, or platform endianness is part of the format.

All section payload starts are aligned to 8-byte boundaries. Every gap between the end of the header and first payload, between payloads, and between the final payload and directory MUST consist only of zero padding bytes. Directory entries are sorted by ascending section type, then lexicographically by the 16-byte section identifier. Payloads MUST NOT overlap the header, one another, or the directory. Physical payload order is separate from directory order: version 1 writers SHOULD place startup-critical sections first as specified in section 9, then order repeated payloads deterministically within each section type.

## 3. Fixed header

The version 1 header is exactly 64 bytes:

| Offset | Width | Field | Version 1 rule |
|---:|---:|---|---|
| 0 | 8 | Magic | ASCII bytes `TSWEXP\0\0` |
| 8 | 2 | Format major | `1` |
| 10 | 2 | Format minor | `0` |
| 12 | 4 | Header size | `64` |
| 16 | 4 | Header flags | `0`; unknown set bits are invalid |
| 20 | 4 | Directory entry size | `80` |
| 24 | 4 | Directory entry count | Number of entries |
| 28 | 8 | Directory offset | Absolute file offset |
| 36 | 8 | Directory byte length | Must equal count × 80 |
| 44 | 8 | Declared file length | Must equal actual file length |
| 52 | 12 | Reserved | All zero in version 1 |

The magic is an identification aid, not a trust signal. The `.experience` extension is likewise not proof that a file is valid or safe.

A reader MUST reject an unsupported major version. A reader MAY accept a higher minor version only if all encountered feature bits and required section semantics are understood; otherwise it MUST fail closed. Version 1 writers emit minor version zero.

## 4. Section directory

Each directory entry is exactly 80 bytes:

| Offset | Width | Field | Meaning |
|---:|---:|---|---|
| 0 | 4 | Section type | Stable numeric type identifier |
| 4 | 4 | Section flags | Required/compression flags |
| 8 | 16 | Section ID | Stable identifier within this container |
| 24 | 8 | Payload offset | Absolute file offset of stored bytes |
| 32 | 8 | Stored length | Number of bytes physically stored |
| 40 | 8 | Logical length | Number of bytes after decompression |
| 48 | 32 | SHA-256 | Hash of the stored payload bytes |

The directory is a fixed-size array; its byte length MUST equal `entryCount × 80`. Checked multiplication and checked addition are mandatory before any range is used.

A section's identity is the pair `(sectionType, sectionId)`. Duplicate pairs are invalid. A section ID is 16 opaque bytes. Every singleton section type MUST use the all-zero ID; every repeated section type MUST use a distinct nonzero ID. Version 1 readers MUST enforce the registry cardinality: exactly one `ExperienceManifest`, exactly one `IntegrityMetadata`, at most one `ResourceIndex`, and zero or more of each repeated type. A file with missing or duplicate singleton sections is invalid, even if the IDs differ.

### 4.1 Flags

- Bit 0: **Required**. If the reader does not understand the section type or required semantics, it MUST reject the container.
- Bits 1–2: **Compression**:
  - `00`: none;
  - `01`: Brotli;
  - `10`: reserved;
  - `11`: reserved.
- Bits 3–31: reserved and MUST be zero in version 1.

For uncompressed sections, stored length MUST equal logical length. For compressed sections, the decoder MUST produce exactly the declared logical length, consume exactly the stored payload, and enforce configured decompression/work limits. A reader MUST NOT trust the declared logical length as permission to allocate that amount before applying its own policy limits.

Compression is per section, never one stream around the entire container. This preserves lazy access and allows metadata to be read without decompressing unrelated payloads.

## 5. Version 1 section types

The following numeric identifiers are proposed for the initial registry:

| Type | Name | Cardinality | Purpose |
|---:|---|---|---|
| 1 | ExperienceManifest | Exactly one | Stable Experience identity, display metadata, requested MicroBundle roots, immutable artifact references, and declared compatibility |
| 2 | MicroBundleOverrides | Zero or more | Configuration overrides keyed to exact immutable MicroBundle identity |
| 3 | ResourceIndex | Zero or one | Resource identifiers, logical metadata, and references to resource payload section IDs |
| 4 | ResourcePayload | Zero or more | Independently addressable non-executable resource bytes |
| 5 | OpaqueArtifact | Zero or more | Typed, engine/format-qualified opaque artifact bytes; inert by default |
| 6 | IntegrityMetadata | Exactly one | Container-level integrity and provenance metadata, including optional publisher signature envelope |

A writer MUST include one manifest and one integrity metadata section. Other section types are optional. The integrity metadata section MUST be uncompressed in version 1 to keep verification deterministic and bounded.

The manifest, override, and resource-index payload schemas are owned by their respective domains and must have their own schema versions. The container version does not replace those schema versions.

The Experience configuration section stores only explicit overrides. It does not copy MicroBundle defaults or redefine parameter meaning. Defaults and validation remain owned by the relevant MicroBundle/domain contract.

## 6. Integrity and signatures

The SHA-256 value in each directory entry covers the exact **stored payload bytes** for that section (compressed bytes when compression is enabled). This permits section verification without decompression.

A hash proves only that the bytes match the directory's claimed digest. It does not prove who created the file, whether the publisher is trusted, or whether the host should authorize execution.

The IntegrityMetadata payload must declare:
- integrity metadata schema version;
- an explicit signed or unsigned state;
- signature coverage profile identifier;
- signature format and signature algorithm identifier when signed;
- publisher/key identifier when signed;
- all algorithm parameters that affect verification;
- signature bytes when signed.

Version 1 directory entries always use SHA-256 for their 32-byte stored-payload digest. This is fixed by the directory layout and is not algorithm-agile. IntegrityMetadata MUST NOT imply that it can select a different directory digest algorithm. Any future digest agility requires a new container format version or a directory-entry format that identifies the digest algorithm.

### 6.1 Proposed signature coverage profile 1

The signature MUST bind the complete structural interpretation of the file, not merely the manifest. The proposed canonical signing transcript is:

1. The exact 64-byte header.
2. The exact directory bytes, with the IntegrityMetadata entry's SHA-256 field replaced by 32 zero bytes.
3. For every non-integrity section, the tuple of section type, flags, ID, stored length, logical length, and SHA-256 digest in directory order.
4. The canonical IntegrityMetadata security fields: schema version, signed/unsigned state, signature coverage profile, signature format, signature algorithm, publisher/key identifier, and all verification-affecting algorithm parameters. These fields MUST use a canonical, length-delimited encoding with fixed field identifiers in ascending order; duplicate fields, unknown critical fields, alternate encodings of the same value, and non-canonical encodings are invalid. The signature value itself is the only excluded field.

The IntegrityMetadata section's payload is excluded from the raw transcript to avoid circularity, but its directory entry still binds its type, flags, ID, offset, and lengths. The canonical security-field encoding is appended as the final transcript component. An unsigned record has an explicit unsigned state and no signature-only fields. Unsupported profiles/algorithms are reported as unverifiable, never silently treated as valid.

This remains a proposal requiring cryptographic review and golden signing vectors before implementation. No signed container may be represented as verified until the exact canonical field IDs, byte encoding, and verification test vectors are frozen. The trust store, key rotation, revocation, timestamping, and publisher identity policy are host/repository concerns, not container parser concerns.

## 7. Bounds and malformed-input behavior

The parser treats every file as untrusted input. Version 1 readers MUST:

- verify the actual file length against the declared length and require the directory to terminate exactly at EOF;
- require an 8-byte-aligned directory offset and validate all zero-padding gaps;
- reject files shorter than the fixed header;
- validate magic, version, fixed header size, entry size, reserved bytes, and flags;
- use checked arithmetic for all offset, count, multiplication, and end calculations;
- validate the directory range before reading entries;
- enforce configured maximum file size, section count, stored section size, logical section size, and aggregate decompressed size;
- reject zero-length or out-of-range section IDs where prohibited by section semantics;
- reject duplicate `(type, ID)` pairs and enforce the section registry's exact singleton cardinalities and zero-ID rule;
- reject overlapping payload ranges and payload/directory/header overlap; every payload MUST end at or before the directory offset;
- reject misaligned payload offsets and nonzero padding;
- reject unsupported compression identifiers;
- verify a selected section's hash before exposing its payload bytes to a domain decoder;
- reject malformed or truncated compressed data, output-length mismatch, and trailing compressed bytes;
- reject unknown required sections; unknown optional sections may be skipped after structural range validation without reading or hashing their payload;
- avoid path extraction based on untrusted names; resource paths, if any, are logical identifiers resolved by the host rather than direct filesystem paths.

Proposed conservative default reader limits (hosts may configure stricter limits; raising them requires an explicit policy decision):

| Limit | Proposed default |
|---|---:|
| Total container bytes | 2 GiB |
| Directory entries | 4,096 |
| Stored bytes per section | 512 MiB |
| Logical bytes per section | 512 MiB |
| Aggregate logical bytes | 1 GiB |
| Nesting depth in domain payloads | Domain codec-defined; default maximum 32 |

These are safety policy defaults, not claims that the binary envelope itself can represent no larger values. A reader MUST reject a file exceeding its configured limits before allocating based on its untrusted declarations.

A failed parse MUST NOT expose a partially trusted container object. The reader may expose validated immutable section descriptors only after the header, complete directory, cardinalities, and all payload ranges/padding have passed structural validation. Payload access remains separately verified.

Readers MUST distinguish three states: (1) **structurally valid** — header, directory, cardinalities, ranges, and padding validated; (2) **section verified** — the selected section's stored-byte digest validated before exposure; and (3) **container verified** — every section digest and, when present, the publisher signature validated. A manifest-only preview performs states 1 and 2 for the manifest and MUST NOT read unrelated payloads. It MUST NOT label the entire container verified. Full-container verification is an explicit operation and may read every stored payload.

## 8. Lazy inspection and access model

The reader should support the following staged workflow:

1. Read and validate the fixed header.
2. Read and validate the directory, cardinalities, ranges, and padding.
3. Locate the single manifest section.
4. Verify that section's stored-byte hash.
5. Decompress only the manifest if it is compressed and pass it to the manifest codec.
6. Leave all unrelated resource and opaque artifact payloads unopened and unhashed.
7. On demand, open a section through a bounded slice/stream that cannot read outside its declared stored range.
8. Verify the selected section's hash before exposing bytes to its domain decoder.

The default writer SHOULD leave the manifest, overrides, resource index, and integrity metadata uncompressed. Compression is opt-in per section and SHOULD be used only for larger resource/artifact sections when measured size savings justify decompression cost. The writer SHOULD retain uncompressed bytes when compression does not beat a documented minimum savings threshold. Hosts and repositories may choose different policies based on measured transfer, storage, and startup costs; the container format does not mandate compression.

Preview, indexing, download, and installation MUST NOT execute MicroBundles, opaque artifacts, scripts, native code, or other payloads. Opening a container is not permission to activate it.

## 9. Deterministic writing

For identical logical input, a deterministic writer should produce identical bytes when all inputs, section IDs, compression settings, and metadata are identical. To optimize common preview and startup paths, the writer SHOULD place the manifest first, followed by integrity metadata, MicroBundle overrides, resource index, resource payloads, and opaque artifacts. The directory remains independently sorted by `(sectionType, sectionId)` as specified; directory order and physical payload order need not be coupled if the writer records each physical offset accurately.

To make that possible:
- sort directory entries by the specified order;
- canonicalize each domain payload according to its own schema;
- zero all reserved and padding bytes;
- use fixed algorithm identifiers and deterministic compression settings;
- avoid timestamps, random IDs, or host-specific paths in canonical content;
- make section IDs stable when they represent stable logical resources;
- write the final directory only after section offsets and lengths are known.

If a domain requires random identifiers or time metadata, those values are explicit logical inputs; byte-for-byte reproducibility is not promised when they differ.

## 10. Reader/writer API shape

The container API should remain separate from domain payload codecs. Candidate responsibilities:

- `ExperienceContainerReader`: parse header/directory, validate structure, expose section descriptors, and open bounded section streams.
- `ExperienceContainerWriter`: accept typed opaque section descriptors and byte sources, write aligned payloads, then directory and header.
- `ExperienceSectionDescriptor`: immutable type, flags, ID, offsets, lengths, and digest metadata.
- `ExperienceContainerLimits`: caller-controlled limits with safe defaults.
- `ExperienceContainerFormatException`: stable diagnostics for malformed or unsupported container structure.

These are conceptual names only, not committed public API. The implementation should first prove the layout with internal types and golden fixtures before freezing public API.

The reader must not silently own or dispose caller streams unless that ownership is explicit in its constructor/API contract. A non-seekable input may be supported by bounded spooling at a higher layer; the initial reader may require a seekable stream and report that requirement clearly.

## 11. Conformance fixtures and acceptance gates

Before calling version 1 implemented, require:

- a minimal valid container with manifest + integrity metadata;
- a multi-MicroBundle container with override-only configuration records;
- multiple resource payloads with independent compression;
- deterministic byte-for-byte golden fixtures;
- successful header/directory/manifest-only inspection;
- independent section hash verification;
- valid signed and unsigned examples, plus invalid/tampered signatures;
- truncation at every byte boundary for a small fixture;
- invalid magic/version/header/directory sizes and reserved bits;
- integer overflow, impossible offsets, overlap, misalignment, duplicate IDs, and invalid directory ranges;
- unknown optional section accepted and unknown required section rejected;
- unsupported compression and malformed compressed payload rejection;
- configured file/count/section/decompression limits enforced before dangerous allocation;
- proof that preview does not execute any embedded payload;
- cross-repository review of the section schema and immutable artifact identity contracts.

## 12. Open decisions before freezing version 1

The following are explicit review items, not silently settled facts:

1. Confirm the magic bytes and section type registry.
2. Confirm whether a 64-byte header and 80-byte directory entry meet implementation and future extension needs.
3. Review signature coverage and algorithm policy with a cryptography/security reviewer.
4. Confirm the proposed default size limits against expected Experience sizes and target devices.
5. Confirm the ResourceIndex model and whether resources should use one section per resource or grouped payload blocks.
6. Confirm whether non-seekable streams are in scope for the first reader.
7. Confirm section-ID generation/canonicalization rules for each repeated section type.
8. Confirm whether IntegrityMetadata is required for all files or whether a clearly unsigned development profile is needed.

Until those decisions are reviewed, this document is a proposal, not a frozen public format contract.

## 13. Related ownership issues

- [AnyApp #49 — binary .experience container umbrella](https://github.com/TrentBest/AnyApp/issues/49)
- [FSM_Serialization #1 — representation boundary and container envelope](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/issues/1)
- [MicroBundleDomain #11 — override-only configuration](https://github.com/TrentBest/TheSingularityWorkshop.MicroBundleDomain/issues/11)
- [MicroBundleRepository #22 — immutable artifact retrieval](https://github.com/TrentBest/TheSingularityWorkshop.MicroBundleRepository/issues/22)
- [MicroBundleRepository #24 — deterministic immutable dependency resolution](https://github.com/TrentBest/TheSingularityWorkshop.MicroBundleRepository/issues/24)
- [FSM_COS #51 — immutable dependency pins](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/issues/51)
- [FSM_Foundry #23 — RuntimeAssembly execution contract](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Foundry/issues/23)

---

**Publication and release:** this work is documentation/design only. It does not authorize package version changes, NuGet publication, release, deployment, merge, or changes to `master`.


### Signature transcript clarification (required for review)

The IntegrityMetadata section is excluded from the generic section-digest tuple to avoid a circular signature. That does **not** mean its security-relevant descriptor fields may be mutable without detection. Under proposed coverage profile 1, the canonical transcript MUST additionally include the IntegrityMetadata schema version, signature profile identifier, signature algorithm identifier, publisher/key identifier, and any algorithm parameters that affect verification. Only the signature value itself is excluded. The exact canonical encoding of these fields must be specified and covered by golden signing vectors before the profile is implemented. A parser must reject ambiguous or duplicate encodings of these fields.

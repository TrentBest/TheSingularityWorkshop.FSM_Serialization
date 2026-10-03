# TheSingularityWorkshop.FSM_Serialization

[![Verify](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/workflows/verify.yml/badge.svg)](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/workflows/verify.yml)
[![codecov](https://codecov.io/gh/TrentBest/TheSingularityWorkshop.FSM_Serialization/graph/badge.svg)](https://codecov.io/gh/TrentBest/TheSingularityWorkshop.FSM_Serialization)
[![NuGet](https://img.shields.io/nuget/v/TheSingularityWorkshop.FSM_Serialization.svg)](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_Serialization/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TheSingularityWorkshop.FSM_Serialization.svg)](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_Serialization/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

![FSM Serialization — The Binary Boundary](docs/images/fsm-serialization-01-boundary.jpg)

**A small, explicit binary representation boundary for The Singularity Workshop FSM ecosystem.**

**TheSingularityWorkshop.FSM_Serialization** provides the low-level contracts and stream adapters required to move domain-owned state across a byte-oriented representation boundary.

It deliberately does **not** attempt to become a general-purpose object graph serializer, persistence framework, schema registry, database abstraction, or application framework.

> **The type that owns the meaning should own the representation rules. The serialization package should provide the boundary, not invent the meaning.**

---

## At a glance

| Property | Value |
|---|---|
| Package | **TheSingularityWorkshop.FSM_Serialization** |
| Current version | **0.1.0-alpha.2** |
| Target framework | .NET 8 |
| Language | C# |
| License | MIT |
| Repository | [GitHub](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization) |
| Package | [NuGet](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_Serialization/) |
| Coverage | [Codecov](https://codecov.io/gh/TrentBest/TheSingularityWorkshop.FSM_Serialization) |
| CI | GitHub Actions |

This is the **1.0.0 stable release**. The API is intentionally small, and the binary contracts are treated as explicit infrastructure: changes to representation semantics should be made deliberately, documented, and protected by tests.

---

## What problem does this solve?

As a system grows, semantic state eventually has to cross boundaries.

A state machine may need to persist state. A composition system may need to represent a manifest. A runtime may need to move configuration between processes. A cache may need a compact representation. A host may need to reconstruct an object from bytes.

Without a shared boundary, every subsystem tends to invent its own serialization abstraction.

~~~text
Application A ──> custom binary format
Application B ──> another stream abstraction
Application C ──> JSON-specific representation
Application D ──> database-specific representation
~~~

That creates coupling in the wrong place.

FSM_Serialization extracts the reusable part:

~~~text
                    SEMANTIC DOMAIN
                          │
                          │ owns meaning
                          ▼
                 IBinaryPackable
                          │
                        Pack
                          │
                          ▼
                    IBinaryStream
                          │
             ┌────────────┼────────────┐
             ▼            ▼            ▼
           memory       .NET        host/storage
                         Stream       boundary
             │            │            │
             └────────────┼────────────┘
                          ▼
                 IBinaryUnpackable
                          │
                       Unpack
                          │
                          ▼
                  SEMANTIC DOMAIN
~~~

The package owns the **representation boundary**.

The calling type owns the **semantic contract**.

That distinction is the foundation of the package.

---

## Why binary?

Binary is useful when a system needs an explicit, compact representation with predictable byte-level behavior.

It can be appropriate for:

- runtime state;
- compact configuration;
- persistence representations;
- cache entries;
- manifests;
- dependency metadata;
- identity and version records;
- lineage metadata;
- transport payloads;
- generated or hand-authored machine representations.

Binary is not automatically better than JSON, XML, text, or another representation. The point of this package is not to declare a universal wire format.

The point is to provide a **small representation boundary** that higher-level systems can use when binary representation is appropriate.

---

## Installation

### .NET CLI

~~~bash
dotnet add package TheSingularityWorkshop.FSM_Serialization --version 1.0.0
~~~

### PackageReference

~~~xml
<PackageReference Include="TheSingularityWorkshop.FSM_Serialization" Version="1.0.0" />
~~~

The package is available from [NuGet.org](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_Serialization/).

> **Stable release:** `1.0.0` establishes the first stable public package contract. Future releases that change binary representation semantics should document the compatibility implications and increment the package version accordingly.

---

## Requirements

The current package targets **.NET 8**.

The test project also targets .NET 8.

The package intentionally has no dependency on an application host, database, filesystem, browser runtime, Unity runtime, GUI framework, or other platform-specific execution environment.

---

# Core API

The public surface is deliberately small.

## IBinaryStream

IBinaryStream is the byte-oriented boundary used by packable and unpackable types.

~~~csharp
public interface IBinaryStream : IDisposable
{
    bool CanRead { get; }
    bool CanWrite { get; }

    long Position { get; set; }
    long Length { get; }

    int Read(Span<byte> buffer);
    void Write(ReadOnlySpan<byte> buffer);
    void Flush();
}
~~~

It exposes only the operations needed by the current binary contract:

- whether reading is supported;
- whether writing is supported;
- current position;
- length;
- byte reads;
- byte writes;
- flushing;
- disposal.

The abstraction intentionally does not expose filesystem paths, filenames, database connections, sockets, serialization schemas, or domain metadata.

### Read semantics

Read returns the number of bytes actually read.

A caller must not assume that one call necessarily fills the supplied buffer.

For fixed-width values, implementations should verify that the required number of bytes was received before interpreting the value.

~~~csharp
Span<byte> buffer = stackalloc byte[sizeof(int)];

if (stream.Read(buffer) != buffer.Length)
    throw new EndOfStreamException();
~~~

This is important because **the stream boundary does not silently manufacture missing data**.

---

## IBinaryPackable

IBinaryPackable defines the outbound direction:

~~~csharp
public interface IBinaryPackable
{
    void Pack(IBinaryStream stream);
}
~~~

The implementing type decides:

- which fields are represented;
- their order;
- their binary widths;
- their encoding;
- whether optional data is present;
- whether version information is included;
- how nested values are represented.

The package does not infer any of those decisions.

---

## IBinaryUnpackable

IBinaryUnpackable defines the inbound direction:

~~~csharp
public interface IBinaryUnpackable
{
    void Unpack(IBinaryStream stream);
}
~~~

The implementing type owns reconstruction semantics.

This is intentionally symmetrical with Pack, but the two contracts remain independent.

A type may be pack-only or unpack-only when its domain requires that distinction.

---

## IBinarySerializable

IBinarySerializable combines the two directional contracts:

~~~csharp
public interface IBinarySerializable :
    IBinaryPackable,
    IBinaryUnpackable
{
}
~~~

It is a composition of capabilities, not a separate serializer engine.

The package does not require every type to implement it.

---

# Stream implementations

## MemoryBinaryStream

MemoryBinaryStream provides an in-memory implementation of IBinaryStream.

It is useful for:

- unit tests;
- buffering;
- temporary representations;
- round-trip verification;
- building a byte representation before handing it to another subsystem.

Example:

~~~csharp
using var stream = new MemoryBinaryStream();

stream.Write([1, 2, 3, 4]);

stream.Position = 0;

Span<byte> buffer = stackalloc byte[4];
var count = stream.Read(buffer);
~~~

The current contents can also be copied with:

~~~csharp
byte[] bytes = stream.ToArray();
~~~

The constructor can start from an existing byte array:

~~~csharp
using var stream = new MemoryBinaryStream(existingBytes);
~~~

The stream starts at position zero after construction.

---

## StreamBinaryStream

StreamBinaryStream adapts an ordinary .NET System.IO.Stream to IBinaryStream.

Example:

~~~csharp
using var file = File.OpenRead("state.bin");
using var stream = new StreamBinaryStream(file);
~~~

This lets domain serialization code depend on IBinaryStream while the host remains free to choose a standard .NET stream implementation.

### Ownership

The current adapter owns the wrapped Stream.

Disposing StreamBinaryStream disposes the underlying .NET stream.

If a host requires different lifetime semantics, that policy belongs to a future adapter or a higher-level abstraction rather than being silently assumed by the current implementation.

---

# Quick start

A type defines its own binary representation.

The following example stores one 32-bit integer:

~~~csharp
using System.Buffers.Binary;
using TheSingularityWorkshop.FSM_Serialization;

public sealed class ExampleState : IBinarySerializable
{
    public int Value { get; private set; }

    public ExampleState()
    {
    }

    public ExampleState(int value)
    {
        Value = value;
    }

    public void Pack(IBinaryStream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Value);
        stream.Write(buffer);
    }

    public void Unpack(IBinaryStream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];

        if (stream.Read(buffer) != buffer.Length)
            throw new EndOfStreamException();

        Value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }
}
~~~

A round trip is then straightforward:

~~~csharp
using var stream = new MemoryBinaryStream();

var original = new ExampleState(42);

original.Pack(stream);

stream.Position = 0;

var restored = new ExampleState();

restored.Unpack(stream);

Console.WriteLine(restored.Value); // 42
~~~

The important part is not the integer.

The important part is the ownership boundary:

~~~text
ExampleState
    │
    │ defines representation
    ▼
Pack / Unpack
    │
    ▼
IBinaryStream
    │
    ▼
bytes
~~~

FSM_Serialization supplies the last boundary. ExampleState supplies the meaning.

---

# Designing a binary representation

FSM_Serialization deliberately leaves format design to the type that owns the data.

An implementation should answer questions such as:

1. What fields are represented?
2. In what order are they written?
3. What width does each field use?
4. What byte ordering is used?
5. How are strings encoded?
6. How are collections represented?
7. How are optional values represented?
8. How is version information represented?
9. What happens when data is incomplete?
10. What happens when an older or newer representation is encountered?

For example:

~~~text
[int id][int value]
~~~

is a representation contract.

So is:

~~~text
[version][id][flags][payload-length][payload]
~~~

FSM_Serialization provides the mechanism for writing and reading those bytes. It does not declare which format is correct for a particular domain.

---

# Determinism

A binary representation can be deterministic, but the package does not automatically make arbitrary objects deterministic.

Determinism is a property of the representation contract.

If a type needs deterministic output, it should explicitly control:

- field ordering;
- collection ordering;
- byte ordering;
- string encoding;
- optional-field rules;
- version fields;
- identifier representation;
- nested representation rules.

For example, a semantically unordered collection may need a canonical ordering before it is packed.

~~~text
semantic collection
       │
       ▼
canonical ordering
       │
       ▼
binary representation
~~~

This matters when serialized bytes are used for:

- content identity;
- caching;
- synchronization;
- reproducible builds;
- comparison;
- lineage;
- integrity checks.

The package does not pretend that Pack alone guarantees any of those properties.

---

# Round-trip semantics

The fundamental operation is a semantic round trip:

~~~text
       A
       │
     Pack
       │
       ▼
     bytes
       │
    Unpack
       │
       ▼
       A'
~~~

A' does not need to be the same object instance as A.

It needs to contain the information required by the owning semantic contract.

This distinction is especially important for stateful systems.

A serializer should not be judged by whether it reproduces an object graph's incidental runtime identity. It should be judged against the semantic information the domain says is persistent.

---

# Versioning and compatibility

Binary formats are contracts.

Once bytes are persisted, cached, transmitted, or published, the representation may outlive the code that produced it.

That makes compatibility an explicit design concern.

A type that expects long-lived binary data should consider:

~~~text
identity
version
format version
field representation
dependency versions
compatibility rules
~~~

A useful distinction is:

- **object version** — the version of the semantic object or package;
- **format version** — the version of the byte representation;
- **dependency version** — the version of another representation required to interpret the data.

These are not necessarily the same thing.

FSM_Serialization provides the byte boundary. It does not impose a versioning scheme because different domains have different compatibility requirements.

---

# Storage is not serialization

A common architectural mistake is allowing serialization code to absorb storage concerns.

These are different responsibilities:

~~~text
Representation
    │
    ▼
FSM_Serialization
    │
    ▼
Storage / transport / host
~~~

Serialization answers:

> **How are these semantic values represented as bytes?**

Storage answers:

> **Where do those bytes live?**

Transport answers:

> **How do those bytes move between boundaries?**

A database, filesystem, cache, object store, HTTP service, message bus, or network protocol can consume the representation without becoming part of FSM_Serialization.

---

# Representation is not reality

The package follows a broader architectural principle used throughout The Singularity Workshop ecosystem:

> **A representation is not the thing it represents.**

For example:

~~~text
FSM state
   │
   │ semantic meaning
   ▼
domain object
   │
   │ representation
   ▼
FSM_Serialization
   │
   │ bytes
   ▼
storage / transport / host
~~~

The bytes are not the state.

They are a representation from which the required semantic state can be reconstructed.

Keeping that distinction explicit prevents representation mechanics from becoming accidental domain architecture.

---

# Relationship to the FSM ecosystem

FSM_Serialization is intended to sit below higher-level FSM composition and host systems.

A conceptual architecture is:

~~~text
                 FSM_API
                    │
                    ▼
              domain behavior
                    │
                    ▼
                FSM_COS
                    │
             composition / manifests
                    │
                    ▼
        FSM_Serialization
                    │
          binary representation
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
      storage              transport
          │                   │
          └─────────┬─────────┘
                    ▼
              host / runtime
                    │
          WebPage / Unity / Desktop
~~~

The exact dependency graph can evolve.

The responsibility boundary should remain stable.

| Layer | Responsibility |
|---|---|
| FSM_API | State-machine behavior and runtime state primitives |
| FSM_COS | Composition, manifests, dependencies, MicroBundles, arbitration |
| FSM_Serialization | Binary representation boundary |
| Storage layer | Persistence and allocation |
| Transport layer | Movement between external boundaries |
| Host | Execution and manifestation |
| Experience | What a user encounters |

FSM_Serialization should remain below application-specific manifestation.

---

# MicroBundles and manifests

The package is intentionally compatible with the broader idea of representing an Experience as a composition rather than as one giant application-specific object graph.

Conceptually:

~~~text
Experience
 ├── identity
 ├── version
 ├── ontology
 ├── MicroBundles
 │    ├── identity
 │    ├── version
 │    ├── configuration
 │    └── dependencies
 └── lineage
      ├── source
      ├── contributors
      └── ancestry
~~~

That does **not** mean FSM_Serialization currently defines an Experience manifest schema.

It does not.

The package supplies the low-level representation boundary through which a higher-level system can eventually encode such structures.

---

# What belongs in this package?

Good candidates include:

- byte-oriented serialization contracts;
- stream adapters;
- small representation primitives;
- representation-level validation helpers;
- future format-neutral binary utilities that remain independent of domain meaning.

The strongest test is:

> **Can this feature be useful to multiple domain packages without requiring it to understand their semantics?**

If yes, it may belong here.

---

# What does not belong here?

This package should not become:

- an FSM rules engine;
- a game system;
- an AEC model;
- a GUI framework;
- a browser framework;
- a Unity integration layer;
- a database abstraction;
- a filesystem framework;
- an HTTP client;
- an application configuration system;
- a MicroBundle implementation;
- an Experience runtime;
- a reflection-driven object graph serializer;
- a global serializer registry.

Those systems can **use** FSM_Serialization. They should not be forced into it.

---

# Explicit design trade-offs

### Benefits

- **Small surface area** — the core contracts are easy to inspect.
- **Explicit semantics** — domain types control representation.
- **Binary-first** — suitable for compact byte representations.
- **Storage-neutral** — representation is separated from where bytes live.
- **Transport-neutral** — the same representation can cross different boundaries.
- **Testable** — the contracts can be tested without external infrastructure.
- **Composable** — higher-level packages can build their own representations above the primitive boundary.
- **No hidden reflection** — no automatic object graph traversal is imposed.
- **No global registry** — types do not need to register themselves with a central serializer.

### Costs

- **You own the format.**
- **You must define compatibility rules.**
- **Binary data is less immediately human-readable than text formats.**
- **Schema evolution is an application/domain responsibility.**
- **Partial reads must be handled correctly.**
- **The package does not automatically provide version negotiation.**
- **The package does not automatically provide persistence.**

These are deliberate trade-offs rather than missing features.

---


# Performance

The published **0.1.0-alpha.2** package was benchmarked through the companion **FSM_Serialization_Benchmarks** project using BenchmarkDotNet 0.15.2 on .NET 8.0.31, running on an Intel Core i5-10400F (6 physical / 12 logical cores) with RyuJIT AVX2. The benchmark suite exercised sizes of **16, 1,024, 10,000, and 100,000 bytes**.

The results show an important distinction between the package's **representation overhead** and the cost of copying data into MemoryBinaryStream.

## Results

| Operation | Size | MemoryStream | FSM_Serialization | Relative result |
|---|---:|---:|---:|---:|
| Construct empty | 16–100,000 | ~7.1–7.3 ns | ~10.2–10.7 ns | ~1.4–1.5× |
| Write | 100,000 | 39,894.85 ns | 40,005.59 ns | ~1.00× |
| Read via StreamBinaryStream | 100,000 | 2,272.47 ns | 2,298.40 ns | ~1.01× |
| ToArray | 100,000 | 80,063.31 ns | 80,227.68 ns | ~1.00× |
| Position + overwrite | 100,000 | 39,824.53 ns | 80,220.26 ns | ~2.01× |
| Pack + Unpack round trip | 100,000 | — | 60,152.74 ns | end-to-end contract measurement |

The empty construction benchmark measured MemoryBinaryStream at about **10.2 ns** versus **7.1–7.3 ns** for MemoryStream, with the package stream allocating 88 B versus 64 B. fileciteturn317file0L25-L40

For bulk writes, the abstraction overhead effectively disappears as payload size grows: at 10,000 bytes the package measured **540.28 ns** versus **541.06 ns**, and at 100,000 bytes **40,005.59 ns** versus **39,894.85 ns**. StreamBinaryStream was similarly close at those sizes. fileciteturn317file6L25-L40

StreamBinaryStream is particularly significant for the adapter design. At 100,000 bytes, reading through the adapter measured **2,298.40 ns** versus **2,272.47 ns** for direct MemoryStream access, a ratio of about **1.01×**. At 10,000 bytes the measured ratio was about **1.02×**. fileciteturn317file3L25-L40

ToArray also converged at larger payloads: **80,227.68 ns** for MemoryBinaryStream versus **80,063.31 ns** for MemoryStream at 100,000 bytes, with the same reported 200 KB-scale allocation. fileciteturn317file5L25-L35

The end-to-end IBinarySerializable benchmark measured **64.08 ns**, **207.47 ns**, **1,481.61 ns**, and **60,152.74 ns** at 16, 1,024, 10,000, and 100,000 bytes respectively. This benchmark includes creation of the test value, stream creation, packing, rewinding, creation of the destination value, allocation of its payload, and unpacking. fileciteturn317file4L25-L31

## An important benchmark qualification

The existing-data constructor and direct MemoryBinaryStream read benchmarks are **not apples-to-apples allocation comparisons** with MemoryStream.

MemoryStream(byte[], writable: false) can wrap the supplied array without copying it, while the current MemoryBinaryStream(byte[]) constructor copies the supplied bytes into its internal memory stream. Consequently, the benchmark deliberately exposes the cost of that current design rather than isolating only interface dispatch.

For example, constructing from 100,000 bytes measured **6.05 ns / 64 B** for MemoryStream versus **39,975.93 ns / 100,122 B** for MemoryBinaryStream; reading 100,000 bytes measured **2,272.47 ns / 64 B** for MemoryStream versus **43,128.59 ns / 100,122 B** for MemoryBinaryStream. fileciteturn317file1L25-L35 fileciteturn317file3L25-L39

That result should therefore be read as a **copying-cost measurement**, not evidence that the IBinaryStream abstraction itself is intrinsically 19× slower.

Likewise, the position/overwrite benchmark includes a common input-array copy on both sides; the package path then performs its write through the abstraction. At 100,000 bytes it measured **80,220.26 ns** versus **39,824.53 ns** for the baseline. fileciteturn317file2L25-L35

### What the first benchmark establishes

The first benchmark pass gives the package a useful baseline:

- the empty stream abstraction has a small fixed construction cost;
- bulk writes converge with MemoryStream as payload size increases;
- StreamBinaryStream adds very little overhead for large reads;
- ToArray is effectively at the same throughput at 100 KB;
- the current MemoryBinaryStream(byte[]) copy is a measurable cost and should remain visible rather than being mistaken for interface overhead;
- the Pack/Unpack contract is fast enough to establish a concrete end-to-end performance baseline for future representation work.

These numbers are **environment-specific measurements, not guarantees**. Future releases should rerun the benchmark suite when representation or stream implementation changes.

---

## Benchmark source and methodology

The published performance section above is backed by the executable **[FSM_Serialization_Benchmarks](https://github.com/TrentBest/FSM_Serialization_Benchmarks)** project.

The benchmark source is intentionally separate from the package so readers can inspect the experiment itself.

When reading the benchmark, look for:

- `[MemoryDiagnoser]` — allocation measurements;
- `[SimpleJob(RuntimeMoniker.Net80)]` — explicit .NET runtime;
- `[Params(...)]` — payload sizes;
- `[Benchmark(Baseline = true)]` — the local `MemoryStream` comparison;
- `[Benchmark]` — the Workshop implementation under test.

The first benchmark pass targeted package version `0.1.0-alpha.2`. Its results are historical evidence tied to that implementation and environment.

The benchmark's most important lesson is methodological: the large cost seen in the existing-data `MemoryBinaryStream(byte[])` path is substantially a **copying cost**, because the BCL comparison can wrap the original array while the current Workshop constructor copies it.

For a reader-friendly explanation of the experiment, the interpretation of the numbers, and reproduction instructions, see **[docs/BENCHMARKING.md](docs/BENCHMARKING.md)**.

# Testing

The repository includes an xUnit test project covering the current contracts and adapters.

The current suite verifies:

- empty MemoryBinaryStream behavior;
- readable/writable capability reporting;
- byte writes and reads;
- preservation of written bytes;
- initialization from existing bytes;
- null-input rejection;
- end-of-input behavior;
- positioning and overwrite behavior;
- StreamBinaryStream capability delegation;
- StreamBinaryStream null-input rejection;
- writes reaching the wrapped .NET stream;
- pack/unpack round trips;
- combined IBinarySerializable behavior;
- incomplete binary input handling.

The test project targets .NET 8 and references the library project directly.

The repository verification workflow collects Cobertura coverage and uploads the report to Codecov.

---

# Continuous integration and publication

The repository uses GitHub Actions through **.github/workflows/verify.yml**.

The verification workflow performs:

1. restore;
2. Release build;
3. test execution;
4. Cobertura coverage collection;
5. coverage normalization;
6. Codecov upload;
7. NuGet package creation;
8. package artifact upload;
9. test and coverage artifact upload.

NuGet publication is a separate manual step using NuGet Trusted Publishing through GitHub's OIDC identity.

The separation is intentional:

~~~text
verify
  │
  ├── build
  ├── test
  ├── coverage
  └── pack
        │
        ▼
   package artifact
        │
        ▼
manual publish
~~~

A successful build does not silently publish a package.

---

# Package contents

The package is built from the root project **TheSingularityWorkshop.FSM_Serialization.csproj**.

The package includes:

- the compiled library;
- XML documentation;
- this README as the NuGet package README;
- the MIT license file;
- the documentation images used by the package README.

Build output and local artifacts are excluded from source control.

---

# Repository structure

~~~text
TheSingularityWorkshop.FSM_Serialization/
│
├── .github/
│   └── workflows/
│       └── verify.yml
│
├── docs/
│   ├── THEORY.md
│   └── images/
│
├── src/
│   ├── IBinaryStream.cs
│   ├── IBinaryPackable.cs
│   ├── IBinaryUnpackable.cs
│   ├── IBinarySerializable.cs
│   ├── MemoryBinaryStream.cs
│   └── StreamBinaryStream.cs
│
├── TheSingularityWorkshop.FSM_Serialization.Tests/
│   ├── BinarySerializationTests.cs
│   └── TheSingularityWorkshop.FSM_Serialization.Tests.csproj
│
├── LICENSE.txt
├── README.md
├── TheSingularityWorkshop.FSM_Serialization.csproj
└── TheSingularityWorkshop.FSM_Serialization.slnx
~~~

The implementation project is intentionally small and the tests live beside the solution.

---

# Documentation map

This repository has two complementary documentation layers.

### README

This document answers:

> **What is the package, why would I use it, how do I install it, and how do I use the API?**

### Theory

See [docs/THEORY.md](docs/THEORY.md).

The theory answers:

> **Why does this boundary exist, and what architectural principles should remain intact as the package grows?**

The theory covers:

- representation versus reality;
- binary boundaries;
- stream boundaries;
- representation versus storage;
- canonical representation;
- composition manifests;
- demand-driven reconstruction;
- versioning;
- lineage;
- format neutrality;
- extraction from the WebPage proving ground.

---

# Project status

**Current status: Stable (1.0.0)**

The package is intentionally small and is being established as reusable infrastructure for the broader FSM ecosystem.

The current release establishes the primitive binary boundary:

~~~text
IBinaryPackable
        │
        ▼
  IBinaryStream
        │
        ▼
IBinaryUnpackable
~~~

Higher-level serializers, schemas, manifests, and domain-specific representations should only be added when they have a clear architectural owner and do not compromise the boundary established here.

---

# Contributing and evolution

When changing this package, preserve the following invariants:

### 1. Keep semantic ownership outside the primitive

The serialization layer should not need to understand what a domain object means.

### 2. Keep the byte boundary explicit

Avoid introducing hidden global state or implicit serialization behavior.

### 3. Prefer small composable contracts

A type should be able to depend on the smallest capability it actually requires.

### 4. Treat binary formats as contracts

If a change affects representation semantics, document the compatibility implications and add tests.

### 5. Test behavior, not implementation trivia

Tests should establish the observable contract of streams and representations.

### 6. Do not pull application infrastructure downward

Storage, transport, UI, host lifecycle, and application policy belong above this layer.

### 7. Preserve format neutrality at the architecture level

Binary is the current concrete representation boundary. It should not become an excuse to couple semantic models to one storage or transport technology.

---

# License

This project is licensed under the MIT License.

See [LICENSE.txt](LICENSE.txt).

---

# The Singularity Workshop

**TheSingularityWorkshop.FSM_Serialization** is part of The Singularity Workshop ecosystem.

The broader architectural direction is:

~~~text
             behavior
                │
             FSM_API
                │
                ▼
            composition
                │
             FSM_COS
                │
                ▼
         representation
                │
       FSM_Serialization
                │
          ┌─────┴─────┐
          ▼           ▼
       storage     transport
          │           │
          └─────┬─────┘
                ▼
              host
                │
        manifestation / experience
~~~

The package's job is deliberately narrow:

> **Provide a clean boundary across which semantic state can become bytes, and bytes can become semantic state again.**

Everything else should have to earn its way into a higher-level layer.

---

## 🔗 Resources & Support

### 📦 Get FSM_API

- **Unity Asset Store:** [FSM_API for Unity](https://assetstore.unity.com/packages/slug/332450)
- **Core NuGet:** [TheSingularityWorkshop.FSM_API](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_API)
- **Source Code:** [TheSingularityWorkshop.FSM_Serialization on GitHub](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization)
- **This Package:** [TheSingularityWorkshop.FSM_Serialization](https://www.nuget.org/packages/TheSingularityWorkshop.FSM_Serialization)

### 💖 Support The Singularity Workshop

- **Patreon:** [Support us on Patreon](https://www.patreon.com/c/TheSingularityWorkshop)
- **PayPal:** [Make a donation](https://www.paypal.com/donate/?hosted_button_id=3Z7263LCQMV9J)

<p align="center">
  <a href="https://github.com/TrentBest/FSM_API">
    <img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" height="200">
  </a>
</p>

<p align="center">
  <em>The Singularity Workshop — Tools for the curious, the bold, and the systemically inclined.</em><br>
  <strong>Because state shouldn't be a mess.</strong>
</p>


---

## 🔗 The Singularity Workshop

This project is part of a deliberately troublesome ecosystem:

- **[FSM_API](https://github.com/TrentBest/FSM_API)** — behavior and state.
- **[FSM_COS](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS)** — composition and runtime assembly.
- **[FSM_Serialization](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization)** — representation and the byte boundary.
- **[WebPage](https://github.com/TrentBest/WebPage)** — browser manifestation and proving ground.
- **[FSM_API_Unity](https://github.com/TrentBest/FSM_API_Unity)** — Unity manifestation.

<p align="center"><em>The Singularity Workshop — Tools for the curious, the bold, and the systemically inclined.</em><br><strong>Because state shouldn't be a mess.</strong><br><em>And because static boundaries are invitations to cause trouble.</em></p>

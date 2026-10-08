# FSM_Serialization Benchmarking

The serialization package has an executable companion benchmark project:

**[FSM_Serialization_Benchmarks](https://github.com/TrentBest/FSM_Serialization_Benchmarks)**

The benchmark exists to answer a specific question:

> **Does the representation boundary add meaningful cost to the operations for which we intend to use it?**

## What was measured?

The benchmark compares the Workshop implementations with .NET `MemoryStream` for:

- empty construction;
- writing;
- reading;
- construction from existing data;
- `ToArray`;
- position/overwrite;
- an end-to-end `IBinarySerializable` round trip.

Payload sizes:

~~~text
16 B
1,024 B
10,000 B
100,000 B
~~~

The recorded run used BenchmarkDotNet 0.15.2 on .NET 8.0.31, Windows 10 22H2, Intel Core i5-10400F, 6 physical / 12 logical cores, RyuJIT AVX2.

The benchmark targeted package version `0.1.0-alpha.2`. These numbers are therefore historical measurements, not a guarantee for the later stable package.

## Representative results

| Operation | Size | MemoryStream | FSM_Serialization | Relative result |
|---|---:|---:|---:|---:|
| Construct empty | 16–100,000 | ~7.1–7.3 ns | ~10.2–10.7 ns | ~1.4–1.5× |
| Write | 100,000 B | 39,894.85 ns | 40,005.59 ns | ~1.00× |
| Read via StreamBinaryStream | 100,000 B | 2,272.47 ns | 2,298.40 ns | ~1.01× |
| ToArray | 100,000 B | 80,063.31 ns | 80,227.68 ns | ~1.00× |
| Position + overwrite | 100,000 B | 39,824.53 ns | 80,220.26 ns | ~2.01× |
| Pack + Unpack | 100,000 B | — | 60,152.74 ns | end-to-end |

## What do these numbers mean?

The most important result is not a single ratio.

At 100 KB, bulk write is almost identical in the recorded run:

~~~text
MemoryStream          39,894.85 ns
MemoryBinaryStream    40,005.59 ns
~~~

Likewise, reading through `StreamBinaryStream` was close:

~~~text
MemoryStream          2,272.47 ns
StreamBinaryStream    2,298.40 ns
~~~

This establishes evidence that the stream abstraction can be very close to the BCL baseline for large sequential operations in the measured environment.

## The copying caveat

The benchmark also deliberately exposes a current implementation cost.

`MemoryStream(byte[], writable: false)` can wrap an existing array.

The current `MemoryBinaryStream(byte[])` constructor copies that data.

Therefore a large difference in the existing-data constructor is primarily evidence about **copying**, not proof that the abstraction is intrinsically slower.

At 100 KB:

~~~text
MemoryStream(byte[])          6.05 ns / 64 B
MemoryBinaryStream(byte[])   39,975.93 ns / 100,122 B
~~~

The corresponding read path showed:

~~~text
MemoryStream                   2,272.47 ns / 64 B
MemoryBinaryStream             43,128.59 ns / 100,122 B
~~~

That distinction matters. A good benchmark does not merely report the larger number; it helps the reader understand **why** the number is large.

## End-to-end Pack + Unpack

The semantic round-trip benchmark measured:

| Payload | Mean |
|---:|---:|
| 16 B | 64.08 ns |
| 1,024 B | 207.47 ns |
| 10,000 B | 1,481.61 ns |
| 100,000 B | 60,152.74 ns |

The benchmark includes value creation, stream creation, packing, rewinding, destination creation, payload allocation, and unpacking.

It is therefore an end-to-end contract baseline, not an isolated measurement of `Pack`.

## How to recognize the benchmark

Open the companion source file and look for:

- `[MemoryDiagnoser]` — allocation measurement;
- `[SimpleJob(RuntimeMoniker.Net80)]` — explicit runtime;
- `[Params(...)]` — payload-size matrix;
- `[Benchmark(Baseline = true)]` — BCL comparison;
- `[Benchmark]` — Workshop implementation under test.

The evidence chain is:

~~~text
package documentation
       │
       ▼
benchmark repository
       │
       ▼
benchmark class
       │
       ▼
[Benchmark] method
       │
       ▼
BenchmarkDotNet output
       │
       ▼
interpretation
~~~

## Reproduce the measurement

From the benchmark repository:

~~~bash
dotnet run -c Release
~~~

For a meaningful comparison, keep package version, benchmark source, runtime, payload sizes, and BenchmarkDotNet version stable.

Performance measurements are sensitive to hardware, runtime, OS, thermal state, background activity, and benchmark configuration.

## Why the benchmark is separate from CI

Correctness belongs in normal CI.

Performance experiments are different: they are observations of a workload on a machine.

The benchmark repository is therefore maintained as an executable experiment archive rather than as a mandatory performance gate on every commit.

## When should we rerun it?

Rerun after changes to:

- `MemoryBinaryStream`;
- `StreamBinaryStream`;
- buffer ownership;
- copying behavior;
- `IBinaryStream`;
- packing/unpacking;
- allocation strategy;
- binary representation primitives;
- runtime targets.

The goal is not to "win" a benchmark.

The goal is to know what changed.

**Measure first. Change one thing. Measure again.**

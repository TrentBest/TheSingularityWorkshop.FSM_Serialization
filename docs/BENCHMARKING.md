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

## Relating the package benchmark to FSM_COS stress results

An end-to-end stress-test improvement reported in FSM_COS/Simple Demos is valuable evidence about that workload, but it is not automatically a measurement of FSM_Serialization. The current BenchmarkDotNet numbers above are historical microbenchmarks of the stream primitives and an illustrative pack/unpack value. Keep those evidence classes separate.

### What the first seven-trial comparison actually shows

The [SimpleDemos CI run 40162369632](https://github.com/TrentBest/SimpleDemos/actions/runs/40162369632) uploaded a raw CSV sample for commit 82fa35031137f0d8687794c39651290b1a8a7288. The runner used .NET 8.0.32 on Ubuntu 24.04.5 LTS with four logical CPUs, Release configuration, 1,000 workers, 20 warm-up ticks, and 200 measured ticks per trial. Every trial passed the equivalence gate: 1,000 worker updates and 3,000 payload operations per measured tick on both paths.

The seven measured steady-state tick-time rows yield:

| Summary | Direct FSM_API | FSM_COS-composed | Apparent reduction |
|---|---:|---:|---:|
| Median of all 7 raw tick times | 0.180824 ms | 0.143573 ms | 20.6% |
| Median of paired per-trial percentage changes | — | — | 2.2% |

The paired improvements vary substantially: approximately +20.7%, +0.8%, -6.8%, +40.5%, +2.2%, -3.3%, and +5.1% (positive means FSM_COS ticked faster). This disagreement between summary methods, plus large trial-to-trial variation, means the ~21% figure is an **early observation, not a statistically robust conclusion**. The raw artifact is available from the [CI run's artifacts](https://github.com/TrentBest/SimpleDemos/actions/runs/40162369632). The canonical measurement contract and next-experiment gates are documented in [SimpleDemos' FSM_COS stress benchmark protocol](https://github.com/TrentBest/SimpleDemos/blob/development/docs/FSM_COS_STRESS_BENCHMARK.md).

Steady-state allocations were effectively equal in this sample: 96,728 bytes/tick for most rows, with 96,731 bytes in the first direct row and 96,728 bytes in its paired FSM_COS row. Setup/composition and bundle-load times varied and should be reported separately from tick time.

Also keep the workloads distinct: the original Stress/Program.cs stress test ramps to tens of thousands of workers and records FPS, operations/frame, and memory delta until the 30-FPS threshold is crossed. The new direct-versus-FSM_COS comparator is a bounded 1,000-worker, interval-1 experiment that measures mean tick time, allocations, and startup/composition. These are complementary experiments, **not yet a direct apples-to-apples replication of the original scaling curve**.

### Required next comparison

Preserve and rerun the original SimpleDemos baseline alongside the FSM_COS path using the same worker counts, tick intervals, runtime, and threshold policy. Record exact source commits and raw CSV output. For serialization itself, add separate measurements with identical semantic input:

1. FSM workload without serialization in the measured path.
2. Pack only into an in-memory stream.
3. Unpack from a prepared byte buffer.
4. Pack + unpack round trip.
5. Snapshot capture plus pack/unpack, if snapshot construction is part of the real consumer.

Measure elapsed time/throughput and allocations/bytes copied. Vary active FSM count, state/transition count, context payload size, and update cadence. Separate snapshot consistency/capture cost from encoding cost. Do not attribute the full ~21% end-to-end observation to the serializer until an isolated experiment supports that conclusion.

The resulting report should include the original baseline and new measurements side by side, exact commit IDs, raw stress-harness output, and an explanation of which layer accounts for each change. A faster end-to-end result can coexist with a slower serialization sub-operation if another part of the workload improved more.

See [the FSM_Serialization test matrix](TEST_MATRIX.md) for the domain questions and acceptance gates that accompany the benchmark plan.

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

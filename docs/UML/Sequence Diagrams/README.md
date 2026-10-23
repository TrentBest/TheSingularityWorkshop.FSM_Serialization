# Sequence Diagrams and Complexity

A sequence diagram is tied to a named use case or scenario. Show participants, method calls/returns, important branches, loops, error paths, and ownership/lifetime boundaries. Record the source methods and spans supporting each interaction.

For every involved method, provide a complexity table:

| Method | Time complexity | Space complexity | Assumptions/input variables | Evidence/confidence | Risk |
|---|---|---|---|---|---|
| To be generated | Unknown until analyzed | Unknown until analyzed | Must be stated | Source spans + analyzer result | Unassessed |

Flag likely O(n²), O(n³), exponential, or otherwise input-amplifying work for review. Also flag repeated linear searches, nested iteration, recursion, unbounded loops, avoidable copying/allocation, and I/O in hot paths.

Big-O results are estimates unless a formal proof exists. Calls through interfaces, dynamic dispatch, third-party libraries, I/O, scheduling, and concurrency can prevent a precise static bound. Do not manufacture an upper-bound latency from complexity notation. Practical estimates require reproducible benchmarks/profiling, representative workload sizes, environment details, and explicit assumptions.

Track findings with severity, source evidence, confidence, validation method, and disposition (unreviewed, accepted, optimized, or false positive). Generator and analyzer work is tracked in [issue #7](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/issues/7).

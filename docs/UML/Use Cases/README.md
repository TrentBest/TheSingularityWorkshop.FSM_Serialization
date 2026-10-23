# Use Cases

**One use case per file.** Organize by primary intent, not by class name. A use case may touch many types; link to the related class diagrams, neighborhoods, sequences, and tests.

Start with these category folders:
- [User Goals](User%20Goals/README.md) — outcomes visible to a user or consuming application.
- [API and Contract](API%20and%20Contract/README.md) — public API obligations and caller-visible behavior.
- [Data and Serialization](Data%20and%20Serialization/README.md) — packing, unpacking, round trips, malformed/truncated input, and compatibility.
- [Operational](Operational/README.md) — lifecycle, ownership, disposal, failure, and recovery.
- [Integration](Integration/README.md) — package and runtime boundaries.
- [Performance](Performance/README.md) — latency, throughput, allocation, and scaling.
- [Security and Abuse](Security%20and%20Abuse/README.md) — invalid input, resource exhaustion, misuse, and defensive behavior.
- [Maintenance and Evolution](Maintenance%20and%20Evolution/README.md) — versioning, migration, compatibility, and extension.

## Required template

Each use-case file states: ID/title, primary category, actors, goal, preconditions, trigger, main success flow, alternate/error flows, postconditions, relevant source declarations, tests/evidence, related diagrams, and unresolved questions. Avoid duplicated copies; use tags and cross-links for secondary categories.

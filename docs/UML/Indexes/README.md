# UML Indexes and Traceability

Generated indexes should include:
- declaration catalog and one-to-one diagram coverage;
- source files/declarations with no diagram;
- neighborhood relationship catalog;
- sequence/use-case cross-links;
- complexity/performance findings;
- generation timestamp, source commit, generator version, and configuration;
- stale-output and unresolved-link reports.

Each model node and edge should have a stable identity, source path/span where applicable, and provenance. CI should regenerate and compare outputs so stale diagrams fail validation rather than quietly drifting.

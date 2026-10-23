# Class Neighborhoods

Create one focus view per type. Show the focus type centrally, with direct collaborators/dependencies around it and a clear relationship direction.

Include:
- types it calls, constructs, contains, implements, or delegates to;
- types that call or depend on it, where static source evidence or test/runtime evidence exists;
- relevant method/member names and source locations;
- edge provenance: Roslyn source, reflection metadata, test-observed, runtime-observed, inferred, or manually authored;
- confidence and known blind spots.

Expand a UML data card to inspect the members or related types that establish the edge. Do not imply a type is unused merely because reflection or a single source scan did not find a reference: reflection, dependency injection, configuration, generated code, and external callers can hide uses.

Generator: planned under [issue #7](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/issues/7).

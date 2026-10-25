# Release Notes — 1.0.1 candidate

**Status:** candidate on `development`; not published. NuGet publication requires explicit release approval.

## Intended scope

This is a documentation-led patch following the published 1.0.0 package. It does not intentionally change the public API or impose a new binary representation format.

- Reorganizes the README around What / Why / How / See it in a minute / Documentation and provides a complete .NET 8 console walkthrough.
- Adds `Gallery.md`, byte-operation diagrams, a serialization domain inventory, focused UML documents, and companion Markdown cards beside each source file.
- Includes the consumer-facing Markdown guides and diagrams in the NuGet package while excluding the internal Sith branch-reconciliation ledger.
- Expands stream tests for copied initial data, wrapped-stream disposal, short-read propagation, position delegation, and unsupported underlying capabilities.
- Pins all GitHub Actions references in the release workflow to immutable commit SHAs.
- Adds pull-request CI verification and keeps NuGet publication behind a manual workflow dispatch, an explicit `publish: true` input, and the `development` branch condition.

## Test and domain hardening follow-up

The candidate is also being hardened before release review:

- Expanded stream contract tests for empty buffers, seeking beyond the end, negative positions, disposed-stream behavior, flush delegation, and completing fixed-size fields across repeated short reads.
- Added a golden little-endian Int32 byte fixture, signed Int32 boundary round trips, and rejection checks for every truncated prefix of the example field.
- Corrected the README quick-start example to loop until a fixed-size field is complete or a zero-byte read proves end-of-input; a single short read is not necessarily EOF.
- Added a serialization domain inventory and test matrix that distinguish primitive stream behavior from format-specific requirements, FSM definition versus runtime snapshot decisions, compatibility, canonical bytes, resource limits, abuse testing, and performance evidence.
- Added CI minimums of 90% line coverage and 85% branch coverage. The previously inspected baseline was 100% for both (31/31 lines, 4/4 branches), but the expanded test suite and new gate require a fresh successful run before the candidate's current coverage can be claimed.

The reported roughly 21% FSM_COS/Simple Demos stress-test improvement is treated as an end-to-end observation, not automatically attributed to serialization. The benchmark guide now specifies a controlled plan to isolate pack, unpack, round-trip, snapshot-capture, allocation, and copying costs.

## Compatibility

- No public API changes are intended.
- No universal object-graph serializer, schema registry, migration engine, or persistence layer is introduced.
- The concrete domain type remains responsible for its binary field order, encoding, lengths, validation, and compatibility rules.

## Verification required before release

- Pre-reconciliation CI evidence: [run 39988802838](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/39988802838), commit `f794b1d7f33f061d86af4b3e8a1ed2b92912c27d`, passed restore, Release build, **17/17 tests**, coverage report preparation/upload, package creation, and artifact upload. The NuGet publish job was skipped.
- The exact `.nupkg` from that run was downloaded and inspected. It contains version 1.0.1, the net8.0 DLL and XML documentation, README, Gallery, license, release notes, consumer documentation, six source companion cards, UML documentation, and SVG/JPG assets. The internal `docs/SITH_BRANCH_RECONCILIATION.md` is absent. The package metadata records source commit `f794b1d7f33f061d86af4b3e8a1ed2b92912c27d`.
- The first post-fix verification, [run 40163719191](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/40163719191), passed and produced an artifact containing both the 1.0.1 `.nupkg` and `.snupkg`. The symbol package was inspected and contains a PDB. The workflow now uploads both extensions, and the guarded publish job downloads both to the same folder. NuGet's documented behavior is to publish the primary `.nupkg` first and its matching `.snupkg` second when both are present in that folder. [Microsoft's symbol-package guidance](https://learn.microsoft.com/nuget/create-packages/symbol-packages-snupkg) describes this behavior.
- The README's verification badge points to the actual `.github/workflows/build.yml`. The shared Workshop Documentation Standard reference from `master` has been preserved in the active README.
- The branch ledger now records the four `master`-only commits and their disposition. Final branch comparison and a fresh CI run/package inspection are still required after the documentation reconciliation commits.
- Keep publication gated until the repository owner explicitly approves release.

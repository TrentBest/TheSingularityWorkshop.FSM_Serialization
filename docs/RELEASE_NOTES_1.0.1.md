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

## Compatibility

- No public API changes are intended.
- No universal object-graph serializer, schema registry, migration engine, or persistence layer is introduced.
- The concrete domain type remains responsible for its binary field order, encoding, lengths, validation, and compatibility rules.

## Verification required before release

- Confirm the CI run after immutable action pinning passes build, all tests, coverage upload, and package creation. Run [39910505899](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/39910505899) passed **17/17 tests**, with 100% line coverage (31/31) and 100% branch coverage (4/4).
- Inspect the exact 1.0.1 `.nupkg` and confirm the consumer docs are present and internal operational notes are absent. This content check has passed for artifact built at commit `0669623b83a7534513385afe3a2a47fd70c1b9d6`: version 1.0.1, all required consumer docs and assets present, internal ledger absent.
- Complete final review of stream edge cases, package metadata, README claims, and branch reconciliation; rerun CI after the final evidence/documentation edits.
- Keep publication gated until the repository owner explicitly approves release.

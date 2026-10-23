# Release Notes — 1.0.1 candidate

**Status:** candidate on `development`; not published. NuGet publication requires explicit release approval.

## Intended scope

This is a documentation-led patch following the published 1.0.0 package. It does not intentionally change the public API or impose a new binary representation format.

- Reorganizes the README around What / Why / How / See it in a minute / Documentation and provides a complete .NET 8 console walkthrough.
- Adds `Gallery.md`, byte-operation diagrams, a serialization domain inventory, focused UML documents, and companion Markdown cards beside each source file.
- Includes the consumer-facing Markdown guides and diagrams in the NuGet package while excluding the internal Sith branch-reconciliation ledger.
- Expands stream tests for copied initial data, wrapped-stream disposal, and short-read propagation.
- Adds pull-request CI verification and keeps NuGet publication behind a manual workflow dispatch, an explicit `publish: true` input, and the `development` branch condition.

## Compatibility

- No public API changes are intended.
- No universal object-graph serializer, schema registry, migration engine, or persistence layer is introduced.
- The concrete domain type remains responsible for its binary field order, encoding, lengths, validation, and compatibility rules.

## Verification required before release

- Confirm the latest CI run passes build, all tests, coverage upload, and package creation.
- Inspect the exact 1.0.1 `.nupkg` and confirm the consumer docs are present and internal operational notes are absent.
- Complete final review of stream edge cases, package metadata, README claims, and branch reconciliation.
- Keep publication gated until the repository owner explicitly approves release.

# Sith Branch Reconciliation — FSM_Serialization

**Policy:** the intended final branch set is exactly `master` and `development`. Preserve unique work before any surplus branch is removed. Branch deletion remains the repository owner's manual action; this ledger does not delete branches.

## Snapshot at start of reconciliation

| Branch | Related PR | Unique work | Reconciliation disposition |
|---|---:|---|---|
| `development` | — | Active integration line; source, tests, existing docs, and gated build workflow | Keep |
| `master` | — | Release line | Keep |
| `docs/readme-standard` | #5 | Tutorial-first README and cleaned-up theory | Reworked tutorial and theory are now on `development`; the active development README also retains the five-section Workshop structure and adds living diagrams |
| `docs/benchmarking` | #3 | Benchmark methodology guide, README benchmark evidence links, and publication gate hardening | `docs/BENCHMARKING.md` and the methodology section have been reconciled into `development`; the active build workflow retains an explicit manual publication gate |
| `chore/standardize-ci-fsm-serialization` | #4 | Pull-request CI trigger and standardized verification workflow | Pull-request validation trigger is now reconciled into `development`; retain the existing stronger manual publish condition |
| `hardening/manual-nuget-publication` | #2 | Reworked manual-only publication workflow with verification and Trusted Publishing | The active `build.yml` gates publication on manual dispatch, explicit `publish: true`, and the `development` ref. The redundant legacy `.github/workflows/publish.yml` was removed from `development`, leaving one publication path to audit. |

## Documentation work now on development

- Root `Gallery.md` (also included in the NuGet package).
- `docs/BYTE_OPERATIONS.md` and two animated SVG flows (packing and unpacking); documentation Markdown is now included in the NuGet package alongside its images.
- UML cards under `docs/UML/`.
- Companion Markdown class cards beside all six source files.
- Tutorial-first README, theory cleanup, benchmark guide, fixed CI badge, and pull-request CI trigger.

## PR and branch disposition

PRs [#2](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/2), [#3](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/3), [#4](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/4), and [#5](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/5) are now **closed as superseded**, with a reconciliation comment on each. Their unique work has been copied or replaced by the active implementation on `development`. The four surplus branch refs still exist intentionally; they are not deleted by this workflow. The repository owner will delete them only after explicitly being told reconciliation is complete.

## Final verification and handoff

- Latest verified CI: [run 39910661977](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/39910661977) passed after the immutable action pins and documentation updates. It reports **17/17 tests passed**, **100% line coverage (31/31)**, and **100% branch coverage (4/4)** for the current assembly.
- The actual 1.0.1 package artifact was inspected: version 1.0.1, README/Gallery/license, serialization-domain inventory, candidate release notes, focused UML documents, all six source companion cards, and both animated SVGs are present. The internal Sith ledger is excluded.
- The stream audit covers short reads, end-of-stream behavior in the example format, position delegation, unsupported underlying capabilities, wrapped-stream disposal, and round-trip/truncated-input behavior.
- All workflow action references are pinned to immutable commit SHAs. The publication job remains gated on manual dispatch, explicit `publish: true`, and the `development` ref. No publication was performed.
- Branch comparison confirms the four surplus branches have no unharvested unique file changes: the benchmark guide and theory are exact matches, README/CI changes are reconciled, and the separate manual publisher's verification/publish gate is represented by the active build workflow. PRs #2–#5 are closed, no PRs remain open, and all four surplus branch refs remain present.

**Branch target:** `master` + `development` only.  
**Manual branch handoff:** the four surplus branches are ready for the repository owner to delete manually. No branch has been deleted by this work.  
**Package publication:** not performed or authorized by this work.  
**Readiness estimate:** 85% — the 1.0.1 candidate is ready for repository-owner review. The remaining 15% is reserved for review feedback and any corrections it reveals; NuGet publication still requires explicit approval.

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
| `hardening/manual-nuget-publication` | #2 | Reworked manual-only publication workflow with verification and Trusted Publishing | The active `build.yml` already gates publication on manual dispatch, explicit `publish: true`, and the `development` ref. No second publisher is needed; the legacy `publish.yml` remains disabled and needs final release-policy review |

## Documentation work now on development

- Root `Gallery.md`.
- `docs/BYTE_OPERATIONS.md` and two animated SVG flows (packing and unpacking).
- UML cards under `docs/UML/`.
- Companion Markdown class cards beside all six source files.
- Tutorial-first README, theory cleanup, benchmark guide, fixed CI badge, and pull-request CI trigger.

## PR and branch disposition

PRs [#2](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/2), [#3](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/3), [#4](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/4), and [#5](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/5) are now **closed as superseded**, with a reconciliation comment on each. Their unique work has been copied or replaced by the active implementation on `development`. The four surplus branch refs still exist intentionally; they are not deleted by this workflow.

## Remaining reconciliation checks

- Inspect CI after the workflow change and confirm build, tests, coverage, and pack succeed.
- Verify animated SVGs render in GitHub and in a browser; keep them explanatory, not a substitute for tests.
- Audit the complete public API, stream edge cases, test coverage, package contents, and current release metadata.
- Review the legacy disabled `publish.yml` before the next release.
- Confirm each open PR is either merged or closed as superseded only after its unique content is present on `development`.
- Then the owner can manually delete the four surplus branches. Do not delete them before this ledger and PR state are reviewed.

**Branch target:** `master` + `development` only.  
**Package publication:** not performed or authorized by this work.

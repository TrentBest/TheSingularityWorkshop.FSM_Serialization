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


## `master`-only work found in the current comparison

The current ref comparison is `development` 80 commits ahead and 4 commits behind `master` (merge base `4ebf9545f47d8b997be6656b79994f56c986440d`). These four `master`-only commits were reviewed individually:

- `9234ba8` disabled automatic NuGet publication. The active `development` workflow supersedes that temporary hard-disable with a stricter explicit manual-dispatch gate: `publish: true` **and** `refs/heads/development`; no publication was performed.
- `81d0cfa` added the shared Workshop moniker to the README. The current `development` README already uses the animated Workshop moniker from Renderer.
- `7716f79` prefixed the README heading with the shared-standard section number. The active README uses the repository's tailored five-section structure (`01 What` through `04 See it in a minute`) rather than copying the generic heading verbatim.
- `21a9a50` added a link to the Workshop Documentation Standard. This unique documentation reference is being preserved in the active `development` README.

The first three changes are functionally superseded or intentionally adapted by the current development version; the shared documentation-standard link is retained explicitly. Do not treat the numeric ahead/behind count as proof that the branch diffs are identical; re-check after the final documentation commit.

## Package artifact follow-up

A fresh inspection of workflow run `40163607694` confirmed that `dotnet pack` generated both the version 1.0.1 `.nupkg` and `.snupkg`, but the workflow artifact glob (`./artifacts/*.nupkg`) uploaded only the primary package. The active `.github/workflows/build.yml` now explicitly uploads both extensions. The next run must confirm both files are in the artifact; the guarded publish job can then download both into the same folder, allowing NuGet's documented push behavior to publish the main package followed by its matching symbol package. This does not authorize or trigger publication.

## Documentation work now on development

- Root `Gallery.md` (also included in the NuGet package).
- `docs/BYTE_OPERATIONS.md` and two animated SVG flows (packing and unpacking); documentation Markdown is now included in the NuGet package alongside its images.
- UML cards under `docs/UML/`.
- Companion Markdown class cards beside all six source files.
- Tutorial-first README, theory cleanup, benchmark guide, fixed CI badge, and pull-request CI trigger.

## PR and branch disposition

PRs [#2](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/2), [#3](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/3), [#4](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/4), and [#5](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/pull/5) are now **closed as superseded**, with a reconciliation comment on each. Their unique work has been copied or replaced by the active implementation on `development`. The four surplus branch refs still exist intentionally; they are not deleted by this workflow. The repository owner will delete them only after explicitly being told reconciliation is complete.

## Final verification and handoff

- Latest CI before the current documentation reconciliation: [run 39988802838](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/actions/runs/39988802838) passed on commit `f794b1d7f33f061d86af4b3e8a1ed2b92912c27d`; **17/17 tests passed**, coverage preparation/upload passed, and package/artifact upload passed. The publish job was skipped.
- The exact `.nupkg` artifact from that run was downloaded and inspected: version 1.0.1; net8.0 DLL/XML docs; README/Gallery/license; serialization-domain inventory; candidate release notes; focused UML docs; all six source companion cards; SVG/JPG assets; internal Sith ledger excluded.
- The stream audit covers short reads, end-of-stream behavior in the example format, position delegation, unsupported underlying capabilities, wrapped-stream disposal, and round-trip/truncated-input behavior.
- All workflow action references are pinned to immutable commit SHAs. The publication job remains gated on manual dispatch, explicit `publish: true`, and the `development` ref. No publication was performed.
- Branch comparison confirms the four surplus branches have no unharvested unique file changes: the benchmark guide and theory are exact matches, README/CI changes are reconciled, and the separate manual publisher's verification/publish gate is represented by the active build workflow. PRs #2–#5 are closed, no PRs remain open, and all four surplus branch refs remain present.

**Branch target:** `master` + `development` only.  
**Manual branch handoff:** the four surplus branches are ready for the repository owner to delete manually. No branch has been deleted by this work.  
**Package publication:** not performed or authorized by this work.  
**Readiness estimate:** 85% — the candidate's pre-reconciliation CI and package artifact are verified, but final docs changes require a fresh run. The remaining gates are post-edit CI/package inspection, a final diff/branch-ledger check, and repository-owner review. NuGet publication still requires explicit approval.

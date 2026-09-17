# AGENTS.md — Tap Away

Brief for Cursor and Codex working on this repository.

## Product

- Working title: **Tap Away** / **Block Escape**
- Stack: **Unity + C#**
- Store: **Android / RuStore**
- Phase 0: foundation only — **no gameplay**

## Source of truth

- Remote: https://github.com/alex1c/tapAwayGameRuStore
- Branch: `main`
- Cursor workdir: `D:\PetProject\tapAwayGameRuStore`
- Codex / Android QA workdir: `D:\petProject\tapAwayGameRuStore`

Sync with `git fetch` + `git pull --ff-only origin main` before work on either PC.

## Roles

| Agent | Responsibility |
|-------|----------------|
| **Cursor** | Implement major phases, keep GitHub updated |
| **Codex** | Independent checkpoint audits, Edit/PlayMode review, Android device/runtime QA, reports |

## Unity lock

Exact editor: **6000.3.22f1** (Unity 6.3 LTS) on both machines.

## Hard constraints

- No force push
- No secrets / keystores / passwords in repo
- No production keystore generation by agents
- No release AAB / RuStore upload unless explicitly requested
- Ads never during active gameplay (later)
- Keep puzzle Core free of presentation / ads / analytics coupling
- Levels must be completable without ads / IAP / boosters
- Real-device Android QA at important checkpoints (safe area!)

## Assemblies

- `TapAway.Core` — pure C# (`noEngineReferences`)
- `TapAway.Runtime` — Unity presentation
- `TapAway.Core.Tests` — EditMode
- `TapAway.PlayMode.Tests` — PlayMode

## Stop conditions

After completing an assigned phase, **stop**. Do not start the next phase
(mechanics, solver, ads, polish, etc.) until asked.

# Tap Away / Block Escape

Unity + C# puzzle game for Android / RuStore (ForestMusic).

## Source of truth

**GitHub only:** https://github.com/alex1c/tapAwayGameRuStore (`main`)

Do not treat either PC workdir as source of truth.

| Role | Path |
|------|------|
| Cursor (remote) | `D:\PetProject\tapAwayGameRuStore` |
| Codex / Android QA (local) | `D:\petProject\tapAwayGameRuStore` |

## Stack

- **Engine:** Unity **6000.3.22f1** (Unity 6.3 LTS) РІР‚вЂќ required on both PCs
- **Language:** C#
- **Template:** Minimal 3D (Built-in Render Pipeline)
- **Target:** Android / RuStore, **Portrait**
- **Package:** `com.calculatorplatform.tapaway` (locked)

## Workflow

```text
Cursor (implement phase) РІвЂ вЂ™ push GitHub РІвЂ вЂ™ Codex pull / checkpoint audit
  РІвЂ вЂ™ Android real-device QA when required РІвЂ вЂ™ report РІвЂ вЂ™ next phase
```

Before switching PCs:

```bash
git status
git fetch origin
git pull --ff-only origin main
```

## Hard rules

- No force push / force-with-lease
- No secrets, passwords, or production keystores in git
- Production keystore: user-created only; store under signing vault (not repo)
- No release AAB / RuStore upload on foundation phases without explicit go-ahead
- Ads must never interrupt active gameplay
- Core puzzle rules stay independent of Renderer / Camera / UI / Ads / Analytics / MonoBehaviour where practical
- Every puzzle level must be completable without ads, rewarded ads, IAP, or boosters
- Solver/validator (later) must prove level solvability
- Important checkpoints require real Android device QA (AVD alone is not enough for bottom safe area)

## Project layout

```text
Assets/_Project/
  Core/          # pure C# puzzle model (noEngineReferences)
  Runtime/       # Unity presentation / bootstrap
  Gameplay/ Levels/ Input/ Camera/ UI/ Audio/ VFX/
  Persistence/ Analytics/ Ads/ Config/
  Tests/EditMode/ Tests/PlayMode/
  Scenes/Bootstrap.unity
```

## Docs

- `PROJECT_PLAYBOOK.md` РІР‚вЂќ ForestMusic-adapted Unity rules
- `AGENTS.md` РІР‚вЂќ Cursor / Codex agent brief

## Phase status

**PHASE 4** — generated gameplay QA sequence / Direction UX V2.  
See [Docs/PHASE4_GENERATED_GAMEPLAY.md](Docs/PHASE4_GENERATED_GAMEPLAY.md).

## Phase 3

See [Docs/PHASE3_CONTENT_PIPELINE.md](Docs/PHASE3_CONTENT_PIPELINE.md).

## Phase 4

See [Docs/PHASE4_GENERATED_GAMEPLAY.md](Docs/PHASE4_GENERATED_GAMEPLAY.md).

Runtime hotfix: `FoundationMarker` disabled during gameplay; `GameplayPhase` drives input after level transitions (Tutorial→QA / Next Level).

## Phase 5

See [Docs/PHASE5_CAMPAIGN.md](Docs/PHASE5_CAMPAIGN.md).

Home → 30-level campaign with local JSON progress; block counts capped for mobile readability (max 20).


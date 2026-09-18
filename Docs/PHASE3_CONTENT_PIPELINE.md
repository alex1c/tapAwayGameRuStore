# Phase 3 — Solver / Generator / Level Quality / Difficulty

## Pipeline

```
seed + GeneratorConfig
  → LevelGenerator (solution-by-construction)
  → PuzzleSolver (independent validation + replay)
  → LevelQualityAnalyzer (initial + intermediate topology)
  → DifficultyAnalyzer (V1 score + band)
  → Accepted GeneratedLevel | LevelRejectionReason
```

Hard `MaxPipelineAttempts` / `MaxConstructionAttempts` — never unbounded loops.

## Solver

- Pure C# in `TapAway.Core` (`noEngineReferences`).
- State = chunked `ActiveMask` (supports **>64** blocks).
- **Monotonic invariant:** legal removals only clear occupancy; they cannot add ray blockers. Any legal move from a solvable state preserves solvability. Lowest-`BlockId` greedy search is therefore a complete decision procedure.
- Deterministic: blocks ordered by ascending `BlockId`; legal moves scanned in dense index order.
- Stored solutions are **not** gameplay authority — `PuzzleRules` / `PuzzleState` remain authoritative.

## Generator

- Reverse / prepend construction: each new block is face-adjacent, currently free, and becomes the new first removal in the construction hint.
- Seeded via `DeterministicRng` (xorshift64*). Same generator version + seed → same logical level.
- Construction history is a hint only; `PuzzleSolver` must independently accept the candidate.

## Level quality (not PuzzleRules)

Reject mathematically valid but visually poor content:

- disconnected initial face-graph (when required)
- premature singletons mid-solution
- diagonal-only attachment islands
- severe fragmentation before endgame allowance

Endgame (few blocks left) is allowed to fragment. Thresholds live in `LevelQualityConfig`.

Alternate-path strategy: sample a few deterministic alternate solutions (2nd/3rd legal move at first choice point, then greedy) and apply the same intermediate checks.

## Difficulty V1

Features include block count, forced/choice ratios, branching, spans, density, direction diversity, occlusion proxy.

Produces numeric score + band: Tutorial / Easy / Medium / Hard / Expert.

**Not player-calibrated.** Explanation string lists contributing features.

## Preview

- Play Mode → menu `TapAway/Phase3/Preview Generated Seed` (default seed 4242)
- Or call `LevelBootstrap.TryLoadGeneratedSeed(seed)`
- `TapAway/Phase3/Load Prototype Level` restores the Phase 1 fixture

## Stress

Menu / batch: `TapAway.Editor.Phase3Tools.RunStressSuiteBatch`  
Writes `Logs/phase3-stress-report.txt`.

Example accepted seeds (generator v1.0.0 stress suite):

| Band | Example seed |
|------|--------------|
| Easy | 41000 |
| Medium | 1000 |
| Expert | 11000 |

Tutorial / Hard may appear with other seeds; do not invent them if a run did not produce them.

## Known presentation follow-ups (not Phase 3)

1. **Direction readability:** indicator must be *semantically* obvious as an arrow from useful views — “technically visible” is not enough. Calibrate later against generated shapes.
2. **Visual coherence** of intermediates is enforced in LevelQuality; keep refining thresholds with device QA.
3. **Difficulty V1** is objective feature scoring only — not player-calibrated.

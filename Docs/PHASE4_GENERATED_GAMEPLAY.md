# Phase 4 — Generated Gameplay / Direction UX V2 / Device QA Set

## Flow

```
Tutorial (Phase1PrototypeLevel)
  → Victory → «Следующий уровень»
QA-1 … QA-7 (deterministic generated seeds)
  → final «Серия пройдена»
```

Restart always rebuilds the **same** seed/config/generator version.

## QA level set (generator 1.1.0)

| Index | Label | Seed | Blocks |
|------:|-------|-----:|-------:|
| 1 | QA-1 | 41000 | 8 |
| 2 | QA-2 | 42000 | 10 |
| 3 | QA-3 | 1000 | 12 |
| 4 | QA-4 | 43000 | 15 |
| 5 | QA-5 | 44000 | 18 |
| 6 | QA-6 | 45000 | 22 |
| 7 | QA-7 | 11000 | 28 |

Not a production campaign — device QA sequence only.

## Direction UX V2

**Rule:** Direction must be *semantically* understandable before tap. Visibility alone is insufficient.

Implementation:

- Clear asymmetric 3D arrow (shaft + triangular head wings + dark tail) along world `EscapeDirection`
- Optional camera-aware face glyph: at most one projection of the **same** escape vector onto the best camera-facing face
- Unlit high-contrast materials retained
- Adaptive arrow scale by level size

Core escape vectors never change; presentation never reverses head/tail meaning.

## Camera

- Framing from actual generated bounds
- Zoom min/max scale with puzzle radius
- Pivot set at level start / restart / next; **not** recentered every removal

## Metrics (local / Development only)

Per level: elapsed time, successful removals, blocked taps, block taps, orbit gestures, pinch gestures, seed, block count, Difficulty V1.

Logged at victory. No network analytics.

## Monotonic rules reminder

Under current base rules, legal removals only clear occupancy. Difficulty is primarily spatial inspection / direction readability, not destructive move planning.

## Presentation invariants (Phase 4 hotfix)

- Every gameplay `BlockView` ↔ exactly one active Core `PuzzleBlock`
- At settled states: `Core.ActiveCount == active BlockView count`
- Victory remains Core-authoritative (`ActiveCount == 0`); presentation must settle to match
- `FoundationMarker` is a Phase 0 diagnostic cube — **disabled during gameplay** so it cannot appear as an orphan grey block
- `GameplayPhase`: LoadingLevel → Playing → RemovingBlock → Victory → Transitioning
- After Restart / Next / Tutorial→QA: phase becomes **Playing**, input enabled, gestures cleared, Victory raycasts off

## Editor

- `TapAway/Phase4/Preview QA Level`
- `TapAway/Phase4/Next QA Level`
- `TapAway/Phase4/Direction Readability Views`
- `TapAway/Phase4/Discover QA Seeds`

# Phase 5 — Campaign / Progression / Persistence / Large-level Mobile UX

## Flow

```
Startup → load progress → Home
  Играть / Продолжить
    → Tutorial (if not completed)
    → Campaign level
  Уровни → select unlocked/completed
  Обучение → replay tutorial (does not reset campaign)

Gameplay → Victory → unlock N+1 → save → Next / Replay / Home
```

**Continue:** restarts the last played unlocked level from a fresh puzzle state.
Mid-level block occupancy is **not** resumed in Phase 5.

## Campaign (30 levels, generator 1.1.0)

Curated accepted seeds. Max **20** blocks (OPPO QA: ~28 was too small).

| L | Seed | N | Notes |
|--:|-----:|--:|-------|
| 1–3 | 50000–50002 | 6 | onboarding |
| 4–5 | 51000–51001 | 7 | |
| 6–7 | 41000, 52000 | 8 | 41000 = Phase4 QA-1 |
| 8–10 | 53000–53002 | 9 | |
| 11–12 | 42000, 54000 | 10 | 42000 = Phase4 QA-2 |
| 13–15 | 1000, 55000–55001 | 12 | 1000 = Phase4 QA-3 |
| 16–18 | 56000–56002 | 14 | |
| 19–21 | 43000, 57000–57001 | 15 | 43000 = Phase4 QA-4 |
| 22–24 | 58000–58002 | 16 | |
| 25–27 | 44000, 59000–59001 | 18 | 44000 = Phase4 QA-5 |
| 28–30 | 60000–60002 | 20 | hardest Phase 5 |

Difficulty V1 scores rise Easy→Medium→Hard but are **not** the selection authority.
Selection used block count, spans, density, legal moves, and mobile radius proxy.

## Unlock

- Level 1 unlocked initially
- Completing N unlocks N+1
- No stars / currency / ads gates

## Persistence

- Schema v1 JSON in `persistentDataPath/TapAway/`
- Files: `campaign_progress.json`, `.bak.json`, `.tmp.json`
- Atomic write: serialize → temp → validate → promote backup → replace primary
- Corrupt primary → backup; both bad → fresh default
- Migration dispatcher ready for v2+ (`ProgressMigration`)

Stored: `tutorialCompleted`, `highestUnlockedLevel`, `lastPlayedLevel`, per-level best time / blocked taps / orbits.

## Large-level readability

Hard rule: difficulty must not come from microscopic blocks.

- `MobileReadabilityMath` sets overview vs inspection zoom
- Initial framing = overview; pinch can zoom to ~7% screen-height block size
- Pan not added — orbit + zoom sufficient for ≤20-block campaign shapes
- `CampaignContentQuality` rejects oversized radius / sparse giants

## Pause / Back

- Pause: Continue / Restart / Home (timer excludes pause/background)
- Android Back: Levels→Home, Gameplay→Pause, Home→Quit

## No monetization / analytics

No ads. No AppMetrica. Local metrics only.

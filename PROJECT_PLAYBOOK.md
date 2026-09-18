# Tap Away вЂ” Project Playbook

Adapted from ForestMusic Dev Playbook (`FORESTMUSIC_DEV_PLAYBOOK.md`, 2026-09-10)
for Unity + C# / Android / RuStore. RN/Expo/Skia-specific rules are not carried over.

Version: 2026-09-17 (Phase 0)

## 1. Principles

- Prefer simple, stable solutions; do not change a working process without cause.
- Do not repeat failed approaches without new evidence.
- Cursor owns large implementation phases; Codex owns independent checkpoint audits
  and Android runtime/release QA.
- GitHub is the only source of truth.
- Serious defect flow: root cause в†’ fix в†’ regression test в†’ playbook note.
- Onboarding / training remains required in shipped apps (later phases).

## 2. Git and directories

| Machine | Path |
|---------|------|
| Cursor (remote) | `D:\PetProject\tapAwayGameRuStore` |
| Codex / Android QA | `D:\petProject\tapAwayGameRuStore` |

- Before switching PC: `git status`, `git fetch origin`,
  `git pull --ff-only origin main`.
- Never force push (`--force` / `--force-with-lease`).
- Do not relocate the project to short/temp paths for diagnostics.
- Commit `.meta` files with assets; never commit `Library/`, `Temp/`, `Logs/`,
  `UserSettings/`, `Build(s)/`, generated `*.csproj` / `*.sln`.

## 3. Unity version lock

- **Required editor:** Unity **6000.3.22f1** (Unity 6.3 LTS).
- Same exact version on both PCs.
- Prefer Unity HubвЂ“managed Android Build Support, SDK, NDK, OpenJDK.
- Do not use beta / alpha / tech-stream for novelty.

## 4. Architecture

- `TapAway.Core`: pure C# puzzle domain (`noEngineReferences`).
- `TapAway.Runtime`: Unity presentation, scenes, input wiring.
- Puzzle rules must not depend on Renderer, Camera, UI, Ads, Analytics, or
  MonoBehaviour lifecycle beyond thin adapters.
- Every level must be beatable without ads, rewarded ads, IAP, or boosters.
- Solver / generator / level-quality / difficulty V1 live in `TapAway.Core`
  (see `Docs/PHASE3_CONTENT_PIPELINE.md`).
- Phase 4 generated QA gameplay + Direction UX V2:
  `Docs/PHASE4_GENERATED_GAMEPLAY.md`.

## 5. Android / RuStore QA

- Target platform: Android / RuStore, orientation **Portrait**.
- Application id: `com.calculatorplatform.tapaway` (locked).
- AVD is useful for smoke; it is **not** sufficient for bottom safe-area QA.
- Important CTAs / bottom UI must sit above system gesture / navigation insets.
- Before release, verify key bottom CTAs on at least one physical Android phone.
- Android Back behavior must be considered at UX checkpoints.
- Do not spend time fighting heavy emulators when a light AVD or device suffices.

## 6. Ads (later phases)

- Monetize clearly but not annoyingly.
- **Ads must never interrupt active gameplay.**
- Prefer natural break points (level clear, return to hub) for interstitial.
- Rough budget: about one interstitial per session unless product decides otherwise.
- No-fill / ad errors must never block core play.
- Rewarded only with a natural opt-in scenario; never required to clear a level.

## 7. Analytics (later phases)

- Log feature usage facts, not personal puzzle content beyond what product allows.
- Analytics failure must never block gameplay or UI.

## 8. Screenshots (later)

- RuStore shots: strictly **1080Г—1920**, **9:16**.
- Crop/resize without distortion; verify dimensions programmatically.

## 9. Release order (later)

Functionality в†’ UX в†’ naming в†’ icon в†’ ads/analytics в†’ native checkpoint в†’
privacy в†’ screenshots в†’ release artifacts в†’ production signing в†’ final AAB в†’
RuStore upload.

## 10. Production signing

- Production keystore is created **only by the user**, locally.
- Never generate production keystores or invent passwords in agents.
- Never store signing secrets in the repository.
- Recommended vault layout (outside repo):
  `D:\PetProject\secure\android-signing\<repo>\`
  (or `D:\secure\android-signing\<repo>\` when that path exists).
- After release builds: verify alias, cert SHA1/SHA256, and AAB SHA256.
- Fingerprint mismatch в†’ **STOP**.

## 11. Permissions / store honesty (later)

- Do not justify sensitive permissions until the merged release manifest is checked.
- Remove unused permissions; declare only real data uses in RuStore forms.
- Privacy policy must match the real app; URL must return HTTP 200 before review.
- Developer site reference: `https://forest-music.ru`.

## 12. Backup / persistence (later, if applicable)

- Prefer versioned manifests and portable representations.
- Missing files в†’ fallback, never crash.
- Protect archives against path traversal.

## 13. Failure / retry policy

- Do not вЂњfixвЂќ issues by inventing a new editor install path, new signing key,
  new package id, or new architecture until root cause is proven.
- Root cause first; minimal change second.
- After a serious bug: add a regression test when practical.

## 14. Phase 0 exclusions

Not in Phase 0: gameplay, ads SDKs, AppMetrica, RuStore SDK, billing, push,
production signing, release AAB upload.

# Session Memory: Map Prefab Test Scene

Date: 2026-07-25 17:28  
Status: `COMPLETED_WITH_UNITY_VERIFICATION_BLOCKED`

## User request

Đổi scene hiện tại thành scene test map/prefab, gắn thẳng `MapRuntime.prefab` vào scene, và cập nhật plan tương ứng.

## Context loaded

- `memory_bank/README.md` and `_session_template.md`.
- Previous prefab refactor handoff `memory_bank/2026-07-25-1709-refactor-map-prefab.md`.
- Current map MVP plan files and Unity scene/prefab assets.

## Plan

1. Rename `Gameplay.unity` to a map prefab test scene while preserving Unity metadata GUID.
2. Replace the host-based scene setup with a direct `MapRuntime.prefab` instance.
3. Remove unused host script and update plan/build references.
4. Run static validation and record Unity verification status.

## TODO

- [x] Load relevant context.
- [x] Rename scene and update build settings.
- [x] Directly attach map prefab to test scene.
- [x] Remove unused host path and update plans.
- [x] Validate static references and handoff.

## What was done

- Renamed `Assets/_Game/Scenes/Gameplay.unity` to `Assets/_Game/Scenes/MapPrefabTest.unity` and moved its `.meta`, preserving scene GUID `fdc91680e805497dae5cbc20433d9076`.
- Updated `EditorBuildSettings.asset` to point to `Assets/_Game/Scenes/MapPrefabTest.unity`.
- Replaced `MapRuntimeHost` scene setup with a direct `MapRuntime.prefab` prefab instance in `MapPrefabTest.unity`.
- Deleted unused `MapRuntimeHost.cs` and `.meta`.
- Updated all Map MVP plan references from the host/`Gameplay.unity` pattern to direct `MapRuntime.prefab` instance usage and `MapPrefabTest.unity` as the test scene.

## Files touched

- `memory_bank/2026-07-25-1728-map-prefab-test-scene.md` — session handoff.
- `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity` — renamed map prefab test scene with direct prefab instance.
- `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity.meta` — moved scene metadata preserving GUID.
- `countdown-game/Assets/_Game/Scenes/Gameplay.unity` — removed via rename.
- `countdown-game/Assets/_Game/Scenes/Gameplay.unity.meta` — removed via rename.
- `countdown-game/ProjectSettings/EditorBuildSettings.asset` — updated scene path.
- `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapRuntimeHost.cs` — deleted unused host script.
- `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapRuntimeHost.cs.meta` — deleted unused host script metadata.
- `plans/260725-1327-map-mvp/plan.md` — direct prefab instance architecture.
- `plans/260725-1327-map-mvp/phase-01-scene-tilemap-foundation.md` — test scene rename and direct prefab instance requirement.
- `plans/260725-1327-map-mvp/phase-02-grid-logic-and-occupancy.md` — removed host script from file list.
- `plans/260725-1327-map-mvp/phase-03-map-authoring-and-validation.md` — future edits target prefab/test scene.
- `plans/260725-1327-map-mvp/phase-04-gameplay-integration.md` — future edits target prefab/test scene.
- `plans/260725-1327-map-mvp/phase-05-testing-and-polish.md` — future verification targets prefab/test scene.

## Key decisions

- Use direct prefab instance in the test scene and future shared scene.
- Remove `MapRuntimeHost` to avoid two competing scene integration patterns.
- Keep `MapPrefabTest.unity` as a dedicated map/prefab test scene; it is not the main shared gameplay scene.
- In a future shared scene, add `MapRuntime.prefab` directly as a prefab instance, same as the test scene.

## Verification

- Documentation checks: `PASS` — plan files have one H1 each, balanced code fences, and no stale `Gameplay.unity`/`MapRuntimeHost` references in active plan files.
- Unity compilation: `NOT RUN`
- Unity tests: `NOT RUN`
- Play Mode: `NOT RUN`
- Other: `PASS/PARTIAL` — scene file rename/path checks passed; scene `.meta` GUID preserved; Build Settings path updated; direct `PrefabInstance` references `MapRuntime.prefab`; static GUID checks passed; map script brace balance passed.

## Blockers and next steps

- Unity verification may still be blocked by the earlier `Unity.Licensing.Client.exe` issue.
- Need Unity open/import verification for prefab instance validity, missing scripts, Console compile status, and Play Mode smoke harness.

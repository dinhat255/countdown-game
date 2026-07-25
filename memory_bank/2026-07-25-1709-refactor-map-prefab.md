# Session Memory: Refactor Map Runtime To Prefab

Date: 2026-07-25 17:09  
Status: `COMPLETED_WITH_UNITY_VERIFICATION_BLOCKED`

## User request

Sửa plan và implementation để map dễ merge với team khi dự án có scene chung khác.

## Context loaded

- `memory_bank/README.md` and `_session_template.md`.
- Previous Phase 2 handoff `memory_bank/2026-07-25-1643-implement-map-phase-2.md`.
- Map MVP plan files under `plans/260725-1327-map-mvp/`.
- Current `Gameplay.unity`, Phase 1/2 map scripts, and existing `_Game/Prefabs` folder.

## Plan

1. Update plan architecture so map runtime lives in a prefab and shared scenes keep only a lightweight host/reference.
2. Add `MapRuntimeHost`, `MapRuntime.prefab`, and adjust scene/config behavior for prefab use.
3. Run static validation and document Unity verification status.

## TODO

- [x] Load relevant context.
- [x] Update plan files.
- [x] Add prefab/host implementation.
- [x] Validate static references and handoff.

## What was done

- Updated map MVP plan to state that map hierarchy lives in `MapRuntime.prefab` and shared scenes should only keep a lightweight host/reference.
- Added `MapRuntimeHost` for runtime prefab instantiation.
- Added `Assets/_Game/Prefabs/Map/MapRuntime.prefab` containing the Phase 1/2 map hierarchy, Tilemaps, `MapSceneConfigurator`, `MapSceneTestPainter`, and `MapController`.
- Replaced `Gameplay.unity` map hierarchy with a lightweight `Gameplay/MapRuntimeHost` that references `MapRuntime.prefab`.
- Updated `MapSceneConfigurator` so prefab usage does not require a serialized scene camera reference; it uses `Camera.main` only during Play Mode fallback.

## Files touched

- `memory_bank/2026-07-25-1709-refactor-map-prefab.md` — session handoff.
- `plans/260725-1327-map-mvp/plan.md` — prefab/host architecture and merge-friendly DoD.
- `plans/260725-1327-map-mvp/phase-01-scene-tilemap-foundation.md` — prefab-based hierarchy and scene-host requirement.
- `plans/260725-1327-map-mvp/phase-02-grid-logic-and-occupancy.md` — `MapController` lives in prefab; added `MapRuntimeHost` file.
- `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapRuntimeHost.cs` — lightweight scene/shared-scene host for instantiating map prefab.
- `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapRuntimeHost.cs.meta` — Unity metadata for host script.
- `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapSceneConfigurator.cs` — prefab-safe camera fallback and removed component context menu.
- `countdown-game/Assets/_Game/Prefabs/Map.meta` — Unity metadata for map prefab folder.
- `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab` — merge-friendly map runtime prefab.
- `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab.meta` — Unity metadata for map runtime prefab.
- `countdown-game/Assets/_Game/Scenes/Gameplay.unity` — reduced to Main Camera, Global Light 2D, `Gameplay/MapRuntimeHost`.

## Key decisions

- Prefer prefab-based map runtime for mergeability.
- Shared scenes should reference a lightweight host or prefab, not own the full map hierarchy directly.
- Keep implementation direct-file based; no Unity editor menu workflow.
- `Gameplay.unity` is now a demo/bootstrap scene, not the authoritative owner of map hierarchy.
- `MapRuntime.prefab` is the unit map designers/gameplay implementers should edit for map hierarchy.
- `MapSceneConfigurator` skips scene camera fallback outside Play Mode so prefab import/editing does not serialize scene-camera references.

## Verification

- Documentation checks: `PASS` — plan files have one H1 each and balanced code fences.
- Unity compilation: `NOT RUN`
- Unity tests: `NOT RUN`
- Play Mode: `NOT RUN`
- Other: `PASS/PARTIAL` — static GUID checks passed for scene/prefab/config references; map scripts brace balance passed; `Gameplay.unity` no longer contains direct Tilemap/controller object names; `MapRuntime.prefab` contains expected Map/Grid/Tilemap/Systems hierarchy.

## Blockers and next steps

- Unity verification may still be blocked by the earlier `Unity.Licensing.Client.exe` issue.
- Need Unity open/import verification for prefab validity, missing scripts, Console compile status, and runtime host instantiation.

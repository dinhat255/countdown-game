# Repository Instructions

This repository contains the active gameplay specification and the Unity project for **Countdown**.

## Project overview

- Unity project root: `countdown-game/`
- Unity version: `6000.5.5f1`
- Template: 2D Universal Render Pipeline
- Input: Unity Input System
- UI: Unity UI (`uGUI`)
- Game-owned assets: `countdown-game/Assets/_Game/`
- Active gameplay specification: `docs/gameplay/`

Do not apply assumptions from another Unity project. Verify project facts from this repository before editing.

## Source-of-truth order

When sources disagree, use this order:

1. The user's latest explicit instruction.
2. Active documents in `docs/gameplay/`.
3. Implemented behavior under `countdown-game/Assets/_Game/`, once implementation exists.
4. Unity configuration in `countdown-game/Packages/` and `countdown-game/ProjectSettings/`.
5. Historical root source files, if present.

Do not silently resolve a meaningful gameplay conflict. Report it and update every affected active document after the rule is confirmed.

## Repository map

- `docs/gameplay/` — synchronized active gameplay specification
- `countdown-game/Assets/_Game/Art/` — animations, materials, sprites, tilemaps, and VFX
- `countdown-game/Assets/_Game/Audio/` — music and sound effects
- `countdown-game/Assets/_Game/Data/` — game data and ScriptableObject assets
- `countdown-game/Assets/_Game/Prefabs/` — game-owned prefabs
- `countdown-game/Assets/_Game/Scenes/` — game-owned scenes
- `countdown-game/Assets/_Game/Scripts/` — game-owned C# code
- `countdown-game/Assets/_Game/UI/` — UI assets such as fonts
- `countdown-game/Assets/Settings/` — URP and template settings
- `countdown-game/Packages/` — Unity package manifest and lock file
- `countdown-game/ProjectSettings/` — committed Unity project settings
- `memory_bank/` — concise session handoffs for future agents

## Unity project rules

### Before editing

1. Read the latest relevant session file in `memory_bank/`, if one exists.
2. Inspect the target files and their direct dependencies.
3. Confirm the Unity version from `countdown-game/ProjectSettings/ProjectVersion.txt`.
4. Keep game-owned work under `countdown-game/Assets/_Game/` unless Unity requires another location.

### Assets and metadata

- Preserve every Unity `.meta` file and its GUID.
- Move or rename Unity assets together with their `.meta` files.
- Never invent a replacement GUID for an existing asset.
- Do not edit binary assets as text.
- Prefer Unity Editor for scene, prefab, animation, and serialized-asset changes when practical.
- Do not put general assets in `Resources` unless runtime loading specifically requires it.

### Generated folders

Never commit or treat these as source of truth:

- `countdown-game/Library/`
- `countdown-game/Temp/`
- `countdown-game/Logs/`
- `countdown-game/Obj/`
- `countdown-game/UserSettings/`
- IDE-generated project files such as `.csproj`, `.sln`, and `.vs/`

### Packages

- Edit direct dependencies in `countdown-game/Packages/manifest.json`.
- Let Unity reconcile `packages-lock.json`, then commit both files together.
- Do not add a package when the same result is small and safe to implement locally.
- Keep the current URP, Input System, Tilemap, Tilemap Extras, Sprite, and uGUI dependencies unless the task explicitly changes the technical direction.

### C# conventions

- Use PascalCase for C# files and types.
- Keep MonoBehaviours focused; put reusable rules in plain C# classes or ScriptableObjects where appropriate.
- Avoid global mutable state unless the architecture explicitly requires it.
- Do not introduce a framework, service locator, or dependency-injection package without a concrete need.
- Match namespaces to the project structure once namespaces are introduced.

## Gameplay documentation

These rules apply whenever a change edits any file under `docs/gameplay/`.

### Before editing

1. Read every directly related gameplay document.
2. Always read `docs/gameplay/README.md` and `docs/gameplay/gameplay-summary.md`.
3. Use `rg` across `docs/gameplay/` for every affected term, alias, timing rule, `TBD`, and removed mechanic before deciding the edit scope.

### Dependency matrix

- Beat or timing changes must review `beat-and-action-system.md`, `gameplay-summary.md`, `win-condition-and-progression.md`, `environmental-hazards.md`, `enemies-and-spawning.md`, and `map-ui-and-game-flow.md`; also review player and skill documents when movement or cooldown is involved.
- Enemy changes must also review player and skill documents whenever hit, damage, status, movement, or cooldown behavior is affected.
- Skill changes must review every gameplay document affected through WC, timing, movement, damage, UI, summary, terminology, or links.

### Keep the specification synchronized

- Treat `docs/gameplay/` as one synchronized specification, not independent notes.
- Update every dependent gameplay document in the same change, including summaries, active rules, gameplay `TBD` or tunable values, UI, terminology, and links where applicable.
- Do not leave a fixed rule described as `TBD`.
- Separate fixed behavior from tunable values such as amount, formula, range, duration, or weight.

### Active specification only

- `docs/gameplay/` contains only the active gameplay specification.
- Do not create prototype scopes, playtest plans or metrics, source history, decision logs, or changelogs in `docs/gameplay/` unless the user explicitly requests them.
- When a rule is replaced, remove stale wording instead of preserving rule history.
- Record genuine gameplay `TBD` items in the relevant active gameplay document.

### Preserve historical sources

If any of these root files are present, do not edit them unless the user explicitly requests it:

- `gameplay.md`
- `SkillItem.md`
- `editgdd.txt`
- `countdown_gameplay_prototype_1.md`

### Validate gameplay documentation

After editing:

1. Run `rg` again for stale terminology, removed mechanics, duplicated `TBD` items, and contradictions.
2. Verify every Markdown file has exactly one H1.
3. Verify code fences are balanced.
4. Verify every relative Markdown link resolves.
5. Keep `docs/gameplay/gameplay-summary.md` between 150 and 220 lines.

## Validation

Use the closest relevant checks and report their actual status:

- Markdown-only changes: validate headings, fences, links, terminology, and required line counts.
- C# changes: check Unity Console compilation and run relevant Edit Mode tests when available.
- Gameplay changes: exercise the affected flow in Play Mode when possible.
- Scene or prefab changes: open the affected asset and check for missing scripts or broken serialized references.
- Package changes: reopen Unity and wait for Package Manager resolution.

Never claim Unity compilation, tests, or Play Mode verification if they were not run.

## Workflow memory

Use `memory_bank/_session_template.md` as the canonical session-memory template. The root `_session_template.md` only points to that canonical file.

### At the start of a substantial session

1. Read `memory_bank/README.md`.
2. Read `memory_bank/_session_template.md`.
3. Read the newest dated session file relevant to the task, if one exists.
4. Create one session file named `YYYY-MM-DD-HHmm-short-title.md`.

Do not load every historical session unless the task requires it.

### During and after work

- Keep one session file for the conversation; do not create duplicates.
- Update the plan and TODO state when scope changes.
- Before finishing, record what changed, files touched, key decisions, unresolved risks, and actual verification status.
- Keep entries concise and useful to the next agent.
- Do not create `wf_*` memory files.

Create or maintain session memory for meaningful code or asset edits, multi-file documentation work, investigations, and incomplete handoffs. Skip it for short read-only answers or tiny no-change tasks.

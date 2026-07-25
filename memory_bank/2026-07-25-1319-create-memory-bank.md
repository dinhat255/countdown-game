# Session Memory: Create Memory Bank

Date: 2026-07-25 13:19  
Status: `COMPLETED`

## User request

Create a memory-bank directory and update `AGENTS.md` and `_session_template.md`.

## Context loaded

- Existing root agent instructions and session template.
- Current Unity version, package manifest, asset layout, and gameplay-document index.
- Gameplay documentation rules supplied for this repository.

## Plan

1. Remove instructions inherited from an unrelated Unity project.
2. Create a single canonical memory template and repository-specific memory rules.
3. Validate paths, Markdown structure, and repository references.

## TODO

- [x] Rewrite `AGENTS.md` for Countdown.
- [x] Create `memory_bank/` documentation and canonical template.
- [x] Replace the root template with a compatibility pointer.
- [x] Record and validate this session.

## What was done

- Replaced guidance inherited from an unrelated project with Countdown-specific Unity and gameplay rules.
- Added a concise memory-bank workflow with one dated handoff per substantial conversation.
- Added accurate Unity-oriented verification fields instead of Node.js and Go checks.

## Files touched

- `AGENTS.md` — repository, Unity, gameplay-document, validation, and memory rules.
- `_session_template.md` — pointer to the canonical template.
- `memory_bank/README.md` — memory-bank purpose and usage.
- `memory_bank/_session_template.md` — canonical Unity session template.
- `memory_bank/2026-07-25-1319-create-memory-bank.md` — current handoff.

## Key decisions

- `memory_bank/_session_template.md` is the sole canonical template to avoid maintaining duplicate content.
- Memory files provide handoff context only; `docs/gameplay/` remains the active gameplay authority.
- Agent rules use the actual Unity project root and version detected in this repository.

## Verification

- Documentation checks: `PASS`
- Unity compilation: `NOT RUN`
- Unity tests: `NOT RUN`
- Play Mode: `NOT RUN`
- Other: file paths and Markdown structure checked locally.

## Blockers and next steps

- Open Unity once after the earlier package cleanup so Package Manager can reconcile `packages-lock.json`.

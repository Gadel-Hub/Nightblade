# Nightblade AI Agent Rules

This repository is a small Unity 6 2D action-platformer.

These rules apply to any AI coding agent working in this repo.

## Unity

Use exactly:

`6000.3.24f1`

Do not upgrade or downgrade Unity.

Do not intentionally reserialize the project with another Unity version.

## Branches

Never work directly on `main`.

Before editing:

```bash
git status
git branch --show-current
````

Work only on the assigned feature branch.

Do not merge, rebase, cherry-pick, rewrite history, or push unless explicitly instructed.

## Scope

Implement only the requested task.

Do not opportunistically:

* refactor unrelated code
* rename unrelated files
* reorganize folders
* change accepted gameplay behavior
* introduce frameworks
* modify project-wide settings without a concrete need

If you notice unrelated problems, report them instead of fixing them.

## Git

Use realistic atomic commits.

A commit should represent one coherent, independently understandable change.

Do not:

* create giant milestone commits
* create fake micro-commits
* commit one file at a time just because files are separate
* use `git add .` blindly
* mention AI, Codex, prompts, or autonomous generation in commit messages

Before every commit:

```bash
git diff
git diff --staged
```

Stage only the intended change.

## Unity-generated files

Unity touches many files automatically.

Do not commit unrelated churn just because Unity modified it.

Be especially careful with:

```text
ProjectSettings/
Packages/
*.asset
*.meta
```

If the task does not intentionally change project settings, revert unrelated `ProjectSettings` and `Packages` changes.

Do not casually delete or regenerate `.meta` files.

## Code style

Keep the code simple and readable.

Prefer:

* focused MonoBehaviours
* normal scenes and prefabs
* serialized Inspector fields where useful
* small explicit state machines
* direct Unity APIs

Avoid speculative architecture such as:

* custom ECS
* dependency injection frameworks
* service locators
* global event buses
* generalized ability systems
* unnecessary manager layers
* reflection-heavy systems

## Comments

Do not add comments that restate obvious code.

Bad:

```csharp
// Set current health
currentHealth = maxHealth;
```

Bad:

```csharp
// Check if grounded
if (isGrounded)
```

Comments should only explain non-obvious constraints, workarounds, invariants, or ordering requirements.

Do not write comments in an AI-assistant tone.

## Gameplay boundaries

Do not casually modify maintainer-owned player foundation code:

```text
Assets/Scripts/Player/
Assets/Scripts/Combat/
Assets/Prefabs/Player.prefab
```

Do not change accepted movement or combat tuning as part of unrelated work.

Gameplay colliders and hitboxes must remain independent from sprite artwork.

Current visual convention:

```text
32 pixels = 1 Unity world unit
reference resolution = 320x180
```

## Art

Production art belongs to the art/design team.

Do not generate or invent final sprites, palettes, environments, HUD art, or visual direction.

Programmer placeholders are allowed when required.

## Validation

Validate only what the task needs.

Do not add permanent validation scaffolding solely for the agent.

Do not create:

* one-off Python validators
* temporary test harnesses
* reflection-based validation helpers
* generated audit scripts

unless explicitly requested.

Temporary validation should stay outside the repository when possible.

Do not claim tests or runtime validation that were not actually performed.

## Documentation

Do not create new documentation files unless explicitly requested.

Do not generate handoff documents, reports, design docs, or implementation essays into the repo.

Only update existing documentation when the task explicitly requires it.

## Before finishing

Run:

```bash
git diff --check
git status
git log --oneline -10
```

Confirm there are no unrelated changes or generated Unity junk.

Report only:

* what changed
* commits created
* validation performed
* anything requiring human verification

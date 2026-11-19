# CLAUDE.md: project guide for Claude Code

## What this is
**Midnight Hour**, a Persona-inspired 3D tower defense in **Unity**, built for Assignment 2 of Video Game Design and Development (Fall 2026).
The student is targeting the **CSCI 780 (graduate) requirements at A+ level**. Never break a graded requirement.

### Graded requirements (CSCI 780). Keep all of these working.
- C: place towers; towers fire continuously at a set rate; towers aim at an enemy; enemies follow predetermined **non-straight** paths; the game ends when an enemy completes its path.
- B: ≥2 tower types that differ in **rate of fire, projectile speed AND tower height**; a text overlay explaining how to place towers; a limited number of towers; towers aim at the **predicted** position from the target's current velocity and position.
- A: multiple enemy types with different **speed and HP**; non-basic assets.
- A+: above and beyond (Persona systems, ballistic aim, path-aware aim, physics knockdowns, and so on).
- Must use **3D physics** (Rigidbodies, colliders, forces).

## How the code is organised
- All scripts live in `Assets/Scripts/MidnightHour/`, namespace `MidnightHour`.
- `Core/GameBootstrap.cs` builds the entire scene at runtime. A scene only needs one GameObject with this component.
- All tuning numbers are in `Data/DefaultContent.cs` (defaults) and are editable on the GameBootstrap Inspector.
- `Towers/Aiming.cs` holds the intercept math. Don't "simplify" the Euler correction away.
- UI is IMGUI (`UI/HUD.cs`) on a virtual 1080p-tall canvas. Don't add TextMeshPro or Canvas dependencies without asking.

## Conventions
- **Input:** always go through `InputCompat` (`Core/Compat.cs`). Never call `Input.*` or `Keyboard.current` directly. Add new keys to the `GameKey` enum **and both tables**.
- **Rigidbody velocity:** use `rb.GetVelocity()` / `rb.SetVelocity()`, because Unity 6 renamed `velocity` to `linearVelocity`.
- **Finding objects:** use `FindCompat.First<T>()` or the `#if UNITY_2023_1_OR_NEWER` pattern, since `FindObjectOfType` is obsolete.
- **Visuals:** use `VisualFactory`, not `Shader.Find("Standard")`, so materials work in every render pipeline.
- One MonoBehaviour per file, with the file name matching the class name (Unity requirement).
- Keep `[Tooltip]` / `[Header]` on Inspector fields, since the student tunes values there.

## Working with Unity from Claude Code
- You can't press Play or see the Editor. After changes, tell the student exactly what to test and ask them to paste any **Console errors**.
- Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/` or `.meta` files by hand.
- When creating a new script, mention that Unity needs focus to recompile.

## Docs to keep in sync
- **README.md:** if a feature changes, update the requirement table and the A+ list.
- **DEVLOG.md:** at the end of every session, add a new entry at the top using the template in that file. Cover what was done, problems and fixes, key prompts, and next steps. Use today's date.

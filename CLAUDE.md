# CLAUDE.md: project guide for Claude Code

## What this is
**Midnight Hour**, a Persona 3-inspired 3D tower defense in **Unity**, built for Assignment 2 of Video Game Design and Development (Fall 2026).
The student is targeting the **CSCI 780 (graduate) requirements at A+ level**. Never break a graded requirement.

### Graded requirements (CSCI 780). Keep all of these working.
- C: place towers; towers fire continuously at a set rate; towers aim at an enemy; enemies follow predetermined **non-straight** paths; the game ends when an enemy completes its path.
- B: ≥2 tower types that differ in **rate of fire, projectile speed AND tower height**; a text overlay explaining how to place towers; a limited number of towers; towers aim at the **predicted** position from the target's current velocity and position.
- A: multiple enemy types with different **speed and HP**; non-basic assets.
- A+: above and beyond (Persona systems, ballistic aim, path-aware aim, physics knockdowns, and so on).
- Must use **3D physics** (Rigidbodies, colliders, forces).

## The design: `Docs/STORY.md` is the source of truth
`Docs/STORY.md` is the approved design. Read the relevant section before starting any issue. Summary:

- **Setting:** original *Persona 3* / FES, **September**, the **Tziah I** block of Tartarus (floors 114-138). Every Persona is in its **base form**: no evolutions, and no Thanatos. Tziah's look is **gold**.
- **Story:** S.E.E.S. is sealed in on a floor. Clearing a floor's waves breaks its seal. There are **6 floors**, and floor 6 is the boss, **the Jailer** (an original Shadow that chains towers and summons waves).
- **Party:** Makoto always goes, plus **3 members** chosen from the roster. Members can be swapped at the break between floors, where Fuuka previews the next floor's Shadows. **Fuuka is the navigator, not a tower.**
- **Towers:** placing is **free**. Each member places **one tower** (their own Persona). Makoto (the Wild Card) can place **any of his 11 Personas, each once**.
- **Field limit:** the Jailer's suppression caps the **total** towers on the field per floor at **8 / 7 / 6 / 6 / 5 / 4**. When a new floor's limit is lower, the player dismisses towers before the floor starts. **There's no way to win slots back.**
- **19 towers, each with a unique role, with no overlaps** (STORY.md §5):
  - **Members:** Yukari/Io pushback, Junpei/Hermes artillery, Akihiko/Polydeuces brawler, Mitsuru/Penthesilea controller, Aigis/Palladion sniper, Ken/Nemesis executioner, Koromaru/Cerberus debuffer, Shinjiro/Castor tank buster.
  - **Makoto:** Orpheus generalist, Jack Frost trapper, Sarasvati support aura, Oberon chain lightning, Thoth SP battery, Shiisaa last line of defense, Thor anti-air, Melchizedek pulse, Lakshmi resistance breaker, Pixie multishot, Valkyrie interceptor.
- **Skills:** one **shared SP pool** (proposed: max 100, start 50, +2/s, +5 per kill, +3 more on a weakness kill). Each member has a **signature skill** (click the button, then click a target) costing 20-50 SP with a 10-30 s cooldown. Makoto's only skill is **Persona Swap**.
- **Damage types:** Slash, Strike, Pierce, Fire, Ice, Elec, Wind, Light, Dark, Almighty. A Shadow can have **several** weaknesses and resistances. A weakness hit knocks the Shadow down, and when every Shadow is down, an **All-Out Attack** triggers.
- **Shadows:** the Tziah I roster, by floor (STORY.md §3). Drives and Wheels are **fast**, Tiaras and Idols **fly**. Floor 3 ends with three Arcane Turrets, and floor 5 ends with the Sleeping Table.
- **Between floors:** talk with one member for a small Persona stat boost (bonds).
- **Losing:** a Shadow reaches the team's position.

### Starter code → new design
The code in the repo is still the **starter kit** (Dark Hour / Clock Tower theme). Issues migrate it one piece at a time, so map old to new as you go:

| Starter kit | New design |
|---|---|
| Nights (waves) + Full Moon | Floors 1-6 + the Jailer |
| Free Time + Social Links | Floor break + bonds with a member |
| Clock Tower (goal) | The team's position |
| SP spent to buy towers; limit +1 per Fool rank | Placing is free; the per-floor field limit is fixed |
| Agi / Bufu / Zio | Fire / Ice / Elec (plus the other damage types) |
| 3 generic towers | 8 member towers + 11 Makoto Personas |
| The Reaper | The Jailer |

Graded requirements that each new system touches:
- **Field limit = "limited number of towers."**
- **Floors still end the game when a Shadow completes its path.**
- **Tower roles must keep rate of fire, projectile speed AND height different, and projectile towers must keep predictive aim.**

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
- **Serialized data:** `Main.unity` stores the GameBootstrap lists, and enums are stored as **ints**. Never renumber an existing enum value. When a field changes shape, migrate the old data (`[FormerlySerializedAs]` or `ISerializationCallbackReceiver`) so the scene doesn't silently lose values.

## Working with Unity from Claude Code
- You can't press Play or see the Editor. After changes, tell the student exactly what to test and ask them to paste any **Console errors**.
- Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/` or `.meta` files by hand.
- When creating a new script, mention that Unity needs focus to recompile.
- `Logs/Editor.log` shows the last compile result (search it for `error CS`).

## Workflow
- **One branch per issue**, named `issue-<number>-<short-name>` (for example `issue-1-damage-types`), branched from an up-to-date `main`.
- **Every PR body says `Closes #<number>`.**
- **Every PR adds a `DEVLOG.md` entry** (newest at the top, using the template in that file).
- **Never change anything in `Docs/STORY.md` without the student's approval.** If an issue needs a design change, stop and ask.
- **STORY.md must always match the code.** When the student approves a design decision, such as an element or a number, update STORY.md on `main` in the same session. If code would have to differ from STORY.md, ask first; don't let them drift apart.
- **Anything STORY.md marks `[CHECK]` (or `[DECIDE]`) gets flagged to the student, not guessed.** The same goes for details the doc doesn't specify, such as an element it never names: use a clearly marked placeholder and list it in the PR as an open question.
- The student reviews and merges PRs. Don't merge your own PRs.
- GitHub repo: `hmin6/TowerDefense`. Use the `gh` CLI. Labels: core, tower-role, skills, boss, story, ui, requirement, docs, polish. Milestones run M1 Core systems → M5 Submission.

## Docs to keep in sync
- **README.md:** if a feature changes, update the requirement table and the A+ list.
- **DEVLOG.md:** at the end of every session, add a new entry at the top using the template in that file. Cover what was done, problems and fixes, key prompts, and next steps. Use today's date.

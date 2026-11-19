# Midnight Hour: Devlog

A running log of what was built, what broke, and what's next. Newest entries go at the top.

<!--
Template: copy this block for each session.

## YYYY-MM-DD: Short title
**Time spent:** ~Xh
**Goal:** what I set out to do

### Done
- 

### Problems & fixes
- 

### AI prompts that mattered
- 

### Next
- [ ] 

**Screenshot / GIF:** Docs/devlog/YYYY-MM-DD.png
-->

---

## 2026-11-18: Repo setup, issue plan, and #1 damage types + data model
**Time spent:** _(fill in)_
**Goal:** put the project on GitHub, turn STORY.md into issues, and lay the data foundation for the new design.

### Done
- Initialised git, committed the working starter kit as the baseline on `main`, then committed `Docs/STORY.md` separately. Pushed both to `hmin6/TowerDefense`.
- Rewrote CLAUDE.md around the STORY.md design (Tziah I, field limit, 19 Persona towers, shared SP) and added a Workflow section (one branch per issue, `Closes #N`, a DEVLOG entry per PR, no STORY.md changes without approval, flag [CHECK] items).
- Created 9 labels, 5 milestones (M1-M5) and 18 issues from STORY.md.
- **#1:** expanded the damage types from Agi / Bufu / Zio to Slash, Strike, Pierce, Fire, Ice, Elec, Wind, Light, Dark and Almighty. Shadows can now have several weaknesses and resistances, plus a movement type (Ground / Fast / Flying) and a rank (Normal / FloorBoss / Boss / Treasure).
- **#1:** added the S.E.E.S. roster data: `MemberDef` for the 8 members, and 11 Makoto Personas as `TowerDef`s with user, Arcana, role and source. They're on the GameBootstrap Inspector but aren't placeable yet (that's #2).
- Decisions from review: STORY.md marked APPROVED on `main`, and Makoto's Persona elements were added to it (Orpheus Fire, Jack Frost Ice, Sarasvati neutral, Oberon Elec, Thoth Fire, Shiisaa Strike, Thor Elec, Melchizedek Light, Lakshmi Ice, Pixie Elec, Valkyrie Slash). Cerberus is Dark, with Fire as flavour only. Both are in the roster data now.
- Added #20 (import non-basic assets) under M5.

### Problems & fixes
- `Main.unity` saves enums as ints (`element: 3` meant Zio). Renumbering the enum would silently turn old data into the wrong element, so the new enum keeps the old numbers (Fire = 1, Ice = 2, Elec = 3, Almighty = 4) and adds the new types after them.
- The single `weakness` / `resist` / `isBoss` fields became lists and a rank. `EnemyDef` now migrates old scene data in `OnAfterDeserialize`, so nothing is lost and no Inspector reset is needed.
- Couldn't press Play from Claude Code, so I compiled the scripts against Unity's DLLs with `dotnet build` (0 errors) and ran a small console check of the data model and migration (all pass).

### AI prompts that mattered
- "Turn STORY.md into GitHub issues, each with a description, a checklist of what 'done' means, and a reference to the STORY.md section."
- "Never change anything in STORY.md without my approval; anything marked [CHECK] gets flagged, not guessed."
- Flagging the missing Persona elements on the PR, instead of guessing them, got a quick, complete answer.

### Next
- [ ] Press Play and confirm the starter game plays exactly as before (see the PR test list)
- [ ] #2 party selection

---

## 2026-11-17: Day 1: Concept and starter kit
**Goal:** pick a theme that can reach A+ on the CSCI 780 track, then get a full playable loop working on the first day.

### Concept
- Theme: **Persona-inspired**. A school that turns into a dungeon at midnight, Shadows walking toward a Clock Tower, and the player "summoning" towers.
- Persona's battle system maps neatly onto tower defense:
  - Elemental **weaknesses** become tower choice and counter-play.
  - **Knockdown / One More** becomes a stun plus an instant extra shot.
  - **All-Out Attack** becomes a field-wide bonus when every enemy is down.
  - **Social Links** become between-wave upgrades.
  - The **calendar** becomes waves as "nights", with the Full Moon as the boss wave.

### Done
- Generated the starter codebase with Claude: a runtime `GameBootstrap` that builds the whole scene from one component.
- Path: a Catmull-Rom spline, so enemies follow a **non-straight** path (a C-level requirement for 780).
- Three towers with different fire rate, projectile speed and **height**.
- Predictive aim: a closed-form quadratic intercept, plus a ballistic solver with gravity for the mortar.
- Four enemy types with different speed and HP, including the Reaper boss.
- Tower limit and SP economy, placement ghost and range ring, and an always-visible placement help overlay.
- README with a requirement-to-code mapping table.

### Problems & fixes
- _(fill in after the first Play-mode test: Console errors, balance problems, etc.)_

### AI prompts that mattered
- "Design a Persona-themed tower defense that hits every CSCI 780 requirement, plus A+ extras."
- "Make the ballistic tower lead moving targets and land exactly despite PhysX integration."

### Next
- [ ] Open in Unity, press Play, fix any Console errors
- [ ] Playtest all 6 nights and tune HP / SP / costs in the Inspector
- [ ] Import non-basic assets (character models, music, SFX, a font) and fill in the README credits
- [ ] Add Bloom (URP Global Volume) for the glow
- [ ] Take screenshots / a GIF for the README
- [ ] Push to GitHub before the deadline

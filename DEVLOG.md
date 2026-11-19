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

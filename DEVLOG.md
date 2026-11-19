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

## 2026-11-18: #3 Field limit per floor
**Time spent:** _(fill in)_
**Goal:** placing is free; the only limit is the Jailer's field limit (8, 7, 6, 6, 5, 4), with each Persona on the field once and a dismiss screen when a floor's limit drops.

### Done
- Merged PR #21 (#2) after the retest passed. Decided with the student that bonds also boost Makoto's Personas of the member's Arcana (STORY.md and #14 updated).
- **Field limit:** `fieldLimits` on the GameBootstrap Inspector (default 8, 7, 6, 6, 5, 4). Until floors exist (#4), night N uses floor N's limit. `TowerLimit` became `FieldLimit`, and the HUD reads "FIELD 5/6".
- **Placing is free:** removed `TowerDef.cost`, the refund (`refundFraction`, `RefundFor`) and the SP check. SP stays for the skills (#10).
- **Each Persona once:** this covers both "one tower per member" and "each Makoto Persona once". Placed Personas show **ON FIELD**, their cards are disabled, key 4 skips them, and the ghost explains why ("Yukari's Io is already on the field (one tower per member)"). A successful placement ends the selection.
- **Dismiss screen** (`GameState.Dismiss`, "THE SEAL TIGHTENS"): opens after the party screen when the field is over the new limit. You mark towers with 1-8 or a click, and Continue unlocks once you're at or under the limit. The party screen already warns you ("the field limit drops from 8 to 7 ... you'll dismiss 1 next").
- **No way to win slots back:** removed the Fool Social Link (+1 tower slot) from Free Time, the sidebar and `TowerLimit`. The rest of the Social Links go in #14.
- README: the limited-towers row, a field-limit A+ bullet, and the controls (dismiss screen, Free Time 1-3).

### Problems & fixes
- `Destroy` only happens at the end of the frame, so a tower removed by a party swap still counted toward the limit on that frame and could wrongly open the dismiss screen. Removed towers are now deactivated first (`SetActive(false)` takes them out of `Tower.All` right away), then destroyed.

### AI prompts that mattered
- "Start the next issue in M1 order and stop at the PR."

### Next
- [ ] Test the PR for #3 in Unity (checklist in the PR)
- [ ] #4 floor progression (the stairs breather replaces Free Time)

---

## 2026-11-18: #2 Party selection
**Time spent:** _(fill in)_
**Goal:** pick Makoto + 3 members before the climb, place only the party's Personas, and swap members at the break.

### Done
- Merged PR #19 (#1) after testing it in Unity (0 errors).
- New **party screen** (`GameState.PartySelect`). Makoto is fixed as leader, and the player picks exactly 3 of the 8 members (keys 1-8 or click, Enter to confirm). It opens before the first night, after every restart (the party is cleared, so you choose again), and at every break after Free Time.
- **Swapping at the break:** the screen lists which placed towers will leave the field, and confirming removes them. Makoto's Personas always stay.
- **New tower bar:** the party row (3 member Personas, keys 1-3) sits above Makoto's row (11 Personas, key 4 cycles, or click). Hovering any card shows its full stats: rate, projectile speed, height, range, damage and role.
- Removed the 3 starter towers (Zio Spark, Bufu Frost, Agi Inferno). Every placeable tower is now a Persona from the roster. Placing is free (cost 0), and the tower limit still applies.
- Added `GameKey.Choice5`-`Choice8` (appended to the enum and to both InputCompat tables).
- README: updated the requirement rows (placing, tower types, limited towers), controls, A+ list and aiming section.
- **Review fixes (after the first Unity test, 0 errors):**
  - Hover highlights only clickable things. Disabled cards (party full, tower limit reached) and the disabled confirm button are dimmed, have no hover and ignore clicks.
  - "Selected" looks clearly different from hover. Selected is a bright red fill with a thick white border, and party members also get a white **IN PARTY** badge. Hover is a slightly lighter fill with a faint outline.
  - Restart clears the party.
  - Makoto's cards are sized from the measured width of their longest text (`GUIStyle.CalcSize`), so nothing is cut off. When they don't fit in one row (4:3), they split into balanced rows (6 + 5), and the party row and start button move up.
  - At the tower limit, keys 1-4 do nothing either, and the bar says "Tower limit reached: press X on a tower to dismiss it".
- Updated issues #4 (the stairs breather replaces Free Time) and #14 (remove the Social Links and their side panel), and opened #22 (camera framing and HUD overlap at any aspect ratio).

### Problems & fixes
- Free Time also uses keys 1-4, so the key that picks a Social Link could have toggled a member on the same frame. The party keys skip the frame the screen opened (`StateChangedFrame`).
- 14 cards don't fit in one row at 1080p, so the bar now has two rows (full cards for the party, compact cards for Makoto), with the details moved into a hover tooltip.
- There's no floor break yet (#4), so the swap hooks into the existing Free Time → Planning transition. #4 can move it with the break.
- At 4:3, eleven fixed-width cards were too narrow ("Melchizedek", "Last line of defen..."). Fixed by measuring the text instead of guessing a width.

### AI prompts that mattered
- "Start issue #2 on a new branch ... include a test checklist I can follow in Unity, and stop for my review."

### Next
- [ ] Test PR for #2 in Unity (checklist in the PR)
- [ ] #3 field limit per floor (one tower per member, each Makoto Persona once)

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

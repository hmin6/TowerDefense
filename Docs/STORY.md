# STORY.md: Midnight Hour (draft for review)

> **Status:** APPROVED (2026-11-18). This is the design the build follows; changes need the student's approval.
> Items marked **[CHECK]** are details to verify against the game.

---

## 1. Premise

**When:** September, during the school semester. This is the window when the whole S.E.E.S. roster is active, Shinjiro included (he's in the party from early September until the 10/4 full moon). Every Persona is in its **base form**, and there are **no evolutions**.

**Where:** the first part of the **Tziah** block of Tartarus, **Tziah I** (floors 114-138 in the original *Persona 3*). It opens on 9/10, and its barrier sits on floor 139, so this is where S.E.E.S. would be climbing in mid-to-late September. Tziah's look is **gold**, which sets the art direction for every floor.

**Version:** this doc follows the **original *Persona 3* / FES** for dates, floors and Shadows, so all the details come from the same game.

S.E.E.S. is climbing Tartarus as usual. Partway up the block, the team takes the stairs to a new floor, and the staircase **seals shut behind them** and ahead of them. They can't go up or down.

Fuuka senses the cause: a powerful Shadow higher up the block is influencing the floors below it. It has sealed the staircases and is sending its Shadows to wear the team down. The seal on each floor only breaks once the waves on it are cleared.

S.E.E.S. is **stuck and defending themselves**. To get out, they have to fight their way up floor by floor, survive each assault, break each seal, and reach the Shadow behind it all.

## 2. How it plays

### Before the climb: choose the party
- A short **story intro** plays before the party screen: the stairs seal, and Fuuka explains the situation.
- Like entering Tartarus, you **pick who goes in**.
- Makoto (the leader) always goes. You choose **3 more** from the S.E.E.S. roster.
- **Proposed:** you can **swap members at the break between floors**. Fuuka previews the next floor's Shadows, so you can adjust the party to counter them.

### Towers: who places what
- **Placing a Persona costs its owner SP**, and each Persona has its own placement cost (see "Skills and SP"). The field limit caps how many towers can be on the field at once.
- **Each party member places one tower**: their own Persona.
- **Makoto is the Wild Card.** He can place **any of his Personas, as many as the field limit allows**, but each Persona only once. There's no limit specific to Makoto.
- **Every tower has its own role**, with no overlaps (see section 5).
- Towers go **anywhere beside the path**, just not too close to the Shadows' corridor itself.

### The seal suppresses Personas (the field limit)
- The boss Shadow's influence doesn't just seal the stairs. It also **suppresses Personas**, which limits **how many towers can be on the field at once**.
- **The closer the team gets to the Shadow, the stricter the limit.** The limit counts everyone together, Makoto's Personas and the members' towers alike. How you split it is up to you.

| Floor | Field limit (total towers) |
|---|---|
| 1 | 8 |
| 2 | 7 |
| 3 | 6 |
| 4 | 6 |
| 5 | 5 |
| 6 (boss) | 4 |

- **Every floor has its own layout, so towers don't carry over.** Each floor starts with an empty field, and you redeploy within that floor's limit.
- There's **no way to win slots back** during a floor. The limit for each floor is fixed.
- Fewer towers *and* tougher Shadows makes the difficulty climb fast, so skills (SP) become more important on later floors. The numbers will need playtesting.

### Skills and SP: each member's signature move
- Every deployed member has a **signature skill** you trigger yourself: click their skill button, then click where to use it.
- **SP is individual.** Each party member has their own SP. **Makoto has one pool shared by all his Personas.**
- SP is spent only when **placing a Persona** (each Persona has its own placement cost) and when **using a skill**. Each skill has a **cooldown** after use.
- There's **no drain over time**. A placed Persona stays on the field even if its owner's SP hits 0. At **0 SP**, that member can't place their Persona or use skills until they regain SP.
- SP comes back from:
  - **weakness hits:** a little, to the attacker
  - **Thoth:** while he's on the field, he regenerates SP for every party member
  - **the Opulent Hand:** SP to every party member, Makoto included

  **Kills give no SP.**
- **Proposed:** a small SP top-up for everyone at the stairs breather between floors.
- There's no separate game-over rule for SP. If nobody can afford to place anything, the normal loss happens when the Shadows reach the team. If nobody can place at the start of a floor, Fuuka warns: **"S.E.E.S. is out of SP!"**
- **Proposed:** only the **members** have signature skills. **Makoto** has one skill of his own: **Persona Swap**, which instantly swaps one of his placed Personas for another (the Wild Card). His Personas don't have separate skills, which keeps the number of buttons sensible.
- **Numbers (all to tune in playtesting):**
  - **SP per member:** starting SP and maximum.
  - **Placement cost** of each Persona.
  - **SP from a weakness hit:** a little, to the attacker.
  - **Thoth's regeneration rate**, the **Opulent Hand's bonus**, and the **breather top-up** (proposed).
  - **Skills:** cost 20-50 SP, with 10-30 second cooldowns.

### On each floor
1. The team arrives, and the staircase behind them seals.
2. Fuuka scans the floor and calls out what's coming, including weaknesses.
3. You deploy towers anywhere beside the corridors, around **where the team is holding out**, up to the floor's field limit. The field starts empty on every floor.
4. Shadows come down the floor's corridors toward the team.
5. You use skills at the right moments (hitting a weakness knocks Shadows down; knock them all down for an **All-Out Attack**).
6. Clearing every wave breaks the seal on the stairs up.

**Floor 1 is a guided tutorial**, with Fuuka's lines: placing towers, weaknesses, SP costs and skills.

### Between floors
- There's a short breather on the stairs. You talk with one party member, which gives a small boost to their Persona's stats. Personas stay in their base forms.
- Like Social Links in the game, the bond also boosts **Makoto's Persona(s) of that member's Arcana**. For example, Junpei (Magician) boosts Jack Frost, Akihiko (Emperor) boosts Oberon, and Shinjiro (Hierophant) boosts both Thoth and Shiisaa.
- **Fuuka** can also be chosen at the breather, alongside the party members. Bonding with her boosts **Sarasvati** (Priestess). She's still the navigator, not a tower.
- **Orpheus** (Fool) gets a small boost each time the team clears a floor, because the Fool Social Link is S.E.E.S. as a whole.

### Final floor
- You reach the Shadow causing the seal (the boss). It fights while summoning its own waves.

### Losing
- If the Shadows break through to the team's position, S.E.E.S. is overrun. You then choose:
  - **Retry floor:** the same floor again with the same party, redeploying from an empty field.
  - **Retreat:** leave Tartarus and climb again from Floor 1, choosing the party again.

## 3. Floors

| Floor | What's new | Shadows (all from Tziah I) | Notes |
|---|---|---|---|
| 1 | Basic Shadows | Ill-fated Maya, Blue Sigil | Teaches placement and weaknesses |
| 2 | **Fast** Shadows | Killer Drive, Brave Wheel | Teaches using skills |
| 3 | **Tanky** Shadows | Champion Knight; the floor ends with **three Arcane Turrets** | The original game has three Arcane Turrets as the boss on 122F |
| 4 | **Flying** Shadows | Writhing Tiara, Tranquil Idol | |
| 5 | Everything at once | Death Twins, Elegant Mother, Constancy Relic, Magical Magus, plus **Hakurou Musha** (the strongest Shadow in Tziah I); the floor ends with the **Sleeping Table** | The original game has the Sleeping Table as the sub-boss on 135F |
| 6 | **Boss floor** | **The Jailer** and its summons | Its lair sits just below the Tziah barrier (139F) |

**Proposed:** six floors (five plus the boss). This matches the field-limit table, and each floor introduces one new thing.

**Layouts:** each of the six floors has its own unique path and layout, styled after Tartarus's Tziah I block: gold, palace-like corridors.

**How "fast" and "flying" are decided:** *Persona 3*'s battles are turn-based, so Shadows don't have a speed or a flying type. For the tower defense, they're assigned by how each Shadow looks: Drives and Wheels are vehicles, so they're fast; Tiaras and Idols float, so they fly.

**Optional extra:** the **Opulent Hand** (a rare treasure Shadow in Tziah I) could show up now and then and run for the exit. Defeat it before it escapes, and every party member, Makoto included, gets SP.

**Weaknesses (original *Persona 3*, Tziah I):**

| Shadow | Weak to | Notes |
|---|---|---|
| Ill-fated Maya | Elec, Wind | |
| Blue Sigil | Dark | |
| Killer Drive | Light | |
| Brave Wheel | Ice | |
| Champion Knight | Wind | Resists Slash, Strike and Pierce |
| Arcane Turret | Elec | Highest HP of the regular Shadows here |
| Writhing Tiara | Ice | |
| Tranquil Idol | Dark | |
| Death Twins | Strike | |
| Elegant Mother | Elec, Dark | |
| Constancy Relic | Fire | |
| Magical Magus | Fire | |
| Hakurou Musha | Light, Dark | |
| Sleeping Table | Ice, Light, Dark | Blocks all ailments |
| Opulent Hand | None | |

Source: a Persona 3 FES enemy table for Tziah I (floors 114-138), plus the Megami Tensei Wiki for the Sleeping Table. The table didn't label its columns. I read them as Slash, Strike, Pierce, Fire, Ice, Elec, Wind, Light, Dark, in the game's usual order, and that reading matches the other sources for Arcane Turret (elec), Brave Wheel (ice) and Hakurou Musha (light, dark).

This means the game needs **Slash, Strike, Pierce, Light (Hama) and Dark (Mudo)** as damage types, alongside Fire, Ice, Elec and Wind. Every party member's element now has Shadows to exploit: Ken's Hama hits Killer Drive and Hakurou Musha, Koromaru's Mudo hits Blue Sigil and Tranquil Idol, and Shinjiro's Strike hits Death Twins.

## 4. The boss (original)

**Proposed: the Jailer**, an original Shadow. It's a hulking figure wrapped in chains and padlocks.

- **Story:** its chains are what seal the staircases, and each floor's seal is one of its locks breaking. They're also what **suppresses the Personas**, so the closer S.E.E.S. gets, the tighter its hold and the lower the field limit.
- **In the fight:** it **chains one tower at a time**, shutting that tower down for a few seconds, while summoning waves of Shadows.

Other ideas considered: the Hourglass (sand draining as the seals break) and the Choir (masks singing a seal over the stairs).

**Approved (2026-11-18):** the Jailer, as described above.

## 5. Roles: every tower does something different

Fuuka is the **navigator** (Lucia), not a tower. She scans and talks to the team.

### The S.E.E.S. members (one tower each)

| Member | Persona | Element | Role | What it does | Signature skill (suggested) |
|---|---|---|---|---|---|
| Yukari | Io | Garu | **Pushback** | Wind arrows knock Shadows backward along the path | **Magaru:** wind blast on an area that blows Shadows back |
| Junpei | Hermes | Agi | **Artillery** | Slow, arcing fireballs that explode on an area | **Agilao:** one huge fire hit on a single target |
| Akihiko | Polydeuces | Zio | **Brawler** | Rapid, short-range hits that stack damage on one Shadow | **Mazio:** electric on an area, with a chance to stun |
| Mitsuru | Penthesilea | Bufu | **Controller** | Slows the Shadows she hits | **Mabufu:** ice on an area, with a chance to freeze |
| Aigis | Palladion | Pierce | **Sniper** | Longest range; bullets pierce through a line of Shadows | **Orgia Mode:** huge fire-rate boost for a few seconds, then a short overheat |
| Ken | Nemesis | Hama | **Executioner** | Chance to instantly kill weakened, non-boss Shadows | **Hama** family: light on an area, instant-kill chance |
| Koromaru | Cerberus | Mudo (Agi is secondary flavor only) | **Debuffer** | Curses Shadows, so they take more damage from everyone | **Mudo** family: curses every Shadow in an area **[CHECK]** |
| Shinjiro | Castor | Strike | **Tank buster** | Very slow but massive physical hits; bonus damage to high-HP Shadows | **[CHECK]** His heavy physical skill |

### Makoto's Personas (from *Persona 3 The Movie*)

In the movies, Makoto gains a Persona from each full moon Shadow, matching its Arcana. Below are the ones he has **by September** that match **S.E.E.S. Arcana**:

| Persona | Arcana (whose) | Element | Role | What it does |
|---|---|---|---|---|
| Orpheus | Fool (Makoto) | Agi | **Generalist** | Balanced at everything; a solid tower to place first |
| Jack Frost | Magician (Junpei) | Bufu | **Trapper** | Plants frost mines on the path that burst when Shadows step on them |
| Sarasvati | Priestess (Fuuka) | Neutral | **Support aura** | Makes nearby towers fire faster |
| Oberon | Emperor (Akihiko) | Zio | **Chain lightning** | Each hit jumps to several more Shadows |
| Thoth | Hierophant (Shinjiro) | Agi | **SP battery** | While he's on the field, regenerates SP for every party member, fitting for the god of knowledge |
| Shiisaa | Hierophant (Shinjiro) | Strike | **Last line of defense** | Short range, but very heavy damage to any Shadow that gets close to the team |
| Thor | Chariot (Aigis) | Zio | **Anti-air** | Heavy damage to flying Shadows |
| Melchizedek | Justice (Ken) | Hama | **Pulse** | Damages every Shadow around it every few seconds, with no projectile |
| Lakshmi *(game)* | Empress (Mitsuru) | Bufu | **Resistance breaker** | Shadows near it lose their resistance, so resisted elements do normal damage |
| Pixie *(game)* | Lovers (Yukari) | Zio | **Multishot** | Fires small shots at up to 3 Shadows at once |
| Valkyrie *(game)* | Strength (Koromaru) | Slash | **Interceptor** | Targets the fastest Shadow in range first and hits fast Shadows harder |

**Sarasvati** is Neutral: her shots have no element.

**Filling the gaps:** the movies don't show Makoto with a Persona of the Empress, Lovers or Strength Arcana before September. For those three, Lakshmi, Pixie and Valkyrie (marked *game*) are taken from the game's Persona list for that Arcana instead. That gives Makoto one Persona for every S.E.E.S. Arcana, with Hierophant getting two.

**Not in this set:** Taraka and Arahabaki (both Hermit, which is Jin's Arcana, not a S.E.E.S. member's). Also out: everything he gets after September (Siegfried, Norn, Hecatoncheires, Attis, Odin, Surt, Mother Harlot, Scathach, Kohryu, Messiah).

**Thanatos is left out.** He isn't part of the story in September. The events around him (Death sealed inside Makoto, Pharos, Ryoji) come later in the year.

**[CHECK]** Skill names, plus the Arcana sources marked as inferred: Shiisaa and Thoth (Hierophant), Thor (Chariot), Melchizedek (Justice), Lakshmi (Empress), Valkyrie (Strength). Jack Frost, Sarasvati and Oberon are confirmed from the movies, and Pixie is confirmed as Lovers.

## 6. How this covers the CSCI 780 requirements

| Requirement | How the story covers it |
|---|---|
| Place towers | Deploy party members and Makoto's Personas on each floor |
| Limited towers | The boss's suppression sets a field limit that shrinks floor by floor (8 down to 4) |
| Different tower types (rate, projectile speed, height) | 19 towers, each with a different role and stats |
| Non-straight path | Each floor's corridors wind toward the team |
| Game ends when an enemy completes the path | Shadows reach the team, so S.E.E.S. is overrun |
| Multiple enemy types | New Shadow types on each floor, then the boss |
| A+ | Party selection, the Wild Card, 19 distinct tower roles, signature skills with SP and cooldowns, Fuuka's scans, All-Out Attacks, the original boss |

---

## Approval
- [x] Premise approved (September, base Personas, no evolutions)
- [x] Party swapping between floors (proposed: yes, at the break)
- [x] Makoto picks freely; suppression limits only the total on the field
- [x] Field limit numbers per floor confirmed
- [x] Roles approved (no overlaps) and skills checked
- [x] Thanatos left out (not part of the September story)
- [x] Shiisaa as last line of defense (proposed)
- [x] Skills: members only, plus Persona Swap for Makoto (proposed)
- [x] Skill resource: individual SP per member (Makoto's Personas share one pool), replacing the shared team pool
- [x] Tower cost: each Persona costs its owner's SP to place (no drain over time)
- [x] SP starting numbers (proposed; tuned in playtesting)
- [x] SP returns from weakness hits, Thoth and the Opulent Hand; kills give no SP; no separate out-of-SP game over
- [ ] SP top-up at the stairs breather (proposed)
- [x] Suppression stays; no way to win slots back
- [x] Setting: Tziah I (original *Persona 3*), floors 114-138
- [x] Six floors with the Tziah I Shadow lineup (proposed)
- [x] Shadow weaknesses (original *Persona 3*)
- [x] Opulent Hand bonus (optional)
- [x] Boss: the Jailer (proposed)
- [x] Losing: Retry floor or Retreat
- [x] Each floor has its own Tziah I layout; towers are redeployed every floor
- [x] Placement: anywhere beside the path (not too close to it)
- [x] Story intro before the party screen, and Floor 1 as a guided tutorial

Once these are checked, the build gets split into GitHub issues and done in Claude Code.

# Midnight Hour: a Persona-inspired 3D Tower Defense

> When the clock strikes midnight, Seiran Academy twists into a labyrinth and the Shadows march toward the Clock Tower. If even **one** of them reaches it, the Midnight Hour never ends.

**Course:** Video Game Design and Development, Fall 2026, Assignment 2 (Tower Defense)
**Requirements targeted:** CSCI 780 (graduate), aiming for **A+**
**Engine:** Unity (3D physics)
**Author:** _Your Name_ (_your GitHub handle_)

![Gameplay screenshot](Docs/screenshot.png)
<!-- Take a screenshot in Play mode and save it as Docs/screenshot.png -->

---

## Quick start

1. Open the project in **Unity 6 LTS** (6000.0+). Unity 2022.3 LTS also works.
   Built-in, URP and HDRP are all supported because the game clones the pipeline's default material.
2. Open `Assets/Scenes/Main.unity` and press **Play**.

**First-time scene setup** (only once, if `Main.unity` doesn't exist yet):
open any scene → `GameObject > Create Empty` → name it `Game` → **Add Component → `GameBootstrap`** → `File > Save As` `Assets/Scenes/Main.unity` → Play.
The board, path, camera, lighting, HUD and every system are built at runtime, so no prefab wiring is needed.
Also add the scene in `File > Build Profiles` (or `Build Settings`) so it ends up in builds.

Both the new **Input System** and the old **Input Manager** work, so the *Active Input Handling* setting doesn't matter.

---

## CSCI 780 requirement checklist

| Grade | Requirement | How it's met | Where in the code |
|---|---|---|---|
| C | Player can place towers | Press 1/2/3 or click a card, then left-click the ground. A ghost previews the spot (green = valid, red = blocked). | `Towers/TowerPlacer.cs`, `GameManager.TryBuildTower` |
| C | Towers fire continuously at a set rate | Each tower has a `fireRate` (shots/sec) cooldown and fires whenever a target is in range. | `Towers/Tower.cs` (`Update`) |
| C | Towers aim directly at an enemy | The head turns to the shot direction every frame and targets the in-range Shadow that is furthest along the path. | `Tower.FindTarget`, `Tower.Update` |
| C | Enemies travel predetermined **non-straight** paths | Control points are smoothed with a **Catmull-Rom spline** into a winding S-shaped route. | `Map/EnemyPath.cs` |
| C | Game ends when an enemy completes its path | The first Shadow to reach the Clock Tower triggers **Game Over** immediately. | `Enemy.ReachedGoal`, `GameManager.EnemyReachedGoal` |
| B | ≥2 tower types with different rate of fire, projectile speed **and** tower height | **3 towers:** Zio Spark (3.0/s, 30 m/s, 1.2 m), Bufu Frost (1.1/s, 16 m/s, 2.4 m), Agi Inferno (0.55/s, 9 m/s arc, 4.0 m). | `Data/DefaultContent.cs` |
| B | Text overlay clearly explains tower placement | A "HOW TO PLACE TOWERS" panel is always on screen (toggle with H). Tooltips at the cursor explain *why* a spot is blocked. | `UI/HUD.cs` (`DrawHelp`, `DrawPlacementHint`) |
| B | Limited number of towers | A hard **tower limit** (6, +1 per Fool rank) plus an SP cost. Towers can be dismissed for a 50% refund. | `GameManager.TowerLimit` |
| B | Towers aim at the **predicted** location from current velocity and position | The closed-form quadratic intercept for straight shots, and a ballistic solver for arcing shots. Press **L** to see the lead lines. | `Towers/Aiming.cs`, `Tower.ComputeShot` |
| A | Multiple enemy types with different speed and HP | **4 Shadows:** Cowardly Mask (fast/fragile), Gloom Slime (medium), Iron Knight (slow/tanky), The Reaper (boss). | `Data/DefaultContent.cs` |
| A | Non-basic assets | _Fill in: list the imported models, textures, audio and fonts you used._ | `Credits` below |
| A+ | Above and beyond | See the next section. | |

### A+ features

- **Persona battle system as tower defense**
  - **Weakness / Resist:** Persona 3's ten damage types (Slash, Strike, Pierce, Fire, Ice, Elec, Wind, Light, Dark, Almighty). A Shadow can be weak to several (×1.5 damage) and resist several (×0.5), and Almighty ignores both. Its **glowing eye color shows its first weakness**, and its health bar shows a pip for each one.
  - **Knockdown (real physics):** a weakness hit switches the Shadow from a kinematic to a dynamic Rigidbody and launches it with an impulse and torque. When the stun ends it recovers and walks back onto the path.
  - **ONE MORE!:** a tower that knocks a Shadow down gets an instant extra shot.
  - **ALL-OUT ATTACK!:** when every Shadow on the field is knocked down at once, they all take 50% of their max HP, with a screen shake.
- **Ballistic predictive aim:** the Agi Inferno fires a gravity-affected Rigidbody. The solver iterates the flight time against the target's predicted position. It then **corrects for PhysX's semi-implicit Euler integration** (−½·g·Δt), so the simulated shell actually lands where the math says.
- **Path-aware aim mode (toggle P):** instead of linear lead, towers predict *along the spline*, so they hit Shadows rounding corners. The assignment's "current velocity" version stays the default, and you can switch modes live to compare them.
- **Day / Night calendar + Social Links:** six nights ending in a **FULL MOON** boss wave. Between nights you pick one Social Link to rank up (damage and fire-rate boosts per element, or +1 tower slot).
- **Physics juice:** splash damage with `AddExplosionForce`, death shards, swept sphere-cast collisions (fast shots never tunnel), screen shake, trails and flashes.
- **Quality of life:** game speed ×1/×2/×3, pause, restart without reloading the scene, camera pan and zoom, a range ring while placing, and health bars with weakness and slow indicators.
- **Data-driven:** every tower, enemy and wave is editable in the Inspector, and any of them can take a custom model prefab.

---

## Controls

| Input | Action |
|---|---|
| **1 / 2 / 3** or click card | Select tower type (press again to deselect) |
| **Left-click** | Place tower |
| **Right-click / Esc** | Cancel placement |
| **X** (hovering a tower) | Dismiss tower (50% refund) |
| **Enter** | Start game / begin the night |
| **1–4** (Free Time) | Choose a Social Link |
| **Space** | Pause |
| **F** | Cycle game speed |
| **P** | Toggle aim mode: linear lead ↔ path-aware |
| **L** | Toggle aim lines (shows predicted intercept points) |
| **H** | Toggle the help overlay |
| **WASD / Arrows**, **Scroll** | Pan / zoom camera |
| **R** | Restart (when paused or after the game ends) |

---

## How the predictive aiming works

**Straight projectiles** (Zio, Bufu). Find the time `t` when a projectile with speed `s` meets a target at `P + V·t`:

```
|P + V·t − S| = s·t   →   (V·V − s²)t² + 2(D·V)t + D·D = 0,   D = P − S
```

Take the smallest positive root. The aim point is `P + V·t`.

**Ballistic projectiles** (Agi). The horizontal speed is fixed. Iterate the flight time `T = horizontalDistance(S, P + V·T) / speed` a few times, then solve the launch velocity exactly:

```
v0 = (A − S)/T − ½·g·T − ½·g·Δt      (last term cancels Euler integration drift)
```

**Path-aware mode** swaps `P + V·t` for `path.Evaluate(distanceTravelled + speed·t)`, so the prediction follows curves.

---

## Project structure

```
Assets/Scripts/MidnightHour/
├── Core/          GameBootstrap (scene builder), GameManager (rules/flow), VisualFactory, Compat (input/physics shims)
├── Data/          TowerDef / EnemyDef / WaveDef + DefaultContent (all the numbers)
├── Map/           EnemyPath (Catmull-Rom spline path)
├── Enemies/       Enemy (path following, weakness, knockdown physics)
├── Spawning/      WaveSpawner, Factory (builds towers/enemies/effects)
├── Towers/        Tower, Projectile, Aiming (math), TowerPlacer
├── UI/            HUD (IMGUI overlay, menus, banners, health bars)
└── CameraControl/ CameraRig
```

## Using your own art

Select the `Game` object. In **GameBootstrap → Towers / Enemies**, drag a prefab into `Model Prefab`.
- **Towers:** give the prefab a child named `Head` (rotates to aim) with a child named `Muzzle` (where shots spawn). If those are missing, they're created automatically.
- **Enemies:** any model works. If it has no collider, a capsule collider is added automatically.
- **Ground / goal / spawn**, music and SFX slots are on the same component.
- For the glowing look, add **Bloom** to the URP Global Volume.

---

## Credits

| Asset | Author / source | License / permission |
|---|---|---|
| _e.g. Character models_ | _source_ | _license_ |
| _e.g. Music_ | _source_ | _license_ |
| _e.g. SFX_ | _source_ | _license_ |

Inspired by the *Persona* series (Atlus). This is a fan-made class project and is not affiliated with Atlus or SEGA.

## AI assistance

As the assignment allows, this project was developed with **Claude** (Anthropic), working in VS Code through Claude Code. The prompts, design direction, testing, tuning and integration in Unity are my own. See [DEVLOG.md](DEVLOG.md) for a session-by-session record.

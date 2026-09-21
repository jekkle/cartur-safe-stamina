# Cartur's Safe Stamina

Stamina costs nothing while nothing is hunting you.

Chopping, mining, sprinting, jumping, swimming, sneaking, building, repairing and
weapon swings are all free out in the quiet. Then something hostile walks into
range and every cost comes straight back, at full price, mid-swing.

The bar also **refills** while you swim and while you swing — vanilla stops
regeneration dead during both.

![Every setting in the ConfigurationManager panel](https://raw.githubusercontent.com/jekkle/cartur-safe-stamina/master/docs/images/configuration-manager.png)

## What counts as safe

No hostile character within `SafeRadius` metres of you — 25 by default.

Hostility is the game's own check rather than a list of prefab names, so tamed
animals are ignored, a boar that hasn't noticed you still counts, and anything a
mod adds is judged by the same rule.

## Settings

`BepInEx/config/com.jekkle.valheim.cartursafestamina.cfg`.

| Setting | Default | What it does |
| --- | --- | --- |
| `SafeRadius` | 25 | Metres. How close a hostile has to be before costs return. |
| `AffectAttacks` | true | Chopping, mining, weapon swings. |
| `AffectSprint` | true | Sprint drain. |
| `AffectJump` | true | Jump cost. |
| `AffectBuild` | true | Build, repair, remove piece. |
| `AffectSwim` | true | Swim drain. |
| `AffectSneak` | true | Crouch and sneak drain. |

Each cost is its own switch, so you can leave combat or swimming paying full price.

[ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/)
makes every setting adjustable in game from the F1 menu, no restart. Optional.

## Worth knowing

**Free swimming also means you can't drown while safe.** The drown timer only
starts once stamina is empty, so a swim that costs nothing never gets there. That
is why `AffectSwim` has its own switch — turn it off if you want the water to stay
dangerous.

**Skills still level.** Run, swim, sneak and weapon XP accrue exactly as before.

**Other characters pay normally.** Nothing is waived for the boar or draugr inside
your safe radius.

Mods that change stamina *amounts* stack fine — they set the number, this waives it.

## Install

Use a mod manager (r2modman / Thunderstore / Gale) and it pulls in BepInEx for you.
Manually: drop `CarturSafeStamina.dll` into `BepInEx/plugins`.

---

*Free, and always will be. If it improved your game you can [tip me on Patreon](https://www.patreon.com/c/cartur).*

**More from Cartur:**
[HD Blood](https://thunderstore.io/c/valheim/p/Cartur/Carturs_HD_Blood/) ·
[Map Pins](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Map_Pins/) ·
[Compass and Clock](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Compass_and_Clock/) ·
[Follow Command](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Follow_Command/) ·
[Flooring](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Flooring/) ·
[UI HUD](https://thunderstore.io/c/valheim/p/Cartur/Carturs_UI_HUD/) ·
[Waste Management](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Waste_Management/)

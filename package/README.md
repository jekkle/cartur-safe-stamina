# Cartur's Safe Stamina

*Free, and always will be — if it improved your game you can [tip me on Patreon](https://www.patreon.com/c/cartur).*

**More from Cartur:** [HD Blood](https://thunderstore.io/c/valheim/p/Cartur/Carturs_HD_Blood/) ·
[Map Pins](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Map_Pins/) ·
[Follow Command](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Follow_Command/) ·
[Compass and Clock](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Compass_and_Clock/) ·
[Flooring](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Flooring/) ·
[UI HUD](https://thunderstore.io/c/valheim/p/Cartur/Carturs_UI_HUD/)

Stamina costs nothing while nothing is hunting you.

Clearing a forest, sinking a mine shaft, running supplies across the meadows,
throwing up scaffolding — none of it drains the bar. Then something hostile
walks into range and every cost comes straight back, at full price, mid-swing.
The fight is still the fight. The chores stop pretending to be one.

## What's free when you're safe

Chopping and mining · weapon swings · sprinting · jumping · swimming ·
sneaking · building, repairing and removing pieces.

Each of those is its own switch, so you can leave combat or swimming paying
full price if you'd rather.

The bar also **refills** while you swim and while you swing. Vanilla stops
regeneration dead during both, so a free swim still left you stranded at
whatever stamina you went in with; while safe it now climbs back the way it
does standing still.

## What counts as safe

No hostile character within `SafeRadius` metres of you — 25 by default.

Hostility is the game's own `BaseAI.IsEnemy` check, not a list of prefab names,
so tamed animals are ignored, a boar that hasn't noticed you still counts, and
anything a mod adds is judged by the same rule as everything else. The scan is
cached for a quarter second rather than run every frame.

## Config

`BepInEx/config/com.jekkle.valheim.cartursafestamina.cfg`, written on first run.

| Setting | Default | What it does |
| --- | --- | --- |
| `SafeRadius` | 25 | Metres. How close a hostile has to be before costs return. |
| `AffectAttacks` | true | Chopping, mining, weapon swings. |
| `AffectSprint` | true | Sprint drain. |
| `AffectJump` | true | Jump cost. |
| `AffectBuild` | true | Build, repair, remove piece. |
| `AffectSwim` | true | Swim drain. |
| `AffectSneak` | true | Crouch/sneak drain. |

Editing the file by hand works, but
[ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/)
is easier — install it and every setting here is adjustable in-game from the F1
menu, no restart needed. It is optional; this mod does not depend on it.

![Every setting in the ConfigurationManager panel](https://raw.githubusercontent.com/jekkle/cartur-safe-stamina/master/docs/images/configuration-manager.png)

## Worth knowing

**Free swimming also means you can't drown while safe.** The game only starts
the drown timer once stamina is empty, so a swim that never costs anything
never gets there. That follows from the premise rather than being bolted on,
and it's why `AffectSwim` has its own switch — turn it off if you want the
water to stay dangerous.

**Skills still level.** Nothing here skips the original methods, so run, swim,
sneak and weapon XP accrue exactly as they always did.

**Other characters pay normally.** The attack patch checks the swinger is you
before waiving anything — otherwise every boar and draugr inside your safe
radius would swing for free too, which is precisely when they'd benefit.

## Compatibility

Nothing is replaced outright: every patch either zeroes a cost the game is
about to charge or restores what it borrowed. Mods that change stamina
*amounts* stack fine — they set the number, this waives it.

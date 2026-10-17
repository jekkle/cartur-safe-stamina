# Cartur's Safe Stamina

Stamina costs nothing while nothing hostile is near you, and the full costs return the moment something is.

![Chopping a tree with no enemies near. The stamina bar, bottom left, stays full](https://raw.githubusercontent.com/jekkle/cartur-safe-stamina/master/docs/images/ingame-stamina.jpg)

![The settings in the ConfigurationManager panel, as of version 1.0.2. Later versions have more.](https://raw.githubusercontent.com/jekkle/cartur-safe-stamina/master/docs/images/configuration-manager.png)

## What it does

Out in the quiet, chopping, mining, weapon swings, sprinting, jumping, swimming, sneaking, and building, repairing and removing pieces cost no stamina. Planting with the cultivator and levelling with the hoe go through the build cost, so they are free too. When a hostile character walks into range, every cost applies again at full price, even in the middle of a swing.

The bar also refills in two situations where vanilla stops regeneration completely: while you swim and while you attack or dodge. This only happens when you are safe, and the swim and attack switches below control it. Skill gain is not touched by the mod.

Only the local player is affected. A boar or a draugr inside your safe radius still pays for its own attacks.

## What counts as safe

No hostile character within `SafeRadius` metres of you, 25 by default. Dead characters do not count.

Hostility is the game's own enemy check (`BaseAI.IsEnemy`) rather than a list of prefab names. Tamed animals are ignored, a boar that has not noticed you still counts, and anything a mod adds is judged by the same rule. The check runs four times a second rather than every frame.

If a modded creature is in an enemy faction but never fights, put it in `IgnoredCreatures` and it will stop counting.

## Settings

Config file: `BepInEx/config/com.jekkle.valheim.cartursafestamina.cfg`.

| Setting | Default | What it does |
| --- | --- | --- |
| `SafeRadius` | 25 | Metres. How close a hostile has to be before costs return. |
| `AffectAttacks` | true | Chopping, mining, weapon swings. |
| `AffectSprint` | true | Sprint drain. |
| `AffectJump` | true | Jump cost. |
| `AffectBuild` | true | Build, repair, remove piece. Every build tool, so planting with the cultivator and the hoe too. |
| `AffectSwim` | true | Swim drain. |
| `AffectSneak` | true | Crouch and sneak drain. |
| `AffectEncumbered` | false | Over the weight limit: walking costs nothing and stamina refills. Off keeps the vanilla penalty. |
| `IgnoredCreatures` | (empty) | Comma-separated creatures that never count as a hostile nearby, by in-game name or prefab name (for example `T.W.I.G`). |

Each cost is its own switch, so you can leave combat or swimming paying full price.

[ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/) makes every setting adjustable in game from the F1 menu, without a restart. It is optional.

## Known issues and things to know

Free swimming also means you cannot drown while safe. The drown timer only starts once stamina is empty, so a swim that costs nothing never gets there. That is why `AffectSwim` has its own switch. Turn it off if you want the water to stay dangerous.

Holding a drawn bow still costs stamina inside the safe radius. That drain does not go through the same cost as other attacks and the mod does not cover it.

`AffectEncumbered` is off by default. Being over the weight limit costs stamina and blocks regeneration as in vanilla unless you turn it on.

## Compatibility

Mods that change stamina amounts work alongside this one. They set the number and this mod zeroes it afterwards, hooking the game's own stamina modifier calls rather than rewriting shared fields. That is how it avoids clashing with Speedy Paths, which wrapped the same value.

## Install

Use a mod manager (r2modman, Thunderstore or Gale) and it pulls in BepInEx for you. To install by hand, drop `CarturSafeStamina.dll` into `BepInEx/plugins`.

---

*Free, and always will be. If it improved your game you can [tip me on Patreon](https://www.patreon.com/c/cartur).*

**[Discord](https://discord.gg/nd5RqpwNkz)**: bug reports, install help, and mod requests.
Bug reports get their own thread so nothing is lost in a chat scroll, and requests are voted on.

**More from Cartur:**
[HD Blood](https://thunderstore.io/c/valheim/p/Cartur/Carturs_HD_Blood/) ·
[Map Pins](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Map_Pins/) ·
[Compass and Clock](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Compass_and_Clock/) ·
[Follow Command](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Follow_Command/) ·
[Flooring](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Flooring/) ·
[UI HUD](https://thunderstore.io/c/valheim/p/Cartur/Carturs_UI_HUD/) ·
[Waste Management](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Waste_Management/) ·
[Feeding Trough](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Feeding_Trough/) ·
[Combat Text](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Combat_Text/)

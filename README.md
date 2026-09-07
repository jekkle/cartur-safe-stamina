# NoStaminaWhenSafe

BepInEx/Harmony mod for Valheim. Sprint, jump, swim, and
build/repair/remove-piece stamina cost drop to zero whenever no hostile
character is within a configurable radius (default 25m) of the player.

## How it works

Four Harmony patches on `Player`:

- `CheckRun` (sprint drain) — zeroes the `m_runStaminaDrain` field for the
  duration of the original call, then restores it, so skill XP gain and
  other side effects still run.
- `OnJump` — same trick on `m_jumpStaminaUsage`.
- `OnSwimming` — zeroes **both** `m_swimStaminaDrainMinSkill` and
  `m_swimStaminaDrainMaxSkill`, because the drain is a
  `Mathf.Lerp` between them by swim skill; zeroing one end would still leave a
  cost at other skill levels. Swim-skill XP still accrues.
- `GetBuildStamina` — postfix zeroes the returned cost. This single method
  feeds all three build-tool stamina call sites (place, repair, remove).

**Swimming has a side effect worth knowing:** `OnSwimming` only starts the
drown timer once stamina is empty (`if (!HaveStamina()) m_drownDamageTimer += dt`),
so free swim stamina also means you can't drown while safe. That follows from
the mod's premise, and it's why `AffectSwim` is separately toggleable.

Enemy detection uses `Character.GetAllCharacters()` + the game's own
`BaseAI.IsEnemy(a, b)` hostility check (respects faction/tame/aggro state),
cached for 0.25s to avoid re-scanning every frame.

## Build

Requires .NET 8 SDK and a Valheim install with BepInEx already installed.

```
cd src
dotnet build
```

Managed DLLs for compiling are read from the raw Steam install
(`VALHEIM_INSTALL`, defaults to the standard Steam path). The **built plugin**
is deployed to wherever the game actually loads mods from at runtime — if you
launch through r2modman/Thunderstore Mod Manager (this repo's default
assumption), that's a profile folder under
`%APPDATA%\r2modmanPlus-local\Valheim\profiles\<profile>\BepInEx\plugins\`,
**not** the Steam install's own `BepInEx/plugins`. Override either path if
yours differs:

```
dotnet build -p:VALHEIM_INSTALL="D:\SteamLibrary\steamapps\common\Valheim" -p:R2MODMAN_PROFILE="C:\Users\you\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\MyProfile"
```

After building, fully quit Valheim through r2modman (not just close the
window) and relaunch — BepInEx only scans plugins on startup.

## Config

After first run, edit
`BepInEx/config/com.jekkle.valheim.nostaminawhensafe.cfg`:

- `SafeRadius` (float, default 25) — meters.
- `AffectSprint` / `AffectJump` / `AffectBuild` / `AffectSwim` (bool, default true) — toggle
  each independently.

## Untested

Not yet launch-tested in game. Next step: fully restart Valheim via
r2modman, confirm the plugin loads (check
`%APPDATA%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\LogOutput.log`
for "NoStaminaWhenSafe 1.0.0 loaded."), and verify stamina behavior in and
out of combat.

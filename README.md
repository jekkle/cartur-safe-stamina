# Cartur's Safe Stamina

BepInEx/Harmony mod for Valheim. Attack, sprint, jump, swim, sneak and
build/repair/remove-piece stamina costs drop to zero whenever no hostile
character is within a configurable radius (default 25m) of the player.

Thunderstore package name `Carturs_Safe_Stamina`, plugin GUID
`com.jekkle.valheim.cartursafestamina`. Was `NoStaminaWhenSafe` before 1.0.0 —
renamed before it was ever published, so there is no old package to migrate.

## How it works

Six Harmony patches:

- `Attack.GetAttackStamina` — postfix zeroes the returned cost. That one return
  feeds both the spend and its `HaveStamina` gate at all three call sites in
  `Attack`, so chopping, mining and weapon swings are covered by it alone. The
  bow's `m_drawStaminaDrain` needs no patch: nothing in `assembly_valheim`
  reads that field, checked instruction by instruction.
- `Player.CheckRun` (sprint drain) — zeroes the `m_runStaminaDrain` field for
  the duration of the original call, then restores it, so skill XP gain and
  other side effects still run.
- `Player.OnJump` — same trick on `m_jumpStaminaUsage`.
- `Player.OnSwimming` — zeroes **both** `m_swimStaminaDrainMinSkill` and
  `m_swimStaminaDrainMaxSkill`, because the drain is a `Mathf.Lerp` between
  them by swim skill; zeroing one end would still leave a cost at other skill
  levels. Swim-skill XP still accrues.
- `Player.GetBuildStamina` — postfix zeroes the returned cost. This single
  method feeds all three build-tool stamina call sites (place, repair, remove).
- `Player.OnSneaking` — zeroes `m_sneakStaminaDrain`. That field is read in
  exactly one place in `assembly_valheim` (`Character.OnSneaking` is an empty
  virtual), and everything downstream of it is multiplicative — the skill lerp,
  the equipment modifier, the status-effect pass — so zeroing it is the whole
  cost. Sneak-skill XP still accrues.

`Attack` runs for every character in the world, not just the player, so the
attack postfix bails unless the attacker is `Player.m_localPlayer`. Without
that guard every hostile inside the safe radius swings for free too — and the
radius is only wide open when nothing hostile is near, which is exactly when a
wandering boar would collect on it. The five `Player` patches need no such
check; they are only reachable through the local player already.

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

## Packaging

```
powershell -ExecutionPolicy Bypass -File tools\pack.ps1
```

Builds Release and writes `dist\Carturs_Safe_Stamina-<version>.zip` with
`manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md` and
`plugins\CarturSafeStamina.dll`. It refuses to pack if `manifest.json` and
`PluginVersion` in `src\Plugin.cs` disagree. `tools\make_icon.py` redraws
`package\icon.png` if the icon needs changing.

## Config

After first run, edit
`BepInEx/config/com.jekkle.valheim.cartursafestamina.cfg`:

- `SafeRadius` (float, default 25) — metres.
- `AffectAttacks` / `AffectSprint` / `AffectJump` / `AffectBuild` /
  `AffectSwim` / `AffectSneak` (bool, default true) — toggle each
  independently.

Every value is read at the call site, so
[ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/)
can change any of them in-game (F1) and it takes effect immediately. Optional —
nothing here depends on it.

## State

Sprint, jump, build and swim are in-game confirmed. `AffectAttacks` and the
rename are built and deployed but not yet launch-tested: restart Valheim,
confirm `Cartur's Safe Stamina 1.0.0 loaded.` in
`%APPDATA%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\LogOutput.log`,
then check a tree costs nothing alone and costs full price with a boar on you.

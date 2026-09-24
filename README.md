# Cartur's Safe Stamina

BepInEx/Harmony mod for Valheim. Attack, sprint, jump, swim, sneak and
build/repair/remove-piece stamina costs drop to zero whenever no hostile
character is within a configurable radius (default 25m) of the player.

Thunderstore package name `Carturs_Safe_Stamina`, plugin GUID
`com.jekkle.valheim.cartursafestamina`. Was `NoStaminaWhenSafe` before 1.0.0 —
renamed before it was ever published, so there is no old package to migrate.

## How it works

Seven Harmony patches:

- `Attack.GetAttackStamina` — postfix zeroes the returned cost. That one return
  feeds both the spend and its `HaveStamina` gate at all three call sites in
  `Attack`, so chopping, mining and weapon swings are covered by it alone.
  Holding a **bow drawn** is deliberately not covered: that cost doesn't come
  through `GetAttackStamina` at all. `ItemData.GetDrawStaminaDrain()` is read
  only by `Player.UpdateAttackBowDraw`, which spends it per tick while the
  string is held. Patching it would be a second patch and a separate decision.
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
- `Player.UpdateStats(float)` — postfix that regenerates stamina on the frames
  vanilla refuses to. `UpdateStats` sets the regen rate to `0f` outright when
  `(IsSwimming() && !IsOnGround()) || InAttack() || InDodge() || m_wallRunning ||
  IsEncumbered()`, and the status-effect pass that follows is a multiply, so
  nothing downstream can revive a zeroed rate. Zeroing a cost therefore left the
  bar flat while swimming or swinging rather than refilling it. The rate is a
  method local — only a transpiler could reach it — so the postfix re-runs that
  one regen line for exactly the blocked frames instead. It stays out of the way
  otherwise: it does nothing unless the regen delay timer has already expired, so
  a real spend still pauses regeneration normally, and it honours the existing
  switches (swimming answers to `AffectSwim`, attacks and dodges to
  `AffectAttacks`). **Encumbrance is not covered** — being over-loaded still
  stops your stamina refilling, because vanilla's encumbered *drain* only runs
  while you're moving (`if (m_moveDir.magnitude > 0.1f)`), so covering the
  regen block would hand out free stamina for standing still under too much
  weight. The ZDO stamina write happens before the postfix, so a remote
  player's view of the bar lags one frame; the local HUD reads `m_stamina`.

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

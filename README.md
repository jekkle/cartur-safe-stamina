# NoStaminaWhenSafe

BepInEx/Harmony mod for Valheim. Sprint, jump, and build/repair/remove-piece
stamina cost drop to zero whenever no hostile character is within a
configurable radius (default 25m) of the player.

## How it works

Three Harmony patches on `Player`:

- `CheckRun` (sprint drain) — zeroes the `m_runStaminaDrain` field for the
  duration of the original call, then restores it, so skill XP gain and
  other side effects still run.
- `OnJump` — same trick on `m_jumpStaminaUsage`.
- `GetBuildStamina` — postfix zeroes the returned cost. This single method
  feeds all three build-tool stamina call sites (place, repair, remove).

Enemy detection uses `Character.GetAllCharacters()` + the game's own
`BaseAI.IsEnemy(a, b)` hostility check (respects faction/tame/aggro state),
cached for 0.25s to avoid re-scanning every frame.

## Build

Requires .NET 8 SDK and a Valheim install with BepInEx already installed.

```
cd src
dotnet build
```

Build auto-copies the DLL into
`<Valheim install>/BepInEx/plugins/NoStaminaWhenSafe/`. If your Valheim
install isn't at the default Steam path, override it:

```
dotnet build -p:VALHEIM_INSTALL="D:\SteamLibrary\steamapps\common\Valheim"
```

## Config

After first run, edit
`BepInEx/config/com.jekkle.valheim.nostaminawhensafe.cfg`:

- `SafeRadius` (float, default 25) — meters.
- `AffectSprint` / `AffectJump` / `AffectBuild` (bool, default true) — toggle
  each independently.

## Untested

Not yet launch-tested in game. Next step: start Valheim, confirm the plugin
loads (check `BepInEx/LogOutput.log` for "NoStaminaWhenSafe 1.0.0 loaded."),
and verify stamina behavior in and out of combat.

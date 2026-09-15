# Changelog

## 1.0.4

- Page only - the mod itself is unchanged from 1.0.3. Adds a link to Cartur's Flooring,
  so every mod of mine points at the others.

## 1.0.3

- Sneaking is now free while safe, on its own `AffectSneak` switch. Crouching
  charged `m_sneakStaminaDrain` every tick even with nothing around, which is
  the one movement cost the mod had missed.
- Documented ConfigurationManager as the easy way to change the settings
  in-game, with a screenshot of the panel.
- Added Follow Command to the links to my other mods.

## 1.0.2

- Moved the links to my other mods to the top of the page.

## 1.0.1

- Added links to my other mods.

## 1.0.0

First release.

Stamina costs drop to zero while no hostile character is within `SafeRadius`
(default 25m), and come back the moment one does. Covers attacks — chopping,
mining and weapon swings — sprint, jump, swim, and build/repair/remove, each
on its own switch.

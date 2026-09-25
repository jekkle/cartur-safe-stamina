using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CarturSafeStamina
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jekkle.valheim.cartursafestamina";
        public const string PluginName = "Cartur's Safe Stamina";
        public const string PluginVersion = "1.0.7";

        public static ConfigEntry<float> SafeRadius;
        public static ConfigEntry<bool> AffectSprint;
        public static ConfigEntry<bool> AffectJump;
        public static ConfigEntry<bool> AffectBuild;
        public static ConfigEntry<bool> AffectSwim;
        public static ConfigEntry<bool> AffectAttacks;
        public static ConfigEntry<bool> AffectSneak;

        private void Awake()
        {
            SafeRadius = Config.Bind("General", "SafeRadius", 25f,
                "No stamina cost for sprint/jump/build/swim/attacks/sneak when no enemy is within this many meters.");
            AffectSprint = Config.Bind("General", "AffectSprint", true, "Free sprint stamina when safe.");
            AffectJump = Config.Bind("General", "AffectJump", true, "Free jump stamina when safe.");
            AffectBuild = Config.Bind("General", "AffectBuild", true, "Free build/repair/remove stamina when safe.");
            AffectSwim = Config.Bind("General", "AffectSwim", true,
                "Free swim stamina when safe. Note this also removes the drowning risk while safe, since drowning only starts once stamina is empty.");
            AffectAttacks = Config.Bind("General", "AffectAttacks", true,
                "Free attack stamina when safe - chopping wood, mining, and weapon swings all pay through the same cost.");
            AffectSneak = Config.Bind("General", "AffectSneak", true, "Free sneak/crouch stamina when safe.");

            Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        // Cache the "is it safe" check per short interval - GetAllCharacters() walks every
        // loaded character, no need to redo that every single frame CheckRun runs.
        private static float _lastCheckTime = -999f;
        private static bool _lastResult = true;
        private const float CheckInterval = 0.25f;

        public static bool IsSafe(Player player)
        {
            if (player == null)
                return true;

            if (Time.time - _lastCheckTime < CheckInterval)
                return _lastResult;

            _lastCheckTime = Time.time;
            _lastResult = true;

            float radius = SafeRadius.Value;
            float sqrRadius = radius * radius;
            Vector3 pos = player.transform.position;

            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == player || c.IsDead())
                    continue;

                if (!BaseAI.IsEnemy(player, c))
                    continue;

                if ((c.transform.position - pos).sqrMagnitude <= sqrRadius)
                {
                    _lastResult = false;
                    break;
                }
            }

            return _lastResult;
        }
    }

    // Sprint: Player.CheckRun drains m_runStaminaDrain * dt each tick it runs.
    // Zero the drain field for the duration of the original call, then restore it,
    // so skill-xp / other side effects in CheckRun still run normally.
    [HarmonyPatch(typeof(Player), "CheckRun")]
    public static class Patch_CheckRun
    {
        private static float _saved;

        static void Prefix(Player __instance, ref float ___m_runStaminaDrain)
        {
            _saved = ___m_runStaminaDrain;
            if (Plugin.AffectSprint.Value && Plugin.IsSafe(__instance))
                ___m_runStaminaDrain = 0f;
        }

        static void Postfix(ref float ___m_runStaminaDrain)
        {
            ___m_runStaminaDrain = _saved;
        }
    }

    // Jump: Player.OnJump drains m_jumpStaminaUsage. Same zero/restore trick.
    [HarmonyPatch(typeof(Player), "OnJump")]
    public static class Patch_OnJump
    {
        private static float _saved;

        static void Prefix(Player __instance, ref float ___m_jumpStaminaUsage)
        {
            _saved = ___m_jumpStaminaUsage;
            if (Plugin.AffectJump.Value && Plugin.IsSafe(__instance))
                ___m_jumpStaminaUsage = 0f;
        }

        static void Postfix(ref float ___m_jumpStaminaUsage)
        {
            ___m_jumpStaminaUsage = _saved;
        }
    }

    // Build/repair/remove-piece all route their stamina cost through GetBuildStamina().
    // Zeroing the return value here covers all three call sites at once.
    [HarmonyPatch(typeof(Player), "GetBuildStamina")]
    public static class Patch_GetBuildStamina
    {
        static void Postfix(Player __instance, ref float __result)
        {
            if (Plugin.AffectBuild.Value && Plugin.IsSafe(__instance))
                __result = 0f;
        }
    }

    // Chopping, mining and every weapon swing pay through Attack.GetAttackStamina. It feeds both
    // the gate and the spend, at all three sites in Attack:
    //
    //     if (attackStamina > 0f && !character.HaveStamina(attackStamina + 0.1f))   // Start
    //     m_character.UseStamina(GetAttackStamina());                               // DoMeleeAttack
    //     ... HaveStamina(attackStamina); UseStamina(attackStamina);                // the held attack
    //
    // so zeroing the return covers all of them, gate included.
    //
    // One stamina cost on a weapon is deliberately left uncovered: holding a bow drawn. It does not
    // come through GetAttackStamina. ItemData.m_shared.m_drawStaminaDrain is read by
    // ItemData.GetDrawStaminaDrain(), whose only caller in assembly_valheim is
    // Player.UpdateAttackBowDraw, and that method spends it per tick while the string is held:
    //
    //     float num = item.GetDrawStaminaDrain();
    //     ... m_seman.ModifyAttackStaminaUsage(num, ref num, true);
    //     UseStamina(num * dt);
    //
    // An earlier version of this comment claimed the field had no consumer at all. It was wrong.
    // Covering the bow draw is a second patch and a separate decision, and that decision has not
    // been made - so drawing a bow still costs stamina inside the safe radius.
    //
    // Attack runs for every character in the world, not just the player, so this checks the
    // attacker is the local player. Without that, every draugr inside the safe radius swings for
    // free too - and the radius is only ever large when nothing hostile is near, which is exactly
    // when a wandering boar would benefit.
    [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
    public static class Patch_GetAttackStamina
    {
        // ___m_character is declared Humanoid on Attack, not Character - injecting it as anything
        // else is asking Harmony to tolerate a widening it has no reason to.
        static void Postfix(ref float __result, Humanoid ___m_character)
        {
            if (!Plugin.AffectAttacks.Value || __result == 0f)
                return;

            if (!(___m_character is Player player) || player != Player.m_localPlayer)
                return;

            if (Plugin.IsSafe(player))
                __result = 0f;
        }
    }

    // Swimming: Player.OnSwimming lerps between m_swimStaminaDrainMinSkill and
    // m_swimStaminaDrainMaxSkill by swim skill, so both ends have to be zeroed for the lerp to
    // produce 0 at any skill level. Letting the original method still run keeps swim-skill XP
    // gain intact.
    //
    // Side effect worth knowing: OnSwimming starts the drown timer only once stamina is empty
    // (`if (!HaveStamina()) m_drownDamageTimer += dt`), so free swim stamina also means no
    // drowning while safe. That's consistent with the mod's intent, and it's why AffectSwim is
    // its own toggle.
    [HarmonyPatch(typeof(Player), "OnSwimming")]
    public static class Patch_OnSwimming
    {
        private static float _savedMin;
        private static float _savedMax;

        static void Prefix(Player __instance, ref float ___m_swimStaminaDrainMinSkill, ref float ___m_swimStaminaDrainMaxSkill)
        {
            _savedMin = ___m_swimStaminaDrainMinSkill;
            _savedMax = ___m_swimStaminaDrainMaxSkill;
            if (Plugin.AffectSwim.Value && Plugin.IsSafe(__instance))
            {
                ___m_swimStaminaDrainMinSkill = 0f;
                ___m_swimStaminaDrainMaxSkill = 0f;
            }
        }

        static void Postfix(ref float ___m_swimStaminaDrainMinSkill, ref float ___m_swimStaminaDrainMaxSkill)
        {
            ___m_swimStaminaDrainMinSkill = _savedMin;
            ___m_swimStaminaDrainMaxSkill = _savedMax;
        }
    }

    // Sneak: Player.OnSneaking is the only thing in assembly_valheim that reads
    // m_sneakStaminaDrain - Character.OnSneaking is an empty virtual, so the field is the
    // single source of the cost. Everything downstream of it in that method is multiplicative:
    //
    //     use  = dt * m_sneakStaminaDrain * Lerp(1, 0.25, sneakSkill)
    //     use += use * GetEquipmentSneakStaminaModifier()
    //     m_seman.ModifySneakStaminaUsage(use, ref use, minZero: true)
    //
    // so zeroing the field makes the equipment term zero too, and the minZero flag on the
    // status-effect pass keeps a negative modifier from pushing it back above 0. Same
    // zero/restore shape as sprint, which leaves the sneak-skill XP at the tail of the method
    // running normally.
    [HarmonyPatch(typeof(Player), "OnSneaking")]
    public static class Patch_OnSneaking
    {
        private static float _saved;

        static void Prefix(Player __instance, ref float ___m_sneakStaminaDrain)
        {
            _saved = ___m_sneakStaminaDrain;
            if (Plugin.AffectSneak.Value && Plugin.IsSafe(__instance))
                ___m_sneakStaminaDrain = 0f;
        }

        static void Postfix(ref float ___m_sneakStaminaDrain)
        {
            ___m_sneakStaminaDrain = _saved;
        }
    }

    // Zeroing a cost is not the same as refilling. Player.UpdateStats(float) kills the regen
    // rate outright for the frames that matter most:
    //
    //     float num = 1f;
    //     if (IsBlocking()) num *= 0.8f;
    //     if ((IsSwimming() && !IsOnGround()) || InAttack() || InDodge() || m_wallRunning || flag) num = 0f;
    //     float num2 = (m_staminaRegen + (1f - m_stamina / maxStamina) * m_staminaRegen * m_staminaRegenTimeMultiplier) * num;
    //     m_seman.ModifyStaminaRegen(ref staminaMultiplier);  // multiplies - cannot revive a zeroed rate
    //
    // (flag is IsEncumbered()). So while swimming or mid-swing the bar sat flat instead of
    // filling, even with the cost patched to 0. num is a method local, and the only way to
    // reach a local is a transpiler; re-running that one regen line in a Postfix for exactly
    // the frames vanilla skipped is smaller and does not break on the next game update.
    //
    // The regen delay timer is left alone: it has already been decremented by the original
    // method, and a value above 0 means something really did spend stamina, so this stays off
    // until vanilla would have started regenerating anyway. The ZDO stamina write happens
    // before this runs, so a remote player's view of the bar lags one frame - the local HUD
    // reads m_stamina directly.
    [HarmonyPatch(typeof(Player), "UpdateStats", new[] { typeof(float) })]
    public static class Patch_UpdateStats
    {
        static void Postfix(Player __instance, float dt, ref float ___m_stamina, float ___m_staminaRegenTimer)
        {
            if (__instance != Player.m_localPlayer || ___m_staminaRegenTimer > 0f)
                return;

            if (!BlockedAndAllowed(__instance) || !Plugin.IsSafe(__instance))
                return;

            float max = __instance.GetMaxStamina();
            if (___m_stamina >= max)
                return;

            float rate = __instance.m_staminaRegen
                         + (1f - ___m_stamina / max) * __instance.m_staminaRegen * __instance.m_staminaRegenTimeMultiplier;

            float multiplier = 1f;
            __instance.GetSEMan().ModifyStaminaRegen(ref multiplier);

            ___m_stamina = Mathf.Min(max, ___m_stamina + rate * multiplier * dt * Game.m_staminaRegenRate);
        }

        // True only on the frames vanilla zeroed the regen rate, and only when the toggle that
        // owns that reason is on - swimming belongs to AffectSwim, attacking and dodging to
        // AffectAttacks. Wall-running has no toggle of its own.
        //
        // Encumbrance used to be on that last line too, on the belief that the encumbered drain
        // runs every frame and so keeps the regen timer above 0, which would have kept this
        // dormant. That belief was wrong. Player.UpdateStats gates the drain on movement:
        //
        //     if (flag)                                   // flag is IsEncumbered()
        //     {
        //         if (m_moveDir.magnitude > 0.1f)
        //             UseStamina(m_encumberedStaminaDrain * dt);
        //         m_seman.AddStatusEffect(...);           // the "Encumbered" effect
        //     }
        //
        // so a player standing still while over-loaded spends nothing, the regen timer runs down
        // to 0, and this postfix then refilled the bar through the bare IsEncumbered() branch -
        // free stamina for standing still under too much weight. Vanilla kills the regen rate for
        // the whole encumbered state, moving or not, and that is the point of being over-loaded.
        // This mod is about enemies being absent, not about carry weight.
        static bool BlockedAndAllowed(Player p)
        {
            if (p.IsSwimming() && !p.IsOnGround())
                return Plugin.AffectSwim.Value;

            if (p.InAttack() || p.InDodge())
                return Plugin.AffectAttacks.Value;

            return p.IsWallRunning();
        }
    }
}

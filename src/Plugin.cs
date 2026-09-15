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
        public const string PluginVersion = "1.0.4";

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
                "No stamina cost for sprint/jump/build/swim/sneak when no enemy is within this many meters.");
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
    // so zeroing the return covers all of them, gate included. Nothing else to reach for: the bow's
    // m_drawStaminaDrain field has no consumer anywhere in assembly_valheim, checked instruction by
    // instruction - it is vestigial in this build, not a second path that was missed.
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
}

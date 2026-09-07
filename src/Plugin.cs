using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NoStaminaWhenSafe
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jekkle.valheim.nostaminawhensafe";
        public const string PluginName = "NoStaminaWhenSafe";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<float> SafeRadius;
        public static ConfigEntry<bool> AffectSprint;
        public static ConfigEntry<bool> AffectJump;
        public static ConfigEntry<bool> AffectBuild;

        private void Awake()
        {
            SafeRadius = Config.Bind("General", "SafeRadius", 25f,
                "No stamina cost for sprint/jump/build when no enemy is within this many meters.");
            AffectSprint = Config.Bind("General", "AffectSprint", true, "Free sprint stamina when safe.");
            AffectJump = Config.Bind("General", "AffectJump", true, "Free jump stamina when safe.");
            AffectBuild = Config.Bind("General", "AffectBuild", true, "Free build/repair/remove stamina when safe.");

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
}

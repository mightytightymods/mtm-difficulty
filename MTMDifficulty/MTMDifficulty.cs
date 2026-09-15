using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;

namespace MTMDifficulty
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class MTMDifficulty : BaseUnityPlugin
    {
        public const string PluginGUID = "mightytightymods.mtm-difficulty";
        public const string PluginName = "MTMDifficulty";
        public const string PluginVersion = "1.0.0";
        
        public static ConfigEntry<float> CreatureHealthMult;
        public static ConfigEntry<float> CreatureDamageMult;
        public static ConfigEntry<float> BossHealthMult;
        public static ConfigEntry<float> BossDamageMult;
        public static ConfigEntry<float> PerStarHealthMult;
        public static ConfigEntry<float> PerStarDamageMult;
        
        public static ConfigEntry<bool> VerboseLogging;


        readonly Harmony harmony = new Harmony(PluginGUID);
        
        internal static ManualLogSource Log;
        
        private void Awake()
        {
            Log = base.Logger;
            
            // When running a server, IsAdminOnly enforces server-authoritative configuration.
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };
            var range = new AcceptableValueRange<float>(0.1f, 5.0f);

            CreatureHealthMult = Config.Bind("Creatures", "HealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all non-boss creature hitpoints. 1.0 = vanilla.", range, adminOnly));

            CreatureDamageMult = Config.Bind("Creatures", "DamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all non-boss creature damage output. 1.0 = vanilla.", range, adminOnly));

            PerStarHealthMult = Config.Bind("Creatures", "PerStarHealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier to creature health added per star level. 1.0 = vanilla star health scaling -> +100% health per star." +
                    "NOTE: This is multiplicative with HealthMultiplier.", range, adminOnly));

            PerStarDamageMult = Config.Bind("Creatures", "PerStarDamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier to creature damage added per star level. 1.0 = vanilla star damage scaling -> +50% damage per star." +
                    "NOTE: This is multiplicative with DamageMultiplier.", range, adminOnly));

            BossHealthMult = Config.Bind("Bosses", "BossHealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to boss health. 1.0 = vanilla.", range, adminOnly));

            BossDamageMult = Config.Bind("Bosses", "BossDamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all boss damage output. 1.0 = vanilla.", range, adminOnly));

            // Unlike the other configs, verbose logging can be enabled client-side by all players, not just admin.
            #if DEBUG
            VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", true,
                new ConfigDescription("Logs detailed per-hit and per-creature scaling calculations."));
            #else
            VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", false,
                new ConfigDescription("Logs detailed per-hit and per-creature scaling calculations."));
            #endif
            
            harmony.PatchAll();
            
            Logger.LogInfo("MTM Difficulty initialized.");
        }
    }
}
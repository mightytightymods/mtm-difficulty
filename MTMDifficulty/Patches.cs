using System.Collections.Generic;
using HarmonyLib;

namespace MTMDifficulty
{
    [HarmonyPatch(typeof(Character), nameof(Character.SetupMaxHealth))]
    public static class CharacterSetupMaxHealthPatch
    {
        static void Postfix(Character __instance)
        {
            if (__instance.IsPlayer())
                return; // never touch player health

            // Character.SetupMaxHealth is called by both Awake and SetLevel, so we can reliably know whether the 
            // creature is 0, 1 or 2-stars.                                                                                                                 
            
            // GetMaxHealthBase() applies Valheim's vanilla World difficulty multipliers, so changes made here are made
            // ON TOP of the builtin changes.
            var baseHealth = __instance.GetMaxHealthBase();
            var stars = __instance.GetLevel() - 1;
            var isBoss = __instance.m_boss;

            var mult = isBoss ? MTMDifficulty.BossHealthMult.Value : MTMDifficulty.CreatureHealthMult.Value;
            // Vanilla health scales at 100% per star
            var starBonus = 1f + (stars * 1.0f * MTMDifficulty.PerStarHealthMult.Value);
            var finalHealth = baseHealth * mult * starBonus;

            __instance.SetMaxHealth(finalHealth);

            if (MTMDifficulty.VerboseLogging.Value)
                MTMDifficulty.Log.LogInfo(
                    $"[HEALTH PATCHED] {__instance.name} | stars={stars} isBoss={isBoss} " +
                    $"baseHealth={baseHealth:F1} mult={mult:F2} starBonus={starBonus:F2} => final={finalHealth:F1}");
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class CharacterDamagePatch
    {
        private struct DamageComponent
        {
            public string Name { get; set; }
            public float Before { get; set; }
            public float After { get; set; }
        }
        
        static void Prefix(Character __instance, ref HitData hit)
        {
            var attacker = hit.GetAttacker();
            if (attacker == null || attacker.IsPlayer())
                return;

            var stars = attacker.GetLevel() - 1;
            var isBoss = attacker.m_boss;

            var mult = isBoss ? MTMDifficulty.BossDamageMult.Value : MTMDifficulty.CreatureDamageMult.Value;
            // Vanilla damage scales at 50% per star, and it has already been applied in the HitData so it needs to be
            // taken into account.
            var vanillaStarMult = 1f + (stars * 0.5f);
            var desiredStarMult = 1f + (stars * 0.5f * MTMDifficulty.PerStarDamageMult.Value);
            var totalMult = mult * (desiredStarMult / vanillaStarMult);

            var before = hit.m_damage;
            hit.ApplyModifier(totalMult);
            var after = hit.m_damage;

            if (MTMDifficulty.VerboseLogging.Value)
            {
                // Log the individual damage components, particularly to highlight that chop and pickaxe are also
                // multiplied--they contribute to the value of HitData.GetTotalDamage(), but aren't usually
                // relevant.
                var components = new[]
                {
                    new DamageComponent { Name = "blunt", Before = before.m_blunt, After = after.m_blunt },
                    new DamageComponent { Name = "slash", Before = before.m_slash, After = after.m_slash },
                    new DamageComponent { Name = "pierce", Before = before.m_pierce, After = after.m_pierce },
                    new DamageComponent { Name = "chop", Before = before.m_chop, After = after.m_chop },
                    new DamageComponent { Name = "pickaxe", Before = before.m_pickaxe, After = after.m_pickaxe },
                    new DamageComponent { Name = "fire", Before = before.m_fire, After = after.m_fire },
                    new DamageComponent { Name = "frost", Before = before.m_frost, After = after.m_frost },
                    new DamageComponent { Name = "lightning", Before = before.m_lightning, After = after.m_lightning },
                    new DamageComponent { Name = "poison", Before = before.m_poison, After = after.m_poison },
                    new DamageComponent { Name = "spirit", Before = before.m_spirit, After = after.m_spirit },
                };
 
                var lines = new List<string>();
                foreach (var c in components)
                {
                    if (c.Before != 0f || c.After != 0f)
                        lines.Add($"  {c.Name}: {c.Before:F1} => {c.After:F1}");
                }
                var breakdown = string.Join("\n", lines);
 
                MTMDifficulty.Log.LogInfo(
                    $"[DMG PATCHED] attacker={attacker.name} stars={stars} isBoss={isBoss} " +
                    $"mult={mult:F2} starBonus={desiredStarMult:F2} totalMult={totalMult:F2}\n" +
                    $"{breakdown}");
            }
        }
    }
}
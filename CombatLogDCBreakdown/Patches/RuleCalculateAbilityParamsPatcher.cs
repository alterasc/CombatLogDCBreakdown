using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.Settings;
using Kingmaker.Settings.Difficulty;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches RuleCalculateAbilityParams to store DC sources information in BreakdownStorage
/// Currently recalculates stat bonus again, because it's easier than writing transpiler
/// So code is just copy-pasted from source method with some additions to store neeeded data
/// </summary>
[HarmonyPatch]
public static class RuleCalculateAbilityParamsPatcher
{
    [HarmonyPatch(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.OnTrigger))]
    [HarmonyPostfix]
    public static void AfterParamsCalculation(RuleCalculateAbilityParams __instance)
    {
        int num = __instance.ReplaceCasterLevel ?? -1;
        int num2 = __instance.ReplaceSpellLevel ?? -1;
        int num3 = __instance.ReplaceDC ?? -1;
        int num4 = __instance.ReplaceConcentration ?? -1;
        StatType statBonusSource = StatType.Unknown;
        if (num < 0)
        {
            Spellbook spellbook = __instance.Spellbook;
            num = ((spellbook != null) ? spellbook.EffectiveCasterLevel : __instance.Initiator.Descriptor.Progression.CharacterLevel);
        }
        num = Math.Max(1, num) + __instance.m_BonusCasterLevel;
        if (num2 < 0)
        {
            num2 = (__instance.m_SpellLevel ?? (num / 2));
            num2 += __instance.m_BonusSpellLevel;
            if (__instance.m_MetamagicData != null)
            {
                num2 -= __instance.m_MetamagicData.SpellLevelCost;
                if (__instance.m_MetamagicData.Has(Metamagic.Heighten))
                {
                    num2 += __instance.m_MetamagicData.HeightenLevel;
                }
            }
        }
        int num5;
        if (__instance.ReplaceStatBonusModifier == null)
        {
            StatType? replaceStat = __instance.ReplaceStat;
            StatType statType;
            if (replaceStat == null)
            {
                Spellbook spellbook2 = __instance.Spellbook;
                statType = ((spellbook2 != null) ? spellbook2.Blueprint.CastingAttribute : StatType.Charisma);
            }
            else
            {
                statType = replaceStat.GetValueOrDefault();
            }
            StatType statType2 = statType;
            if (__instance.ReplaceStat != null && __instance.Initiator.State.Features.DomainsByIntelligenceFeature)
            {
                BlueprintAbility blueprintAbility = __instance.Blueprint as BlueprintAbility;
                if (blueprintAbility != null && blueprintAbility.IsDomainAbility)
                {
                    statType2 = StatType.Intelligence;
                }
            }
            if (statType2 == StatType.BaseAttackBonus)
            {
                num5 = __instance.Initiator.Stats.BaseAttackBonus;
            }
            else
            {
                ModifiableValueAttributeStat modifiableValueAttributeStat = __instance.Initiator.Stats.GetStat<ModifiableValueAttributeStat>(statType2);
                if (modifiableValueAttributeStat == null)
                {
                    PFLog.Default.Error(__instance.Initiator.View, string.Format("Can't use stat for casting: '{0}' ({1})", statType2, __instance.Blueprint.name), Array.Empty<object>());
                    modifiableValueAttributeStat = __instance.Initiator.Stats.Charisma;
                }
                num5 = modifiableValueAttributeStat.Bonus;
            }
            statBonusSource = statType2;
        }
        else
        {
            num5 = __instance.ReplaceStatBonusModifier.Value;
        }
        if (num3 < 0)
        {
            num3 = 10 + num2;
            if (__instance.AbilityData != null && __instance.AbilityData.Spellbook != null && __instance.AbilityData.Spellbook.IsStandaloneMythic)
            {
                num3 += __instance.AbilityData.Caster.Progression.MythicLevel;
            }
            else
            {
                num3 += num5;
            }
        }
        if (!__instance.IgnoreDCBonuses)
        {
            num3 += __instance.m_BonusDC;
        }
        int difficultyBonus = 0;
        int dcCap = 0;
        if (__instance.Initiator.IsPlayersEnemy)
        {
            DifficultyPresetsList.StatsAdjustmentPreset adjustmentPreset = BlueprintRoot.Instance.DifficultyList.GetAdjustmentPreset(SettingsRoot.Difficulty.StatsAdjustments);
            num3 += adjustmentPreset.AbilityDCBonus;
            if (__instance.Blueprint.GetComponent<AbilityDifficultyLimitDC>() != null)
            {
                var num3new = RuleCalculateAbilityParams.LimitDC(num3, adjustmentPreset.AbilityDCLimit, adjustmentPreset.AbilityDCLimitCoeff);
                if (num3new < num3)
                {
                    dcCap = num3new;
                }
                num3 = num3new;
            }
        }

        BreakdownStorage.BreakdownTable.Add(__instance.Result, new DCBreakdown
        {
            BaseDC = __instance.ReplaceDC ?? 10,
            SpellLevel = __instance.Result.SpellLevel,
            StatBonus = num5,
            StatBonusSource = statBonusSource,
            IgnoreDCBonuses = __instance.IgnoreDCBonuses,
            BonusDC = __instance.m_BonusDC,
            EnemyDifficultyBonus = difficultyBonus,
            EnemyDCCap = dcCap
        });
    }
}

using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using System.Runtime.CompilerServices;

namespace CombatLogDCBreakdown;

/// <summary>
/// Data class for storing DC breakdown
/// </summary>
public class DCBreakdown
{
    public readonly int BaseDC;
    public readonly int SpellLevel;
    public readonly int StatBonus;
    public readonly bool IgnoreDCBonuses;
    private readonly ModifiableBonus bonusDC;
    public readonly StatType StatBonusSource;
    public readonly int EnemyDifficultyBonus;
    public readonly int EnemyDCCap;

    public DCBreakdown(int baseDC, int spellLevel, int statBonus, bool ignoreDCBonuses, ModifiableBonus bonusDC, StatType statBonusSource, int enemyDifficultyBonus, int enemyDCCap)
    {
        BaseDC = baseDC;
        SpellLevel = spellLevel;
        StatBonus = statBonus;
        IgnoreDCBonuses = ignoreDCBonuses;
        this.bonusDC = bonusDC;
        StatBonusSource = statBonusSource;
        EnemyDifficultyBonus = enemyDifficultyBonus;
        EnemyDCCap = enemyDCCap;
    }

    public IEnumerable<Modifier> Modifiers => IgnoreDCBonuses ? [] : bonusDC?.Modifiers ?? [];
}

public static class BreakdownStorage
{
    public static ConditionalWeakTable<AbilityParams, DCBreakdown> BreakdownTable = new();
    public static ConditionalWeakTable<RuleSavingThrow, DCBreakdown> RuleSavingThrowTable = new();
    /// <summary>
    /// Secondary bonuses that are applied to RuleSavingThrow, not to calculation in AbilityParams.
    /// Stored separately to not pollute main breakdown object, because for continuous spells multiple
    /// saves (and RuleSavingThrow objects) are made.
    /// </summary>
    public static ConditionalWeakTable<RuleSavingThrow, ModifiableBonus> RuleSavingThrowSecondaryBonusTable = new();
}
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
    public int BaseDC;
    public int SpellLevel;
    public int StatBonus;
    public bool IgnoreDCBonuses;
    public ModifiableBonus BonusDC;
    public ModifiableBonus SecondaryBonusDC;
    public StatType StatBonusSource;
    public int EnemyDifficultyBonus;
    public int EnemyDCCap;
}

public static class BreakdownStorage
{
    public static ConditionalWeakTable<AbilityParams, DCBreakdown> BreakdownTable = new();
    public static ConditionalWeakTable<RuleSavingThrow, DCBreakdown> RuleSavingThrowTable = new();
}
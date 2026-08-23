using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using System.Runtime.CompilerServices;

namespace CombatLogDCBreakdown;

public class DCBreakdown
{
    public int BaseDC;
    public int SpellLevel;
    public int StatBonus;
    public ModifiableBonus BonusDC;
    internal StatType StatBonusSource;
}

public static class BreakdownStorage
{
    public static ConditionalWeakTable<AbilityParams, DCBreakdown> BreakdownTable = new();
    public static ConditionalWeakTable<RuleSavingThrow, DCBreakdown> RuleSavingThrowTable = new();
}
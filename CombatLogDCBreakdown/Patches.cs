using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CombatLogDCBreakdown;

/// <summary>
/// Patches the calls to RuleCalculateAbilityParams.AddBonusDC to use a custom method that adds the bonus DC 
/// with a reference to the source that added it, so we can track it later for the breakdown.
/// </summary>
internal static class CalculateAbilityParamsPatcher
{
    private static MethodInfo _method = AccessTools.Method(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.AddBonusDC));

    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(_method))
            {
                yield return instruction;
            }
            else
            {
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(CalculateAbilityParamsPatcher), nameof(AddBonusDCComp));
                Main.log.Log("Replaced AddBonusDC call!");
            }
        }
    }
    public static void AddBonusDCComp(RuleCalculateAbilityParams evt, int dc, ModifierDescriptor desc, UnitFactComponentDelegate comp)
    {
        var modifier = new Modifier(dc, comp.Fact, desc);
        evt.m_BonusDC ??= new ModifiableBonus();
        evt.m_BonusDC.Add(modifier);
    }
}

/// <summary>
/// Patches the calls to ContextActionSavingThrow.CreateSavingThrow to use a custom method that passes breakdown information 
/// from AbilityParams to RuleSavingThrow
/// </summary>
internal static class SavingThrowCreatePatcher
{
    private static MethodInfo _method = AccessTools.Method(typeof(ContextActionSavingThrow), nameof(ContextActionSavingThrow.CreateSavingThrow));

    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(_method))
            {
                yield return instruction;
            }
            else
            {
                yield return CodeInstruction.Call(typeof(SavingThrowCreatePatcher), nameof(CreateSavingThrowExtended));
                Main.log.Log("Replaced CreateSavingThrow call!");
            }
        }
    }

    public static RuleSavingThrow CreateSavingThrowExtended(ContextActionSavingThrow action, UnitEntityData unit, int dc, bool persistentSpell)
    {
        var result = action.CreateSavingThrow(unit, dc, persistentSpell);
        if (BreakdownStorage.BreakdownTable.TryGetValue(action.Context.Params, out var breakdown))
        {
            BreakdownStorage.RuleSavingThrowTable.Add(result, breakdown);
        }
        return result;
    }
}

/// <summary>
/// Patches creation of RuleSavingThrow in AbilityEffectRunAction 
/// to use a custom method that passes breakdown information
/// </summary>
internal static class AbilityEffectRunActionPatch
{
    public static void CreateSavingThrowExtended(AbilityEffectRunAction __instance, UnitEntityData unit, AbilityExecutionContext context, bool persistentSpell, RuleSavingThrow __result)
    {
        if (BreakdownStorage.BreakdownTable.TryGetValue(context.Params, out var breakdown))
        {
            BreakdownStorage.RuleSavingThrowTable.Add(__result, breakdown);
        }
    }
}

/// <summary>
/// Patches SavingThrowMessage which creates saving throw message log
/// to include DC breakdown information in the log message
/// </summary>
public static class SavingThrowMessagePatcher
{
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions)
            .MatchEndForward(
                CodeMatch.LoadsField(AccessTools.Field(typeof(SavingThrowMessage), nameof(SavingThrowMessage.Tooltip))),
                CodeMatch.Calls(AccessTools.Method(typeof(LocalizedString), "op_Implicit")),
                CodeMatch.Calls(AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.Append), [typeof(string)]))
            )
            .InsertAfter(
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(SavingThrowMessagePatcher), nameof(AppendModifiersBreakdownExtended))
            );
        return matcher.Instructions();
    }

    private static LocalizedString spellLevel = new LocalizedString { m_Key = "6221aa8e-5d21-44d8-9c1f-921e081c4ae3" };
    public static StringBuilder AppendModifiersBreakdownExtended(StringBuilder builder, RuleSavingThrow rule)
    {
        if (BreakdownStorage.RuleSavingThrowTable.TryGetValue(rule, out var breakdown))
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine($"<b>{ModLocalization.DCString} {rule.DifficultyClass}</b>");
            if (breakdown.BaseDC != 0)
            {
                builder.AppendLine($"{UIStrings.Instance.Tooltips.BaseValue}: {breakdown.BaseDC}");
            }
            if (breakdown.SpellLevel > 0)
            {
                builder.Append($"{spellLevel}: ");
                AppendStat(builder, breakdown.SpellLevel);
            }
            if (breakdown.StatBonus != 0)
            {
                builder.Append($"{UIUtility.GetStatText(breakdown.StatBonusSource)}: ");
                AppendStat(builder, breakdown.StatBonus);
            }
            IEnumerable<Modifier> allBonuses = breakdown.BonusDC?.Modifiers ?? new List<Modifier>();
            if (breakdown.SecondaryBonusDC != null)
            {
                allBonuses = allBonuses.Concat(breakdown.SecondaryBonusDC?.Modifiers ?? new List<Modifier>());
            }
            foreach (var modifier in allBonuses)
            {
                try
                {
                    string name;
                    if (modifier.Fact?.SourceItem != null)
                    {
                        name = modifier.Fact?.SourceItem?.Name ?? modifier.Fact?.SourceItem?.Blueprint?.name;
                    }
                    else
                    {
                        name = modifier.Fact?.Name ?? modifier.Fact?.Blueprint?.name;
                    }
                    StatModifiersBreakdown.AppendBonus(builder, modifier.Value, name, modifier.Descriptor, null);
                }
                catch (Exception)
                {
                    builder.AppendLine($"error: {UIUtility.AddSign(modifier.Value)}");
                }
            }
        }
        return builder;
    }

    private static void AppendStat(StringBuilder sb, int bonusValue)
    {
        string value = (bonusValue < 0) ? StatModifiersBreakdown.PenaltyColor : StatModifiersBreakdown.BonusColor;
        sb.Append("<color=#").Append(value).Append('>');
        sb.Append(UIUtility.AddSign(new int?(bonusValue)));
        sb.Append("</color>");
        sb.AppendLine();
    }
}

/// <summary>
/// Patches RuleCalculateAbilityParams to store DC sources information in BreakdownStorage
/// Currently recalculates stat bonus again, because it's easier than writing transpiler
/// </summary>
public static class RuleCalculateAbilityParamsPatcher
{
    public static void AfterParamsCalculation(RuleCalculateAbilityParams __instance)
    {
        int num3 = __instance.ReplaceDC ?? -1;
        int statBonus;
        StatType statBonusSource;
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
            statBonusSource = statType;
            StatType statType2 = statType;
            if (__instance.ReplaceStat != null && __instance.Initiator.State.Features.DomainsByIntelligenceFeature)
            {
                BlueprintAbility blueprintAbility = __instance.Blueprint as BlueprintAbility;
                if (blueprintAbility != null && blueprintAbility.IsDomainAbility)
                {
                    statType2 = StatType.Intelligence;
                    statBonusSource = StatType.Intelligence;
                }
            }
            if (statType2 == StatType.BaseAttackBonus)
            {
                statBonus = __instance.Initiator.Stats.BaseAttackBonus;
            }
            else
            {
                ModifiableValueAttributeStat modifiableValueAttributeStat = __instance.Initiator.Stats.GetStat<ModifiableValueAttributeStat>(statType2);
                if (modifiableValueAttributeStat == null)
                {
                    modifiableValueAttributeStat = __instance.Initiator.Stats.Charisma;
                }
                statBonus = modifiableValueAttributeStat.Bonus;
            }
        }
        else
        {
            statBonus = __instance.ReplaceStatBonusModifier.Value;
            statBonusSource = __instance.ReplaceStat ?? StatType.Unknown;
        }
        if (num3 < 0)
        {
            if (__instance.AbilityData != null && __instance.AbilityData.Spellbook != null && __instance.AbilityData.Spellbook.IsStandaloneMythic)
            {
                statBonusSource = __instance.ReplaceStat ?? StatType.Unknown;
            }
        }

        BreakdownStorage.BreakdownTable.Add(__instance.Result, new DCBreakdown
        {
            BaseDC = __instance.ReplaceDC ?? 10,
            SpellLevel = __instance.Result.SpellLevel,
            StatBonus = statBonus,
            StatBonusSource = statBonusSource,
            BonusDC = __instance.m_BonusDC
        });
    }

    public static void CopyOnClone(AbilityParams __instance, AbilityParams __result)
    {
        if (BreakdownStorage.BreakdownTable.TryGetValue(__instance, out var breakdown))
        {
            BreakdownStorage.BreakdownTable.Add(__result, breakdown);
        }
    }
}

public static class IncreaseCastersSavingThrowTypeDCPatcher
{
    private static MethodInfo _method = AccessTools.Method(typeof(RuleSavingThrow), nameof(RuleSavingThrow.AddBonusDC));
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(_method))
            {
                yield return instruction;
            }
            else
            {
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(IncreaseCastersSavingThrowTypeDCPatcher), nameof(AddBonusDCComp));
                Main.log.Log("Replaced RuleSavingThrow.AddBonusDC call!");
            }
        }
    }
    public static void AddBonusDCComp(RuleSavingThrow rule, int bonus, IncreaseCastersSavingThrowTypeDC comp)
    {
        rule.AddBonusDC(bonus);
        if (BreakdownStorage.RuleSavingThrowTable.TryGetValue(rule, out var breakdown))
        {
            breakdown.SecondaryBonusDC ??= new();
            breakdown.SecondaryBonusDC.Add(new Modifier(bonus, comp.Fact, ModifierDescriptor.UntypedStackable));
        }
    }
}
using HarmonyLib;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using System.Reflection;
using System.Reflection.Emit;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches the calls to RuleCalculateAbilityParams.AddBonusDC to use a custom method that adds the bonus DC 
/// with a reference to the source that added it, so we can track it later for the breakdown.
/// </summary>
[HarmonyPatch]
internal static class AddBonusDCPatches
{
    private static readonly MethodInfo _method = AccessTools.Method(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.AddBonusDC));

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(AbilityFocusParametrized), nameof(AbilityFocusParametrized.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(ArcaneBloodlineArcana), nameof(ArcaneBloodlineArcana.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(ExpandedArsenalMagicSchools), nameof(ExpandedArsenalMagicSchools.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseAllSpellsDC), nameof(IncreaseAllSpellsDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseSpellContextDescriptorDC), nameof(IncreaseSpellContextDescriptorDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseSpellDC), nameof(IncreaseSpellDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseSpellDescriptorDC), nameof(IncreaseSpellDescriptorDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseSpellSchoolDC), nameof(IncreaseSpellSchoolDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(IncreaseSpellSpellbookDC), nameof(IncreaseSpellSpellbookDC.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(SpellFocusParametrized), nameof(SpellFocusParametrized.OnEventAboutToTrigger));
        yield return AccessTools.Method(typeof(AbilityResourceOverride), nameof(AbilityResourceOverride.OnEventAboutToTrigger));
    }

    [HarmonyTranspiler]
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
                yield return CodeInstruction.Call(typeof(AddBonusDCPatches), nameof(AddBonusDCComp));
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


[HarmonyPatch]
internal static class AddBonusDCPatches2
{
    private static readonly MethodInfo _method = AccessTools.Method(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.AddBonusDC));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(IncreaseSpellDC), "<OnEventAboutToTrigger>g__AddDc|12_0")]
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
                yield return CodeInstruction.Call(typeof(AddBonusDCPatches), nameof(AddBonusDCComp));
                Main.log.Log("Replaced AddBonusDC call in IncreaseSpellDC!");
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

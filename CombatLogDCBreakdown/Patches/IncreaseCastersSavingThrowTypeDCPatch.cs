using HarmonyLib;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace CombatLogDCBreakdown.Patches;

[HarmonyPatch(typeof(IncreaseCastersSavingThrowTypeDC), nameof(IncreaseCastersSavingThrowTypeDC.OnEventAboutToTrigger))]
public static class IncreaseCastersSavingThrowTypeDCPatch
{
    private static MethodInfo _method = AccessTools.Method(typeof(RuleSavingThrow), nameof(RuleSavingThrow.AddBonusDC));

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
                yield return CodeInstruction.Call(typeof(IncreaseCastersSavingThrowTypeDCPatch), nameof(AddBonusDCComp));
                Main.log.Log("Replaced RuleSavingThrow.AddBonusDC call!");
            }
        }
    }
    public static void AddBonusDCComp(RuleSavingThrow rule, int bonus, IncreaseCastersSavingThrowTypeDC comp)
    {
        var origBonus = rule.DifficultyClassMod;
        rule.AddBonusDC(bonus);
        //Main.log.Log($"IncreaseCastersSavingThrowTypeDC: Added {bonus} to {rule.StatType} saving throw for {RuntimeHelpers.GetHashCode(rule)}. New mod: {rule.DifficultyClassMod}");
        if (BreakdownStorage.RuleSavingThrowTable.TryGetValue(rule, out _))
        {
            var secondaryBonus = BreakdownStorage.RuleSavingThrowSecondaryBonusTable.GetOrCreateValue(rule);
            secondaryBonus.Add(new Modifier(bonus, comp.Fact, ModifierDescriptor.UntypedStackable));
        }
    }
}

[HarmonyPatch(typeof(RuleSavingThrow), nameof(RuleSavingThrow.DifficultyClassMod), MethodType.Setter)]
public static class RuleSavingThrow_DifficultyMod_Setter_Patch
{
    [HarmonyPostfix]
    public static void After(RuleSavingThrow __instance, int value)
    {
        Main.log.Log($"RuleSavingThrow.DifficultyClassMod setter called for {RuntimeHelpers.GetHashCode(__instance)}. New mod: {value}");
    }
}
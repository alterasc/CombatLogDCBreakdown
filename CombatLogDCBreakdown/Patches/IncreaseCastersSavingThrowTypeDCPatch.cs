using HarmonyLib;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using System.Reflection;
using System.Reflection.Emit;

namespace CombatLogDCBreakdown.Patches;

[HarmonyPatch(typeof(IncreaseCastersSavingThrowTypeDC), nameof(IncreaseCastersSavingThrowTypeDC.OnEventAboutToTrigger))]
public static class IncreaseCastersSavingThrowTypeDCPatch
{
    private static MethodInfo _method = AccessTools.Method(typeof(RuleSavingThrow), nameof(RuleSavingThrow.AddBonusDC));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var patched = false;
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
                Main.log.Log($"Replaced RuleSavingThrow.AddBonusDC call in {__originalMethod.ShortDescription()}");
                patched = true;
            }
        }
        if (!patched)
        {
            Helpers.ReportTranspilerFailure(__originalMethod);
        }
    }
    public static void AddBonusDCComp(RuleSavingThrow rule, int bonus, IncreaseCastersSavingThrowTypeDC comp)
    {
        rule.AddBonusDC(bonus);
        if (BreakdownStorage.RuleSavingThrowTable.TryGetValue(rule, out _))
        {
            var secondaryBonus = BreakdownStorage.RuleSavingThrowSecondaryBonusTable.GetOrCreateValue(rule);
            secondaryBonus.Add(new Modifier(bonus, comp.Fact, ModifierDescriptor.UntypedStackable));
        }
    }
}
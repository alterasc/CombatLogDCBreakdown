using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics;
using System.Reflection;
using System.Reflection.Emit;

namespace CombatLogDCBreakdown.Patches;

/// <summary>
/// Patches creation of RuleSavingThrow in MechanicsContext.TryAffectByShadow
/// to use a custom method that passes breakdown information
/// </summary>
[HarmonyPatch(typeof(MechanicsContext), nameof(MechanicsContext.TryAffectByShadow))]
internal static class ShadowDisbelieveSavePatcher
{
    private static readonly ConstructorInfo _constructor =
        AccessTools.Constructor(typeof(RuleSavingThrow), [typeof(UnitEntityData), typeof(SavingThrowType), typeof(int)]);

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallReplacingTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var patched = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode != OpCodes.Newobj || !_constructor.Equals(instruction.operand))
            {
                yield return instruction;
            }
            else
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0); // MechanicsContext this
                yield return CodeInstruction.Call(typeof(ShadowDisbelieveSavePatcher), nameof(CreateSavingThrowExtended));
                Main.log.Log($"Replaced RuleSavingThrow constructor call in {__originalMethod.ShortDescription()}!");
                patched = true;
            }
        }
        if (!patched)
        {
            Helpers.ReportTranspilerFailure(__originalMethod);
        }
    }

    public static RuleSavingThrow CreateSavingThrowExtended(UnitEntityData unit, SavingThrowType saveType, int difficultyClass, MechanicsContext context)
    {
        var result = new RuleSavingThrow(unit, saveType, difficultyClass);
        if (BreakdownStorage.BreakdownTable.TryGetValue(context.Params, out var breakdown))
        {
            BreakdownStorage.RuleSavingThrowTable.Add(result, breakdown);
        }
        return result;
    }
}

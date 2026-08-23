using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System.Reflection;
using UnityModManagerNet;

namespace CombatLogDCBreakdown;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        log = modEntry.Logger;
#if DEBUG
        modEntry.OnUnload = OnUnload;
#endif
        modEntry.OnGUI = OnGUI;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

        var callReplacer = new HarmonyMethod(typeof(CalculateAbilityParamsPatcher), nameof(CalculateAbilityParamsPatcher.CallReplacingTranspiler));

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(AbilityFocusParametrized), nameof(AbilityFocusParametrized.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(ArcaneBloodlineArcana), nameof(ArcaneBloodlineArcana.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(ExpandedArsenalMagicSchools), nameof(ExpandedArsenalMagicSchools.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseAllSpellsDC), nameof(IncreaseAllSpellsDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseSpellContextDescriptorDC), nameof(IncreaseSpellContextDescriptorDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseSpellDC), nameof(IncreaseSpellDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseSpellDescriptorDC), nameof(IncreaseSpellDescriptorDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseSpellSchoolDC), nameof(IncreaseSpellSchoolDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(IncreaseSpellSpellbookDC), nameof(IncreaseSpellSpellbookDC.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(SpellFocusParametrized), nameof(SpellFocusParametrized.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(AbilityResourceOverride), nameof(AbilityResourceOverride.OnEventAboutToTrigger)),
            transpiler: callReplacer);

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.OnTrigger)),
            postfix: new HarmonyMethod(typeof(RuleCalculateAbilityParamsPatcher), nameof(RuleCalculateAbilityParamsPatcher.AfterParamsCalculation)));

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(ContextActionSavingThrow), nameof(ContextActionSavingThrow.RunAction)),
            transpiler: new HarmonyMethod(typeof(SavingThrowCreatePatcher), nameof(SavingThrowCreatePatcher.CallReplacingTranspiler)));

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(AbilityParams), nameof(AbilityParams.Clone)),
            postfix: new HarmonyMethod(typeof(RuleCalculateAbilityParamsPatcher), nameof(RuleCalculateAbilityParamsPatcher.CopyOnClone)));

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(AbilityEffectRunAction), nameof(AbilityEffectRunAction.CreateSavingThrow)),
            postfix: new HarmonyMethod(typeof(AbilityEffectRunActionPatch), nameof(AbilityEffectRunActionPatch.CreateSavingThrowExtended)));

        HarmonyInstance.Patch(
            original: AccessTools.Method(typeof(SavingThrowMessage), nameof(SavingThrowMessage.GetData)),
            transpiler: new HarmonyMethod(typeof(SavingThrowMessagePatcher), nameof(SavingThrowMessagePatcher.CallReplacingTranspiler)));

        return true;
    }

    public static void OnGUI(UnityModManager.ModEntry modEntry)
    {

    }

#if DEBUG
    public static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
#endif
}

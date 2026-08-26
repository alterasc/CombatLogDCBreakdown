using HarmonyLib;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Utility;

namespace CombatLogDCBreakdown;

/// <summary>
/// Mod localization
/// </summary>
[HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.LoadPack), typeof(Locale))]
internal static class ModLocalization
{
    private readonly static Dictionary<string, string> translations = new()
    {
            {"enGB", "Difficulty (DC):"},
            {"deDE", "Schwierigkeitsgrad (SG):"},
            {"esES", "Dificultad (CD):"},
            {"frFR", "Difficulté (DD) :"},
            {"itIT", "Difficoltà (CD):"},
            {"ptBR", "Dificuldade (CD):"},
            {"ruRU", "Сложность:"},
            {"zhCN", "难度（DC）："},
    };

    public static LocalizedString DCString = new() { m_Key = "293aa85a-cc72-4e40-a658-6961ff23dc23" };

    [HarmonyPostfix]
    public static void Init(LocalizationPack __result, Locale locale)
    {
        var name = translations.Get(locale.ToString()) ?? translations["enGB"];
        __result.PutString(DCString.m_Key, name);
    }
}

using HarmonyLib;
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
    internal static bool IsTTTEnabled = false;
    internal static Settings Settings;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        log = modEntry.Logger;
        Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        modEntry.OnSaveGUI = Settings.Save;
        modEntry.OnGUI = Settings.OnGUI;
#if DEBUG
        modEntry.OnUnload = OnUnload;
#endif
        IsTTTEnabled = UnityModManager.modEntries.Any(mod => mod.Info.Id.Equals("TabletopTweaks-Base") && mod.Enabled);
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        try
        {
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch
        {
            HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
            throw;
        }
        return true;
    }

#if DEBUG
    public static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
#endif
}
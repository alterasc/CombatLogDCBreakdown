using HarmonyLib;
using System.Reflection;
using System.Text;

namespace CombatLogDCBreakdown;

internal static class Helpers
{
    internal static void ReportTranspilerFailure(MethodBase original)
    {
        Main.log.Log($"Failed to patch {original.ShortDescription()}.");
        PrintAppliedPatches(original);
    }

    internal static void PrintAppliedPatches(MethodBase original)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Patches currently applied to {original.FullDescription()}:");
        var info = Harmony.GetPatchInfo(original);
        if (info == null || (info.Prefixes.Count == 0 && info.Postfixes.Count == 0 && info.Transpilers.Count == 0 && info.Finalizers.Count == 0))
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var prefix in info.Prefixes)
                sb.AppendLine($"  Prefix  : {prefix.owner} -> {prefix.PatchMethod.FullDescription()} (priority: {prefix.priority}, before: {string.Join(",", prefix.before ?? [])}, after: {string.Join(",", prefix.after ?? [])})");
            foreach (var postfix in info.Postfixes)
                sb.AppendLine($"  Postfix: {postfix.owner} -> {postfix.PatchMethod.FullDescription()} (priority: {postfix.priority}, before: {string.Join(",", postfix.before ?? [])}, after: {string.Join(",", postfix.after ?? [])})");
            foreach (var transpiler in info.Transpilers)
                sb.AppendLine($"  Transpiler: {transpiler.owner} -> {transpiler.PatchMethod.FullDescription()} (priority: {transpiler.priority})");
            foreach (var finalizer in info.Finalizers)
                sb.AppendLine($"  Finalizer: {finalizer.owner} -> {finalizer.PatchMethod.FullDescription()} (priority: {finalizer.priority})");
        }
        Main.log.Log(sb.ToString().TrimEnd());
    }

    internal static string ShortDescription(this MethodBase method)
    {
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }
}
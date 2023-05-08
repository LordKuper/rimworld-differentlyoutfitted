using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace DifferentlyOutfitted.Patches
{
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw")]
    public static class ApparelScorePatch
    {
        [SuppressMessage("ReSharper", "RedundantAssignment"), SuppressMessage("ReSharper", "InconsistentNaming"),
         UsedImplicitly]
        private static bool Prefix(ref float __result, Pawn pawn, Apparel ap)
        {
            __result = ApparelScoreCalculator.ApparelScoreRaw(pawn, ap);
            return false;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Outfitted;
using RimWorld;
using Verse;

namespace DifferentlyOutfitted.Patches
{
    [HarmonyPatch(typeof(OutfittedMod), nameof(OutfittedMod.ApparelScoreRaw))]
    public static class OutfittedModPatch
    {
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [UsedImplicitly]
        private static bool Prefix(ref float __result, Pawn pawn, Apparel apparel)
        {
            __result = ApparelScoreCalculator.ApparelScoreRaw(pawn, apparel);
            return false;
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace DifferentlyOutfitted.Patches
{
    public static class OutfittedOverlayPatch
    {
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [UsedImplicitly]
        internal static bool Prefix(ref List<float> __result, Pawn pawn)
        {
            __result = pawn.apparel.WornApparel.Select(apparel => ApparelScoreCalculator.ApparelScoreRaw(pawn, apparel))
                .ToList();
            return false;
        }
    }
}
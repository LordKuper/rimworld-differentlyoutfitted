using System.Reflection;
using DifferentlyOutfitted.Patches;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DifferentlyOutfitted
{
    [UsedImplicitly]
    public class DifferentlyOutfittedMod : Mod
    {
        public DifferentlyOutfittedMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("LordKuper.DifferentlyOutfitted");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("Outfitted.Thing_DrawGUIOverlay_Patch"), "ScoresForPawn"),
                new HarmonyMethod(typeof(OutfittedOverlayPatch), nameof(OutfittedOverlayPatch.Prefix)));
        }
    }
}
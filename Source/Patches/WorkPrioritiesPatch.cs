using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Outfitted;
using Verse;

namespace DifferentlyOutfitted.Patches
{
    [HarmonyPatch(typeof(WorkPriorities))]
    public static class WorkPrioritiesPatch
    {
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [UsedImplicitly]
        [HarmonyPatch("DefaultPriorities")]
        [HarmonyPrefix]
        private static bool DefaultPrioritiesPrefix(ref List<StatPriority> __result, WorkTypeDef worktype)
        {
            if (worktype == WorkTypeDefOf.Art)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.ArtWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.BasicWorker)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.BaseWorkerStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Cleaning)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.CleaningWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Cooking)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.CookingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Construction)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.ConstructionWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Crafting)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.CraftingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Doctor)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.DoctorWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Firefighter)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.FirefighterWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Growing)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.GrowingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Handling)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.HandlingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Hauling)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.HaulingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Hunting)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.HuntingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Mining)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.MiningWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.PlantCutting)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.PlantCuttingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Research)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.ResearchWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Smithing)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.SmithingWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Tailoring)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.TailoringWorkTypeStatPriorities);
                return false;
            }
            if (worktype == WorkTypeDefOf.Warden)
            {
                __result = new List<StatPriority>(DefaultWorkTypePriorities.WardenWorkTypeStatPriorities);
                return false;
            }
            return true;
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [UsedImplicitly]
        [HarmonyPatch(nameof(WorkPriorities.WorktypeStatPriorities), typeof(Pawn))]
        [HarmonyPrefix]
        private static bool WorktypeStatPrioritiesPrefix(ref List<StatPriority> __result, Pawn pawn)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            var workTypePriorities = new Dictionary<WorkTypeDef, int>();
            var workTypeWeights = new Dictionary<WorkTypeDef, IEnumerable<StatPriority>>();
            foreach (var workType in workTypes)
            {
                var priority = pawn.workSettings?.GetPriority(workType) ?? 0;
                if (priority <= 0) { continue; }
                workTypePriorities.Add(workType, priority);
                workTypeWeights.Add(workType, WorkPriorities.WorktypeStatPriorities(workType));
            }
            if (!workTypePriorities.Any()) { __result = new List<StatPriority>(); }
            else
            {
                var priorityRange = new IntRange(workTypePriorities.Min(s => s.Value),
                    workTypePriorities.Max(s => s.Value));
                var weightedPriorities = new List<StatPriority>();
                foreach (var workTypePriority in workTypePriorities)
                {
                    var normalizedWorkPriority = priorityRange.min == priorityRange.max
                        ? 1f
                        : 1f - (float) (workTypePriority.Value - priorityRange.min) /
                        (priorityRange.max + 1 - priorityRange.min);
                    weightedPriorities.AddRange(workTypeWeights[workTypePriority.Key].Select(statPriority =>
                        new StatPriority(statPriority.Stat, statPriority.Weight * normalizedWorkPriority)));
                }
                __result = weightedPriorities.Select(o => o.Stat).Distinct().Select(stat =>
                        new StatPriority(stat, weightedPriorities.Where(o => o.Stat == stat).Average(o => o.Weight)))
                    .ToList();
            }
            return false;
        }
    }
}
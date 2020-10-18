using System;
using System.Collections.Generic;
using System.Linq;
using Outfitted;
using Verse;

namespace DifferentlyOutfitted
{
    internal static class StatPriorityHelper
    {
        public static IEnumerable<StatPriority> CalculateStatPriorities(Pawn pawn,
            IEnumerable<StatPriority> statPriorities, bool autoWorkPriorities)
        {
            var originalStatPriorities = statPriorities.ToList();
            if (!Enumerable.Any(originalStatPriorities)) { return originalStatPriorities; }
            #if DEBUG
            Log.Message($"DifferentlyOutfitted: Normalizing stat priorities (autoWorkPriorities = {autoWorkPriorities})",
                true);
            #endif
            var normalizedStatPriorities = originalStatPriorities
                .Select(statPriority => new StatPriority(statPriority.Stat, statPriority.Weight)).ToList();
            List<StatPriority> workStatPriorities = null;
            if (autoWorkPriorities)
            {
                workStatPriorities = WorkPriorities.WorktypeStatPriorities(pawn).ToList();
                foreach (var workStatPriority in workStatPriorities)
                {
                    var sourceStatPriority = normalizedStatPriorities.Find(o => o.Stat == workStatPriority.Stat);
                    if (sourceStatPriority == null)
                    {
                        normalizedStatPriorities.Add(new StatPriority(workStatPriority.Stat, workStatPriority.Weight));
                    }
                    else { sourceStatPriority.Weight = (sourceStatPriority.Weight + workStatPriority.Weight) / 2; }
                }
            }
            NormalizeStatPriorities(normalizedStatPriorities);
            #if DEBUG
            Log.Message("DifferentlyOutfitted: Normalized stat priorities -----", true);
            foreach (var statPriority in normalizedStatPriorities)
            {
                var originalWeight = originalStatPriorities.Find(o => o.Stat == statPriority.Stat)?.Weight ?? 0;
                var workWeight = autoWorkPriorities
                    ? workStatPriorities.Find(o => o.Stat == statPriority.Stat)?.Weight ?? 0
                    : 0;
                Log.Message(
                    $"DifferentlyOutfitted: {statPriority.Stat.defName} = {statPriority.Weight} ({originalWeight} original) ({workWeight} work)",
                    true);
            }
            Log.Message("DifferentlyOutfitted: ------------------------------", true);
            #endif
            return normalizedStatPriorities;
        }

        private static void NormalizeStatPriorities(ICollection<StatPriority> statPriorities)
        {
            if (statPriorities == null) { throw new ArgumentNullException(nameof(statPriorities)); }
            if (!statPriorities.Any()) { return; }
            var weightSum = statPriorities.Sum(priority => Math.Abs(priority.Weight));
            foreach (var statPriority in statPriorities)
            {
                statPriority.Weight *= ApparelScoreCalculator.ApparelTotalStatWeight / weightSum;
            }
        }
    }
}
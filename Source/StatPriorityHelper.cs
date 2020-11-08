using System;
using System.Collections.Generic;
using System.Linq;
using Outfitted;
using RimWorld;
using Verse;

namespace DifferentlyOutfitted
{
    internal static class StatPriorityHelper
    {
        private const double Tolerance = 0.01;

        public static IEnumerable<StatPriority> CalculateStatPriorities(Pawn pawn,
            IEnumerable<StatPriority> outfitStatPriorities, bool autoWorkPriorities)
        {
            var originalStatPriorities = outfitStatPriorities.ToList();
            if (!originalStatPriorities.Any() && !autoWorkPriorities) { return originalStatPriorities; }
            var normalizedStatPriorities = originalStatPriorities
                .Select(statPriority => new StatPriority(statPriority.Stat, statPriority.Weight)).ToList();
            if (autoWorkPriorities)
            {
                var workStatPriorities = WorkPriorities.WorktypeStatPriorities(pawn).ToList();
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

        public static void SetDefaultStatPriority(ICollection<StatPriority> priorities, StatDef stat,
            float defaultWeight)
        {
            var priority = priorities.FirstOrDefault(o => o.Stat == stat);
            if (priority != null)
            {
                if (Math.Abs(priority.Weight - priority.Default) < Tolerance) { priority.Weight = defaultWeight; }
                priority.Default = defaultWeight;
            }
            else { priorities.Add(new StatPriority(stat, defaultWeight, defaultWeight)); }
        }

        public static void SetDefaultStatPriority(ICollection<StatPriority> priorities, string name, float weight)
        {
            var stat = StatDef.Named(name);
            if (stat == null)
            {
                Log.Message($"DifferentlyOutfitted: Could not find apparel stat named '{name}'");
                return;
            }
            SetDefaultStatPriority(priorities, stat, weight);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DifferentlyOutfitted
{
    internal static class OutfitStatHelper
    {
        private const double Tolerance = 0.01;
        public static readonly Dictionary<StatDef, FloatRange> StatRanges = new Dictionary<StatDef, FloatRange>();

        private static FloatRange CalculateStatRange(StatDef stat)
        {
            var statRange = FloatRange.Zero;
            var apparelFilter = new ThingFilter();
            apparelFilter.SetAllow(ThingCategoryDefOf.Apparel, true);
            var apparels = ThingCategoryNodeDatabase.RootNode.catDef.DescendantThingDefs
                .Where(t => apparelFilter.Allows(t) && !apparelFilter.IsAlwaysDisallowedDueToSpecialFilters(t)).ToList()
                .Where(a => a.statBases != null && a.StatBaseDefined(stat) ||
                            a.equippedStatOffsets != null && a.equippedStatOffsets.Any(o => o.stat == stat)).ToList();
            if (apparels.Any())
            {
                foreach (var apparel in apparels)
                {
                    var statBase = apparel.statBases?.Find(sm => sm.stat == stat);
                    var baseStatValue = statBase?.value ?? stat.defaultBaseValue;
                    float statOffsetValue = 0;
                    var statOffset = apparel.equippedStatOffsets?.Find(sm => sm.stat == stat);
                    if (statOffset != null) { statOffsetValue = statOffset.value; }
                    var totalStatValue = baseStatValue + statOffsetValue - stat.defaultBaseValue;
                    if (Math.Abs(statRange.min) < Tolerance && Math.Abs(statRange.max) < Tolerance)
                    {
                        statRange.min = totalStatValue;
                        statRange.max = totalStatValue;
                    }
                    else
                    {
                        if (statRange.min > totalStatValue) { statRange.min = totalStatValue; }
                        if (statRange.max < totalStatValue) { statRange.max = totalStatValue; }
                    }
                }
            }
            else
            {
                statRange.min = stat.defaultBaseValue;
                statRange.max = stat.defaultBaseValue;
            }
            StatRanges.Add(stat, statRange);
            return statRange;
        }

        public static float NormalizeStatValue(StatDef stat, float value)
        {
            var statRange = StatRanges.ContainsKey(stat) ? StatRanges[stat] : CalculateStatRange(stat);
            var valueDeviation = value - stat.defaultBaseValue;
            if (Math.Abs(statRange.min - statRange.max) < Tolerance)
            {
                statRange.min = valueDeviation;
                statRange.max = valueDeviation;
                return 0f;
            }
            if (statRange.min > valueDeviation) { statRange.min = valueDeviation; }
            if (statRange.max < valueDeviation) { statRange.max = valueDeviation; }
            if (Math.Abs(valueDeviation) < Tolerance) { return 0; }
            if (statRange.min < 0 && statRange.max < 0)
            {
                return -1 + (valueDeviation - statRange.min) / (statRange.max - statRange.min);
            }
            if (statRange.min < 0 && statRange.max > 0)
            {
                return -1 + 2 * ((valueDeviation - statRange.min) / (statRange.max - statRange.min));
            }
            return (valueDeviation - statRange.min) / (statRange.max - statRange.min);
        }
    }
}
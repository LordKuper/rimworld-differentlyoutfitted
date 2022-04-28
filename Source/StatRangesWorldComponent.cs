using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DifferentlyOutfitted
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class StatRangesWorldComponent : WorldComponent
    {
        private const double Tolerance = 0.001;
        public Dictionary<StatDef, FloatRange> StatValues = new Dictionary<StatDef, FloatRange>();

        public StatRangesWorldComponent(World world) : base(world)
        {
#if DEBUG
            Log.Message("DifferentlyOutfitted: StatRanges world component created.");
#endif
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref StatValues, nameof(StatValues), LookMode.Def, LookMode.Value);
        }

        public float NormalizeStatValue(StatDef stat, float value)
        {
            if (StatValues == null) { StatValues = new Dictionary<StatDef, FloatRange>(); }
            if (!StatValues.ContainsKey(stat))
            {
#if DEBUG
                Log.Message($"DifferentlyOutfitted: Initializing StatValues for '{stat.defName}'.");
#endif
                StatValues[stat] = new FloatRange(value, value);
            }
            var range = StatValues[stat];
            if (range.min > value)
            {
#if DEBUG
                Log.Message(
                    $"DifferentlyOutfitted: Updating StatMin for '{stat.defName}' from {range.min:N2} to {value:N2}.");
#endif
                range.min = value;
                StatValues[stat] = range;
            }
            if (range.max < value)
            {
#if DEBUG
                Log.Message(
                    $"DifferentlyOutfitted: Updating StatMax for '{stat.defName}' from {range.max:N2} to {value:N2}.");
#endif
                range.max = value;
                StatValues[stat] = range;
            }
            if (Math.Abs(range.max - range.min) < Tolerance) { return 0f; }
            if (range.min < 0 && range.max < 0) { return -1 + (value - range.min) / (range.max - range.min); }
            if (range.min < 0 && range.max > 0) { return -1 + 2 * ((value - range.min) / (range.max - range.min)); }
            return (value - range.min) / (range.max - range.min);
        }
    }
}
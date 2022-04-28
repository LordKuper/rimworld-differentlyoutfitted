using System;
using System.Collections.Generic;
using System.Linq;
using Outfitted;
using RimWorld;
using Verse;
using StatDefOf = RimWorld.StatDefOf;

namespace DifferentlyOutfitted
{
    public static class ApparelScoreCalculator
    {
        private const float AllowedApparelScoreBonus = 100f;
        public const float ApparelTotalStatWeight = 10;
        private const float HumanLeatherScoreBonus = 0.2f;
        private const float HumanLeatherScoreFactor = 0.2f;
        private const float HumanLeatherScorePenalty = 1.0f;
        private const float IncorrectGenderApparelScoreFactor = 0.01f;
        private const float LowQualityApparelScoreFactor = 0.25f;
        private const float MaxInsulationScore = 2;
        private const float RequiredApparelScoreBonus = 1000f;
        private const float SlaveApparelScorePenalty = 1.0f;
        private const float TaintedApparelScoreFactor = 0.2f;
        private const float TaintedApparelScorePenalty = 1.0f;
        private const float VisionBlockingApparelScorePenalty = 1.0f;

        private static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.25f, 0.1f),
            new CurvePoint(0.5f, 0.25f),
            new CurvePoint(0.75f, 1f)
        };

        private static readonly SimpleCurve InsulationScoreCurve = new SimpleCurve
        {
            new CurvePoint(-10f, -MaxInsulationScore),
            new CurvePoint(-5f, -6f * MaxInsulationScore),
            new CurvePoint(0f, 0),
            new CurvePoint(5f, 0.6f * MaxInsulationScore),
            new CurvePoint(10f, MaxInsulationScore)
        };

        private static StatRangesWorldComponent StatRanges => Find.World.GetComponent<StatRangesWorldComponent>();

        public static float ApparelScoreRaw(Pawn pawn, Apparel apparel)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (apparel == null) { throw new ArgumentNullException(nameof(apparel)); }
            if (!(pawn.outfits.CurrentOutfit is ExtendedOutfit outfit))
            {
                Log.ErrorOnce("DifferentlyOutfitted: Not an ExtendedOutfit, something went wrong.", 399441);
                return 0f;
            }
#if DEBUG
            Log.Message($"DifferentlyOutfitted: ----- '{pawn.Name}' - '{apparel.def.defName}' ({apparel.Label}) -----");
#endif
            var statPriorities =
                StatPriorityHelper.CalculateStatPriorities(pawn, outfit.StatPriorities, outfit.AutoWorkPriorities);
            var score = 0.1f + apparel.def.apparel.scoreOffset + ApparelScoreRawPriorities(apparel, statPriorities);
            ApplyHitPointsScoring(apparel, ref score);
            ApplySpecialScoring(apparel, ref score);
            ApplyVisionBlockingScoring(apparel, ref score);
            ApplyInsulationScoring(pawn, apparel, outfit, ref score);
            ApplyTaintedScoring(pawn, apparel, outfit, ref score);
            ApplyHumanLeatherScoring(pawn, apparel, ref score);
            ApplyGenderScoring(pawn, apparel, ref score);
            ApplyRequirementScoring(pawn, apparel, ref score);
            ApplyQualityScoring(pawn, apparel, ref score);
            ApplySlaveScoring(pawn, apparel, ref score);
#if DEBUG
            Log.Message($"DifferentlyOutfitted: Total score of '{apparel.Label}' for pawn '{pawn.Name}' = {score:N2}");
            Log.Message("DifferentlyOutfitted: -----------------------------------------------------------------");
#endif
            return score;
        }

        private static float ApparelScoreRawPriorities(Thing apparel, IEnumerable<StatPriority> statPriorities)
        {
            var statScores = new Dictionary<StatDef, float>();
            foreach (var statPriority in statPriorities)
            {
                if (statScores.ContainsKey(statPriority.Stat)) { continue; }
                var statValue = apparel.GetStatValue(statPriority.Stat);
                var statOffset = apparel.def.equippedStatOffsets.GetStatOffsetFromList(statPriority.Stat);
                var totalValue = statValue + statOffset;
                var valueDeviation = totalValue - statPriority.Stat.defaultBaseValue;
                var normalizedValue = StatRanges.NormalizeStatValue(statPriority.Stat, valueDeviation);
                var statScore = normalizedValue * statPriority.Weight;
                statScores.Add(statPriority.Stat, statScore);
#if DEBUG
                if (Math.Abs(statScore) > 0.01)
                {
                    var range = StatRanges.StatValues[statPriority.Stat];
                    Log.Message(
                        $"DifferentlyOutfitted: Value of '{statPriority.Stat}' ({statPriority.Weight:N2}) [{range.min:N2},{range.max:N2}] = {statValue:N2} + {statOffset:N2} = {totalValue:N2} ({valueDeviation:N2} dev) ({normalizedValue:N2} norm) ({statPriority.Stat.defaultBaseValue:N2} def) ({statScore:N2} score)");
                }
#endif
            }
            var apparelScore = !statScores.Any() ? 0 : statScores.Sum(pair => pair.Value);
#if DEBUG
            Log.Message($"DifferentlyOutfitted: Stat score of {apparel.Label} = {apparelScore:N2}");
#endif
            return apparelScore;
        }

        private static void ApplyGenderScoring(Pawn pawn, Thing apparel, ref float score)
        {
            if (!apparel.def.apparel.CorrectGenderForWearing(pawn.gender))
            {
                score *= IncorrectGenderApparelScoreFactor;
            }
        }

        private static void ApplyHitPointsScoring(Thing apparel, ref float score)
        {
            if (!apparel.def.useHitPoints) { return; }
            var hitPointsScoreCoefficient =
                HitPointsPercentScoreFactorCurve.Evaluate((float) apparel.HitPoints / apparel.MaxHitPoints);
#if DEBUG
            Log.Message($"DifferentlyOutfitted: Hit point score coefficient = {hitPointsScoreCoefficient:N2}");
#endif
            score *= hitPointsScoreCoefficient;
        }

        private static void ApplyHumanLeatherScoring(Pawn pawn, Thing apparel, ref float score)
        {
            if (apparel.Stuff != ThingDefOf.Human.race.leatherDef) { return; }
            if (ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelSad))
            {
#if DEBUG
                Log.Message("DifferentlyOutfitted: Penalizing human leather apparel");
#endif
                score -= HumanLeatherScorePenalty;
                if (score > 0f) { score *= HumanLeatherScoreFactor; }
            }
            if (ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelHappy))
            {
#if DEBUG
                Log.Message("DifferentlyOutfitted: Promoting human leather apparel");
#endif
                score += HumanLeatherScoreBonus;
            }
        }

        private static void ApplyInsulationScoring(Pawn pawn, Apparel apparel, ExtendedOutfit outfit, ref float score)
        {
#if DEBUG
            Log.Message("DifferentlyOutfitted: Calculating scores for insulation");
#endif
            if (pawn.apparel.WornApparel.Contains(apparel)) { return; }
            var currentRange = pawn.ComfortableTemperatureRange();
            var candidateRange = currentRange;
            if (outfit.AutoTemp)
            {
                var seasonalTemp = pawn.Map.mapTemperature.SeasonalTemp;
                outfit.targetTemperatures = new FloatRange(seasonalTemp - outfit.autoTempOffset,
                    seasonalTemp + outfit.autoTempOffset);
            }
            var targetRange = outfit.targetTemperatures;
            candidateRange.min += -apparel.GetStatValue(StatDefOf.Insulation_Cold) +
                apparel.GetStatValue(StatDefOf.ComfyTemperatureMin);
            candidateRange.max += apparel.GetStatValue(StatDefOf.Insulation_Heat) +
                apparel.GetStatValue(StatDefOf.ComfyTemperatureMax);
#if DEBUG
            Log.Message(
                $"DifferentlyOutfitted: ComfyTemperatureMin = {apparel.GetStatValue(StatDefOf.ComfyTemperatureMin)}, ComfyTemperatureMax = {apparel.GetStatValue(StatDefOf.ComfyTemperatureMax)}");
#endif
            foreach (var wornApparel in pawn.apparel.WornApparel.Where(wornApparel =>
                         !ApparelUtility.CanWearTogether(apparel.def, wornApparel.def, pawn.RaceProps.body)))
            {
                candidateRange.min -= -wornApparel.GetStatValue(StatDefOf.Insulation_Cold) +
                    wornApparel.GetStatValue(StatDefOf.ComfyTemperatureMin);
                candidateRange.max -= wornApparel.GetStatValue(StatDefOf.Insulation_Heat) +
                    wornApparel.GetStatValue(StatDefOf.ComfyTemperatureMax);
            }
            var insulationScore = 0f;
            var coldBenefit = candidateRange.min < currentRange.min
                ? currentRange.min <= targetRange.min
                    ? 0
                    :
                    candidateRange.min <= targetRange.min && currentRange.min > targetRange.min
                        ?
                        currentRange.min - targetRange.min
                        : currentRange.min - candidateRange.min
                :
                candidateRange.min <= targetRange.min
                    ? 0
                    :
                    currentRange.min <= targetRange.min && candidateRange.min > targetRange.min
                        ?
                        targetRange.min - candidateRange.min
                        : currentRange.min - candidateRange.min;
            insulationScore += InsulationScoreCurve.Evaluate(coldBenefit);
            var heatBenefit = candidateRange.max < currentRange.max
                ? currentRange.max < targetRange.max
                    ?
                    candidateRange.max - currentRange.max
                    : candidateRange.max < targetRange.max && currentRange.max >= targetRange.max
                        ? candidateRange.max - targetRange.max
                        : 0
                :
                candidateRange.max < targetRange.max
                    ? candidateRange.max - currentRange.max
                    :
                    currentRange.max < targetRange.max && candidateRange.max >= targetRange.max
                        ?
                        targetRange.max - currentRange.max
                        : 0;
            insulationScore += InsulationScoreCurve.Evaluate(heatBenefit);
#if DEBUG
            Log.Message(
                $"DifferentlyOutfitted: target range: {targetRange}, current range: {currentRange}, candidate range: {candidateRange}");
            Log.Message(
                $"DifferentlyOutfitted: cold benefit = {coldBenefit:N2}, heat benefit = {heatBenefit:N2}), insulation score = {insulationScore:N2}");
#endif
            score += insulationScore;
        }

        private static void ApplyQualityScoring(Pawn pawn, Thing apparel, ref float score)
        {
            if (pawn.royalty == null || !pawn.royalty.AllTitlesInEffectForReading.Any()) { return; }
            var qualityCategory = QualityCategory.Awful;
            foreach (var royalTitle in pawn.royalty.AllTitlesInEffectForReading.Where(royalTitle =>
                         royalTitle.def.requiredMinimumApparelQuality > qualityCategory))
            {
                qualityCategory = royalTitle.def.requiredMinimumApparelQuality;
            }
            if (apparel.TryGetQuality(out var qc) && qc < qualityCategory) { score *= LowQualityApparelScoreFactor; }
        }

        private static void ApplyRequirementScoring(Pawn pawn, Thing apparel, ref float score)
        {
            if (!pawn.apparel.AllRequirements.Any(ar => apparel.def.apparel.bodyPartGroups.Any(bpg =>
                    ar.requirement.bodyPartGroupsMatchAny.Contains(bpg)))) { return; }
            var isRequired = false;
            var isAllowed = false;
            foreach (var allRequirement in pawn.apparel.AllRequirements)
            {
                if (allRequirement.requirement.RequiredForPawn(pawn, apparel.def))
                {
                    isRequired = true;
                    break;
                }
                if (allRequirement.requirement.AllowedForPawn(pawn, apparel.def)) { isAllowed = true; }
            }
            if (isRequired) { score += RequiredApparelScoreBonus; }
            else if (isAllowed) { score += AllowedApparelScoreBonus; }
        }

        private static void ApplySlaveScoring(Pawn pawn, Thing apparel, ref float score)
        {
            if (apparel.def.apparel.slaveApparel && !pawn.IsSlave) { score -= SlaveApparelScorePenalty; }
        }

        private static void ApplySpecialScoring(Apparel apparel, ref float score)
        {
            var specialApparelScoreOffset = apparel.GetSpecialApparelScoreOffset();
#if DEBUG
            Log.Message($"DifferentlyOutfitted: Special apparel score offset = {specialApparelScoreOffset:N2}");
#endif
            score += specialApparelScoreOffset;
        }

        private static void ApplyTaintedScoring(Pawn pawn, Apparel apparel, ExtendedOutfit outfit, ref float score)
        {
            if (!outfit.PenaltyWornByCorpse || !apparel.WornByCorpse ||
                !ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.DeadMansApparel)) { return; }
#if DEBUG
            Log.Message("DifferentlyOutfitted: Penalizing tainted apparel");
#endif
            score -= TaintedApparelScorePenalty;
            if (score > 0f) { score *= TaintedApparelScoreFactor; }
        }

        private static void ApplyVisionBlockingScoring(Thing apparel, ref float score)
        {
            if (apparel.def.apparel.blocksVision) { score -= VisionBlockingApparelScorePenalty; }
        }
    }
}
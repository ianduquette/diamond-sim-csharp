namespace DiamondSim;

/// <summary>
/// Resolves ball-in-play outcomes into specific hit types based on batter Power and pitcher Stuff.
/// </summary>
public static class BallInPlayResolver {
    // Base probabilities for average vs. average (Power=50, Stuff=50)
    // These represent the distribution when a ball is put in play
    private const double BaseOutRate = 0.702;      // 70.2%
    private const double BaseSingleRate = 0.195;   // 19.5%
    private const double BaseDoubleRate = 0.060;   // 6.0%
    private const double BaseTripleRate = 0.004;   // 0.4% (~14 triples per season)
    private const double BaseHomeRunRate = 0.039;  // 3.9%
    // Sum = 1.000

    // Adjustment factors control how much Power and Stuff affect outcomes
    private const double PowerAdjustmentFactor = 0.30;  // ±30% for extreme Power
    private const double StuffAdjustmentFactor = 0.20;  // ±20% for extreme Stuff

    // Power distribution weights (how Power affects different hit types)
    private const double PowerToHomeRunWeight = 2.0;    // HR most affected by Power
    private const double PowerToDoubleWeight = 1.0;     // 2B moderately affected
    private const double PowerToTripleWeight = 0.3;     // 3B slightly affected

    /// <summary>
    /// Resolves a ball-in-play outcome into a specific hit type or out.
    /// </summary>
    /// <param name="power">Batter's Power rating (0-100 scale).</param>
    /// <param name="stuff">Pitcher's Stuff rating (0-100 scale).</param>
    /// <param name="random">Random source for deterministic testing.</param>
    /// <returns>The specific ball-in-play outcome.</returns>
    public static BipOutcome ResolveBallInPlay(int power, int stuff, IRandomSource random) {
        // Convert 0-100 ratings to 0.0-1.0 scale
        double powerNormalized = power / 100.0;
        double stuffNormalized = stuff / 100.0;

        // Start with base probabilities
        var probs = new Probabilities {
            Out = BaseOutRate,
            Single = BaseSingleRate,
            Double = BaseDoubleRate,
            Triple = BaseTripleRate,
            HomeRun = BaseHomeRunRate
        };

        // Apply Power adjustment (affects hit type distribution)
        probs = ApplyPowerAdjustment(probs, powerNormalized);

        // Apply Stuff adjustment (affects overall hit rate)
        probs = ApplyStuffAdjustment(probs, stuffNormalized);

        // Normalize to ensure sum = 1.0
        probs = Normalize(probs);

        // Sample from cumulative distribution
        return SampleOutcome(probs, random);
    }

    /// <summary>
    /// Applies Power adjustment to shift probability mass toward extra-base hits.
    /// Higher Power increases HR, 2B, 3B at the expense of singles and outs.
    /// </summary>
    private static Probabilities ApplyPowerAdjustment(Probabilities probs, double power) {
        // power: 0.0 (weak) to 1.0 (elite), neutral = 0.5
        double powerDelta = power - 0.5; // Range: -0.5 to +0.5

        // Calculate boosts for extra-base hits
        double hrBoost = powerDelta * PowerAdjustmentFactor * PowerToHomeRunWeight;
        double doubleBoost = powerDelta * PowerAdjustmentFactor * PowerToDoubleWeight;
        double tripleBoost = powerDelta * PowerAdjustmentFactor * PowerToTripleWeight;

        // Apply boosts to extra-base hits
        probs.HomeRun += hrBoost;
        probs.Double += doubleBoost;
        probs.Triple += tripleBoost;

        // Compensate by reducing singles and outs
        double totalBoost = hrBoost + doubleBoost + tripleBoost;
        probs.Single -= totalBoost * 0.4;  // Singles reduced moderately
        probs.Out -= totalBoost * 0.6;     // Outs reduced more

        return probs;
    }

    /// <summary>
    /// Applies Stuff adjustment to shift probability mass between outs and hits.
    /// Higher Stuff increases outs and decreases all hit types proportionally.
    /// </summary>
    private static Probabilities ApplyStuffAdjustment(Probabilities probs, double stuff) {
        // stuff: 0.0 (weak) to 1.0 (elite), neutral = 0.5
        double stuffDelta = stuff - 0.5; // Range: -0.5 to +0.5

        // Calculate out boost/reduction
        double outBoost = stuffDelta * StuffAdjustmentFactor;

        // Calculate total hit probability before adjustment
        double totalHitProb = probs.Single + probs.Double + probs.Triple + probs.HomeRun;

        // Apply out adjustment
        probs.Out += outBoost;

        // Reduce all hit types proportionally to maintain their relative distribution
        if (totalHitProb > 0) {
            double hitReductionFactor = outBoost / totalHitProb;
            probs.Single -= probs.Single * hitReductionFactor;
            probs.Double -= probs.Double * hitReductionFactor;
            probs.Triple -= probs.Triple * hitReductionFactor;
            probs.HomeRun -= probs.HomeRun * hitReductionFactor;
        }

        return probs;
    }

    /// <summary>
    /// Normalizes probabilities to ensure they sum to 1.0 and are non-negative.
    /// </summary>
    private static Probabilities Normalize(Probabilities probs) {
        // Ensure no negative probabilities
        probs.Out = Math.Max(0.0, probs.Out);
        probs.Single = Math.Max(0.0, probs.Single);
        probs.Double = Math.Max(0.0, probs.Double);
        probs.Triple = Math.Max(0.0, probs.Triple);
        probs.HomeRun = Math.Max(0.0, probs.HomeRun);

        // Calculate sum
        double sum = probs.Out + probs.Single + probs.Double + probs.Triple + probs.HomeRun;

        // Normalize to sum = 1.0
        if (sum > 0) {
            probs.Out /= sum;
            probs.Single /= sum;
            probs.Double /= sum;
            probs.Triple /= sum;
            probs.HomeRun /= sum;
        }

        return probs;
    }

    /// <summary>
    /// Samples an outcome from the probability distribution using cumulative probabilities.
    /// </summary>
    private static BipOutcome SampleOutcome(Probabilities probs, IRandomSource random) {
        double roll = random.NextDouble(); // [0.0, 1.0)
        double cumulative = 0.0;

        cumulative += probs.Out;
        if (roll < cumulative) return BipOutcome.Out;

        cumulative += probs.Single;
        if (roll < cumulative) return BipOutcome.Single;

        cumulative += probs.Double;
        if (roll < cumulative) return BipOutcome.Double;

        cumulative += probs.Triple;
        if (roll < cumulative) return BipOutcome.Triple;

        return BipOutcome.HomeRun; // Remaining probability
    }

    /// <summary>
    /// Internal structure to hold probability values during calculation.
    /// </summary>
    private struct Probabilities {
        public double Out;
        public double Single;
        public double Double;
        public double Triple;
        public double HomeRun;
    }
}

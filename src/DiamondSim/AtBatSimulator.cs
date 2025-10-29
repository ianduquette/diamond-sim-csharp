namespace DiamondSim;

/// <summary>
/// Simulates complete at-bats pitch-by-pitch until a terminal outcome is reached.
/// </summary>
/// <remarks>
/// <para><strong>Probability Knobs and Tuning:</strong></para>
/// <para>
/// This simulator uses several tunable constants that control the behavior and outcome distributions.
/// These constants were calibrated to produce realistic K%, BB%, and BIP% distributions matching
/// typical MLB statistics for average vs. average matchups.
/// </para>
///
/// <para><strong>How the Constants Work:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>BaseInZoneRate (0.60)</term>
/// <description>
/// The percentage of pitches that land in the strike zone for an average Control (50) pitcher.
/// This is adjusted by ±ControlAdjustment based on the pitcher's actual Control rating.
/// Formula: inZoneRate = 0.60 + ((Control - 50) / 50) * 0.12
/// Example: Control=70 → 64.8% in zone; Control=30 → 55.2% in zone
/// </description>
/// </item>
/// <item>
/// <term>InZoneSwingRate (0.72)</term>
/// <description>
/// The percentage of time a batter swings at pitches in the strike zone.
/// This is relatively constant regardless of Patience, as batters must protect the plate.
/// </description>
/// </item>
/// <item>
/// <term>OutOfZoneSwingRate (0.228)</term>
/// <description>
/// The base chase rate for an average Patience (50) batter on pitches outside the zone.
/// This is adjusted by ±PatienceAdjustment based on the batter's actual Patience rating.
/// Formula: chaseRate = 0.228 - ((Patience - 50) / 50) * 0.22
/// Example: Patience=70 → 14.0% chase; Patience=30 → 31.6% chase
/// Note: Higher Patience = lower chase rate (hence the minus sign)
/// </description>
/// </item>
/// <item>
/// <term>FoulRateWithTwoStrikes (0.60) / FoulRateOtherCounts (0.45)</term>
/// <description>
/// When contact is made, these determine the percentage that results in foul balls vs. balls in play.
/// With 2 strikes, batters are more defensive, resulting in more fouls (60%).
/// In other counts, 45% of contact results in fouls, 55% in balls in play.
/// </description>
/// </item>
/// </list>
///
/// <para><strong>Tuning These Constants:</strong></para>
/// <para>
/// These constants can be adjusted to achieve different outcome distributions:
/// </para>
/// <list type="bullet">
/// <item>To increase K%: Decrease InZoneSwingRate or increase OutOfZoneSwingRate</item>
/// <item>To increase BB%: Decrease BaseInZoneRate or increase ControlAdjustment</item>
/// <item>To increase BIP%: Decrease FoulRateWithTwoStrikes or FoulRateOtherCounts</item>
/// </list>
/// <para>
/// Current values produce distributions of approximately:
/// K% = 18-28%, BB% = 7-12%, BIP% = 55-70% for average vs. average matchups.
/// </para>
///
/// <para><strong>Future Enhancement:</strong></para>
/// <para>
/// These constants could be made configurable via constructor parameters or a configuration object
/// to allow runtime tuning without recompilation. For now, they are compile-time constants for
/// simplicity and performance.
/// </para>
/// </remarks>
public class AtBatSimulator {
    // ============================================================================
    // PROBABILITY KNOBS - These constants control simulation behavior and can be
    // tuned to achieve target K%, BB%, and BIP% distributions.
    // See class-level documentation for detailed explanation of how each works.
    // ============================================================================

    // Zone Decision (Pitcher Control)
    private const double BaseInZoneRate = 0.575; // 57.5% for average Control (50) - tuned to achieve target distributions
    private const double ControlAdjustment = 0.14; // ±14% range based on Control rating - increased for more BB% variation

    // Swing Decision (Batter Patience)
    private const double InZoneSwingRate = 0.72; // 72% swing rate on strikes - precisely balanced for all target distributions
    private const double OutOfZoneSwingRate = 0.228; // 22.8% chase rate for average Patience (50) - final precise tuning for target BB%
    private const double PatienceAdjustment = 0.22; // ±22% range based on Patience rating - increased for more variation

    // Foul Rate (Contact Quality)
    private const double FoulRateWithTwoStrikes = 0.58; // 58% fouls with 2 strikes (defensive) - tuned for target BIP%
    private const double FoulRateOtherCounts = 0.43; // 43% fouls in other counts - tuned for target BIP%

    // Hit By Pitch Rate (rare event)
    private const double HitByPitchRate = 0.01; // 1% chance per pitch - realistic MLB rate (~1% of PA)

    // Safety limit to prevent infinite loops
    private const int MaxPitchesPerAtBat = 50;

    private readonly IRandomSource _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtBatSimulator"/> class.
    /// </summary>
    /// <param name="random">The random number generator to use for probabilistic decisions.</param>
    public AtBatSimulator(IRandomSource random) {
        _random = random;
    }

    /// <summary>
    /// Simulates a complete at-bat from start to finish.
    /// </summary>
    /// <param name="pitcher">The pitcher's ratings.</param>
    /// <param name="batter">The batter's ratings.</param>
    /// <returns>An <see cref="AtBatResult"/> containing the terminal outcome, final count, and pitch count.</returns>
    public AtBatResult SimulateAtBat(PitcherRatings pitcher, BatterRatings batter) {
        var state = new GameState(0, 0);
        int pitchCount = 0;

        while (!state.IsTerminal() && pitchCount < MaxPitchesPerAtBat) {
            pitchCount++;

            // 1. Check for HBP (rare event, checked first)
            if (_random.NextDouble() < HitByPitchRate) {
                return new AtBatResult(
                    AtBatTerminal.HitByPitch,
                    state.ToString(),
                    pitchCount
                );
            }

            // 2. Zone Decision: Is the pitch in the strike zone?
            bool inZone = DetermineZone(pitcher.Control);

            // 3. Swing Decision: Does the batter swing?
            bool swings = DetermineSwing(batter.Patience, inZone);

            // 4. Determine pitch outcome
            PitchOutcome outcome = DeterminePitchOutcome(
                inZone,
                swings,
                pitcher,
                batter,
                state.Balls,
                state.Strikes
            );

            // 5. Update count based on outcome
            UpdateCount(state, outcome);

            // 6. Check for terminal outcome
            if (outcome == PitchOutcome.InPlay) {
                return new AtBatResult(
                    AtBatTerminal.BallInPlay,
                    state.ToString(),
                    pitchCount
                );
            }
        }

        // Determine terminal outcome based on final count
        AtBatTerminal terminal;
        if (state.IsStrikeout()) {
            terminal = AtBatTerminal.Strikeout;
        }
        else if (state.IsWalk()) {
            terminal = AtBatTerminal.Walk;
        }
        else {
            // Safety fallback - should not happen with proper logic
            terminal = AtBatTerminal.BallInPlay;
        }

        return new AtBatResult(terminal, state.ToString(), pitchCount);
    }

    /// <summary>
    /// Determines whether a pitch is in the strike zone based on pitcher Control.
    /// </summary>
    /// <param name="control">The pitcher's Control rating (0-100).</param>
    /// <returns><c>true</c> if the pitch is in the zone; otherwise, <c>false</c>.</returns>
    private bool DetermineZone(int control) {
        // Higher Control = more pitches in zone
        double controlDelta = (control - 50) * (ControlAdjustment / 50.0);
        double inZoneRate = BaseInZoneRate + controlDelta;
        inZoneRate = Clamp01(inZoneRate);

        return _random.NextDouble() < inZoneRate;
    }

    /// <summary>
    /// Determines whether the batter swings at a pitch based on Patience and pitch location.
    /// </summary>
    /// <param name="patience">The batter's Patience rating (0-100).</param>
    /// <param name="inZone">Whether the pitch is in the strike zone.</param>
    /// <returns><c>true</c> if the batter swings; otherwise, <c>false</c>.</returns>
    private bool DetermineSwing(int patience, bool inZone) {
        double swingRate;

        if (inZone) {
            // In zone: high swing rate, not much affected by Patience
            swingRate = InZoneSwingRate;
        }
        else {
            // Out of zone: swing rate heavily affected by Patience
            // Lower Patience = higher chase rate
            double patienceDelta = (patience - 50) * (PatienceAdjustment / 50.0);
            swingRate = OutOfZoneSwingRate - patienceDelta; // Note: minus because higher patience = lower chase
            swingRate = Clamp01(swingRate);
        }

        return _random.NextDouble() < swingRate;
    }

    /// <summary>
    /// Determines the outcome of a pitch based on zone, swing decision, and contact probability.
    /// </summary>
    private PitchOutcome DeterminePitchOutcome(
        bool inZone,
        bool swings,
        PitcherRatings pitcher,
        BatterRatings batter,
        int balls,
        int strikes) {

        if (!swings) {
            // Batter takes the pitch
            return inZone ? PitchOutcome.CalledStrike : PitchOutcome.Ball;
        }

        // Batter swings - check for contact
        double baseContact = Probabilities.ContactFromRatings(batter, pitcher);
        double countAdjust = Probabilities.CountContactAdjust(balls, strikes);
        double contactProb = Clamp01(baseContact + countAdjust);

        bool makesContact = _random.NextDouble() < contactProb;

        if (!makesContact) {
            return PitchOutcome.SwingAndMiss;
        }

        // Contact made - determine if foul or in play
        double foulRate = (strikes == 2) ? FoulRateWithTwoStrikes : FoulRateOtherCounts;
        bool isFoul = _random.NextDouble() < foulRate;

        return isFoul ? PitchOutcome.Foul : PitchOutcome.InPlay;
    }

    /// <summary>
    /// Updates the game state count based on the pitch outcome.
    /// </summary>
    private void UpdateCount(GameState state, PitchOutcome outcome) {
        switch (outcome) {
            case PitchOutcome.Ball:
                state.IncrementBalls();
                break;

            case PitchOutcome.CalledStrike:
            case PitchOutcome.SwingAndMiss:
                state.IncrementStrikes();
                break;

            case PitchOutcome.Foul:
                // Foul balls don't add a third strike
                state.IncrementStrikesSafe();
                break;

            case PitchOutcome.InPlay:
                // Terminal outcome - no count update needed
                break;
        }
    }

    /// <summary>
    /// Clamps a probability value to the range [0, 1].
    /// </summary>
    private static double Clamp01(double value) {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}

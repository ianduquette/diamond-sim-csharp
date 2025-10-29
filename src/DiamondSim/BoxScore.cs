namespace DiamondSim;

/// <summary>
/// Represents batting statistics for a single player.
/// </summary>
/// <remarks>
/// Statistics tracked per the v0.2 scope:
/// - AB (at-bats): Excludes BB, HBP. Includes InPlayOut, hits, and ReachOnError.
/// - H (hits): Singles, doubles, triples, home runs (NOT ReachOnError).
/// - Hit breakdown: Singles, Doubles, Triples, HR.
/// - BB (walks), HBP (hit by pitch), K (strikeouts).
/// - RBI (runs batted in): Batter's own run counts only on HR; for other hits, only runners who scored.
/// - R (runs scored): Tracked separately when runner crosses home.
/// - PA (plate appearances): All plate appearances.
/// - TB (total bases): 1B×1 + 2B×2 + 3B×3 + HR×4.
/// </remarks>
public sealed record BatterStats {
    /// <summary>
    /// At-bats (excludes BB, HBP; includes hits, outs, and errors).
    /// </summary>
    public int AB { get; init; }

    /// <summary>
    /// Hits (singles, doubles, triples, home runs).
    /// </summary>
    public int H { get; init; }

    /// <summary>
    /// Singles.
    /// </summary>
    public int Singles { get; init; }

    /// <summary>
    /// Doubles.
    /// </summary>
    public int Doubles { get; init; }

    /// <summary>
    /// Triples.
    /// </summary>
    public int Triples { get; init; }

    /// <summary>
    /// Home runs.
    /// </summary>
    public int HR { get; init; }

    /// <summary>
    /// Walks (base on balls).
    /// </summary>
    public int BB { get; init; }

    /// <summary>
    /// Hit by pitch.
    /// </summary>
    public int HBP { get; init; }

    /// <summary>
    /// Strikeouts.
    /// </summary>
    public int K { get; init; }

    /// <summary>
    /// Runs batted in.
    /// </summary>
    public int RBI { get; init; }

    /// <summary>
    /// Runs scored.
    /// LIMITATION (v1): Only tracks runs scored during the batter's own plate appearance (home runs).
    /// Does not track when a batter who reached base earlier scores as a runner on a subsequent play.
    /// This would require tracking which lineup position occupies each base (not implemented in v1).
    /// See .docs/box_score_runs_limitation.md for details.
    /// NOTE: This field is retained in the data model but omitted from box score output in v1.
    /// </summary>
    public int R { get; init; }

    /// <summary>
    /// Plate appearances (all PAs).
    /// </summary>
    public int PA { get; init; }

    /// <summary>
    /// Total bases (1B×1 + 2B×2 + 3B×3 + HR×4).
    /// </summary>
    public int TB { get; init; }
}

/// <summary>
/// Represents pitching statistics for a single pitcher.
/// </summary>
/// <remarks>
/// Statistics tracked per the v0.2 scope:
/// - BF (batters faced): Total plate appearances against this pitcher.
/// - OutsRecorded: Converts to IP (innings pitched) as outs ÷ 3.
/// - H (hits allowed), R (runs allowed), ER (earned runs - all runs counted as earned in v0.2).
/// - BB (walks allowed), HBP (hit batters), K (strikeouts), HR (home runs allowed).
/// </remarks>
public sealed record PitcherStats {
    /// <summary>
    /// Batters faced (total plate appearances).
    /// </summary>
    public int BF { get; init; }

    /// <summary>
    /// Outs recorded (converts to IP: outs ÷ 3).
    /// </summary>
    public int OutsRecorded { get; init; }

    /// <summary>
    /// Hits allowed.
    /// </summary>
    public int H { get; init; }

    /// <summary>
    /// Runs allowed.
    /// </summary>
    public int R { get; init; }

    /// <summary>
    /// Earned runs (all runs counted as earned in v0.2).
    /// </summary>
    public int ER { get; init; }

    /// <summary>
    /// Walks allowed.
    /// </summary>
    public int BB { get; init; }

    /// <summary>
    /// Hit batters.
    /// </summary>
    public int HBP { get; init; }

    /// <summary>
    /// Strikeouts.
    /// </summary>
    public int K { get; init; }

    /// <summary>
    /// Home runs allowed.
    /// </summary>
    public int HR { get; init; }
}

/// <summary>
/// Tracks player statistics (batting and pitching) during game simulation.
/// </summary>
/// <remarks>
/// Box score maintains statistics for both teams:
/// - Batter stats: Keyed by lineup position (0-8) for each team.
/// - Pitcher stats: Keyed by pitcher ID for each team.
///
/// Statistics are incremented based on plate appearance outcomes per PRD Section 9.5.
/// No rate calculations (AVG, OPS, ERA) are performed - these are left to external analysis.
/// </remarks>
public class BoxScore {
    /// <summary>
    /// Batting statistics for the away team, keyed by lineup position (0-8).
    /// </summary>
    public Dictionary<int, BatterStats> AwayBatters { get; } = new Dictionary<int, BatterStats>();

    /// <summary>
    /// Batting statistics for the home team, keyed by lineup position (0-8).
    /// </summary>
    public Dictionary<int, BatterStats> HomeBatters { get; } = new Dictionary<int, BatterStats>();

    /// <summary>
    /// Pitching statistics for the away team, keyed by pitcher ID.
    /// </summary>
    public Dictionary<int, PitcherStats> AwayPitchers { get; } = new Dictionary<int, PitcherStats>();

    /// <summary>
    /// Pitching statistics for the home team, keyed by pitcher ID.
    /// </summary>
    public Dictionary<int, PitcherStats> HomePitchers { get; } = new Dictionary<int, PitcherStats>();

    /// <summary>
    /// Increments batter statistics based on the plate appearance outcome.
    /// </summary>
    /// <param name="team">The batting team (Away or Home).</param>
    /// <param name="lineupPosition">The batter's lineup position (0-8).</param>
    /// <param name="paType">The type of plate appearance outcome.</param>
    /// <param name="runsScored">The number of runs scored on this PA.</param>
    /// <param name="rbiDelta">The number of RBI to credit to the batter (computed by scorer).</param>
    /// <param name="batterScored">Whether the batter themselves scored (true for HR, false otherwise).</param>
    /// <remarks>
    /// Stat increment rules per PRD Section 9.5 and MLB rules:
    /// - AB increments: K, InPlayOut, Single, Double, Triple, HomeRun, ReachOnError
    /// - AB does NOT increment: BB, HBP
    /// - H increments: Single, Double, Triple, HomeRun (NOT ReachOnError)
    /// - RBI: Explicit delta provided by scorer (no inference from runs or PA type)
    /// - TB: Single=1, Double=2, Triple=3, HomeRun=4, Walk/HBP/Error=0
    /// </remarks>
    public void IncrementBatterStats(Team team, int lineupPosition, PaType paType, int runsScored, int rbiDelta, bool batterScored) {
        var batters = team == Team.Away ? AwayBatters : HomeBatters;

        // Initialize stats if this is the batter's first PA
        if (!batters.ContainsKey(lineupPosition)) {
            batters[lineupPosition] = new BatterStats();
        }

        var current = batters[lineupPosition];

        // Increment PA for all plate appearances
        int newPA = current.PA + 1;

        // Determine AB increment (excludes BB, HBP per MLB rules)
        // AB increments for: K, InPlayOut, Single, Double, Triple, HomeRun, ReachOnError
        int newAB = current.AB;
        if (paType == PaType.K || paType == PaType.InPlayOut || paType == PaType.Single ||
            paType == PaType.Double || paType == PaType.Triple || paType == PaType.HomeRun ||
            paType == PaType.ReachOnError) {
            newAB++;
        }

        // Determine H increment (hits only, not errors)
        int newH = current.H;
        int newSingles = current.Singles;
        int newDoubles = current.Doubles;
        int newTriples = current.Triples;
        int newHR = current.HR;
        int newTB = current.TB;

        switch (paType) {
            case PaType.Single:
                newH++;
                newSingles++;
                newTB += 1;
                break;
            case PaType.Double:
                newH++;
                newDoubles++;
                newTB += 2;
                break;
            case PaType.Triple:
                newH++;
                newTriples++;
                newTB += 3;
                break;
            case PaType.HomeRun:
                newH++;
                newHR++;
                newTB += 4;
                break;
        }

        // Increment BB, HBP, K
        int newBB = current.BB + (paType == PaType.BB ? 1 : 0);
        int newHBP = current.HBP + (paType == PaType.HBP ? 1 : 0);
        int newK = current.K + (paType == PaType.K ? 1 : 0);

        // RBI: Use explicit delta from scorer (no inference)
        int newRBI = current.RBI + rbiDelta;

        // Increment R if batter scored
        int newR = current.R + (batterScored ? 1 : 0);

        // Update the batter's stats
        batters[lineupPosition] = new BatterStats {
            AB = newAB,
            H = newH,
            Singles = newSingles,
            Doubles = newDoubles,
            Triples = newTriples,
            HR = newHR,
            BB = newBB,
            HBP = newHBP,
            K = newK,
            RBI = newRBI,
            R = newR,
            PA = newPA,
            TB = newTB
        };
    }

    /// <summary>
    /// Increments pitcher statistics based on the plate appearance outcome.
    /// </summary>
    /// <param name="team">The pitching team (Away or Home).</param>
    /// <param name="pitcherId">The pitcher's ID.</param>
    /// <param name="paType">The type of plate appearance outcome.</param>
    /// <param name="outsAdded">The number of outs recorded on this PA.</param>
    /// <param name="runsScored">The number of runs scored on this PA.</param>
    /// <remarks>
    /// All runs are counted as earned in v0.2 (ER = R).
    /// </remarks>
    public void IncrementPitcherStats(Team team, int pitcherId, PaType paType, int outsAdded, int runsScored) {
        var pitchers = team == Team.Away ? AwayPitchers : HomePitchers;

        // Initialize stats if this is the pitcher's first batter faced
        if (!pitchers.ContainsKey(pitcherId)) {
            pitchers[pitcherId] = new PitcherStats();
        }

        var current = pitchers[pitcherId];

        // Increment BF (batters faced) for all PAs
        int newBF = current.BF + 1;

        // Increment OutsRecorded
        int newOutsRecorded = current.OutsRecorded + outsAdded;

        // Increment H (hits allowed) - not on errors
        int newH = current.H;
        if (paType == PaType.Single || paType == PaType.Double ||
            paType == PaType.Triple || paType == PaType.HomeRun) {
            newH++;
        }

        // Increment R and ER (all runs counted as earned in v0.2)
        int newR = current.R + runsScored;
        int newER = current.ER + runsScored;

        // Increment BB, HBP, K, HR
        int newBB = current.BB + (paType == PaType.BB ? 1 : 0);
        int newHBP = current.HBP + (paType == PaType.HBP ? 1 : 0);
        int newK = current.K + (paType == PaType.K ? 1 : 0);
        int newHR = current.HR + (paType == PaType.HomeRun ? 1 : 0);

        // Update the pitcher's stats
        pitchers[pitcherId] = new PitcherStats {
            BF = newBF,
            OutsRecorded = newOutsRecorded,
            H = newH,
            R = newR,
            ER = newER,
            BB = newBB,
            HBP = newHBP,
            K = newK,
            HR = newHR
        };
    }

    /// <summary>
    /// Validates that team hits equal the sum of individual batter hits.
    /// </summary>
    /// <param name="team">The team to validate (Away or Home).</param>
    /// <param name="expectedTeamHits">The expected total team hits.</param>
    /// <returns>True if validation passes, false otherwise.</returns>
    public bool ValidateTeamHits(Team team, int expectedTeamHits) {
        var batters = team == Team.Away ? AwayBatters : HomeBatters;
        int totalHits = batters.Values.Sum(b => b.H);
        return totalHits == expectedTeamHits;
    }

    /// <summary>
    /// Validates that defensive outs match expected totals.
    /// </summary>
    /// <param name="team">The defensive team (Away or Home).</param>
    /// <param name="expectedOuts">The expected total outs recorded by defense.</param>
    /// <returns>True if validation passes, false otherwise.</returns>
    /// <remarks>
    /// Expected outs:
    /// - 27 per team in a full 9-inning game
    /// - 24 for away defense if home skips bottom 9th
    /// - Extras add 3 per completed defensive half
    /// </remarks>
    public bool ValidateDefensiveOuts(Team team, int expectedOuts) {
        var pitchers = team == Team.Away ? AwayPitchers : HomePitchers;
        int totalOuts = pitchers.Values.Sum(p => p.OutsRecorded);
        return totalOuts == expectedOuts;
    }

    /// <summary>
    /// Validates that pitcher outs sum equals total outs made by defense.
    /// </summary>
    /// <param name="team">The defensive team (Away or Home).</param>
    /// <returns>The total outs recorded by all pitchers on the team.</returns>
    public int GetTotalPitcherOuts(Team team) {
        var pitchers = team == Team.Away ? AwayPitchers : HomePitchers;
        return pitchers.Values.Sum(p => p.OutsRecorded);
    }
}

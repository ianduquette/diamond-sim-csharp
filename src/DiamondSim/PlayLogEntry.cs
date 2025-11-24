using System.Text;

namespace DiamondSim;

/// <summary>
/// Immutable play-by-play log entry for a single plate appearance.
/// </summary>
public sealed class PlayLogEntry {
    public int Inning { get; init; }
    public InningHalf Half { get; init; }
    public string BatterName { get; init; }
    public string PitchingTeamName { get; init; }
    public PaResolution Resolution { get; init; }
    public bool IsWalkoff { get; init; }
    public int OutsAfter { get; init; }

    public PlayLogEntry(
        int inning,
        InningHalf half,
        string batterName,
        string pitchingTeamName,
        PaResolution resolution,
        bool isWalkoff,
        int outsAfter) {
        Inning = inning;
        Half = half;
        BatterName = batterName ?? throw new ArgumentNullException(nameof(batterName));
        PitchingTeamName = pitchingTeamName ?? throw new ArgumentNullException(nameof(pitchingTeamName));
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        IsWalkoff = isWalkoff;
        OutsAfter = outsAfter;
    }

    /// <summary>
    /// Converts this play log entry to a formatted string for display.
    /// </summary>
    public string ToPlayLogString() {
        var halfStr = Half == InningHalf.Top ? "Top" : "Bot";

        // Walk-off prefix
        var prefix = IsWalkoff ? PlayByPlayPhrases.WalkoffPrefix : "";

        // Format outcome using OutcomeTag
        var outcome = FormatOutcome(Resolution);

        // Format runner movements
        var baseRunners = FormatRunnerMoves(Resolution.Moves);

        // Outs phrase
        var outsPhrase = Resolution.OutsAdded > 0
            ? " " + PlayByPlayPhrases.OutsPhrase(OutsAfter)
            : "";

        // Build complete log entry
        var sb = new StringBuilder();
        sb.Append($"[{halfStr} {Inning}] {BatterName} vs {PitchingTeamName} P — {prefix}{outcome}.");

        if (!string.IsNullOrEmpty(baseRunners)) {
            sb.Append($" {baseRunners}.");
        }

        if (!string.IsNullOrEmpty(outsPhrase)) {
            sb.Append(outsPhrase);
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatOutcome(PaResolution resolution) {
        return resolution.Tag switch {
            OutcomeTag.K => PlayByPlayPhrases.Strikeout(looking: true),
            OutcomeTag.BB => PlayByPlayPhrases.Walk,
            OutcomeTag.HBP => PlayByPlayPhrases.HitByPitch,
            OutcomeTag.Single => PlayByPlayPhrases.Single("CF"),
            OutcomeTag.Double => PlayByPlayPhrases.Double("CF"),
            OutcomeTag.Triple => PlayByPlayPhrases.Triple("CF"),
            OutcomeTag.HR => PlayByPlayPhrases.HomeRun("CF"),
            OutcomeTag.ROE => PlayByPlayPhrases.ReachOnError(6),
            OutcomeTag.SF => PlayByPlayPhrases.SacrificeFly("CF"),
            OutcomeTag.DP => PlayByPlayPhrases.GroundsIntoDP("6-4-3"),
            OutcomeTag.InPlayOut => PlayByPlayPhrases.Groundout("6-3"),
            _ => "Unknown outcome"
        };
    }

    private static string FormatRunnerMoves(IReadOnlyList<RunnerMove>? moves) {
        if (moves == null || moves.Count == 0) {
            return "";
        }

        var parts = new List<string>();
        foreach (var move in moves) {
            // Skip batter's routine advancement to first on single, etc.
            if (move.FromBase == 0 && !move.Scored) {
                continue;
            }

            if (move.Scored) {
                var runnerLabel = move.FromBase == 0 ? "Batter" : $"R{move.FromBase}";
                parts.Add($"{runnerLabel} scores");
            }
            else if (move.FromBase > 0) {
                parts.Add($"R{move.FromBase} to {move.ToBase}B");
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "";
    }
}

using System.Security.Cryptography;
using System.Text;

namespace DiamondSim;

/// <summary>
/// Immutable result of a complete game simulation.
/// Contains all game data: metadata, scores, play log, and final state.
/// </summary>
public sealed class GameResult {
    public GameMetadata Metadata { get; init; }
    public BoxScore BoxScore { get; init; }
    public LineScore LineScore { get; init; }
    public IReadOnlyList<PlayLogEntry> PlayLog { get; init; }
    public GameState FinalState { get; init; }
    public TeamLineup HomeLineup { get; init; }
    public TeamLineup AwayLineup { get; init; }
    public int HomeTotalLOB { get; init; }
    public int AwayTotalLOB { get; init; }

    /// <summary>
    /// SHA-256 hash of PlayLog + FinalScore for determinism verification.
    /// Calculated lazily and cached.
    /// </summary>
    public string LogHash => _logHash ??= CalculateLogHash();
    private string? _logHash;

    public GameResult(
        GameMetadata metadata,
        BoxScore boxScore,
        LineScore lineScore,
        IReadOnlyList<PlayLogEntry> playLog,
        GameState finalState,
        TeamLineup homeLineup,
        TeamLineup awayLineup,
        int homeTotalLOB = 0,
        int awayTotalLOB = 0) {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        BoxScore = boxScore ?? throw new ArgumentNullException(nameof(boxScore));
        LineScore = lineScore ?? throw new ArgumentNullException(nameof(lineScore));
        PlayLog = playLog ?? throw new ArgumentNullException(nameof(playLog));
        FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        HomeLineup = homeLineup ?? throw new ArgumentNullException(nameof(homeLineup));
        AwayLineup = awayLineup ?? throw new ArgumentNullException(nameof(awayLineup));
        HomeTotalLOB = homeTotalLOB;
        AwayTotalLOB = awayTotalLOB;
    }

    /// <summary>
    /// Calculates SHA-256 hash of PlayLog + FinalScore.
    /// </summary>
    private string CalculateLogHash() {
        var sb = new StringBuilder();

        // Include each play log entry
        foreach (var entry in PlayLog) {
            sb.AppendLine($"{entry.Inning}|{entry.Half}|{entry.BatterName}|{entry.PitchingTeamName}|{entry.Resolution.Tag}|{entry.OutsAfter}");
        }

        // Include final score
        sb.AppendLine($"FINAL:{FinalState.AwayScore}-{FinalState.HomeScore}");

        // Calculate SHA-256
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

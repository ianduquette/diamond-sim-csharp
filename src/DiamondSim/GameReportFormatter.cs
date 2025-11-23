using System.Security.Cryptography;
using System.Text;

namespace DiamondSim;

// TODO: REFACTOR - Consider Strategy pattern with IGameResultFormatter interface (see .prd/20251123_02_Refactor-GameSimulator-Return-Object.md)
// Options: 1) Extension method: result.ToConsoleReport()
//          2) Strategy pattern: IGameResultFormatter with implementations (ConsoleFormatter, JsonFormatter, DbFormatter)
//          3) Keep as class but accept GameResult instead of 11 parameters
// Extension method is simplest for now, Strategy pattern better for DI/testing multiple formats
/// <summary>
/// Formats the complete game report including header, line score, play log,
/// team totals, box scores, and footer with LogHash.
/// </summary>
public class GameReportFormatter {
    private readonly string _homeTeamName;
    private readonly string _awayTeamName;
    private readonly int _seed;
    private readonly DateTime _timestamp;
    private readonly LineScore _lineScore;
    private readonly BoxScore _boxScore;
    private readonly List<string> _playLog;
    private readonly GameState _finalState;
    private readonly InningScorekeeper _scorekeeper;
    private readonly List<Batter> _homeLineup;
    private readonly List<Batter> _awayLineup;

    // TODO: REFACTOR - Replace 11-parameter constructor. Future: IGameResultFormatter interface for DI
    public GameReportFormatter(
        string homeTeamName,
        string awayTeamName,
        int seed,
        DateTime timestamp,
        LineScore lineScore,
        BoxScore boxScore,
        List<string> playLog,
        GameState finalState,
        InningScorekeeper scorekeeper,
        List<Batter> homeLineup,
        List<Batter> awayLineup) {

        _homeTeamName = homeTeamName;
        _awayTeamName = awayTeamName;
        _seed = seed;
        _timestamp = timestamp;
        _lineScore = lineScore;
        _boxScore = boxScore;
        _playLog = playLog;
        _finalState = finalState;
        _scorekeeper = scorekeeper;
        _homeLineup = homeLineup;
        _awayLineup = awayLineup;
    }

    /// <summary>
    /// Formats the complete game report.
    /// </summary>
    public string FormatReport() {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(FormatHeader());
        sb.AppendLine();

        // Line Score
        sb.AppendLine(FormatLineScore());
        sb.AppendLine();

        // Play Log
        sb.AppendLine("PLAY LOG");
        sb.AppendLine("========");
        foreach (var entry in _playLog) {
            sb.AppendLine(entry);
        }
        sb.AppendLine();

        // Team Totals
        sb.AppendLine(FormatTeamTotals());
        sb.AppendLine();

        // Box Scores
        sb.AppendLine(FormatBattingBoxScores());
        sb.AppendLine();
        sb.AppendLine(FormatPitchingBoxScores());
        sb.AppendLine();

        // Footer
        sb.AppendLine(FormatFooter());

        return sb.ToString();
    }

    private string FormatHeader() {
        var sb = new StringBuilder();
        sb.AppendLine($"{_awayTeamName} @ {_homeTeamName} — Seed: {_seed}");
        sb.AppendLine(FormatTimestamp(_timestamp));
        sb.AppendLine("DH: ON");
        return sb.ToString().TrimEnd();
    }

    private string FormatTimestamp(DateTime timestamp) {
        // Format: YYYY-MM-DD HH:MM TZ (no seconds)
        // Using numeric offset format
        return timestamp.ToString("yyyy-MM-dd HH:mm zzz");
    }

    private string FormatLineScore() {
        var sb = new StringBuilder();

        // Fixed column widths for line score
        const int TEAM_WIDTH = 15;
        const int INNING_WIDTH = 3;  // Space + 2 digits
        const int RHE_WIDTH = 3;     // Space + 2 digits each

        // Determine max innings to display (at least 9, or more if extras were played)
        int maxInnings = Math.Max(9, Math.Max(_lineScore.AwayInnings.Count, _lineScore.HomeInnings.Count));

        // Header row
        sb.Append($"{"",TEAM_WIDTH}|");
        for (int i = 1; i <= maxInnings; i++) {
            sb.Append($"{i,INNING_WIDTH}");
        }
        sb.AppendLine($" |{" R",RHE_WIDTH}{" H",RHE_WIDTH}{" E",RHE_WIDTH}");

        // Separator: 15 (team) + 1 (|) + (maxInnings * 3) (innings) + 2 ( |) + 9 (R/H/E)
        int separatorLength = TEAM_WIDTH + 1 + (maxInnings * INNING_WIDTH) + 2 + (RHE_WIDTH * 3);
        sb.Append(new string('-', TEAM_WIDTH));
        sb.Append('|');
        sb.Append(new string('-', maxInnings * INNING_WIDTH));
        sb.Append(" |");
        sb.AppendLine(new string('-', RHE_WIDTH * 3));

        // Away team row
        sb.Append($"{_awayTeamName,-TEAM_WIDTH}|");
        for (int i = 1; i <= maxInnings; i++) {
            string display = _lineScore.GetInningDisplay(Team.Away, i);
            sb.Append($"{display,INNING_WIDTH}");
        }
        int awayR = _lineScore.AwayTotal;
        int awayH = _boxScore.AwayBatters.Values.Sum(b => b.H);
        int awayE = CountErrors(Team.Away);
        sb.AppendLine($" |{awayR,RHE_WIDTH}{awayH,RHE_WIDTH}{awayE,RHE_WIDTH}");

        // Home team row
        sb.Append($"{_homeTeamName,-TEAM_WIDTH}|");
        for (int i = 1; i <= maxInnings; i++) {
            string display = _lineScore.GetInningDisplay(Team.Home, i);
            sb.Append($"{display,INNING_WIDTH}");
        }
        int homeR = _lineScore.HomeTotal;
        int homeH = _boxScore.HomeBatters.Values.Sum(b => b.H);
        int homeE = CountErrors(Team.Home);
        sb.AppendLine($" |{homeR,RHE_WIDTH}{homeH,RHE_WIDTH}{homeE,RHE_WIDTH}");

        return sb.ToString().TrimEnd();
    }

    private int CountErrors(Team team) {
        // E = ROE count (simplified v1 - no explicit fielding errors)
        var batters = team == Team.Away ? _boxScore.AwayBatters : _boxScore.HomeBatters;
        // In v1, we don't track ROE separately in box score, so return 0 for now
        // This will be properly tracked when we add error tracking to BoxScore
        return 0;
    }

    private string FormatTeamTotals() {
        var sb = new StringBuilder();

        int awayR = _lineScore.AwayTotal;
        int homeR = _lineScore.HomeTotal;
        int awayH = _boxScore.AwayBatters.Values.Sum(b => b.H);
        int homeH = _boxScore.HomeBatters.Values.Sum(b => b.H);
        int awayE = CountErrors(Team.Away);
        int homeE = CountErrors(Team.Home);
        int awayLOB = _scorekeeper.AwayTotalLOB;
        int homeLOB = _scorekeeper.HomeTotalLOB;

        // Final score line (ties should never occur with extras enabled)
        sb.AppendLine($"Final: {_awayTeamName} {awayR} — {_homeTeamName} {homeR}");

        // Team stats
        sb.AppendLine($"{_awayTeamName}: {awayR} R, {awayH} H, {awayE} E, {awayLOB} LOB");
        sb.AppendLine($"{_homeTeamName}: {homeR} R, {homeH} H, {homeE} E, {homeLOB} LOB");

        return sb.ToString().TrimEnd();
    }

    private string FormatBattingBoxScores() {
        var sb = new StringBuilder();

        // NOTE: R (runs) and individual LOB columns omitted in v1 due to architectural limitations.
        // Individual runs require tracking which lineup position occupies each base.
        // Individual LOB requires tracking base state when each batter makes an out.
        // Team LOB is shown in the totals row and team summary.
        // See .docs/box_score_runs_limitation.md for details.

        // Fixed column widths for batting box score
        const int NAME_WIDTH = 24;
        const int PA_WIDTH = 4;
        const int AB_WIDTH = 4;
        const int H_WIDTH = 4;
        const int RBI_WIDTH = 5;
        const int BB_WIDTH = 5;
        const int K_WIDTH = 4;
        const int HR_WIDTH = 4;

        // Away team batting
        string awayHeader = $"{_awayTeamName.ToUpper()} BATTING".PadRight(NAME_WIDTH);
        sb.AppendLine($"{awayHeader}{"PA",PA_WIDTH}{"AB",AB_WIDTH}{"H",H_WIDTH}{"RBI",RBI_WIDTH}{"BB",BB_WIDTH}{"K",K_WIDTH}{"HR",HR_WIDTH}");
        sb.AppendLine("---------------------------------------------------------");

        for (int i = 0; i < 9; i++) {
            if (_boxScore.AwayBatters.TryGetValue(i, out var stats)) {
                string playerName = _awayLineup[i].Name;
                if (i == 8) playerName += " (DH)"; // Mark 9th batter as DH

                sb.AppendLine($"{playerName,-NAME_WIDTH}{stats.PA,PA_WIDTH}{stats.AB,AB_WIDTH}{stats.H,H_WIDTH}{stats.RBI,RBI_WIDTH}{stats.BB,BB_WIDTH}{stats.K,K_WIDTH}{stats.HR,HR_WIDTH}");
            }
        }

        // Totals
        var awayTotals = _boxScore.AwayBatters.Values;
        int totalPA = awayTotals.Sum(b => b.PA);
        int totalAB = awayTotals.Sum(b => b.AB);
        int totalH = awayTotals.Sum(b => b.H);
        int totalRBI = awayTotals.Sum(b => b.RBI);
        int totalBB = awayTotals.Sum(b => b.BB);
        int totalK = awayTotals.Sum(b => b.K);
        int totalHR = awayTotals.Sum(b => b.HR);

        sb.AppendLine("---------------------------------------------------------");
        sb.AppendLine($"{"TOTALS",-NAME_WIDTH}{totalPA,PA_WIDTH}{totalAB,AB_WIDTH}{totalH,H_WIDTH}{totalRBI,RBI_WIDTH}{totalBB,BB_WIDTH}{totalK,K_WIDTH}{totalHR,HR_WIDTH}");
        sb.AppendLine();

        // Home team batting
        string homeHeader = $"{_homeTeamName.ToUpper()} BATTING".PadRight(NAME_WIDTH);
        sb.AppendLine($"{homeHeader}{"PA",PA_WIDTH}{"AB",AB_WIDTH}{"H",H_WIDTH}{"RBI",RBI_WIDTH}{"BB",BB_WIDTH}{"K",K_WIDTH}{"HR",HR_WIDTH}");
        sb.AppendLine("---------------------------------------------------------");

        for (int i = 0; i < 9; i++) {
            if (_boxScore.HomeBatters.TryGetValue(i, out var stats)) {
                string playerName = _homeLineup[i].Name;
                if (i == 8) playerName += " (DH)"; // Mark 9th batter as DH

                sb.AppendLine($"{playerName,-NAME_WIDTH}{stats.PA,PA_WIDTH}{stats.AB,AB_WIDTH}{stats.H,H_WIDTH}{stats.RBI,RBI_WIDTH}{stats.BB,BB_WIDTH}{stats.K,K_WIDTH}{stats.HR,HR_WIDTH}");
            }
        }

        // Totals
        var homeTotals = _boxScore.HomeBatters.Values;
        totalPA = homeTotals.Sum(b => b.PA);
        totalAB = homeTotals.Sum(b => b.AB);
        totalH = homeTotals.Sum(b => b.H);
        totalRBI = homeTotals.Sum(b => b.RBI);
        totalBB = homeTotals.Sum(b => b.BB);
        totalK = homeTotals.Sum(b => b.K);
        totalHR = homeTotals.Sum(b => b.HR);

        sb.AppendLine("---------------------------------------------------------");
        sb.AppendLine($"{"TOTALS",-NAME_WIDTH}{totalPA,PA_WIDTH}{totalAB,AB_WIDTH}{totalH,H_WIDTH}{totalRBI,RBI_WIDTH}{totalBB,BB_WIDTH}{totalK,K_WIDTH}{totalHR,HR_WIDTH}");

        return sb.ToString().TrimEnd();
    }

    private string FormatPitchingBoxScores() {
        var sb = new StringBuilder();

        // CRITICAL: Cross-check data integrity - batting runs must equal opposing pitching runs
        int homeBattingRuns = _lineScore.HomeTotal;
        int awayBattingRuns = _lineScore.AwayTotal;
        int homePitchingRuns = _boxScore.HomePitchers.Values.Sum(p => p.R);
        int awayPitchingRuns = _boxScore.AwayPitchers.Values.Sum(p => p.R);

        if (homeBattingRuns != awayPitchingRuns) {
            throw new InvalidOperationException(
                $"Data integrity error: Home batting runs ({homeBattingRuns}) != Away pitching runs ({awayPitchingRuns}). " +
                "This indicates a bug in run tracking during the game simulation.");
        }

        if (awayBattingRuns != homePitchingRuns) {
            throw new InvalidOperationException(
                $"Data integrity error: Away batting runs ({awayBattingRuns}) != Home pitching runs ({homePitchingRuns}). " +
                "This indicates a bug in run tracking during the game simulation.");
        }

        // Fixed column widths for pitching box score
        const int NAME_WIDTH = 24;
        const int IP_WIDTH = 5;
        const int BF_WIDTH = 4;
        const int H_WIDTH = 4;
        const int R_WIDTH = 4;
        const int ER_WIDTH = 4;
        const int BB_WIDTH = 5;
        const int K_WIDTH = 4;
        const int HR_WIDTH = 4;

        // Away team pitching
        string awayPitchingHeader = $"{_awayTeamName.ToUpper()} PITCHING".PadRight(NAME_WIDTH);
        sb.AppendLine($"{awayPitchingHeader}{"IP",IP_WIDTH}{"BF",BF_WIDTH}{"H",H_WIDTH}{"R",R_WIDTH}{"ER",ER_WIDTH}{"BB",BB_WIDTH}{"K",K_WIDTH}{"HR",HR_WIDTH}");
        sb.AppendLine("-------------------------------------------------------------");

        if (_boxScore.AwayPitchers.TryGetValue(0, out var awayPitcher)) {
            string pitcherName = $"{_awayTeamName} P";
            string ip = FormatInningsPitched(awayPitcher.OutsRecorded);

            sb.AppendLine($"{pitcherName,-NAME_WIDTH}{ip,IP_WIDTH}{awayPitcher.BF,BF_WIDTH}{awayPitcher.H,H_WIDTH}{awayPitcher.R,R_WIDTH}{awayPitcher.ER,ER_WIDTH}{awayPitcher.BB,BB_WIDTH}{awayPitcher.K,K_WIDTH}{awayPitcher.HR,HR_WIDTH}");
        }
        sb.AppendLine();

        // Home team pitching
        string homePitchingHeader = $"{_homeTeamName.ToUpper()} PITCHING".PadRight(NAME_WIDTH);
        sb.AppendLine($"{homePitchingHeader}{"IP",IP_WIDTH}{"BF",BF_WIDTH}{"H",H_WIDTH}{"R",R_WIDTH}{"ER",ER_WIDTH}{"BB",BB_WIDTH}{"K",K_WIDTH}{"HR",HR_WIDTH}");
        sb.AppendLine("-------------------------------------------------------------");

        if (_boxScore.HomePitchers.TryGetValue(0, out var homePitcher)) {
            string pitcherName = $"{_homeTeamName} P";
            string ip = FormatInningsPitched(homePitcher.OutsRecorded);

            sb.AppendLine($"{pitcherName,-NAME_WIDTH}{ip,IP_WIDTH}{homePitcher.BF,BF_WIDTH}{homePitcher.H,H_WIDTH}{homePitcher.R,R_WIDTH}{homePitcher.ER,ER_WIDTH}{homePitcher.BB,BB_WIDTH}{homePitcher.K,K_WIDTH}{homePitcher.HR,HR_WIDTH}");
        }

        return sb.ToString().TrimEnd();
    }

    private string FormatInningsPitched(int outsRecorded) {
        int fullInnings = outsRecorded / 3;
        int partialOuts = outsRecorded % 3;
        return $"{fullInnings}.{partialOuts:D1}";
    }

    private string FormatFooter() {
        var sb = new StringBuilder();
        sb.AppendLine($"Seed: {_seed}");
        sb.AppendLine($"LogHash: {CalculateLogHash()}");
        return sb.ToString().TrimEnd();
    }

    private string CalculateLogHash() {
        using var sha256 = SHA256.Create();

        // Normalize: trim trailing spaces from each line, join with \n
        var normalized = _playLog.Select(line => line.TrimEnd());
        string combined = string.Join("\n", normalized);

        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

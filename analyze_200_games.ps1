# Run 200 games and analyze scoring patterns including ties
Write-Host "Running 200 games with detailed analysis..." -ForegroundColor Green
Write-Host "This will take a few minutes..." -ForegroundColor Yellow
Write-Host ""

$results = @()
$startTime = Get-Date

for ($i = 1; $i -le 200; $i++) {
    $seed = Get-Random -Minimum 1 -Maximum 999999999

    if ($i % 10 -eq 0) {
        $elapsed = (Get-Date) - $startTime
        $avgPerGame = $elapsed.TotalSeconds / $i
        $remaining = [math]::Round(($avgPerGame * (200 - $i)), 0)
        Write-Host "Progress: $i/200 games ($([math]::Round($i/2, 0))%) - Est. $remaining seconds remaining" -ForegroundColor Yellow
    }

    $output = dotnet run --project src/DiamondSim/DiamondSim.csproj -- --home Home --away Away --seed $seed 2>&1 | Out-String

    if ($LASTEXITCODE -eq 0) {
        # Parse the final score line
        if ($output -match "Final: Away (\d+).+Home (\d+)") {
            $awayScore = [int]$Matches[1]
            $homeScore = [int]$Matches[2]

            # Check for extra innings by looking at the line score
            $extraInnings = $false
            if ($output -match "\|\s+\d+\s+\d+\s+\d+\s+\d+\s+\d+\s+\d+\s+\d+\s+\d+\s+\d+\s+(\d+)") {
                $extraInnings = $true
            }

            $results += [PSCustomObject]@{
                Game = $i
                Seed = $seed
                AwayScore = $awayScore
                HomeScore = $homeScore
                TotalRuns = $awayScore + $homeScore
                Shutout = ($awayScore -eq 0 -or $homeScore -eq 0)
                AwayShutout = ($awayScore -eq 0)
                HomeShutout = ($homeScore -eq 0)
                Tie = ($awayScore -eq $homeScore)
                ExtraInnings = $extraInnings
            }
        }
    } else {
        Write-Host "  Game $i FAILED with exit code $LASTEXITCODE" -ForegroundColor Red
    }
}

$totalTime = (Get-Date) - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "200-GAME ANALYSIS RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Completed in $([math]::Round($totalTime.TotalSeconds, 1)) seconds" -ForegroundColor White
Write-Host ""

# Calculate statistics
$totalGames = $results.Count
$awayWins = ($results | Where-Object { $_.AwayScore -gt $_.HomeScore }).Count
$homeWins = ($results | Where-Object { $_.HomeScore -gt $_.AwayScore }).Count
$ties = ($results | Where-Object { $_.Tie }).Count
$shutouts = ($results | Where-Object { $_.Shutout }).Count
$awayShutouts = ($results | Where-Object { $_.AwayShutout }).Count
$homeShutouts = ($results | Where-Object { $_.HomeShutout }).Count
$extraInningsGames = ($results | Where-Object { $_.ExtraInnings }).Count

$avgAwayScore = ($results | Measure-Object -Property AwayScore -Average).Average
$avgHomeScore = ($results | Measure-Object -Property HomeScore -Average).Average
$avgTotalRuns = ($results | Measure-Object -Property TotalRuns -Average).Average

$maxAwayScore = ($results | Measure-Object -Property AwayScore -Maximum).Maximum
$maxHomeScore = ($results | Measure-Object -Property HomeScore -Maximum).Maximum
$minAwayScore = ($results | Measure-Object -Property AwayScore -Minimum).Minimum
$minHomeScore = ($results | Measure-Object -Property HomeScore -Minimum).Minimum

Write-Host "Win/Loss/Tie Record:" -ForegroundColor White
Write-Host "  Away wins: $awayWins ($([math]::Round($awayWins/$totalGames*100, 1))%)" -ForegroundColor White
Write-Host "  Home wins: $homeWins ($([math]::Round($homeWins/$totalGames*100, 1))%)" -ForegroundColor White
Write-Host "  Ties: $ties ($([math]::Round($ties/$totalGames*100, 1))%)" -ForegroundColor $(if ($ties -gt 0) { "Cyan" } else { "White" })

Write-Host ""
Write-Host "Extra Innings:" -ForegroundColor White
if ($extraInningsGames -gt 0) {
    Write-Host "  Games with extra innings: $extraInningsGames ($([math]::Round($extraInningsGames/$totalGames*100, 1))%)" -ForegroundColor Cyan
} else {
    Write-Host "  No extra innings detected (Extras: OFF - ties allowed)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Scoring Statistics:" -ForegroundColor White
Write-Host "  Average Away score: $([math]::Round($avgAwayScore, 2))" -ForegroundColor White
Write-Host "  Average Home score: $([math]::Round($avgHomeScore, 2))" -ForegroundColor White
Write-Host "  Average total runs: $([math]::Round($avgTotalRuns, 2))" -ForegroundColor White
Write-Host "  Combined average: $([math]::Round(($avgAwayScore + $avgHomeScore)/2, 2)) runs per team" -ForegroundColor White

Write-Host ""
Write-Host "Score Ranges:" -ForegroundColor White
Write-Host "  Away: $minAwayScore to $maxAwayScore" -ForegroundColor White
Write-Host "  Home: $minHomeScore to $maxHomeScore" -ForegroundColor White

Write-Host ""
Write-Host "Shutouts:" -ForegroundColor White
$shutoutPct = [math]::Round($shutouts/$totalGames*100, 1)
Write-Host "  Total shutouts: $shutouts of $totalGames ($shutoutPct%)" -ForegroundColor White
Write-Host "  Away team shut out: $awayShutouts ($([math]::Round($awayShutouts/$totalGames*100, 1))%)" -ForegroundColor White
Write-Host "  Home team shut out: $homeShutouts ($([math]::Round($homeShutouts/$totalGames*100, 1))%)" -ForegroundColor White

Write-Host ""
Write-Host "Score Distribution (by team):" -ForegroundColor White
for ($score = 0; $score -le 15; $score++) {
    $awayCount = ($results | Where-Object { $_.AwayScore -eq $score }).Count
    $homeCount = ($results | Where-Object { $_.HomeScore -eq $score }).Count
    if ($awayCount -gt 0 -or $homeCount -gt 0) {
        Write-Host "  $score runs: Away=$awayCount, Home=$homeCount" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Tie Games Detail:" -ForegroundColor White
if ($ties -gt 0) {
    $tieGames = $results | Where-Object { $_.Tie }
    foreach ($game in $tieGames) {
        Write-Host "  Game $($game.Game): $($game.AwayScore)-$($game.HomeScore) (Seed: $($game.Seed))" -ForegroundColor Cyan
    }
} else {
    Write-Host "  No tie games in this run" -ForegroundColor White
}

Write-Host ""
Write-Host "Analysis vs Real MLB:" -ForegroundColor Yellow
Write-Host "  Home win rate: $([math]::Round($homeWins/$totalGames*100, 1))% (MLB: ~54%)" -ForegroundColor White
Write-Host "  Avg runs/team: $([math]::Round(($avgAwayScore + $avgHomeScore)/2, 2)) (MLB: ~4.5)" -ForegroundColor White
Write-Host "  Shutout rate: $shutoutPct% (MLB: ~5-8%)" -ForegroundColor White
Write-Host "  Tie rate: $([math]::Round($ties/$totalGames*100, 1))% (MLB: 0% - extras always played)" -ForegroundColor White

Write-Host ""
Write-Host "Potential Issues:" -ForegroundColor Yellow
$issues = @()

if ($avgAwayScore -lt 2 -or $avgHomeScore -lt 2) {
    $issues += "  - Average scores seem low (expected ~4-5 runs per team)"
}

if ($shutouts -gt ($totalGames * 0.3)) {
    $issues += "  - High shutout rate (>30%)"
}

if ([math]::Abs($avgAwayScore - $avgHomeScore) -gt 1.5) {
    $issues += "  - Significant home/away scoring imbalance"
}

$homeWinPct = $homeWins / $totalGames
if ($homeWinPct -lt 0.48 -or $homeWinPct -gt 0.62) {
    $issues += "  - Home win percentage outside expected range (48-62%)"
}

if ($ties -eq 0) {
    $issues += "  - No ties detected (expected some with Extras: OFF)"
}

if ($issues.Count -eq 0) {
    Write-Host "  None detected - all metrics within expected ranges!" -ForegroundColor Green
} else {
    foreach ($issue in $issues) {
        Write-Host $issue -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

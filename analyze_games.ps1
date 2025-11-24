# Run games and analyze scoring patterns
Write-Host "Running 20 games with detailed analysis..." -ForegroundColor Green

$results = @()

for ($i = 1; $i -le 20; $i++) {
    $seed = Get-Random -Minimum 1 -Maximum 999999

    Write-Host "Running game $i/20 (seed: $seed)..." -ForegroundColor Yellow

    $output = dotnet run --project src/DiamondSim/DiamondSim.csproj -- --home Home --away Away --seed $seed 2>&1 | Out-String

    if ($LASTEXITCODE -eq 0) {
        # Parse the final score line - using regex that handles the em dash
        if ($output -match "Final: Away (\d+).+Home (\d+)") {
            $awayScore = [int]$Matches[1]
            $homeScore = [int]$Matches[2]

            $results += [PSCustomObject]@{
                Game = $i
                Seed = $seed
                AwayScore = $awayScore
                HomeScore = $homeScore
                TotalRuns = $awayScore + $homeScore
                Shutout = ($awayScore -eq 0 -or $homeScore -eq 0)
            }

            $scoreDisplay = "Away $awayScore - Home $homeScore"
            if ($awayScore -eq 0 -or $homeScore -eq 0) {
                Write-Host "  $scoreDisplay (SHUTOUT)" -ForegroundColor Cyan
            } else {
                Write-Host "  $scoreDisplay" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "  FAILED with exit code $LASTEXITCODE" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ANALYSIS RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Calculate statistics
$totalGames = $results.Count
$awayWins = ($results | Where-Object { $_.AwayScore -gt $_.HomeScore }).Count
$homeWins = ($results | Where-Object { $_.HomeScore -gt $_.AwayScore }).Count
$ties = ($results | Where-Object { $_.AwayScore -eq $_.HomeScore }).Count
$shutouts = ($results | Where-Object { $_.Shutout }).Count
$awayShutouts = ($results | Where-Object { $_.AwayScore -eq 0 }).Count
$homeShutouts = ($results | Where-Object { $_.HomeScore -eq 0 }).Count

$avgAwayScore = ($results | Measure-Object -Property AwayScore -Average).Average
$avgHomeScore = ($results | Measure-Object -Property HomeScore -Average).Average
$avgTotalRuns = ($results | Measure-Object -Property TotalRuns -Average).Average

$maxAwayScore = ($results | Measure-Object -Property AwayScore -Maximum).Maximum
$maxHomeScore = ($results | Measure-Object -Property HomeScore -Maximum).Maximum
$minAwayScore = ($results | Measure-Object -Property AwayScore -Minimum).Minimum
$minHomeScore = ($results | Measure-Object -Property HomeScore -Minimum).Minimum

Write-Host ""
Write-Host "Win/Loss Record:" -ForegroundColor White
Write-Host "  Away wins: $awayWins" -ForegroundColor White
Write-Host "  Home wins: $homeWins" -ForegroundColor White
Write-Host "  Ties: $ties" -ForegroundColor White

Write-Host ""
Write-Host "Scoring Statistics:" -ForegroundColor White
Write-Host "  Average Away score: $([math]::Round($avgAwayScore, 2))" -ForegroundColor White
Write-Host "  Average Home score: $([math]::Round($avgHomeScore, 2))" -ForegroundColor White
Write-Host "  Average total runs: $([math]::Round($avgTotalRuns, 2))" -ForegroundColor White

Write-Host ""
Write-Host "Score Ranges:" -ForegroundColor White
Write-Host "  Away: $minAwayScore to $maxAwayScore" -ForegroundColor White
Write-Host "  Home: $minHomeScore to $maxHomeScore" -ForegroundColor White

Write-Host ""
Write-Host "Shutouts:" -ForegroundColor White
$shutoutPct = [math]::Round($shutouts/$totalGames*100, 1)
Write-Host "  Total shutouts: $shutouts of $totalGames ($shutoutPct percent)" -ForegroundColor White
Write-Host "  Away team shut out: $awayShutouts" -ForegroundColor White
Write-Host "  Home team shut out: $homeShutouts" -ForegroundColor White

Write-Host ""
Write-Host "Potential Issues:" -ForegroundColor Yellow
$issues = @()

if ($avgAwayScore -lt 2 -or $avgHomeScore -lt 2) {
    $issues += "  - Average scores seem low (expected ~4-5 runs per team)"
}

if ($shutouts -gt ($totalGames * 0.3)) {
    $issues += "  - High shutout rate (>30 percent)"
}

if ([math]::Abs($avgAwayScore - $avgHomeScore) -gt 2) {
    $issues += "  - Significant home/away scoring imbalance"
}

if ($awayShutouts -eq 0 -and $homeShutouts -eq 0) {
    $issues += "  - No shutouts at all (might indicate scoring is too high)"
}

if ($issues.Count -eq 0) {
    Write-Host "  None detected - scoring patterns look reasonable!" -ForegroundColor Green
} else {
    foreach ($issue in $issues) {
        Write-Host $issue -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

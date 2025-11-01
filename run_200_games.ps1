# Run 200 games and save output to a text file
# Records all seeds at the end for reproducibility

$outputFile = "200_games_output_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
$seeds = @()

Write-Host "Running 200 games..." -ForegroundColor Green
Write-Host "Output will be saved to: $outputFile" -ForegroundColor Cyan

# Create output file with header
$header = @"
================================================================================
DiamondSim - 200 Game Run
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
================================================================================

"@

$header | Out-File -FilePath $outputFile -Encoding UTF8

# Run 200 games
for ($i = 1; $i -le 200; $i++) {
    # Generate a random seed
    $seed = Get-Random -Minimum 1 -Maximum 999999999
    $seeds += $seed

    Write-Host "Running game $i/200 (seed: $seed)..." -ForegroundColor Yellow

    # Add game separator
    $separator = @"

================================================================================
GAME $i - Seed: $seed
================================================================================

"@
    $separator | Out-File -FilePath $outputFile -Append -Encoding UTF8

    # Run the game and append output
    dotnet run --project src/DiamondSim/DiamondSim.csproj -- --home Home --away Away --seed $seed | Out-File -FilePath $outputFile -Append -Encoding UTF8
}

# Add seeds summary at the end
$seedsSummary = @"


================================================================================
SEEDS USED (for reproducibility)
================================================================================

"@

$seedsSummary | Out-File -FilePath $outputFile -Append -Encoding UTF8

for ($i = 0; $i -lt $seeds.Count; $i++) {
    $gameNum = $i + 1
    "Game $($gameNum) : $($seeds[$i])" | Out-File -FilePath $outputFile -Append -Encoding UTF8
}

# Add final summary
$finalSummary = @"

================================================================================
RUN COMPLETE
================================================================================
Total Games: 200
Output File: $outputFile
Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
================================================================================
"@

$finalSummary | Out-File -FilePath $outputFile -Append -Encoding UTF8

Write-Host "`nRun complete!" -ForegroundColor Green
Write-Host "Output saved to: $outputFile" -ForegroundColor Cyan
Write-Host "All 200 seeds have been recorded at the end of the file." -ForegroundColor Green

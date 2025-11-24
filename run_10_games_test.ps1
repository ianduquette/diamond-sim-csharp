# Quick test script to run 10 games
Write-Host "Running 10 games to verify system stability..." -ForegroundColor Green

$failedGames = 0
$seeds = @()

for ($i = 1; $i -le 10; $i++) {
    $seed = Get-Random -Minimum 1 -Maximum 999999
    $seeds += $seed

    Write-Host "Running game $i/10 (seed: $seed)..." -ForegroundColor Yellow

    $output = dotnet run --project src/DiamondSim/DiamondSim.csproj -- --home Home --away Away --seed $seed 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Success: Game $i completed" -ForegroundColor Green
    } else {
        Write-Host "  Failed: Game $i with exit code $LASTEXITCODE" -ForegroundColor Red
        $failedGames++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Results:" -ForegroundColor Cyan
$successCount = 10 - $failedGames
Write-Host "  Total games: 10" -ForegroundColor White
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failedGames" -ForegroundColor $(if ($failedGames -eq 0) { "Green" } else { "Red" })
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Seeds used:" -ForegroundColor Cyan
for ($i = 0; $i -lt $seeds.Count; $i++) {
    $gameNum = $i + 1
    $seedValue = $seeds[$i]
    Write-Host "  Game ${gameNum}: $seedValue"
}

if ($failedGames -gt 0) {
    exit 1
}

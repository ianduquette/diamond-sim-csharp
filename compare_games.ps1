$seeds = @(192268583, 30157548, 650162642, 337214187, 1187367659, 612099523, 213676624, 1624110099, 1670579016, 124450555, 1031963720, 692792209, 2027149368, 2094557560, 1894401387, 1745202094, 2012257534, 1899310522, 1097523714, 641481502)

Write-Host "Running all 20 games and extracting line scores..." -ForegroundColor Cyan
Write-Host ""

foreach ($seed in $seeds) {
    Write-Host "Seed: $seed" -ForegroundColor Yellow

    $output = & dotnet run --project src/DiamondSim -- --home Robots --away Androids --seed $seed 2>&1 | Out-String

    # Extract just the line score section
    if ($output -match '(?s)([ ]+\|.*?\n-+\|.*?\n.*?\n.*?\n)') {
        Write-Host $matches[1]
    }

    Write-Host ""
}

Write-Host "Complete!" -ForegroundColor Green

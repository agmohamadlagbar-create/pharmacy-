$transcriptPath = 'C:\Users\LENOVO\.gemini\antigravity\brain\d726b2b7-4350-46c2-a664-ce0995567b8c\.system_generated\logs\transcript.jsonl'

# Mapping of files to the step index where the original full content was viewed before modifications
$originalSteps = @{
    'backend\OnlineStore.Api\Controllers\OrdersController.cs' = 30
    'backend\OnlineStore.Api\Program.cs' = 272
    'backend\OnlineStore.Api\Controllers\DeliveryController.cs' = 34
    'backend\OnlineStore.Api\Controllers\CartController.cs' = 841
    'backend\OnlineStore.Api\Contracts\AuthDtos.cs' = 935
    'web\online-store-web\src\App.jsx' = 52
    'web\online-store-web\src\pages\CategoriesPage.jsx' = 56
    'web\online-store-web\src\pages\PaymentPage.jsx' = 863
    'web\online-store-web\src\pages\ProductInfoPage.jsx' = 897
    'web\online-store-web\src\pages\ProductInfoPage.css' = 975
    'backend\OnlineStore.Infrastructure\Persistence\ApplicationDbContext.cs' = 22
}

# List of files created for analytics feature that need to be removed
$filesToDelete = @(
    'backend\OnlineStore.Domain\Entities\StoreMonthlyAnalytics.cs'
    'backend\OnlineStore.Domain\Entities\StoreYearlyAnalytics.cs'
    'backend\OnlineStore.Domain\Entities\Payment.cs'
    'backend\OnlineStore.Api\Services\AnalyticsBackgroundService.cs'
    'backend\OnlineStore.Api\Controllers\StoreAnalyticsController.cs'
    'backend\OnlineStore.Api\Services\IAnalyticsService.cs'
    'backend\OnlineStore.Api\Services\AnalyticsService.cs'
    'backend\OnlineStore.Api\Services\IAnalyticsPdfService.cs'
    'backend\OnlineStore.Api\Services\AnalyticsPdfService.cs'
    'backend\OnlineStore.Api\Contracts\StoreAnalyticsDto.cs'
    'web\online-store-web\src\pages\StoreAnalyticsPage.jsx'
    'web\online-store-web\src\pages\StoreAnalyticsPage.css'
    'mobile\online_store_mobile\lib\store_analytics_page.dart'
)

function Get-OriginalContent {
    param([int]$stepIndex)
    $jsonLine = Get-Content -Path $transcriptPath | Where-Object { $_ -match "\"step_index\": $stepIndex" -and $_ -match "\"type\":\"VIEW_FILE\"" }
    if (-not $jsonLine) { return $null }
    $obj = $jsonLine | ConvertFrom-Json -ErrorAction SilentlyContinue
    $content = $obj.content
    # Strip line numbers like "123: "
    $clean = $content -replace '^\d+:\s?', '' -replace "`r`n", "`n"
    return $clean
}

foreach ($relativePath in $originalSteps.Keys) {
    $step = $originalSteps[$relativePath]
    $original = Get-OriginalContent -stepIndex $step
    if ($null -eq $original) {
        Write-Host "Unable to retrieve original content for $relativePath (step $step)"
        continue
    }
    $fullPath = Join-Path 'd:\\LAGBAR SHOP\\OnlineStore_updated\\OnlineStore' $relativePath
    Set-Content -Path $fullPath -Value $original -Encoding UTF8
    Write-Host "Restored $relativePath"
}

foreach ($rel in $filesToDelete) {
    $full = Join-Path 'd:\\LAGBAR SHOP\\OnlineStore_updated\\OnlineStore' $rel
    if (Test-Path $full) {
        Remove-Item -Path $full -Force
        Write-Host "Deleted $rel"
    } else {
        Write-Host "File not found (already deleted): $rel"
    }
}

Write-Host "Rollback to pre-analytics version completed."

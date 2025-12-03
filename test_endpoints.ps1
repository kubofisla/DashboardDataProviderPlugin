# Test script for DashboardDataProvider HTTP endpoints

$baseUrl = "http://localhost:8080/dashboarddata"

# Helper for nice separators
function Write-Section($title) {
    Write-Host "`n==============================" -ForegroundColor Cyan
    Write-Host $title -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
}

Write-Section "GET /dashboarddata/"
try {
    $get = Invoke-WebRequest -Uri "$baseUrl/" -Method Get
    Write-Host "StatusCode: $($get.StatusCode)"
    Write-Host "Response:  $($get.Content)"
}
catch {
    Write-Host "Failed to reach GET endpoint. Is SimHub running and is the plugin loaded?" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Section "POST /settarget (targetTime: 60)"
try {
    $bodySet = @{ targetTime = 60 } | ConvertTo-Json
    $set = Invoke-WebRequest -Uri "$baseUrl/settarget" -Method Post -Body $bodySet -ContentType "application/json"
    Write-Host "StatusCode: $($set.StatusCode)"
    Write-Host "Response:  $($set.Content)"
}
catch {
    Write-Host "Failed to call /settarget" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Section "POST /adjust (delta: 0.1)"
try {
    $bodyAdjust = @{ delta = 0.1 } | ConvertTo-Json
    $adjust = Invoke-WebRequest -Uri "$baseUrl/adjust" -Method Post -Body $bodyAdjust -ContentType "application/json"
    Write-Host "StatusCode: $($adjust.StatusCode)"
    Write-Host "Response:  $($adjust.Content)"
}
catch {
    Write-Host "Failed to call /adjust" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Section "POST /resettofast"
try {
    $resetFast = Invoke-WebRequest -Uri "$baseUrl/resettofast" -Method Post
    Write-Host "StatusCode: $($resetFast.StatusCode)"
    Write-Host "Response:  $($resetFast.Content)"
}
catch {
    Write-Host "Failed to call /resettofast (may fail if no fastest lap is available)." -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Yellow
}

Write-Section "POST /resettolast"
try {
    $resetLast = Invoke-WebRequest -Uri "$baseUrl/resettolast" -Method Post
    Write-Host "StatusCode: $($resetLast.StatusCode)"
    Write-Host "Response:  $($resetLast.Content)"
}
catch {
    Write-Host "Failed to call /resettolast (may fail if no last lap is available)." -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Yellow
}

Write-Host "`nAll endpoint tests completed." -ForegroundColor Green

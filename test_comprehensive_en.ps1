# ============================================================
# AUHUB PROJECT COMPREHENSIVE TEST
# Execution time: ~5-7 minutes
# Author: OpenCode AI
# Date: 13.05.2026
# ============================================================

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$baseUrl = "http://localhost:5000"
$passCount = 0
$failCount = 0
$totalTests = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200,
        [string]$ExpectedError = $null
    )
    
    $script:totalTests++
    Write-Host "`n[$script:totalTests] $Name" -ForegroundColor Cyan
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            UseBasicParsing = $true
            ErrorAction = 'Stop'
        }
        
        if ($Body) {
            $params['Body'] = $Body
            $params['ContentType'] = 'application/json'
        }
        
        if ($Headers.Count -gt 0) {
            $params['Headers'] = $Headers
        }
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "  PASS: HTTP $($response.StatusCode)" -ForegroundColor Green
            $script:passCount++
            return ($response.Content | ConvertFrom-Json)
        } else {
            Write-Host "  FAIL: Expected $ExpectedStatus, got $($response.StatusCode)" -ForegroundColor Red
            Write-Host "    Response: $($response.Content)" -ForegroundColor Yellow
            $script:failCount++
            return $null
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  PASS: HTTP $statusCode" -ForegroundColor Green
            if ($ExpectedError) {
                Write-Host "    Expected error: $ExpectedError" -ForegroundColor Gray
            }
            $script:passCount++
            return $null
        } else {
            Write-Host "  FAIL: Expected $ExpectedStatus, got $statusCode" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
            # Show response body for debugging
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "    Response: $responseBody" -ForegroundColor Yellow
            } catch {
                Write-Host "    (Could not read response body)" -ForegroundColor Gray
            }
            $script:failCount++
            return $null
        }
    }
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  AUHUB PROJECT COMPREHENSIVE TEST" -ForegroundColor Cyan
Write-Host "  Start: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# ============================================================
# PHASE 1: AUTHENTICATION
# ============================================================
Write-Host "`n=== PHASE 1: AUTHENTICATION ===" -ForegroundColor Yellow

# 1.1 Register Admin
$adminBody = @{
    email = "admin@test.com"
    password = "Admin123!"
    name = "Test Admin"
    role = 1
} | ConvertTo-Json

$adminReg = Test-Endpoint `
    -Name "Register Admin user" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $adminBody `
    -ExpectedStatus 200

$adminToken = $adminReg.accessToken

# 1.2 Register User
$userBody = @{
    email = "user@test.com"
    password = "User123!"
    name = "Test User"
    role = 0
} | ConvertTo-Json

$userReg = Test-Endpoint `
    -Name "Register User" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $userBody `
    -ExpectedStatus 200

$userToken = $userReg.accessToken

# 1.3 Validation: weak password
$weakPwdBody = @{
    email = "weak@test.com"
    password = "weak1"
    name = "Test"
    role = 0
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: weak password (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $weakPwdBody `
    -ExpectedStatus 400 `
    -ExpectedError "Password must be at least 8 characters"

# 1.4 Validation: invalid email
$invalidEmailBody = @{
    email = "notanemail"
    password = "Valid123!"
    name = "Test"
    role = 0
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: invalid email (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $invalidEmailBody `
    -ExpectedStatus 400 `
    -ExpectedError "Invalid email format"

# 1.5 Validation: invalid role
$invalidRoleBody = @{
    email = "test@test.com"
    password = "Valid123!"
    name = "Test"
    role = 99
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: invalid role (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $invalidRoleBody `
    -ExpectedStatus 400 `
    -ExpectedError "Invalid role"

# 1.6 Duplicate email
Test-Endpoint `
    -Name "Duplicate email (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $adminBody `
    -ExpectedStatus 400 `
    -ExpectedError "User with this email already exists"

# 1.7 Login with correct credentials
$loginBody = @{
    email = "admin@test.com"
    password = "Admin123!"
} | ConvertTo-Json

Test-Endpoint `
    -Name "Login with correct credentials" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/login" `
    -Body $loginBody `
    -ExpectedStatus 200

# 1.8 Login with wrong password
$wrongPwdBody = @{
    email = "admin@test.com"
    password = "WrongPassword123!"
} | ConvertTo-Json

Test-Endpoint `
    -Name "Login with wrong password (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/login" `
    -Body $wrongPwdBody `
    -ExpectedStatus 401 `
    -ExpectedError "Invalid email or password"

# ============================================================
# PHASE 2: LOT MANAGEMENT
# ============================================================
Write-Host "`n=== PHASE 2: LOT MANAGEMENT ===" -ForegroundColor Yellow

# 2.1 Create lot as Admin
$lotBody = @{
    title = "Vintage Rolex Watch"
    description = "Rare Rolex Submariner from 1965"
    startingPrice = 5000
    startTime = "2026-05-18T10:00:00Z"
    endTime = "2026-05-18T12:00:00Z"
} | ConvertTo-Json

$adminHeaders = @{
    "Authorization" = "Bearer $adminToken"
}

$lotResult = Test-Endpoint `
    -Name "Create lot as Admin" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $lotBody `
    -Headers $adminHeaders `
    -ExpectedStatus 201

$lotId = $lotResult.lotId
Write-Host "  -> Lot ID: $lotId" -ForegroundColor Gray

# 2.2 User tries to create lot (should be forbidden)
$userHeaders = @{
    "Authorization" = "Bearer $userToken"
}

Test-Endpoint `
    -Name "User tries to create lot (should be forbidden)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $lotBody `
    -Headers $userHeaders `
    -ExpectedStatus 403 `
    -ExpectedError "User does not have the required role(s): Admin"

# 2.3 Create lot without authorization
Test-Endpoint `
    -Name "Create lot without authorization (should be forbidden)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $lotBody `
    -ExpectedStatus 401

# 2.4 Validation: title too short
$shortTitleBody = @{
    title = "AB"
    description = "Valid description"
    startingPrice = 100
    startTime = "2026-05-18T10:00:00Z"
    endTime = "2026-05-18T12:00:00Z"
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: title too short (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $shortTitleBody `
    -Headers $adminHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Title must be at least 3 characters"

# 2.5 Validation: negative price
$negativePriceBody = @{
    title = "ValidTitle"
    description = "Valid description"
    startingPrice = -100
    startTime = "2026-05-18T10:00:00Z"
    endTime = "2026-05-18T12:00:00Z"
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: negative price (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $negativePriceBody `
    -Headers $adminHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Starting price must be greater than 0"

# 2.6 Publish lot by owner
Test-Endpoint `
    -Name "Publish lot by owner" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/publish" `
    -Headers $adminHeaders `
    -ExpectedStatus 200

# 2.7 User tries to publish someone else's lot
$lot2Body = @{
    title = "Second Test Lot"
    description = "For ownership test"
    startingPrice = 1000
    startTime = "2026-05-18T10:00:00Z"
    endTime = "2026-05-18T12:00:00Z"
} | ConvertTo-Json

$lot2Result = Test-Endpoint `
    -Name "Create second lot for ownership test" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $lot2Body `
    -Headers $adminHeaders `
    -ExpectedStatus 201

$lot2Id = $lot2Result.lotId

Test-Endpoint `
    -Name "User tries to publish someone else's lot (should be forbidden)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lot2Id/publish" `
    -Headers $userHeaders `
    -ExpectedStatus 403 `
    -ExpectedError "You are not the owner of this lot"

# 2.8 Get all lots (public endpoint)
Test-Endpoint `
    -Name "Get all lots (public endpoint)" `
    -Method "GET" `
    -Url "$baseUrl/api/lots" `
    -ExpectedStatus 200

# 2.9 Get lot details (public endpoint)
$lotDetails = Test-Endpoint `
    -Name "Get lot details (public endpoint)" `
    -Method "GET" `
    -Url "$baseUrl/api/lots/$lotId" `
    -ExpectedStatus 200

Write-Host "  -> Current Price: $($lotDetails.currentPrice)" -ForegroundColor Gray
Write-Host "  -> Status: $($lotDetails.status)" -ForegroundColor Gray

# ============================================================
# PHASE 3: BIDS (CRITICAL - TESTING FIXED BUG!)
# ============================================================
Write-Host "`n=== PHASE 3: BIDS (TESTING FIXED BUG!) ===" -ForegroundColor Yellow

# 3.1 User places first bid
$bid1Body = @{
    amount = 5500
} | ConvertTo-Json

$bid1Result = Test-Endpoint `
    -Name "User places first bid (5500)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $bid1Body `
    -Headers $userHeaders `
    -ExpectedStatus 200

Write-Host "  -> New Current Price: $($bid1Result.newCurrentPrice)" -ForegroundColor Gray

# 3.2 CRITICAL: Check that CurrentPrice updated in DB!
Start-Sleep -Seconds 1
$lotAfterBid1 = Test-Endpoint `
    -Name "CRITICAL: Check CurrentPrice in DB after first bid" `
    -Method "GET" `
    -Url "$baseUrl/api/lots/$lotId" `
    -ExpectedStatus 200

if ($lotAfterBid1.currentPrice -eq 5500) {
    Write-Host "  *** CRITICAL: CurrentPrice = 5500 in DB! BUG FIXED!" -ForegroundColor Green -BackgroundColor DarkGreen
} else {
    Write-Host "  *** CRITICAL: CurrentPrice = $($lotAfterBid1.currentPrice), expected 5500! BUG NOT FIXED!" -ForegroundColor Red -BackgroundColor DarkRed
}

# 3.3 Register second User
$user2Body = @{
    email = "user2@test.com"
    password = "User123!"
    name = "Test User 2"
    role = 0
} | ConvertTo-Json

$user2Reg = Test-Endpoint `
    -Name "Register second User for bid test" `
    -Method "POST" `
    -Url "$baseUrl/api/auth/register" `
    -Body $user2Body `
    -ExpectedStatus 200

$user2Token = $user2Reg.accessToken
$user2Headers = @{
    "Authorization" = "Bearer $user2Token"
}

# 3.4 Second User places second bid
$bid2Body = @{
    amount = 6000
} | ConvertTo-Json

$bid2Result = Test-Endpoint `
    -Name "Second User places second bid (6000)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $bid2Body `
    -Headers $user2Headers `
    -ExpectedStatus 200

Write-Host "  -> New Current Price: $($bid2Result.newCurrentPrice)" -ForegroundColor Gray

# 3.5 CRITICAL: Check that CurrentPrice updated after second bid!
Start-Sleep -Seconds 1
$lotAfterBid2 = Test-Endpoint `
    -Name "CRITICAL: Check CurrentPrice in DB after second bid" `
    -Method "GET" `
    -Url "$baseUrl/api/lots/$lotId" `
    -ExpectedStatus 200

if ($lotAfterBid2.currentPrice -eq 6000) {
    Write-Host "  *** CRITICAL: CurrentPrice = 6000 in DB! BUG FIXED!" -ForegroundColor Green -BackgroundColor DarkGreen
} else {
    Write-Host "  *** CRITICAL: CurrentPrice = $($lotAfterBid2.currentPrice), expected 6000! BUG NOT FIXED!" -ForegroundColor Red -BackgroundColor DarkRed
}

# 3.6 Try to place bid lower than current price
$lowBidBody = @{
    amount = 5000
} | ConvertTo-Json

Test-Endpoint `
    -Name "Try to place bid lower than current price (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $lowBidBody `
    -Headers $userHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Bid amount must be higher than current price"

# 3.7 Admin tries to bid on own lot
$adminBidBody = @{
    amount = 7000
} | ConvertTo-Json

Test-Endpoint `
    -Name "Admin tries to bid on own lot (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $adminBidBody `
    -Headers $adminHeaders `
    -ExpectedStatus 403 `
    -ExpectedError "You cannot bid on your own lot"

# 3.8 Bid without authorization
Test-Endpoint `
    -Name "Bid without authorization (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $bid1Body `
    -ExpectedStatus 401

# 3.9 Validation: negative bid
$negativeBidBody = @{
    amount = -100
} | ConvertTo-Json

Test-Endpoint `
    -Name "Validation: negative bid (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $negativeBidBody `
    -Headers $userHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Bid amount must be greater than 0"

# 3.10 Get bid history
$bidsHistory = Test-Endpoint `
    -Name "Get bid history (public endpoint)" `
    -Method "GET" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -ExpectedStatus 200

Write-Host "  -> Number of bids: $($bidsHistory.bids.Count)" -ForegroundColor Gray

# ============================================================
# PHASE 4: LOT COMPLETION AND CANCELLATION
# ============================================================
Write-Host "`n=== PHASE 4: LOT COMPLETION AND CANCELLATION ===" -ForegroundColor Yellow

# 4.1 Create third lot for cancel test
$lot3Body = @{
    title = "Lot for Cancel Test"
    description = "This lot will be cancelled"
    startingPrice = 500
    startTime = "2026-05-18T10:00:00Z"
    endTime = "2026-05-18T12:00:00Z"
} | ConvertTo-Json

$lot3Result = Test-Endpoint `
    -Name "Create third lot for cancel test" `
    -Method "POST" `
    -Url "$baseUrl/api/lots" `
    -Body $lot3Body `
    -Headers $adminHeaders `
    -ExpectedStatus 201

$lot3Id = $lot3Result.lotId

# 4.2 Cancel Draft lot by owner
Test-Endpoint `
    -Name "Cancel Draft lot by owner" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lot3Id/cancel" `
    -Headers $adminHeaders `
    -ExpectedStatus 200

# 4.3 Try to cancel already cancelled lot
Test-Endpoint `
    -Name "Try to cancel already cancelled lot (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lot3Id/cancel" `
    -Headers $adminHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Lot is already cancelled"

# 4.4 Complete active lot by owner
Test-Endpoint `
    -Name "Complete active lot by owner" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/complete" `
    -Headers $adminHeaders `
    -ExpectedStatus 200

# 4.5 User tries to complete someone else's lot
Test-Endpoint `
    -Name "Publish lot2 for completion test" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lot2Id/publish" `
    -Headers $adminHeaders `
    -ExpectedStatus 200

Test-Endpoint `
    -Name "User tries to complete someone else's lot (should be forbidden)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lot2Id/complete" `
    -Headers $userHeaders `
    -ExpectedStatus 403 `
    -ExpectedError "You are not the owner of this lot"

# 4.6 Try to bid on completed lot
$bidOnCompletedBody = @{
    amount = 10000
} | ConvertTo-Json

Test-Endpoint `
    -Name "Try to bid on completed lot (should be rejected)" `
    -Method "POST" `
    -Url "$baseUrl/api/lots/$lotId/bids" `
    -Body $bidOnCompletedBody `
    -Headers $userHeaders `
    -ExpectedStatus 400 `
    -ExpectedError "Lot is not active"

# ============================================================
# FINAL REPORT
# ============================================================
Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  FINAL REPORT" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Total tests: $totalTests" -ForegroundColor White
Write-Host "  Passed: $passCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor Red
Write-Host "  Success rate: $([math]::Round(($passCount / $totalTests) * 100, 2))%" -ForegroundColor $(if ($failCount -eq 0) { 'Green' } else { 'Yellow' })
Write-Host "  End: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

if ($failCount -eq 0) {
    Write-Host "`nALL TESTS PASSED! PROJECT READY FOR DEMO!" -ForegroundColor Green -BackgroundColor DarkGreen
} else {
    Write-Host "`nSOME TESTS FAILED! FIXES REQUIRED!" -ForegroundColor Yellow -BackgroundColor DarkYellow
}

Write-Host "`n"
Read-Host "Press ENTER to exit"

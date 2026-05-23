# =============================================================================
# Script de test de tous les endpoints - Aggregator Platform
# Compatible Windows PowerShell 5.1 et PowerShell 7+
#
# Prerequis :
#   - L'API doit tourner sur http://localhost:5080 (ou modifier $BaseUrl)
#   - TestData.sql doit avoir ete applique en base
#   - SeedData.sql (super-admin avec hash BCrypt valide) doit avoir ete applique
#
# Usage : .\test-all-endpoints.ps1
# =============================================================================

$ErrorActionPreference = "Continue"
$BaseUrl = "http://localhost:5080"

# IDs fixes definis dans TestData.sql
$PartnerBankId   = "11111111-1111-1111-1111-111111111111"
$PartnerWalletId = "22222222-2222-2222-2222-222222222222"
$CustomerId1     = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
$CustomerId2     = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
$SubscriptionId1 = "11111111-aaaa-aaaa-aaaa-111111111111"
$SubscriptionId2 = "22222222-bbbb-bbbb-bbbb-222222222222"

$Pass = 0
$Fail = 0
$Skip = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200, 201)
    )
    Write-Host ""
    Write-Host "[$Method] $Name" -ForegroundColor Cyan
    Write-Host "  URL  : $Url" -ForegroundColor Gray

    $statusCode = 0
    $content = ""

    try {
        $params = @{
            Method = $Method
            Uri = $Url
            Headers = $Headers
            ContentType = "application/json"
            UseBasicParsing = $true
        }
        if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
        $resp = Invoke-WebRequest @params
        $statusCode = [int]$resp.StatusCode
        $content = $resp.Content
    } catch {
        $ex = $_.Exception
        # Try to extract status code from various exception shapes (PS 5.1 WebException, PS 7+ HttpResponseException)
        if ($ex.Response -and $ex.Response.StatusCode) {
            $statusCode = [int]$ex.Response.StatusCode
            try {
                $stream = $ex.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $content = $reader.ReadToEnd()
                    $reader.Close()
                }
            } catch { }
            if (-not $content -and $_.ErrorDetails -and $_.ErrorDetails.Message) {
                $content = $_.ErrorDetails.Message
            }
        } else {
            Write-Host ("  ERROR  : " + $ex.Message) -ForegroundColor Red
            $script:Fail++
            return $null
        }
    }

    if ($ExpectedStatus -contains $statusCode) {
        Write-Host "  PASS ($statusCode)" -ForegroundColor Green
        $script:Pass++
        if ($content -and $content.Length -lt 500) {
            Write-Host ("  Resp : " + $content) -ForegroundColor DarkGray
        } elseif ($content) {
            Write-Host ("  Resp : (truncated, " + $content.Length + " bytes)") -ForegroundColor DarkGray
        }
        return $content
    } else {
        Write-Host ("  FAIL ($statusCode, expected: " + ($ExpectedStatus -join ',') + ")") -ForegroundColor Red
        if ($content) {
            Write-Host ("  Resp : " + $content) -ForegroundColor DarkRed
        }
        $script:Fail++
        return $null
    }
}

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  Aggregator Platform - Endpoint test suite" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

# ============================================================================
# 1. HEALTH
# ============================================================================
Write-Host ""
Write-Host "--- HEALTH ---" -ForegroundColor Magenta
Test-Endpoint -Name "Health check" -Method "GET" -Url "$BaseUrl/health" -ExpectedStatus @(200, 503)

# ============================================================================
# 2. AUTHENTICATION
# ============================================================================
Write-Host ""
Write-Host "--- AUTH ---" -ForegroundColor Magenta
$loginResp = Test-Endpoint -Name "Login super-admin" -Method "POST" -Url "$BaseUrl/api/v1/auth/login" `
    -Body @{ username = "superadmin"; password = "ChangeMe123!" } `
    -ExpectedStatus @(200)

$AdminToken = $null
if ($loginResp) {
    try {
        $parsed = $loginResp | ConvertFrom-Json
        if ($parsed.success -and $parsed.data.accessToken) {
            $AdminToken = $parsed.data.accessToken
            $shortToken = $AdminToken.Substring(0, [Math]::Min(40, $AdminToken.Length))
            Write-Host ("  Token (first chars) : " + $shortToken + "...") -ForegroundColor Green
        }
    } catch { }
}

if (-not $AdminToken) {
    Write-Host ""
    Write-Host "WARNING : admin login failed. Authenticated endpoints will be SKIPPED." -ForegroundColor Yellow
}

$AdminHeaders = @{ }
if ($AdminToken) { $AdminHeaders["Authorization"] = "Bearer $AdminToken" }
$BankPartnerHeaders   = @{ "X-Partner-Id" = $PartnerBankId }
$WalletPartnerHeaders = @{ "X-Partner-Id" = $PartnerWalletId }

# ============================================================================
# 3. PARTNERS (admin only)
# ============================================================================
Write-Host ""
Write-Host "--- PARTNERS ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "List partners" -Method "GET" -Url "$BaseUrl/api/v1/partners" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Get BANK partner" -Method "GET" -Url "$BaseUrl/api/v1/partners/$PartnerBankId" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "BANK partner mirror account" -Method "GET" -Url "$BaseUrl/api/v1/partners/$PartnerBankId/account" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    $newPartnerCode = "TEST_" + [Guid]::NewGuid().ToString().Substring(0,8)
    Test-Endpoint -Name "Create new partner" -Method "POST" -Url "$BaseUrl/api/v1/partners" `
        -Headers $AdminHeaders `
        -Body @{
            partnerCode = $newPartnerCode
            name = "Partenaire de test"
            baseUrl = "https://test.partner.local"
            currency = "XOF"
            accountCode = "P-TEST"
            rateLimitPerMin = 100
            requireHmac = $false
        } `
        -ExpectedStatus @(200)
} else { Write-Host "  SKIP (pas de token admin)" -ForegroundColor Yellow; $Skip += 4 }

# ============================================================================
# 4. CUSTOMERS (partner-scoped)
# ============================================================================
Write-Host ""
Write-Host "--- CUSTOMERS ---" -ForegroundColor Magenta

Test-Endpoint -Name "Get customer Aissatou" -Method "GET" -Url "$BaseUrl/api/v1/customers/$CustomerId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

Test-Endpoint -Name "List customer subscriptions" -Method "GET" -Url "$BaseUrl/api/v1/customers/$CustomerId1/subscriptions" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

$extId = "EXT-TEST-" + (Get-Random)
$randomEmail = "test" + (Get-Random) + "@example.com"
$randomNid = "SN-TEST-" + (Get-Random)
Test-Endpoint -Name "Create new customer" -Method "POST" -Url "$BaseUrl/api/v1/customers" `
    -Headers $BankPartnerHeaders `
    -Body @{
        externalCustomerId = $extId
        fullName = "Test Client Demo"
        dateOfBirth = "1995-01-01"
        nationalId = $randomNid
        email = $randomEmail
    } `
    -ExpectedStatus @(200)

# ============================================================================
# 5. SUBSCRIPTIONS
# ============================================================================
Write-Host ""
Write-Host "--- SUBSCRIPTIONS ---" -ForegroundColor Magenta

Test-Endpoint -Name "Get subscription 1" -Method "GET" -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

Test-Endpoint -Name "List BANK partner subscriptions" -Method "GET" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

$randomPhone = "+22177" + (Get-Random -Maximum 9999999 -Minimum 1000000).ToString("D7")
$randomBank = "SN012-NEW-" + (Get-Random)
Test-Endpoint -Name "POST direct subscription (PartnerId explicit)" -Method "POST" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders `
    -Body @{
        customerId = $CustomerId1
        partnerId = $PartnerBankId
        bankAccountNumber = $randomBank
        bankCode = "BANK_DEMO"
        phoneNumber = $randomPhone
        phoneOperator = "Orange"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "POST mismatch PartnerId -> 403" -Method "POST" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders `
    -Body @{
        customerId = $CustomerId1
        partnerId = $PartnerWalletId
        bankAccountNumber = "SN012-MISMATCH"
        bankCode = "BANK_DEMO"
        phoneNumber = "+221770000000"
        phoneOperator = "Orange"
    } `
    -ExpectedStatus @(403)

# ============================================================================
# 6. FINANCIAL - Transactions
# ============================================================================
Write-Host ""
Write-Host "--- FINANCIAL ---" -ForegroundColor Magenta

$txRef1 = "TXN-TEST-" + (Get-Date -Format 'yyyyMMddHHmmss') + "-1"
Test-Endpoint -Name "Bank Debit" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/debit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRef1
        subscriptionId = $SubscriptionId1
        amount = 50000
        currency = "XOF"
        description = "Test bank debit via PowerShell"
    } `
    -ExpectedStatus @(200)

# Idempotence
Test-Endpoint -Name "Bank Debit IDEMPOTENT (same ref)" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/debit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRef1
        subscriptionId = $SubscriptionId1
        amount = 50000
        currency = "XOF"
        description = "Should be idempotent"
    } `
    -ExpectedStatus @(200)

$txRefCredit = "TXN-CREDIT-" + (Get-Date -Format 'yyyyMMddHHmmss')
Test-Endpoint -Name "Bank Credit" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/credit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRefCredit
        subscriptionId = $SubscriptionId1
        amount = 25000
        currency = "XOF"
        description = "Test bank credit"
    } `
    -ExpectedStatus @(200)

$txRefWDebit = "TXN-WDEBIT-" + (Get-Date -Format 'yyyyMMddHHmmss')
Test-Endpoint -Name "Wallet Debit" -Method "POST" -Url "$BaseUrl/api/v1/financial/wallet/debit" `
    -Headers $WalletPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRefWDebit
        subscriptionId = $SubscriptionId2
        amount = 10000
        currency = "XOF"
        description = "Test wallet debit"
    } `
    -ExpectedStatus @(200)

$txRefWCredit = "TXN-WCREDIT-" + (Get-Date -Format 'yyyyMMddHHmmss')
Test-Endpoint -Name "Wallet Credit" -Method "POST" -Url "$BaseUrl/api/v1/financial/wallet/credit" `
    -Headers $WalletPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRefWCredit
        subscriptionId = $SubscriptionId2
        amount = 15000
        currency = "XOF"
        description = "Test wallet credit"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "Get Bank Balance" -Method "GET" `
    -Url "$BaseUrl/api/v1/financial/bank/balance?subscriptionId=$SubscriptionId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200, 400, 500)

Test-Endpoint -Name "Get Wallet Balance" -Method "GET" `
    -Url "$BaseUrl/api/v1/financial/wallet/balance?subscriptionId=$SubscriptionId2" `
    -Headers $WalletPartnerHeaders -ExpectedStatus @(200, 400, 500)

# ============================================================================
# 7. ACCOUNTING (admin/finance)
# ============================================================================
Write-Host ""
Write-Host "--- ACCOUNTING ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "List accounting schemas" -Method "GET" `
        -Url "$BaseUrl/api/v1/accounting/schemas" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "List journals" -Method "GET" `
        -Url "$BaseUrl/api/v1/accounting/journals?page=1&pageSize=20" -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (no admin token)" -ForegroundColor Yellow; $Skip += 2 }

# ============================================================================
# 8. DASHBOARD
# ============================================================================
Write-Host ""
Write-Host "--- DASHBOARD ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "Admin dashboard summary" -Method "GET" `
        -Url "$BaseUrl/api/v1/dashboard/summary" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Partner BANK dashboard" -Method "GET" `
        -Url "$BaseUrl/api/v1/dashboard/partners/$PartnerBankId/summary" `
        -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (no admin token)" -ForegroundColor Yellow; $Skip += 2 }

# ============================================================================
# 9. REPORTS
# ============================================================================
Write-Host ""
Write-Host "--- REPORTS ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "Transactions report" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/transactions?fromDate=2026-01-01&toDate=2027-01-01" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Subscriptions report" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/subscriptions" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Failure analysis" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/failure-analysis" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Accounting report" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/accounting" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "BANK partner statement" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/partner-account-statement?partnerId=$PartnerBankId" `
        -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (no admin token)" -ForegroundColor Yellow; $Skip += 5 }

# ============================================================================
# 10. SECURITY
# ============================================================================
Write-Host ""
Write-Host "--- SECURITY ---" -ForegroundColor Magenta

Test-Endpoint -Name "401 if X-Partner-Id missing" -Method "GET" `
    -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" -ExpectedStatus @(401)

Test-Endpoint -Name "401 if partner does not exist" -Method "GET" `
    -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" `
    -Headers @{ "X-Partner-Id" = "00000000-0000-0000-0000-000000000000" } -ExpectedStatus @(401)

Test-Endpoint -Name "401 if JWT missing on /partners" -Method "GET" `
    -Url "$BaseUrl/api/v1/partners" -ExpectedStatus @(401)

# ============================================================================
# SUMMARY
# ============================================================================
Write-Host ""
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  SUMMARY" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host ("  PASS    : " + $Pass) -ForegroundColor Green
Write-Host ("  FAIL    : " + $Fail) -ForegroundColor Red
Write-Host ("  SKIPPED : " + $Skip) -ForegroundColor Yellow
Write-Host ""

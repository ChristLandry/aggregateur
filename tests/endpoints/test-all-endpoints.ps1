# =============================================================================
# Script de test de tous les endpoints — Aggregator Platform
#
# Prerequis :
#   - L'API doit tourner sur http://localhost:5080 (ou modifier $BaseUrl)
#   - TestData.sql doit avoir ete applique en base
#   - SeedData.sql (super-admin) doit avoir ete applique
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

    try {
        $params = @{
            Method = $Method
            Uri = $Url
            Headers = $Headers
            ContentType = "application/json"
            SkipHttpErrorCheck = $true
        }
        if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
        $resp = Invoke-WebRequest @params
        $statusCode = [int]$resp.StatusCode

        if ($ExpectedStatus -contains $statusCode) {
            Write-Host "  PASS ($statusCode)" -ForegroundColor Green
            $script:Pass++
            if ($resp.Content.Length -gt 0 -and $resp.Content.Length -lt 500) {
                Write-Host "  Resp : $($resp.Content)" -ForegroundColor DarkGray
            } elseif ($resp.Content.Length -ge 500) {
                Write-Host "  Resp : (truncated, $($resp.Content.Length) bytes)" -ForegroundColor DarkGray
            }
            return $resp.Content
        } else {
            Write-Host "  FAIL ($statusCode, expected: $($ExpectedStatus -join ','))" -ForegroundColor Red
            Write-Host "  Resp : $($resp.Content)" -ForegroundColor DarkRed
            $script:Fail++
            return $null
        }
    } catch {
        Write-Host "  ERROR : $($_.Exception.Message)" -ForegroundColor Red
        $script:Fail++
        return $null
    }
}

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  Aggregator Platform — Endpoint test suite" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

# ============================================================================
# 1. HEALTH
# ============================================================================
Write-Host ""
Write-Host "--- HEALTH ---" -ForegroundColor Magenta
Test-Endpoint -Name "Health check" -Method "GET" -Url "$BaseUrl/health" -ExpectedStatus @(200, 503)

# ============================================================================
# 2. AUTHENTICATION (super-admin du seed)
# ============================================================================
Write-Host ""
Write-Host "--- AUTH ---" -ForegroundColor Magenta
$loginResp = Test-Endpoint -Name "Login super-admin" -Method "POST" -Url "$BaseUrl/api/v1/auth/login" `
    -Body @{ username = "superadmin"; password = "ChangeMe123!" } `
    -ExpectedStatus @(200, 400, 401)

$AdminToken = $null
if ($loginResp) {
    try {
        $parsed = $loginResp | ConvertFrom-Json
        if ($parsed.success -and $parsed.data.accessToken) {
            $AdminToken = $parsed.data.accessToken
            Write-Host "  Token obtenu (premiers chars) : $($AdminToken.Substring(0, [Math]::Min(40, $AdminToken.Length)))..." -ForegroundColor Green
        }
    } catch { }
}

if (-not $AdminToken) {
    Write-Host ""
    Write-Host "ATTENTION : login admin a echoue (hash BCrypt du SeedData a regenerer)." -ForegroundColor Yellow
    Write-Host "Les endpoints proteges par [Authorize] seront SKIPPED." -ForegroundColor Yellow
    Write-Host "Pour generer le hash :" -ForegroundColor Yellow
    Write-Host "  dotnet run --project tests/tools/gen-bcrypt-hash" -ForegroundColor Yellow
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
    Test-Endpoint -Name "Lister les partenaires" -Method "GET" -Url "$BaseUrl/api/v1/partners" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Recuperer partenaire BANK" -Method "GET" -Url "$BaseUrl/api/v1/partners/$PartnerBankId" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Compte miroir partenaire BANK" -Method "GET" -Url "$BaseUrl/api/v1/partners/$PartnerBankId/account" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Creer un nouveau partenaire" -Method "POST" -Url "$BaseUrl/api/v1/partners" `
        -Headers $AdminHeaders `
        -Body @{
            partnerCode = "TEST_$([Guid]::NewGuid().ToString().Substring(0,8))"
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

Test-Endpoint -Name "Recuperer client Aissatou" -Method "GET" -Url "$BaseUrl/api/v1/customers/$CustomerId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

Test-Endpoint -Name "Lister souscriptions du client" -Method "GET" -Url "$BaseUrl/api/v1/customers/$CustomerId1/subscriptions" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

$newCustomerResp = Test-Endpoint -Name "Creer un nouveau client" -Method "POST" -Url "$BaseUrl/api/v1/customers" `
    -Headers $BankPartnerHeaders `
    -Body @{
        externalCustomerId = "EXT-TEST-$(Get-Random)"
        fullName = "Test Client $(Get-Random)"
        dateOfBirth = "1995-01-01"
        nationalId = "SN-TEST-$(Get-Random)"
        email = "test$(Get-Random)@example.com"
    } `
    -ExpectedStatus @(200)

# ============================================================================
# 5. SUBSCRIPTIONS
# ============================================================================
Write-Host ""
Write-Host "--- SUBSCRIPTIONS ---" -ForegroundColor Magenta

Test-Endpoint -Name "Recuperer souscription 1" -Method "GET" -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

Test-Endpoint -Name "Lister souscriptions du partenaire BANK" -Method "GET" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200)

Test-Endpoint -Name "POST direct : creer souscription (avec PartnerId explicite)" -Method "POST" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders `
    -Body @{
        customerId = $CustomerId1
        partnerId = $PartnerBankId
        bankAccountNumber = "SN012-NEW-$(Get-Random)"
        bankCode = "BANK_DEMO"
        phoneNumber = "+221770$(Get-Random -Maximum 999999 -Minimum 100000)"
        phoneOperator = "Orange"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "POST mismatch PartnerId -> 403" -Method "POST" -Url "$BaseUrl/api/v1/subscriptions" `
    -Headers $BankPartnerHeaders `
    -Body @{
        customerId = $CustomerId1
        partnerId = $PartnerWalletId   # mismatch !
        bankAccountNumber = "SN012-MISMATCH"
        bankCode = "BANK_DEMO"
        phoneNumber = "+221770000000"
        phoneOperator = "Orange"
    } `
    -ExpectedStatus @(403)

# ============================================================================
# 6. FINANCIAL — Transactions
# ============================================================================
Write-Host ""
Write-Host "--- FINANCIAL ---" -ForegroundColor Magenta

$txRef1 = "TXN-TEST-$(Get-Date -Format 'yyyyMMddHHmmss')-1"
Test-Endpoint -Name "Initier un Bank Debit" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/debit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRef1
        subscriptionId = $SubscriptionId1
        amount = 50000
        currency = "XOF"
        description = "Test bank debit via PowerShell"
    } `
    -ExpectedStatus @(200)

# Idempotence : meme partnerTransactionRef doit retourner la meme transaction
Test-Endpoint -Name "Bank Debit IDEMPOTENT (meme ref)" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/debit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = $txRef1
        subscriptionId = $SubscriptionId1
        amount = 50000
        currency = "XOF"
        description = "Doit etre idempotent"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "Bank Credit" -Method "POST" -Url "$BaseUrl/api/v1/financial/bank/credit" `
    -Headers $BankPartnerHeaders `
    -Body @{
        partnerTransactionRef = "TXN-CREDIT-$(Get-Date -Format 'yyyyMMddHHmmss')"
        subscriptionId = $SubscriptionId1
        amount = 25000
        currency = "XOF"
        description = "Test bank credit"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "Wallet Debit (partenaire WALLET)" -Method "POST" -Url "$BaseUrl/api/v1/financial/wallet/debit" `
    -Headers $WalletPartnerHeaders `
    -Body @{
        partnerTransactionRef = "TXN-WDEBIT-$(Get-Date -Format 'yyyyMMddHHmmss')"
        subscriptionId = $SubscriptionId2
        amount = 10000
        currency = "XOF"
        description = "Test wallet debit"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "Wallet Credit" -Method "POST" -Url "$BaseUrl/api/v1/financial/wallet/credit" `
    -Headers $WalletPartnerHeaders `
    -Body @{
        partnerTransactionRef = "TXN-WCREDIT-$(Get-Date -Format 'yyyyMMddHHmmss')"
        subscriptionId = $SubscriptionId2
        amount = 15000
        currency = "XOF"
        description = "Test wallet credit"
    } `
    -ExpectedStatus @(200)

Test-Endpoint -Name "Get Bank Balance" -Method "GET" `
    -Url "$BaseUrl/api/v1/financial/bank/balance?subscriptionId=$SubscriptionId1" `
    -Headers $BankPartnerHeaders -ExpectedStatus @(200, 500)  # 500 si APIs externes down

Test-Endpoint -Name "Get Wallet Balance" -Method "GET" `
    -Url "$BaseUrl/api/v1/financial/wallet/balance?subscriptionId=$SubscriptionId2" `
    -Headers $WalletPartnerHeaders -ExpectedStatus @(200, 500)

# ============================================================================
# 7. ACCOUNTING (admin/finance)
# ============================================================================
Write-Host ""
Write-Host "--- ACCOUNTING ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "Lister les schemas comptables" -Method "GET" `
        -Url "$BaseUrl/api/v1/accounting/schemas" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Lister les journaux" -Method "GET" `
        -Url "$BaseUrl/api/v1/accounting/journals?page=1&pageSize=20" -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (pas de token admin)" -ForegroundColor Yellow; $Skip += 2 }

# ============================================================================
# 8. DASHBOARD
# ============================================================================
Write-Host ""
Write-Host "--- DASHBOARD ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "Dashboard admin summary" -Method "GET" `
        -Url "$BaseUrl/api/v1/dashboard/summary" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Dashboard partenaire BANK" -Method "GET" `
        -Url "$BaseUrl/api/v1/dashboard/partners/$PartnerBankId/summary" `
        -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (pas de token admin)" -ForegroundColor Yellow; $Skip += 2 }

# ============================================================================
# 9. REPORTS
# ============================================================================
Write-Host ""
Write-Host "--- REPORTS ---" -ForegroundColor Magenta

if ($AdminToken) {
    Test-Endpoint -Name "Rapport transactions" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/transactions?fromDate=2026-01-01&toDate=2027-01-01" `
        -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Rapport souscriptions" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/subscriptions" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Failure analysis" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/failure-analysis" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Rapport comptable" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/accounting" -Headers $AdminHeaders -ExpectedStatus @(200)

    Test-Endpoint -Name "Releve partenaire BANK" -Method "GET" `
        -Url "$BaseUrl/api/v1/reports/partner-account-statement?partnerId=$PartnerBankId" `
        -Headers $AdminHeaders -ExpectedStatus @(200)
} else { Write-Host "  SKIP (pas de token admin)" -ForegroundColor Yellow; $Skip += 5 }

# ============================================================================
# 10. SECURITY tests (rate limit, missing partner id, etc.)
# ============================================================================
Write-Host ""
Write-Host "--- SECURITY ---" -ForegroundColor Magenta

Test-Endpoint -Name "401 si X-Partner-Id manquant sur endpoint protege" -Method "GET" `
    -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" -ExpectedStatus @(401)

Test-Endpoint -Name "401 si partenaire inexistant" -Method "GET" `
    -Url "$BaseUrl/api/v1/subscriptions/$SubscriptionId1" `
    -Headers @{ "X-Partner-Id" = "00000000-0000-0000-0000-000000000000" } -ExpectedStatus @(401)

Test-Endpoint -Name "401 si JWT manquant sur /partners" -Method "GET" `
    -Url "$BaseUrl/api/v1/partners" -ExpectedStatus @(401)

# ============================================================================
# RAPPORT FINAL
# ============================================================================
Write-Host ""
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  RESUME" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  PASS    : $Pass" -ForegroundColor Green
Write-Host "  FAIL    : $Fail" -ForegroundColor Red
Write-Host "  SKIPPED : $Skip" -ForegroundColor Yellow
Write-Host ""
Write-Host "Apres execution, verifie en base SQL avec :" -ForegroundColor Cyan
Write-Host "  SELECT TOP 20 * FROM Transactions ORDER BY InitiatedAt DESC;" -ForegroundColor Gray
Write-Host "  SELECT TOP 20 * FROM PartnerAccountMovements ORDER BY MovementDate DESC;" -ForegroundColor Gray
Write-Host "  SELECT TOP 20 * FROM JournalEntries ORDER BY EntryDate DESC;" -ForegroundColor Gray
Write-Host "  SELECT TOP 20 * FROM AuditLogs ORDER BY PerformedAt DESC;" -ForegroundColor Gray
Write-Host ""

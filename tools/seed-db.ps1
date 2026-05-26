[CmdletBinding()]
param(
    [string] $Server       = 'localhost,1433',
    [string] $Database     = 'AggregatorDB',
    [string] $UserId       = 'sa',
    [string] $Password     = 'P@ssw0rd',
    [switch] $SkipSchema,
    [switch] $ResetDatabase
)

# =====================================================================
# seed-db.ps1
# Cree la base AggregatorDB (si absente), applique la migration
# InitialCreate, puis joue les 3 scripts de seed :
#   1) src/AggregatorPlatform.Infrastructure/Persistence/Seed/SeedData.sql
#   2) src/AggregatorPlatform.Infrastructure/Persistence/Seed/TestData.sql
#   3) tools/SeedDemoExtra.sql
#
# Utilisation :
#   .\tools\seed-db.ps1
#   .\tools\seed-db.ps1 -ResetDatabase   # DROP + CREATE de la base
#   .\tools\seed-db.ps1 -Server 'localhost,1433' -Password 'P@ssw0rd'
# =====================================================================

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$schemaSql = Join-Path $root 'AggregatorDB-InitialCreate.sql'
$seedSql   = Join-Path $root 'src\AggregatorPlatform.Infrastructure\Persistence\Seed\SeedData.sql'
$testSql   = Join-Path $root 'src\AggregatorPlatform.Infrastructure\Persistence\Seed\TestData.sql'
$extraSql  = Join-Path $root 'tools\SeedDemoExtra.sql'

function Write-Step($msg) { Write-Host ">> $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "   OK $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "   ! $msg" -ForegroundColor Yellow }

# -----------------------------------------------------------------------------
# Resoudre la commande sqlcmd : preferer go-sqlcmd / sqlcmd ; fallback Invoke-Sqlcmd
# -----------------------------------------------------------------------------
$sqlcmdCmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
$sqlcmdExe = if ($sqlcmdCmd) { $sqlcmdCmd.Source } else { $null }
if (-not $sqlcmdExe) {
    Write-Warn "sqlcmd.exe absent du PATH ; tentative via le module PowerShell SqlServer..."
    try {
        Import-Module SqlServer -ErrorAction Stop
    } catch {
        throw "Ni sqlcmd ni le module SqlServer ne sont disponibles. Installer SQL Server Command Line Tools (sqlcmd) ou : Install-Module SqlServer -Scope CurrentUser"
    }
}

function Invoke-Sql {
    param(
        [Parameter(Mandatory)] [string] $TargetDb,
        [Parameter(Mandatory, ParameterSetName='File')]  [string] $File,
        [Parameter(Mandatory, ParameterSetName='Query')] [string] $Query
    )
    if ($sqlcmdExe) {
        $args = @('-S', $Server, '-U', $UserId, '-P', $Password, '-d', $TargetDb, '-b', '-C')
        if ($PSCmdlet.ParameterSetName -eq 'File') {
            $args += @('-i', $File)
        } else {
            $args += @('-Q', $Query)
        }
        # -I active QUOTED_IDENTIFIER (requis par les filtered indexes et triggers).
        $args += @('-I')
        & $sqlcmdExe @args
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd a echoue (code $LASTEXITCODE)"
        }
    } else {
        $common = @{
            ServerInstance = $Server
            Database       = $TargetDb
            Username       = $UserId
            Password       = $Password
            TrustServerCertificate = $true
            ErrorAction    = 'Stop'
        }
        if ($PSCmdlet.ParameterSetName -eq 'File') {
            Invoke-Sqlcmd @common -InputFile $File
        } else {
            Invoke-Sqlcmd @common -Query $Query
        }
    }
}

# -----------------------------------------------------------------------------
# 1. Verifier la connectivite vers le serveur SQL
# -----------------------------------------------------------------------------
Write-Step "Connexion au serveur SQL $Server"
try {
    Invoke-Sql -TargetDb 'master' -Query "SELECT @@VERSION" | Out-Null
    Write-OK "connexion etablie"
} catch {
    Write-Host "Impossible de joindre $Server avec l'utilisateur '$UserId'." -ForegroundColor Red
    Write-Host "  Detail : $_" -ForegroundColor Red
    throw
}

# -----------------------------------------------------------------------------
# 2. (option) Drop + recreate
# -----------------------------------------------------------------------------
if ($ResetDatabase) {
    Write-Step "Suppression de la base $Database (option -ResetDatabase)"
    Invoke-Sql -TargetDb 'master' -Query @"
IF DB_ID('$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$Database];
END
"@
    Write-OK "base supprimee"
}

# -----------------------------------------------------------------------------
# 3. Creer la base si absente
# -----------------------------------------------------------------------------
Write-Step "Verification / creation de la base $Database"
Invoke-Sql -TargetDb 'master' -Query "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];"
Write-OK "base prete"

# -----------------------------------------------------------------------------
# 4. Schema (migration InitialCreate)
# -----------------------------------------------------------------------------
if (-not $SkipSchema) {
    Write-Step "Application du schema (AggregatorDB-InitialCreate.sql)"
    if (-not (Test-Path $schemaSql)) { throw "Fichier introuvable : $schemaSql" }
    Invoke-Sql -TargetDb $Database -File $schemaSql
    Write-OK "schema applique"
} else {
    Write-Warn "Schema ignore (-SkipSchema)"
}

# -----------------------------------------------------------------------------
# 5. Seed admin + parametres systeme
# -----------------------------------------------------------------------------
Write-Step "Seed admin / parametres systeme (SeedData.sql)"
if (-not (Test-Path $seedSql)) { throw "Fichier introuvable : $seedSql" }
Invoke-Sql -TargetDb $Database -File $seedSql
Write-OK "donnees admin + parametres systeme inserees"

# -----------------------------------------------------------------------------
# 6. Donnees de test (partners, customers, subscriptions, fees, schemas)
# -----------------------------------------------------------------------------
Write-Step "Donnees de test (TestData.sql)"
if (-not (Test-Path $testSql)) { throw "Fichier introuvable : $testSql" }
Invoke-Sql -TargetDb $Database -File $testSql
Write-OK "donnees de test inserees"

# -----------------------------------------------------------------------------
# 7. Donnees enrichies (transactions, journal, mouvements, webhooks, audit)
# -----------------------------------------------------------------------------
Write-Step "Donnees enrichies (SeedDemoExtra.sql)"
if (-not (Test-Path $extraSql)) { throw "Fichier introuvable : $extraSql" }
Invoke-Sql -TargetDb $Database -File $extraSql
Write-OK "donnees enrichies inserees"

# -----------------------------------------------------------------------------
# 8. Recapitulatif
# -----------------------------------------------------------------------------
Write-Step "Recapitulatif des comptages"
$recap = @"
SELECT 'Users'                  AS Entite, COUNT(*) AS N FROM Users UNION ALL
SELECT 'Partners',                COUNT(*) FROM Partners UNION ALL
SELECT 'PartnerAccounts',         COUNT(*) FROM PartnerAccounts UNION ALL
SELECT 'PartnerAccountMovements', COUNT(*) FROM PartnerAccountMovements UNION ALL
SELECT 'Customers',               COUNT(*) FROM Customers UNION ALL
SELECT 'Subscriptions',           COUNT(*) FROM Subscriptions UNION ALL
SELECT 'Transactions',            COUNT(*) FROM Transactions UNION ALL
SELECT 'Movements',               COUNT(*) FROM Movements UNION ALL
SELECT 'AccountingSchemas',       COUNT(*) FROM AccountingSchemas UNION ALL
SELECT 'AccountingSchemaLines',   COUNT(*) FROM AccountingSchemaLines UNION ALL
SELECT 'WebhookLogs',             COUNT(*) FROM WebhookLogs UNION ALL
SELECT 'AuditLogs',               COUNT(*) FROM AuditLogs UNION ALL
SELECT 'SystemParameters',        COUNT(*) FROM SystemParameters
ORDER BY Entite;
"@
Invoke-Sql -TargetDb $Database -Query $recap

Write-Host ""
Write-Host "=== Seed termine avec succes ===" -ForegroundColor Green
Write-Host "Comptes par defaut (mot de passe : ChangeMe123!) :"
Write-Host "  - superadmin (SuperAdmin)"
Write-Host "  - admin      (Admin)"
Write-Host "  - finance    (Finance)"
Write-Host "  - partner_demo (Partner)"

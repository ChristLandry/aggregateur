# Aggregator Platform Backend

Plateforme **agrégateur de flux financiers** centralisant et orchestrant les transactions entre clients finaux, banques partenaires et opérateurs de monnaie électronique (EME / wallets).

## Stack

- **.NET 8 LTS** — ASP.NET Core Web API
- **Entity Framework Core 8** (Code First, SQL Server 2022)
- **Clean Architecture** (4 couches : Domain, Application, Infrastructure, API)
- **CQRS** avec MediatR
- **In-memory cache** (`IMemoryCache`) pour cache partenaire, rate limiting et caches dashboard/KYC
- **JWT Bearer** + middleware custom Partner-ID
- **Serilog** (logs structurés)
- **Polly** (retry + circuit breaker)
- **Swagger** / OpenAPI 3.0
- **xUnit + Moq + WebApplicationFactory** pour les tests
- **Docker + docker-compose**

## Arborescence

```
AggregatorPlatform/
├── src/
│   ├── AggregatorPlatform.API/              # Couche Présentation
│   ├── AggregatorPlatform.Application/      # Couche Application (CQRS)
│   ├── AggregatorPlatform.Domain/           # Couche Domaine
│   └── AggregatorPlatform.Infrastructure/   # Couche Infrastructure
├── tests/
│   ├── AggregatorPlatform.UnitTests/
│   └── AggregatorPlatform.IntegrationTests/
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

## Démarrage rapide — Docker (recommandé)

```bash
# Depuis la racine du projet
docker compose up --build
```

Au premier démarrage, l'API attend que SQL Server soit healthy, puis exécute les migrations EF automatiquement (`AUTO_MIGRATE=true`).

- **API** : http://localhost:8080
- **Swagger** : http://localhost:8080/swagger
- **Health** : http://localhost:8080/health
- **Metrics (Prometheus)** : http://localhost:8080/metrics

> **IMPORTANT** : Avant un déploiement réel, remplace toutes les valeurs `CHANGE_ME` dans `docker-compose.yml` (JWT secret, clé/IV AES, mot de passe SQL).

---

## Démarrage local (sans Docker)

### Prérequis

- .NET 8 SDK
- SQL Server 2022 (instance locale ou container)

### Étapes

```bash
# 1. Restaurer les dépendances
dotnet restore

# 2. Créer la base et appliquer la migration initiale
dotnet ef migrations add InitialCreate \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# 3. Lancer le seed (utilisateur SUPER_ADMIN + paramètres système)
sqlcmd -S localhost -U sa -P 'YourPass123!' -d AggregatorDB \
  -i src/AggregatorPlatform.Infrastructure/Persistence/Seed/SeedData.sql

# 4. Démarrer l'API
dotnet run --project src/AggregatorPlatform.API
```

---

## Variables d'environnement clés

| Clé | Description | Exemple |
|-----|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Chaîne SQL Server | `Server=sqlserver,1433;Database=AggregatorDB;User Id=sa;Password=...;TrustServerCertificate=true` |
| `Jwt__Secret` | Clé HS256 (≥32 chars) | `un_secret_long_aléatoire` |
| `Jwt__Issuer` | Issuer JWT | `AggregatorPlatform` |
| `Jwt__Audience` | Audience JWT | `AggregatorClients` |
| `Jwt__ExpiryMinutes` | Durée access token | `60` |
| `Jwt__RefreshExpiryDays` | Durée refresh token | `7` |
| `Encryption__Key` | Clé AES-256 (Base64 32 bytes) | `K2v6YH3p+...=` |
| `Encryption__IV` | IV AES (Base64 16 bytes) | `Q1xLZ3Wq3R2pV5sN9oQwLg==` |
| `AUTO_MIGRATE` | Exécute `Database.Migrate()` au démarrage | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Staging` / `Production` | `Development` |

### Générer une clé AES-256 + IV

```bash
# Linux/macOS
openssl rand -base64 32   # → clé 32 bytes
openssl rand -base64 16   # → IV 16 bytes
```

```powershell
# Windows PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
```

---

## Tests

```bash
# Tous les tests
dotnet test

# Uniquement les tests unitaires
dotnet test tests/AggregatorPlatform.UnitTests

# Uniquement les tests d'intégration
dotnet test tests/AggregatorPlatform.IntegrationTests
```

---

## Exemples d'appels API (curl)

### 1. Authentification — Login

```bash
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "superadmin",
    "password": "ChangeMe123!"
  }'
```

Réponse :
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "k7n5...",
    "expiresAt": "2026-05-23T15:30:00Z",
    "role": "SuperAdmin"
  },
  "timestamp": "2026-05-23T14:30:00Z"
}
```

### 2. Créer un partenaire (Admin)

```bash
curl -X POST http://localhost:8080/api/v1/partners \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "partnerCode": "BANK_X",
    "name": "Bank X SA",
    "baseUrl": "https://api.bankx.example.com",
    "currency": "XOF",
    "accountCode": "P-BANKX",
    "webhookUrl": "https://hook.bankx.example.com/aggregator",
    "rateLimitPerMin": 200,
    "ipWhitelist": "203.0.113.10",
    "requireHmac": true
  }'
```

> La clé API en clair n'est **retournée qu'une seule fois** dans la réponse. Stocke-la côté partenaire — la base ne conserve que son hash SHA-256.

### 3. Activer le partenaire

```bash
curl -X PATCH http://localhost:8080/api/v1/partners/<PARTNER_ID>/status \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{ "status": 1 }'
```

### 4. Créer un client (côté Partenaire)

```bash
curl -X POST http://localhost:8080/api/v1/customers \
  -H "X-Partner-Id: <PARTNER_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "externalCustomerId": "EXT-001",
    "fullName": "Aïssatou Diallo",
    "dateOfBirth": "1990-04-12",
    "nationalId": "SN-1234567",
    "email": "aissatou@example.com"
  }'
```

### 5. Créer une souscription

```bash
curl -X POST http://localhost:8080/api/v1/customers/<CUSTOMER_ID>/subscriptions \
  -H "X-Partner-Id: <PARTNER_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "bankAccountNumber": "SN012-01234567890",
    "bankCode": "BANK_X",
    "phoneNumber": "+221771234567",
    "phoneOperator": "Orange"
  }'
```

### 6. Initier un débit bancaire

```bash
curl -X POST http://localhost:8080/api/v1/financial/bank/debit \
  -H "X-Partner-Id: <PARTNER_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "partnerTransactionRef": "TXN-2026-0001",
    "subscriptionId": "<SUBSCRIPTION_ID>",
    "amount": 50000,
    "currency": "XOF",
    "description": "Achat formation"
  }'
```

> L'**idempotence** est garantie : un second appel avec le même `partnerTransactionRef` renvoie la transaction existante sans la rejouer.

### 7. Consulter le solde wallet

```bash
curl -X GET "http://localhost:8080/api/v1/financial/wallet/balance?subscriptionId=<SUBSCRIPTION_ID>" \
  -H "X-Partner-Id: <PARTNER_ID>"
```

### 8. Créer un schéma comptable (Finance)

```bash
curl -X POST http://localhost:8080/api/v1/accounting/schemas \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "BankDebit standard XOF",
    "partnerId": null,
    "transactionType": 0,
    "transactionSide": 0,
    "channel": 0,
    "priority": 100,
    "description": "Schéma global pour BankDebit",
    "lines": [
      {
        "lineOrder": 1,
        "accountCode": "411",
        "accountType": 0,
        "side": 0,
        "amountFormula": "AMOUNT",
        "label": "Compte client",
        "isConditional": false
      },
      {
        "lineOrder": 2,
        "accountCode": "707",
        "accountType": 0,
        "side": 1,
        "amountFormula": "AMOUNT_NET",
        "label": "Vente nette",
        "isConditional": false
      },
      {
        "lineOrder": 3,
        "accountCode": "70-FEE",
        "accountType": 0,
        "side": 1,
        "amountFormula": "FEE",
        "label": "Commission",
        "isConditional": true,
        "condition": "FEE > 0"
      }
    ]
  }'
```

### 9. Dashboard admin

```bash
curl -X GET http://localhost:8080/api/v1/dashboard/summary \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

### 10. Export rapport transactions (CSV)

```bash
curl -X POST http://localhost:8080/api/v1/reports/export \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -o transactions.csv \
  -d '{
    "reportType": "transactions",
    "format": "csv",
    "fromDate": "2026-05-01T00:00:00Z",
    "toDate": "2026-05-31T23:59:59Z"
  }'
```

---

## Sécurité — points clés

| Aspect | Implémentation |
|--------|----------------|
| Chiffrement AES-256 | `ValueConverter` EF Core sur `BankAccountNumber`, `NationalId`, `PhoneNumber` |
| Hash API key | SHA-256 (la clé en clair n'est retournée qu'à la création/rotation) |
| JWT | HS256 + refresh token rotation (table `RefreshTokens`) |
| 2FA | TOTP (OtpNet) — disponible pour rôles Admin / SuperAdmin |
| Masquage PII | Enricher Serilog (`PhoneNumber`, `BankAccountNumber`, `NationalId`, `Password`, `ApiKey` → `***MASKED***`) |
| HSTS | Activé en production via `app.UseHsts()` |
| CORS | Whitelist d'origines configurable par environnement |
| Rate limiting | Compteur in-memory par partenaire (`ratelimit:{partnerId}:{minute}`) |
| HMAC partenaire | Optionnel via `Partner.RequireHmac` — header `X-Signature` |
| Audit trail | Intercepteur EF `AuditSaveChangesInterceptor` → table `AuditLogs` |
| Soft-delete | Filtre global EF sur `IsDeleted` |

---

## Architecture — moteur comptable

Pour chaque transaction `Success`, l'`AccountingEngine` :

1. Sélectionne le schéma applicable `(TransactionType, TransactionSide, Channel)` — partenaire-spécifique avant global, priorité ascendante.
2. Construit un contexte d'évaluation : `AMOUNT`, `AMOUNT_NET`, `FEE`, `PARTNER.Balance`, `PARTNER.AccountCode`, `CUSTOMER.PhoneNumber`, `TX.Currency`, `TX.Type`.
3. Pour chaque ligne triée par `LineOrder` :
   - Évalue la `Condition` (si conditionnelle) via NCalc.
   - Résout l'`AccountCode` (Fixed ou Dynamic via `AccountExpression`).
   - Évalue la `AmountFormula`.
4. Vérifie l'équilibre `Σ Débits = Σ Crédits` — sinon `AccountingStatus = Error` et **aucun** journal créé.
5. Persiste `JournalEntry` + lignes, met à jour `Transaction.AccountingStatus = Applied`.
6. Met à jour le **compte miroir** partenaire (`PartnerAccount.Balance`) et trace un `PartnerAccountMovement`.

### Formules supportées (NCalc)

- Opérateurs : `+ - * / %`
- Fonctions : `IF(cond, a, b)`, `ROUND(expr, n)`, `ABS(expr)`, `MIN(a, b)`, `MAX(a, b)`
- Comparaisons : `=`, `!=`, `>`, `<`, `>=`, `<=`
- Logique : `AND`, `OR`, `NOT`

Exemples :
```text
AMOUNT * 0.015
IF(AMOUNT > 500000, AMOUNT * 0.01, AMOUNT * 0.02)
MAX(ROUND(AMOUNT * 0.015, 0), 500)
AMOUNT > 0 AND [TX.Currency] = 'XOF'
```

---

## Jobs en arrière-plan

| Job | Fréquence | Rôle |
|-----|-----------|------|
| `ReconciliationJob` | 5 min | Réconcilie les transactions `Pending` plus anciennes que 30 min en interrogeant banque/EME |
| `WebhookDispatchJob` | 30 s | Renvoie les webhooks `Pending` avec backoff exponentiel (1m, 5m, 15m) — max 3 tentatives |

---

## Observabilité

- **Logs structurés** : `logs/app-YYYYMMDD.log` (rolling quotidien, rétention configurable).
- **Health** : `GET /health` → JSON avec status DB + APIs externes.
- **Metrics** : `GET /metrics` → format Prometheus (utilise `prometheus-net`).

---

## Endpoints (résumé)

| Préfixe | Description |
|---------|-------------|
| `/api/v1/auth/*` | Login / refresh / logout |
| `/api/v1/partners/*` | CRUD partenaires + rotation clé (Admin) |
| `/api/v1/customers/*` | Clients + souscriptions (header `X-Partner-Id` requis) |
| `/api/v1/subscriptions/*` | Lecture / changement statut souscriptions |
| `/api/v1/financial/*` | KYC, balance, débit, crédit, cancel — bancaire et wallet |
| `/api/v1/accounting/*` | Schémas comptables + journaux (Admin/Finance) |
| `/api/v1/dashboard/*` | Synthèses admin + partenaire |
| `/api/v1/reports/*` | Rapports + export CSV/XLSX |
| `/health` | Health check global |
| `/metrics` | Métriques Prometheus |
| `/swagger` | UI Swagger (Dev uniquement) |

---

## Migrations EF Core

```bash
# Ajouter une migration
dotnet ef migrations add <Nom> \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Appliquer
dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Générer un script SQL idempotent
dotnet ef migrations script -i \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  -o migration.sql
```

> La migration initiale `InitialCreate` doit être générée la première fois sur ta machine (les migrations ne sont pas committées car elles dépendent de la chaîne de connexion / provider locaux).

---

## Format de réponse standard

Toutes les réponses suivent l'enveloppe `ApiResponse<T>` :

```json
{
  "success": true,
  "transactionId": "guid?",
  "partnerTransactionRef": "string?",
  "status": "OK",
  "data": { /* payload typé */ },
  "errorCode": null,
  "errorMessage": null,
  "timestamp": "2026-05-23T14:30:00Z"
}
```

En cas d'erreur :

```json
{
  "success": false,
  "errorCode": "PARTNER_INACTIVE",
  "errorMessage": "Partner is not active.",
  "timestamp": "2026-05-23T14:30:00Z"
}
```

---

## Conventions de code

- `Nullable reference types` activé partout.
- `async/await` sur toute opération I/O avec `CancellationToken` propagé.
- DTOs en `record` C# (immuables).
- Validations via **FluentValidation** (jamais de `if` métier dans les handlers).
- Mappings via **AutoMapper**.
- Pattern **Result<T>** pour les erreurs métier ; les exceptions sont réservées aux cas exceptionnels (validation, infrastructure).
- Aucune logique métier dans les controllers — uniquement dispatch MediatR.
- Aucun accès direct à `DbContext` hors de la couche Infrastructure.

---

## Licence

Propriétaire — usage interne uniquement.

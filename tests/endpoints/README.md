# Tests des endpoints — Aggregator Platform

Ce dossier contient les scripts pour tester tous les endpoints de l'API et vérifier les données en base.

## Fichiers

| Fichier | Rôle |
|---|---|
| `test-all-endpoints.ps1` | Script PowerShell qui appelle chaque endpoint avec des données de test |
| `verify-db.sql` | Requêtes SQL pour vérifier les enregistrements créés en base |

## Procédure complète

### 1. Démarrer SQL Server

**Option A — Container Docker isolé :**
```bash
docker run -d --name aggregator-sqlserver \
  -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPass123!" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

**Option B — Docker Compose (recommandé) :**
```bash
cd D:/PROJET/AGGREGATEUR/code/back
docker compose up -d sqlserver
```

### 2. Créer la base et appliquer la migration

```bash
cd D:/PROJET/AGGREGATEUR/code/back

# Soit via dotnet ef (recommandé)
dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Soit via le script SQL idempotent
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d master \
  -Q "IF DB_ID('AggregatorDB') IS NULL CREATE DATABASE AggregatorDB"
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i AggregatorDB-InitialCreate.sql
```

### 3. Appliquer les données de seed et de test

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i src/AggregatorPlatform.Infrastructure/Persistence/Seed/SeedData.sql

sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i src/AggregatorPlatform.Infrastructure/Persistence/Seed/TestData.sql
```

### 4. Démarrer l'API

```bash
cd D:/PROJET/AGGREGATEUR/code/back
dotnet run --project src/AggregatorPlatform.API
```

L'API démarre sur `http://localhost:5080`.

### 5. Lancer les tests

```powershell
cd D:/PROJET/AGGREGATEUR/code/back/tests/endpoints
.\test-all-endpoints.ps1
```

### 6. Vérifier les données en base

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i tests/endpoints/verify-db.sql
```

Ou ouvre SQL Server Management Studio / Azure Data Studio et exécute `verify-db.sql`.

## IDs réutilisables (de TestData.sql)

| Type | ID | Description |
|---|---|---|
| Partner | `11111111-1111-1111-1111-111111111111` | BANK_DEMO (banque) |
| Partner | `22222222-2222-2222-2222-222222222222` | WALLET_DEMO (wallet Orange Money) |
| Customer | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | Aïssatou Diallo |
| Customer | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | Mamadou Sow |
| Subscription | `11111111-aaaa-aaaa-aaaa-111111111111` | Aïssatou × BANK_DEMO |
| Subscription | `22222222-bbbb-bbbb-bbbb-222222222222` | Mamadou × WALLET_DEMO |
| Schema | `33333333-3333-3333-3333-333333333333` | BankDebit global |
| Schema | `44444444-4444-4444-4444-444444444444` | WalletDebit global |

## Endpoints testés (résumé)

| Méthode | Path | Rôle requis |
|---|---|---|
| GET | `/health` | Public |
| POST | `/api/v1/auth/login` | Public |
| GET | `/api/v1/partners` | Admin |
| GET | `/api/v1/partners/{id}` | Admin |
| GET | `/api/v1/partners/{id}/account` | Admin/Partner |
| POST | `/api/v1/partners` | Admin |
| GET | `/api/v1/customers/{id}` | Partner |
| POST | `/api/v1/customers` | Partner |
| GET | `/api/v1/customers/{id}/subscriptions` | Partner |
| **POST** | **`/api/v1/subscriptions`** | **Partner (nouveau)** |
| GET | `/api/v1/subscriptions` | Partner |
| GET | `/api/v1/subscriptions/{id}` | Partner |
| POST | `/api/v1/financial/bank/debit` | Partner |
| POST | `/api/v1/financial/bank/credit` | Partner |
| POST | `/api/v1/financial/wallet/debit` | Partner |
| POST | `/api/v1/financial/wallet/credit` | Partner |
| GET | `/api/v1/financial/bank/balance` | Partner |
| GET | `/api/v1/financial/wallet/balance` | Partner |
| GET | `/api/v1/accounting/schemas` | Admin/Finance |
| GET | `/api/v1/accounting/journals` | Admin/Finance |
| GET | `/api/v1/dashboard/summary` | Admin |
| GET | `/api/v1/dashboard/partners/{id}/summary` | Authenticated |
| GET | `/api/v1/reports/transactions` | Authenticated |
| GET | `/api/v1/reports/subscriptions` | Authenticated |
| GET | `/api/v1/reports/failure-analysis` | Authenticated |
| GET | `/api/v1/reports/accounting` | Admin/Finance |
| GET | `/api/v1/reports/partner-account-statement` | Authenticated |

## Notes importantes

1. **L'authentification admin** nécessite que le hash BCrypt du super-admin soit valide dans `SeedData.sql`. Si le login échoue, regénère le hash avec :
   ```csharp
   Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("ChangeMe123!", 12));
   ```
   Puis remplace dans `SeedData.sql`.

2. **Les endpoints de balance / KYC bank et wallet** appellent les APIs externes des partenaires. Comme ces APIs n'existent pas localement, ces appels échoueront — c'est attendu (`HTTP 500` ou timeout). Le script `test-all-endpoints.ps1` accepte ce comportement.

3. **L'idempotence des transactions** est testée : 2 appels successifs avec le même `partnerTransactionRef` doivent retourner la même `TransactionId`.

4. **Le test de mismatch PartnerId** vérifie que poster une souscription avec `partnerId` ≠ partenaire authentifié retourne bien `HTTP 403 PARTNER_MISMATCH`.

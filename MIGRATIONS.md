# Guide des migrations EF Core — Aggregator Platform

## Sommaire

1. [Vue d'ensemble](#vue-densemble)
2. [Migration `InitialCreate` (effectuée)](#migration-initialcreate-effectuée)
3. [Tables créées](#tables-créées)
4. [Appliquer la migration en base](#appliquer-la-migration-en-base)
5. [Refaire la migration depuis zéro](#refaire-la-migration-depuis-zéro)
6. [Ajouter une nouvelle migration](#ajouter-une-nouvelle-migration)
7. [Rollback d'une migration](#rollback-dune-migration)
8. [Production : best practices](#production--best-practices)

---

## Vue d'ensemble

Le projet utilise **Entity Framework Core 8** en mode **Code First** avec **SQL Server 2022**.

- **DbContext** : `AggregatorPlatform.Infrastructure.Persistence.AggregatorDbContext`
- **Configurations EF** : `Infrastructure/Persistence/Configurations/*.cs` (via `IEntityTypeConfiguration<T>`)
- **Dossier des migrations** : `src/AggregatorPlatform.Infrastructure/Persistence/Migrations/`
- **Intercepteur d'audit** : `AuditSaveChangesInterceptor` → alimente automatiquement `AuditLogs` à chaque `SaveChanges`
- **Chiffrement AES-256** : ValueConverter sur `BankAccountNumber`, `PhoneNumber`, `NationalId`
- **Soft-delete** : filtre global EF sur `Partner`, `Customer`, `Subscription` (via `IsDeleted = false`)

---

## Migration `InitialCreate` (effectuée)

### Commande utilisée

```bash
dotnet ef migrations add InitialCreate \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output-dir Persistence/Migrations
```

### Fichiers générés

```
src/AggregatorPlatform.Infrastructure/Persistence/Migrations/
├── 20260523150822_InitialCreate.cs           ← Migration : Up()/Down()
├── 20260523150822_InitialCreate.Designer.cs  ← Snapshot du model au moment de la migration
└── AggregatorDbContextModelSnapshot.cs        ← Snapshot global du model courant
```

### Script SQL idempotent généré

À la racine du projet : **`AggregatorDB-InitialCreate.sql`** (~24 KB)

Généré par :
```bash
dotnet ef migrations script \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output AggregatorDB-InitialCreate.sql \
  --idempotent
```

Le flag `--idempotent` rend le script **rejouable sans erreur** : il vérifie `IF NOT EXISTS` avant chaque création.

---

## Tables créées

La migration `InitialCreate` produit **16 tables** :

| Table | Rôle | Index notables |
|---|---|---|
| `Partners` | Partenaires / EME / banques | UQ(`PartnerCode`), soft-delete |
| `PartnerAccounts` | Compte miroir interne par partenaire | UQ(`PartnerId`), 1-1 avec Partner |
| `PartnerAccountMovements` | Journal des mouvements du compte miroir | IDX(`PartnerId`, `MovementDate`) |
| `Customers` | Clients finaux | IDX(`ExternalCustomerId`), soft-delete |
| `Subscriptions` | Souscription client ↔ partenaire | **UQ(`CustomerId`, `PartnerId`, `PhoneNumber`)**, soft-delete |
| `Transactions` | Toutes les transactions financières | **UQ(`PartnerId`, `PartnerTransactionRef`)**, IDX(`Status`), IDX(`InitiatedAt`) |
| `AccountingSchemas` | Schémas comptables (règles) | IDX(`TransactionType`, `TransactionSide`, `Channel`, `PartnerId`, `IsActive`, `Priority`) |
| `AccountingSchemaLines` | Lignes d'écriture d'un schéma | FK cascade vers schéma |
| `JournalEntries` | Écritures comptables générées | IDX(`EntryDate`) |
| `JournalLines` | Lignes d'une écriture | IDX(`AccountCode`) |
| `AuditLogs` | Audit trail (Action + OldValues + NewValues JSON) | IDX(`EntityType`, `EntityId`), IDX(`PerformedAt`) |
| `SystemParameters` | Configuration runtime (clé/valeur) | PK = `Key` |
| `WebhookLogs` | Sortie webhooks avec retry | IDX(`Status`), IDX(`NextAttemptAt`) |
| `FeeConfigurations` | Configuration des frais | IDX(`PartnerId`, `TransactionType`, `IsActive`) |
| `Users` | Utilisateurs back-office | UQ(`Username`), UQ(`Email`) |
| `RefreshTokens` | Refresh tokens JWT | IDX(`Token`), FK cascade |
| `__EFMigrationsHistory` | Table système EF Core | — |

### Diagramme relationnel (simplifié)

```
              ┌─────────────┐
              │   Partner   │
              └──────┬──────┘
                     │ 1
         ┌───────────┼───────────┐
         │           │           │
         ▼ 1         ▼ N         ▼ N
  ┌──────────────┐ ┌─────────────┐ ┌──────────────────┐
  │PartnerAccount│ │Subscription │ │AccountingSchema  │
  └───────┬──────┘ └──────┬──────┘ └────────┬─────────┘
          │ 1             │ 1               │ 1
          ▼ N             ▼ N               ▼ N
  ┌───────────────────┐ ┌────────────┐ ┌──────────────────────┐
  │PartnerAccountMove │ │Transaction │ │AccountingSchemaLine  │
  └───────────────────┘ └─────┬──────┘ └──────────────────────┘
                              │ 1
                              ▼ N
                      ┌─────────────────┐
                      │  JournalEntry   │
                      └────────┬────────┘
                               │ 1
                               ▼ N
                      ┌─────────────────┐
                      │   JournalLine   │
                      └─────────────────┘
```

---

## Appliquer la migration en base

### Prérequis

- SQL Server 2022 accessible (local, container Docker, ou cloud)
- Chaîne de connexion configurée dans `appsettings.json` (`ConnectionStrings:DefaultConnection`)

### Option A — Avec `dotnet ef` (recommandé en dev)

```bash
dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

> Crée la base si elle n'existe pas et applique toutes les migrations.

### Option B — Avec le script SQL idempotent (recommandé en prod / CI)

```bash
# Avec sqlcmd (Linux/Windows/macOS)
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d master \
  -Q "IF DB_ID('AggregatorDB') IS NULL CREATE DATABASE AggregatorDB"

sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i AggregatorDB-InitialCreate.sql
```

### Option C — Auto-migration au démarrage

Le `Program.cs` exécute automatiquement `db.Database.Migrate()` si :
- `ASPNETCORE_ENVIRONMENT=Development`, ou
- variable d'env `AUTO_MIGRATE=true`

```bash
AUTO_MIGRATE=true dotnet run --project src/AggregatorPlatform.API
```

### Option D — Avec Docker Compose

```bash
docker compose up --build
```

Le service `api` attend que `sqlserver` soit `healthy`, puis applique automatiquement la migration (variable `AUTO_MIGRATE=true` déjà définie dans `docker-compose.yml`).

---

## Refaire la migration depuis zéro

### Étape 1 — Supprimer la migration existante

```bash
# Si la migration n'a PAS encore été appliquée en base :
dotnet ef migrations remove \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Si la migration A DÉJÀ été appliquée, il faut d'abord :
dotnet ef database update 0 \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
# puis remove
```

### Étape 2 — Dropper la base entièrement (option nucléaire)

```bash
dotnet ef database drop --force \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

### Étape 3 — Régénérer la migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output-dir Persistence/Migrations
```

### Étape 4 — Appliquer

```bash
dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

### Étape 5 — Charger les données de seed et de test

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i src/AggregatorPlatform.Infrastructure/Persistence/Seed/SeedData.sql

sqlcmd -S localhost,1433 -U sa -P 'YourPass123!' -d AggregatorDB \
  -i src/AggregatorPlatform.Infrastructure/Persistence/Seed/TestData.sql
```

---

## Ajouter une nouvelle migration

Quand tu modifies une entité (ajout/suppression de propriété, nouvelle relation, etc.) :

```bash
# 1. Modifier l'entité dans Domain ou la config dans Infrastructure
# 2. Générer la migration :
dotnet ef migrations add AddXxxToYyy \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output-dir Persistence/Migrations

# 3. INSPECTER le fichier généré dans Persistence/Migrations/
#    (vérifier qu'il n'y a pas de DROP COLUMN destructif, etc.)

# 4. Appliquer
dotnet ef database update \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# 5. Générer le script SQL pour la prod (incrémental depuis la dernière migration)
dotnet ef migrations script PreviousMigrationName AddXxxToYyy \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output AggregatorDB-AddXxxToYyy.sql \
  --idempotent
```

### Conventions de nommage

- `InitialCreate` pour la première
- `AddXxxToYyy` pour ajout de colonne ou table
- `RenameYyyToZzz` pour renommage
- `RemoveXxx` pour suppression
- Toujours en PascalCase, en anglais

---

## Rollback d'une migration

### Rollback d'une migration appliquée

```bash
# Revenir à la migration précédente
dotnet ef database update PreviousMigrationName \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Revenir à l'état initial (avant toute migration)
dotnet ef database update 0 \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

### Supprimer une migration générée mais non appliquée

```bash
dotnet ef migrations remove \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

> ⚠️ Si tu as déjà commit ou pushé une migration, n'utilise jamais `remove`. Crée plutôt une nouvelle migration `Revert*` qui annule les changements.

---

## Production — best practices

### ✅ À FAIRE

1. **Toujours générer un script SQL idempotent** avant déploiement :
   ```bash
   dotnet ef migrations script LastDeployedMigration NewMigration --idempotent --output deploy.sql
   ```
2. **Tester le script SQL sur un environnement de staging** identique à la prod
3. **Backup complet de la base** avant tout déploiement
4. **Versionner les migrations** dans Git (commiter `Persistence/Migrations/*`)
5. **Code reviewer** chaque migration (un `DROP COLUMN` peut perdre des données)
6. **Séparer les migrations qui suppriment des colonnes** en deux déploiements :
   - 1er déploiement : code n'utilise plus la colonne (mais elle existe encore)
   - 2nd déploiement : migration `DROP COLUMN`

### ❌ À NE JAMAIS FAIRE

1. **`AUTO_MIGRATE=true` en production** — les migrations doivent être un step CI/CD séparé, pas un effet de bord du démarrage de l'app
2. **`dotnet ef database drop` sur la prod**
3. **Modifier une migration déjà déployée** — toujours en créer une nouvelle
4. **Push d'un mot de passe en clair** dans la connection string — utiliser Azure Key Vault / AWS Secrets Manager / `dotnet user-secrets`

### Pipeline CI/CD recommandé

```yaml
# .github/workflows/deploy.yml (extrait)
- name: Generate idempotent migration script
  run: |
    dotnet ef migrations script ${{ env.LAST_DEPLOYED }} ${{ env.NEW_MIGRATION }} \
      --project src/AggregatorPlatform.Infrastructure \
      --startup-project src/AggregatorPlatform.API \
      --idempotent \
      --output migration.sql

- name: Backup DB
  run: az sql db export ...

- name: Apply migration
  run: sqlcmd -S $SQL_HOST -U $SQL_USER -P $SQL_PASSWORD -d $DB_NAME -i migration.sql

- name: Deploy API
  run: az webapp deploy ...
```

---

## Annexes — Commandes utiles

```bash
# Lister les migrations
dotnet ef migrations list \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Voir l'état actuel de la base
dotnet ef migrations has-pending-model-changes \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API

# Générer le bundle EF (exe portable pour déploiement sans .NET SDK)
dotnet ef migrations bundle \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API \
  --output efbundle.exe

# Inspecter le DDL d'une seule migration
dotnet ef migrations script PreviousMigration TargetMigration \
  --project src/AggregatorPlatform.Infrastructure \
  --startup-project src/AggregatorPlatform.API
```

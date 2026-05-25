// Generation du document Word d'architecture AggregatorPlatform
const fs = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, LevelFormat, HeadingLevel,
  BorderStyle, WidthType, ShadingType, PageNumber, PageBreak,
  TableOfContents, PageOrientation,
} = require('docx');

// =====================================================================
// Helpers
// =====================================================================
const FONT = 'Calibri';
const FONT_MONO = 'Consolas';

const border = { style: BorderStyle.SINGLE, size: 4, color: 'B0B0B0' };
const cellBorders = { top: border, bottom: border, left: border, right: border };

function p(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 120 },
    alignment: opts.align || AlignmentType.JUSTIFIED,
    children: [new TextRun({ text, font: FONT, size: opts.size || 22, bold: !!opts.bold, italics: !!opts.italics, color: opts.color })],
  });
}

function h(level, text, bookmark) {
  return new Paragraph({
    heading: ['Heading1', 'Heading2', 'Heading3', 'Heading4'][level - 1] || 'Heading2',
    spacing: { before: 280, after: 140 },
    children: [new TextRun({ text, font: FONT, bold: true, size: [40, 32, 28, 24][level - 1] || 24 })],
  });
}

function code(text) {
  return new Paragraph({
    spacing: { before: 80, after: 80 },
    shading: { type: ShadingType.CLEAR, fill: 'F2F2F2' },
    children: text.split('\n').map((line, i) => {
      const runs = [];
      if (i > 0) runs.push(new TextRun({ break: 1 }));
      runs.push(new TextRun({ text: line, font: FONT_MONO, size: 18 }));
      return runs;
    }).flat(),
  });
}

function bullet(text, level = 0) {
  return new Paragraph({
    numbering: { reference: 'bullets', level },
    spacing: { after: 60 },
    children: parseInline(text),
  });
}

// minimal inline parser: **bold** + `code`
function parseInline(text) {
  const runs = [];
  const re = /(\*\*[^*]+\*\*|`[^`]+`)/g;
  let last = 0; let m;
  while ((m = re.exec(text)) !== null) {
    if (m.index > last) runs.push(new TextRun({ text: text.slice(last, m.index), font: FONT, size: 22 }));
    const tok = m[0];
    if (tok.startsWith('**')) {
      runs.push(new TextRun({ text: tok.slice(2, -2), font: FONT, size: 22, bold: true }));
    } else {
      runs.push(new TextRun({ text: tok.slice(1, -1), font: FONT_MONO, size: 20, shading: { type: ShadingType.CLEAR, fill: 'F2F2F2' } }));
    }
    last = m.index + tok.length;
  }
  if (last < text.length) runs.push(new TextRun({ text: text.slice(last), font: FONT, size: 22 }));
  return runs.length ? runs : [new TextRun({ text, font: FONT, size: 22 })];
}

function cell(text, opts = {}) {
  const para = new Paragraph({
    alignment: opts.align || AlignmentType.LEFT,
    children: [new TextRun({ text, font: FONT, size: 20, bold: !!opts.bold, color: opts.color })],
  });
  return new TableCell({
    borders: cellBorders,
    width: { size: opts.width, type: WidthType.DXA },
    shading: opts.fill ? { type: ShadingType.CLEAR, fill: opts.fill } : undefined,
    margins: { top: 80, bottom: 80, left: 120, right: 120 },
    children: [para],
  });
}

function table(headers, rows, columnWidths) {
  const totalWidth = columnWidths.reduce((a, b) => a + b, 0);
  const headerRow = new TableRow({
    tableHeader: true,
    children: headers.map((t, i) => cell(t, { width: columnWidths[i], bold: true, fill: '2E75B6', color: 'FFFFFF' })),
  });
  const dataRows = rows.map(r =>
    new TableRow({ children: r.map((t, i) => cell(String(t), { width: columnWidths[i] })) })
  );
  return new Table({
    width: { size: totalWidth, type: WidthType.DXA },
    columnWidths,
    rows: [headerRow, ...dataRows],
  });
}

// =====================================================================
// Contenu
// =====================================================================
const content = [];

// --- Page de garde -----------------------------------------------------------
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 2400, after: 0 },
  children: [new TextRun({ text: 'AggregatorPlatform', font: FONT, bold: true, size: 64, color: '1F4E79' })],
}));
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 200, after: 0 },
  children: [new TextRun({ text: 'Documentation d’architecture', font: FONT, size: 36, color: '1F4E79' })],
}));
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 800, after: 0 },
  children: [new TextRun({ text: 'Backend agregateur de flux financiers', font: FONT, italics: true, size: 26 })],
}));
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 80, after: 0 },
  children: [new TextRun({ text: 'Orchestration banques / EME / wallets', font: FONT, italics: true, size: 22, color: '595959' })],
}));
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 1600, after: 0 },
  children: [new TextRun({ text: '.NET 8 LTS  •  Clean Architecture  •  CQRS  •  EF Core 8', font: FONT, size: 22 })],
}));
content.push(new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { before: 1600 },
  children: [new TextRun({ text: 'Mai 2026', font: FONT, size: 22, color: '595959' })],
}));
content.push(new Paragraph({ children: [new PageBreak()] }));

// --- Table des matieres ------------------------------------------------------
content.push(h(1, 'Table des matieres'));
content.push(new TableOfContents('Sommaire', { hyperlink: true, headingStyleRange: '1-3' }));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 1. Vue d'ensemble
// =====================================================================
content.push(h(1, '1. Vue d’ensemble'));

content.push(h(2, '1.1 Objet du projet'));
content.push(p(
  'AggregatorPlatform est un backend qui centralise et orchestre les flux financiers entre des clients finaux ' +
  'et un ensemble heterogene d’etablissements partenaires : banques, etablissements de monnaie electronique (EME) et wallets ' +
  '(Orange Money, Wave, etc.). Il expose une API REST unifiee, gere l’authentification, le routage des transactions, ' +
  'le calcul de frais, la comptabilisation comptable, la reconciliation et la diffusion d’evenements vers les partenaires via webhooks.'
));

content.push(h(2, '1.2 Choix architecturaux'));
content.push(bullet('**Clean Architecture** en 4 couches (Domain, Application, Infrastructure, API) : inversion de dependances, isolation du metier des frameworks.'));
content.push(bullet('**CQRS** via MediatR : separation explicite des Commands et Queries, pipeline de Behaviors (Logging, Validation).'));
content.push(bullet('**Repository + Unit of Work** au-dessus d’EF Core 8 : abstraction des acces donnees, transactions explicites.'));
content.push(bullet('**Resilience** par Polly (Retry exponentiel + Circuit Breaker) pour tous les appels sortants HTTP.'));
content.push(bullet('**Securite par defaut** : JWT HS256 + middleware X-Partner-Id, chiffrement AES-256 transparent en colonnes EF, BCrypt pour mots de passe, TOTP pour 2FA, audit immuable.'));
content.push(bullet('**Observabilite native** : Serilog (logs structures + masquage PII), Prometheus (`/metrics`), HealthChecks (`/health`).'));
content.push(bullet('**Idempotence** des seeds et des handlers de webhooks pour supporter les rejouages.'));

content.push(h(2, '1.3 Stack technique'));
content.push(table(
  ['Domaine', 'Composant', 'Version'],
  [
    ['Runtime', '.NET (LTS)', '8.0'],
    ['Framework web', 'ASP.NET Core', '8.0.27'],
    ['ORM', 'Entity Framework Core', '8.0.27'],
    ['Base de donnees', 'SQL Server', '2022'],
    ['CQRS / mediation', 'MediatR', '12.4.1'],
    ['Validation', 'FluentValidation', '11.11.0'],
    ['Mapping', 'AutoMapper', '13.0.1'],
    ['Resilience HTTP', 'Polly + Microsoft.Extensions.Http.Polly', '8.6.6 / 8.0.27'],
    ['Auth', 'JwtBearer + System.IdentityModel.Tokens.Jwt', '8.0.27 / 8.18.0'],
    ['Hash mot de passe', 'BCrypt.Net-Next', '4.0.3'],
    ['2FA', 'Otp.NET (TOTP)', '1.4.1'],
    ['Logs', 'Serilog.AspNetCore', '8.0.3'],
    ['Metrics', 'prometheus-net.AspNetCore', '8.2.1'],
    ['HealthChecks', 'AspNetCore.HealthChecks.SqlServer', '8.0.2'],
    ['Doc API', 'Swashbuckle.AspNetCore', '6.9.0'],
    ['Rate limiting', 'AspNetCoreRateLimit', '5.0.0'],
    ['Excel export', 'ClosedXML', '0.105.0'],
    ['Evaluation formules', 'NCalcSync', '3.13.1'],
    ['Tests', 'xUnit + Moq + FluentAssertions', '2.9.3 / 4.20.72 / 6.12.2'],
  ],
  [2200, 4500, 2660],
));

content.push(h(2, '1.4 Arborescence des projets'));
content.push(code(
  'AggregatorPlatform/\n' +
  '├── src/\n' +
  '│   ├── AggregatorPlatform.Domain/          # Entites, enums, evenements, interfaces de repo\n' +
  '│   ├── AggregatorPlatform.Application/     # CQRS (Commands/Queries), DTOs, validators, mappings, behaviors\n' +
  '│   ├── AggregatorPlatform.Infrastructure/  # EF Core, repositories, services techniques, HTTP clients, jobs\n' +
  '│   └── AggregatorPlatform.API/             # Controllers, middlewares, Program.cs, Swagger, JWT\n' +
  '├── tests/\n' +
  '│   ├── AggregatorPlatform.UnitTests/\n' +
  '│   └── AggregatorPlatform.IntegrationTests/\n' +
  '├── tools/                                  # seed-db.ps1, SeedDemoExtra.sql, gen-bcrypt-hash\n' +
  '├── .github/workflows/                      # Pipelines CI (build + tests)\n' +
  '├── Dockerfile / docker-compose.yml         # API + SQL Server 2022\n' +
  '└── AggregatorPlatform.sln'
));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 2. Domain
// =====================================================================
content.push(h(1, '2. Bloc Domain'));
content.push(p(
  'La couche **Domain** est le coeur metier. Elle ne depend de rien (aucune reference NuGet hors BCL). ' +
  'Elle decrit les entites, les enums, les evenements et les contrats d’acces aux donnees (interfaces).'
));

content.push(h(2, '2.1 Entites principales'));
content.push(table(
  ['Entite', 'Role', 'Notes cles'],
  [
    ['Partner', 'Etablissement partenaire (banque, EME, wallet)', 'Clef API, base URL, IP whitelist, rate limit, HMAC optionnel'],
    ['PartnerAccount', 'Compte technique du partenaire chez l’agregateur', 'Solde courant, devise (XOF par defaut)'],
    ['PartnerAccountMovement', 'Mouvement debiteur/crediteur sur le compte partenaire', 'BalanceBefore / BalanceAfter pour audit comptable'],
    ['Customer', 'Client final', 'KYC (NotVerified → Verified), NationalId chiffre'],
    ['Subscription', 'Lien Customer ↔ Partner (compte / numero wallet)', 'BankAccountNumber & PhoneNumber chiffres AES-256'],
    ['Transaction', 'Operation financiere (Debit/Credit, Bank/Wallet)', 'Statuts : Pending, Success, Failed, Cancelled, Reversed'],
    ['AccountingSchema / Line', 'Schema comptable parametrable', 'Lignes avec formule NCalc (AMOUNT, FEE, AMOUNT_NET) + conditions'],
    ['JournalEntry / JournalLine', 'Ecriture comptable issue d’une transaction', 'Equilibre debit/credit force a la generation'],
    ['FeeConfiguration', 'Bareme de frais', 'Fixe, Percentage ou Mixte, plafonne par MaxFeeAmount'],
    ['WebhookLog', 'Trace des notifications sortantes', 'AttemptCount, NextAttemptAt, Status (Pending/Delivered/Failed)'],
    ['AuditLog', 'Journal applicatif immuable', 'OldValues/NewValues JSON, PerformedBy, IP, UserAgent'],
    ['User / RefreshToken', 'Compte d’administration backoffice', 'Roles : SuperAdmin / Admin / Finance / Partner / ReadOnly'],
    ['SystemParameter', 'Cle/valeur de configuration runtime', 'Timeout, retry max, TTL caches, retention logs'],
  ],
  [1800, 2900, 4660],
));

content.push(h(2, '2.2 Enums metier'));
content.push(p('Les enums centralisent les valeurs admises et garantissent l’alignement entre DTOs, BD et regles comptables.'));
content.push(bullet('`PartnerStatus`, `CustomerStatus`, `SubscriptionStatus` : Inactive (0), Active (1), Suspended/Blocked (2).'));
content.push(bullet('`KycStatus` : NotVerified, InProgress, Verified, Rejected.'));
content.push(bullet('`TransactionType` : BankDebit, BankCredit, WalletDebit, WalletCredit, WalletCancel.'));
content.push(bullet('`TransactionStatus` : Pending, Success, Failed, Cancelled, Reversed.'));
content.push(bullet('`AccountingStatus` : Pending, Applied, Error — distingue l’etat metier de l’etat comptable.'));
content.push(bullet('`UserRole` : SuperAdmin (0), Admin (1), Finance (2), Partner (3), ReadOnly (4).'));

content.push(h(2, '2.3 Common & evenements'));
content.push(bullet('`BaseEntity` / `AuditableEntity` apportent **CreatedAt / UpdatedAt / IsDeleted / CreatedBy / UpdatedBy**.'));
content.push(bullet('`IDomainEvent` + `DomainEvents.cs` definissent les evenements (transaction reussie, KYC valide, etc.) reutilisables par d’autres bounded contexts.'));
content.push(bullet('`IRepository<T>`, `ISpecificRepositories`, `IUnitOfWork` : contrats que l’Infrastructure doit implementer.'));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 3. Application
// =====================================================================
content.push(h(1, '3. Bloc Application'));
content.push(p(
  'La couche **Application** orchestre les cas d’usage (use cases). Elle ne connait ni EF Core ni HTTP : elle s’appuie sur ' +
  'les interfaces du Domain et les services techniques injectes. C’est ici qu’on implemente le CQRS, la validation et les regles transversales.'
));

content.push(h(2, '3.1 CQRS / MediatR'));
content.push(p(
  'Chaque feature est decoupee en **Commands** (effet de bord : creation/modification) et **Queries** (lecture). Un handler MediatR par message.'
));
content.push(code(
  'Features/\n' +
  '├── Auth/Commands/                # Login, Refresh, ChangePassword, Enable2FA\n' +
  '├── Partners/Commands/            # Create / Update / ChangeStatus / RotateApiKey\n' +
  '├── Partners/Queries/             # GetPartner, ListPartners, GetPartnerBalance\n' +
  '├── Customers/                    # CRUD + KYC\n' +
  '├── Subscriptions/                # Lien client/partenaire (numero chiffre)\n' +
  '├── Financial/Commands/           # BankCommands.cs, WalletCommands.cs, FinancialBaseHandler.cs\n' +
  '├── Financial/Queries/            # Recherche transactions, suivi statut\n' +
  '├── Accounting/Commands/Queries/  # Posting d’ecritures, recuperation journal\n' +
  '├── Dashboard/Queries/            # KPIs (transactions du jour, taux d’echec)\n' +
  '└── Reports/                       # Export Excel via ClosedXML'
));

content.push(h(2, '3.2 Pipeline de Behaviors'));
content.push(p('Tous les messages MediatR traversent deux behaviors enregistres dans `AddApplication()` :'));
content.push(bullet('`LoggingBehavior<TRequest,TResponse>` : log de l’entree/sortie + duree + correlation id.'));
content.push(bullet('`ValidationBehavior<TRequest,TResponse>` : execute tous les `IValidator<TRequest>` FluentValidation. En echec, leve `ValidationException` capturee par le middleware global.'));

content.push(h(2, '3.3 DTOs, ApiResponse, Result, PaginatedResult'));
content.push(bullet('Un fichier DTO par domaine (`PartnerDtos.cs`, `CustomerDtos.cs`, `FinancialDtos.cs`, etc.) — entrees / sorties stables, decouplees des entites.'));
content.push(bullet('`ApiResponse<T>` : enveloppe standard `{ success, data, errorCode, errorMessage }` retournee par tous les endpoints.'));
content.push(bullet('`Result` / `Result<T>` : valeur de retour explicite des handlers (success ou erreur typee) — evite les exceptions pour les regles metier.'));
content.push(bullet('`PaginatedResult<T>` : pagination cursor/offset homogene pour les listings.'));

content.push(h(2, '3.4 Mappings & exceptions'));
content.push(bullet('`Mappings/MappingProfile.cs` : profile AutoMapper unique, parcouru au demarrage par `AddAutoMapper(assembly)`.'));
content.push(bullet('`Common/Exceptions/AppExceptions.cs` : `NotFoundException`, `BusinessRuleException`, `ValidationException`, traduites par `GlobalExceptionHandler` en codes HTTP.'));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 4. Infrastructure
// =====================================================================
content.push(h(1, '4. Bloc Infrastructure'));
content.push(p(
  'La couche **Infrastructure** implemente les contrats du Domain et les interfaces de services declarees dans l’Application. ' +
  'Elle contient EF Core, les services techniques (chiffrement, formules, frais, webhooks, cache, 2FA), les clients HTTP partenaires et les jobs d’arriere-plan.'
));

content.push(h(2, '4.1 Persistence (EF Core 8 + SQL Server)'));
content.push(bullet('`AggregatorDbContext` declare 16 DbSet et applique automatiquement toutes les `IEntityTypeConfiguration<T>` du dossier `Configurations/`.'));
content.push(bullet('`Configurations/*.cs` : index, contraintes, longueur de colonnes, mapping JSON/AES et filtres globaux (soft-delete `IsDeleted`).'));
content.push(bullet('`Interceptors/AuditSaveChangesInterceptor` : alimente `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` a chaque `SaveChanges()` a partir de `ICurrentUserService`.'));
content.push(bullet('`EncryptionValueConverter` : convertisseur EF Core branche sur `IEncryptionService` pour chiffrer/dechiffrer transparentement les colonnes sensibles (NationalId, BankAccountNumber, PhoneNumber).'));
content.push(bullet('`Repositories/Repository<T>` : implementation generique de `IRepository<T>` ; `SpecificRepositories.cs` ajoute les requetes specialisees (PartnerByCode, transactions par periode, etc.).'));
content.push(bullet('`UnitOfWork` regroupe les commits sous une transaction explicite.'));
content.push(bullet('`Migrations/20260523150822_InitialCreate.cs` : schema initial (38 contraintes, 21 index).'));
content.push(bullet('`Seed/SeedData.sql` (admin + parametres) et `Seed/TestData.sql` (partenaires, clients, schemas) joues a la main ou via `tools/seed-db.ps1`.'));

content.push(h(2, '4.2 Services techniques'));
content.push(table(
  ['Service', 'Role', 'Note'],
  [
    ['EncryptionService', 'AES-256-CBC sur les colonnes marquees', 'Clef + IV charges depuis Configuration (Encryption:Key/IV)'],
    ['FormulaEvaluator', 'Resolution `AMOUNT`, `FEE`, `AMOUNT_NET` via NCalc', 'Utilise par les `AccountingSchemaLine.AmountFormula`'],
    ['FeeCalculator', 'Calcul des frais', 'Fixe + Percentage avec plafond (`MaxFeeAmount`)'],
    ['AccountingEngine', 'Produit les `JournalEntry`+`JournalLine`', 'Selectionne le schema (partenaire / type / canal) par priorite'],
    ['WebhookService', 'Construit et persiste les `WebhookLog`', 'Signature HMAC optionnelle, envoie via HttpClient “Webhook”'],
    ['MemoryCacheService', 'Cache process (`IMemoryCache`)', 'TTL parametre via SystemParameter (CACHE_PARTNER_TTL_SECONDS, etc.)'],
    ['TwoFactorService', 'TOTP RFC 6238', 'Base32 secrets, fenetre tolerance 30 s'],
  ],
  [2400, 3600, 3360],
));

content.push(h(2, '4.3 HTTP clients partenaires'));
content.push(bullet('`BankApiClient` / `WalletApiClient` consomment les API partenaires via `IHttpClientFactory`.'));
content.push(bullet('Chaque named client (`PartnerBank`, `PartnerWallet`, `Webhook`) est decore par **Polly** : retry exponentiel (3 tentatives, base 2 s) et circuit breaker (5 erreurs / fenetre 60 s).'));

content.push(h(2, '4.4 Jobs d’arriere-plan'));
content.push(bullet('`ReconciliationJob` : tache hostee qui passe en revue les transactions Pending plus vieilles que `PENDING_RECONCILIATION_MINUTES` (defaut 30 min) et interroge le partenaire pour finaliser le statut.'));
content.push(bullet('`WebhookDispatchJob` : reprend les `WebhookLog` en statut Pending/Failed dont `NextAttemptAt` est echu, applique un backoff exponentiel et met a jour `AttemptCount`.'));

content.push(h(2, '4.5 Composition (DependencyInjection.cs)'));
content.push(p('Tout est cable dans une seule extension `AddInfrastructure(IConfiguration)` :'));
content.push(bullet('Enregistrement `DbContext` + interceptor.'));
content.push(bullet('Enregistrement des 10 repositories + `UnitOfWork`.'));
content.push(bullet('Enregistrement des services (singleton ou scoped selon thread-safety).'));
content.push(bullet('Configuration des 3 named HttpClients (Polly retry / circuit breaker).'));
content.push(bullet('Demarrage des 2 jobs hosted services.'));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 5. API
// =====================================================================
content.push(h(1, '5. Bloc API (presentation)'));
content.push(p(
  'La couche **API** est volontairement mince : elle ne contient ni logique metier, ni acces donnees. ' +
  'Elle expose les Controllers, configure le pipeline ASP.NET Core, l’authentification, Swagger et la collecte de metriques.'
));

content.push(h(2, '5.1 Pipeline (Program.cs)'));
content.push(code(
  'WebApplication.CreateBuilder(args)\n' +
  '  → Serilog (PiiMaskingEnricher)\n' +
  '  → AddControllers + Swagger (Bearer + X-Partner-Id)\n' +
  '  → AddApplication() + AddInfrastructure(Configuration)\n' +
  '  → JwtBearer (HS256 + ClockSkew 30 s)\n' +
  '  → CORS (Cors:AllowedOrigins)\n' +
  '  → HealthChecks (SqlServer + ExternalApiHealthCheck)\n' +
  '  → Build()\n' +
  '  → db.Database.Migrate()       # si Dev ou AUTO_MIGRATE=true\n' +
  '  → UseSwagger / UseHsts        # selon environnement\n' +
  '  → UseSerilogRequestLogging\n' +
  '  → UseMiddleware<GlobalExceptionHandler>\n' +
  '  → UseHttpsRedirection + UseCors(Default)\n' +
  '  → UseAuthentication → UseMiddleware<PartnerAuthMiddleware> → UseAuthorization\n' +
  '  → UseHttpMetrics + MapMetrics (Prometheus)\n' +
  '  → MapControllers + MapHealthChecks("/health")\n' +
  '  → app.Run()'
));

content.push(h(2, '5.2 Controllers'));
content.push(table(
  ['Controller', 'Prefixe', 'Role'],
  [
    ['AuthController', '/api/v1/auth', 'Login, refresh, change password, enable 2FA'],
    ['PartnerController', '/api/v1/partners', 'CRUD partenaires + rotation API key (Admin/SuperAdmin)'],
    ['CustomerController', '/api/v1/customers', 'Clients + KYC'],
    ['SubscriptionController', '/api/v1/subscriptions', 'Abonnements client-partenaire'],
    ['FinancialController', '/api/v1/financial', 'Initiation transactions Bank/Wallet, consultation'],
    ['AccountingController', '/api/v1/accounting', 'Schemas comptables, ecritures'],
    ['DashboardController', '/api/v1/dashboard', 'KPIs cumules (cache courte duree)'],
    ['ReportController', '/api/v1/reports', 'Exports Excel (ClosedXML)'],
    ['BaseApiController', '—', 'Classe de base : injection MediatR, conversion `Result` → ActionResult'],
  ],
  [2500, 2800, 4060],
));

content.push(h(2, '5.3 Middlewares & filtres'));
content.push(bullet('`GlobalExceptionHandler` : traduit `ValidationException`, `NotFoundException`, `BusinessRuleException`, `UnauthorizedAccessException` en codes HTTP coherents + payload `ApiResponse`.'));
content.push(bullet('`PartnerAuthMiddleware` : lit `X-Partner-Id`, charge le partenaire (en cache), valide le statut Active, expose `ICurrentPartnerService` ; rejette les endpoints proteges par `[RequirePartner]`.'));
content.push(bullet('`Filters/RequirePartnerAttribute` : decorateur d’action MVC qui force la presence d’un partenaire authentifie.'));
content.push(bullet('`Services/JwtTokenService` : emet les access tokens (HS256) et les refresh tokens persistes en BD.'));
content.push(bullet('`Services/CurrentUserService` / `CurrentPartnerService` : abstractions injectees dans les couches inferieures pour acceder a l’utilisateur courant sans dependre de `HttpContext`.'));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 6. Securite
// =====================================================================
content.push(h(1, '6. Securite'));
content.push(bullet('**Authentification utilisateur** : JWT Bearer (HS256, secret >=32 chars, ClockSkew 30 s, refresh token persiste `RefreshTokens`).'));
content.push(bullet('**Authentification partenaire** : header `X-Partner-Id` + ApiKey hashee (SHA-256) + IP whitelist optionnelle + HMAC sur le body si `RequireHmac`.'));
content.push(bullet('**Mots de passe** : BCrypt work-factor 12, hash stockes en BD, jamais retournes.'));
content.push(bullet('**Donnees sensibles au repos** : NationalId, BankAccountNumber, PhoneNumber chiffrees **AES-256-CBC** (clef et IV en variables d’environnement, jamais en BD).'));
content.push(bullet('**2FA** : TOTP RFC 6238 (Otp.NET), fenetre 30 s, secret base32 stocke chiffre.'));
content.push(bullet('**Audit** : `AuditSaveChangesInterceptor` + table `AuditLogs` (Old/NewValues JSON) capturent tous les changements sur les entites traceables.'));
content.push(bullet('**Logs PII** : `PiiMaskingEnricher` masque telephones, emails, IBAN dans les sinks Serilog.'));
content.push(bullet('**Rate limiting** : AspNetCoreRateLimit (defaut 100 req/min/partenaire, override par `RateLimitPerMin` sur le Partner).'));
content.push(bullet('**HTTPS** : `UseHttpsRedirection` + `UseHsts` en non-dev, `RequireHttpsMetadata = true`.'));
content.push(bullet('**Tests d’isolation** : un test d’integration verifie qu’un endpoint admin sans JWT renvoie 401.'));

// =====================================================================
// 7. Observabilite
// =====================================================================
content.push(h(1, '7. Observabilite'));
content.push(table(
  ['Outil', 'Endpoint', 'Couverture'],
  [
    ['Serilog (Console + File)', 'Stdout + /app/logs/', 'Logs structures JSON, masquage PII, correlation id'],
    ['Prometheus (prometheus-net)', '/metrics', 'Compteurs HTTP, latences, jobs, custom counters'],
    ['HealthChecks', '/health', 'SQL Server (`AddSqlServer`) + `ExternalApiHealthCheck` (Bank/Wallet)'],
    ['Swagger', '/swagger', 'OpenAPI 3.0, Bearer + X-Partner-Id documentes'],
  ],
  [2800, 2200, 4360],
));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 8. BD + seed
// =====================================================================
content.push(h(1, '8. Base de donnees & seed'));

content.push(h(2, '8.1 Modele relationnel (extraits)'));
content.push(bullet('15 tables metier + `__EFMigrationsHistory` + `RefreshTokens`.'));
content.push(bullet('Cles primaires `UNIQUEIDENTIFIER`, dates en `datetime2`, montants en `decimal(18,4)`.'));
content.push(bullet('Soft-delete generalise via colonne `IsDeleted` + filtre global EF.'));
content.push(bullet('Foreign keys avec strategies adaptees : `ON DELETE CASCADE` pour les enfants comptables, `NO ACTION` pour preserver les pistes d’audit.'));

content.push(h(2, '8.2 Migration initiale'));
content.push(bullet('`20260523150822_InitialCreate` cree l’integralite du schema (snapshot fige dans `AggregatorDbContextModelSnapshot.cs`).'));
content.push(bullet('Le SQL equivalent autonome est livre dans `AggregatorDB-InitialCreate.sql` (utilisable hors EF Core).'));

content.push(h(2, '8.3 Scripts de seed (tools/)'));
content.push(bullet('`tools/seed-db.ps1` orchestre : creation BD → schema → SeedData.sql → TestData.sql → SeedDemoExtra.sql, idempotent (`-ResetDatabase` pour repartir de zero).'));
content.push(bullet('`SeedData.sql` : utilisateur `superadmin` (mot de passe `ChangeMe123!`) + 8 SystemParameters.'));
content.push(bullet('`TestData.sql` : 2 partenaires (BANK_DEMO, WALLET_DEMO), 2 clients, 2 abonnements, 5 frais, 2 schemas comptables.'));
content.push(bullet('`SeedDemoExtra.sql` : 3 utilisateurs additionnels (admin, finance, partner_demo), 5 transactions (success/pending/failed), 2 journal entries equilibres, mouvements de comptes, webhook logs et audit logs.'));

content.push(h(2, '8.4 Comptes par defaut'));
content.push(table(
  ['Utilisateur', 'Role', 'Mot de passe'],
  [
    ['superadmin', 'SuperAdmin', 'ChangeMe123!'],
    ['admin', 'Admin', 'ChangeMe123!'],
    ['finance', 'Finance', 'ChangeMe123!'],
    ['partner_demo', 'Partner', 'ChangeMe123!'],
  ],
  [3000, 3000, 3360],
));

content.push(p('A modifier imperativement avant tout deploiement non-dev (regenerer un hash BCrypt via `tools/gen-bcrypt-hash`).', { italics: true, color: '8B4513' }));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 9. Tests
// =====================================================================
content.push(h(1, '9. Tests'));
content.push(h(2, '9.1 UnitTests'));
content.push(bullet('21 tests, organisation miroir des projets : `API/`, `Application/`, `Infrastructure/`.'));
content.push(bullet('Couvrent FeeCalculator, FormulaEvaluator, AccountingEngine, EncryptionService, JwtTokenService, validators MediatR.'));
content.push(bullet('Persistance simulee via `Microsoft.EntityFrameworkCore.InMemory`.'));

content.push(h(2, '9.2 IntegrationTests'));
content.push(bullet('Bases sur `AggregatorWebAppFactory` (`WebApplicationFactory<Program>`) + base InMemory.'));
content.push(bullet('Verifient : ouverture du pipeline (`PartnerApiTests`, `CustomerApiTests`, `FinancialApiTests`), reponses HTTP, contrats DTO.'));
content.push(bullet('A noter : la suite verifie qu’un endpoint protege sans JWT renvoie bien 401 (regression test pour `[Authorize]`).'));

// =====================================================================
// 10. Build & deploiement
// =====================================================================
content.push(h(1, '10. Build, deploiement et exploitation'));
content.push(h(2, '10.1 Local (sans Docker)'));
content.push(code(
  'dotnet restore AggregatorPlatform.sln\n' +
  'dotnet build   AggregatorPlatform.sln\n' +
  'dotnet test    AggregatorPlatform.sln\n' +
  '\n' +
  '# Seed BD :\n' +
  'pwsh ./tools/seed-db.ps1 -Server \"localhost\\SQL_SERVER_2022\"\n' +
  '\n' +
  '# Run :\n' +
  'dotnet run --project src/AggregatorPlatform.API'
));

content.push(h(2, '10.2 Docker'));
content.push(bullet('`Dockerfile` multi-stage : SDK 8 (build/publish) puis `aspnet:8.0` runtime, HEALTHCHECK HTTP `/health`.'));
content.push(bullet('`docker-compose.yml` : service `api` + `sqlserver:2022-latest`, healthcheck SQL, network bridge dedie, volume persistant `sqlserver-data`.'));
content.push(bullet('`AUTO_MIGRATE=true` declenche `db.Database.Migrate()` au demarrage (utile en CI/dev, a desactiver en prod).'));

content.push(h(2, '10.3 CI'));
content.push(bullet('`.github/workflows/` contient les pipelines (build + tests).'));
content.push(bullet('Etapes : checkout → setup-dotnet 8 → restore → build (Release) → test (xUnit + coverlet) → artefacts.'));

content.push(h(2, '10.4 Configuration runtime'));
content.push(table(
  ['Clef', 'Description', 'Defaut / Exemple'],
  [
    ['ConnectionStrings:DefaultConnection', 'Chaine SQL Server', 'Server=localhost,1433;Database=AggregatorDB;...'],
    ['Jwt:Secret', 'Clef HS256 (>=32 chars)', 'a remplacer'],
    ['Jwt:Issuer', 'Issuer JWT', 'AggregatorPlatform'],
    ['Jwt:Audience', 'Audience JWT', 'AggregatorClients'],
    ['Jwt:ExpiryMinutes', 'Duree access token', '60'],
    ['Jwt:RefreshExpiryDays', 'Duree refresh token', '7'],
    ['Encryption:Key', 'Clef AES-256 base64 (32 bytes)', 'CHANGE_ME'],
    ['Encryption:IV', 'IV AES base64 (16 bytes)', 'CHANGE_ME'],
    ['Cors:AllowedOrigins', 'Origines autorisees', '["https://app.example.com"]'],
    ['AUTO_MIGRATE', 'Migration automatique au boot', 'true / false'],
  ],
  [3000, 3000, 3360],
));
content.push(new Paragraph({ children: [new PageBreak()] }));

// =====================================================================
// 11. Conventions et bonnes pratiques
// =====================================================================
content.push(h(1, '11. Conventions et bonnes pratiques'));
content.push(bullet('Toute regle metier vit dans **Application** (handlers) ou **Domain** (invariants des entites). Aucune dans les controllers.'));
content.push(bullet('Pas d’appel direct a `DbContext` depuis l’Application : passer par les repositories + UnitOfWork.'));
content.push(bullet('Les commandes retournent `Result<T>` (pas d’exception pour les erreurs metier previsibles).'));
content.push(bullet('Les valeurs sensibles (cles, hashes, mots de passe) ne sont **jamais** logguees ni serialisees : `[JsonIgnore]` + masquage Serilog.'));
content.push(bullet('Les endpoints proteges declarent un `[Authorize(Roles=...)]` explicite — ne jamais reposer sur le seul `UseAuthentication`.'));
content.push(bullet('Tout commit doit laisser `dotnet build` propre (0 warning) et les UnitTests verts.'));

// =====================================================================
// Build
// =====================================================================
const doc = new Document({
  creator: 'AggregatorPlatform',
  title: 'Documentation d’architecture',
  description: 'Architecture du backend AggregatorPlatform',
  styles: {
    default: { document: { run: { font: FONT, size: 22 } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 40, bold: true, font: FONT, color: '1F4E79' },
        paragraph: { spacing: { before: 360, after: 240 }, outlineLevel: 0 } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 32, bold: true, font: FONT, color: '2E75B6' },
        paragraph: { spacing: { before: 240, after: 160 }, outlineLevel: 1 } },
      { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true,
        run: { size: 28, bold: true, font: FONT, color: '2E75B6' },
        paragraph: { spacing: { before: 200, after: 120 }, outlineLevel: 2 } },
    ],
  },
  numbering: {
    config: [{
      reference: 'bullets',
      levels: [
        { level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 480, hanging: 240 } } } },
        { level: 1, format: LevelFormat.BULLET, text: '◦', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 960, hanging: 240 } } } },
      ],
    }],
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 }, // A4 portrait
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 },
      },
    },
    headers: {
      default: new Header({
        children: [new Paragraph({
          alignment: AlignmentType.RIGHT,
          children: [new TextRun({ text: 'AggregatorPlatform — Architecture', font: FONT, size: 18, color: '7F7F7F' })],
        })],
      }),
    },
    footers: {
      default: new Footer({
        children: [new Paragraph({
          alignment: AlignmentType.CENTER,
          children: [
            new TextRun({ text: 'Page ', font: FONT, size: 18, color: '7F7F7F' }),
            new TextRun({ children: [PageNumber.CURRENT], font: FONT, size: 18, color: '7F7F7F' }),
            new TextRun({ text: ' / ', font: FONT, size: 18, color: '7F7F7F' }),
            new TextRun({ children: [PageNumber.TOTAL_PAGES], font: FONT, size: 18, color: '7F7F7F' }),
          ],
        })],
      }),
    },
    children: content,
  }],
});

Packer.toBuffer(doc).then(buf => {
  const out = path.join(__dirname, 'AggregatorPlatform-Architecture.docx');
  fs.writeFileSync(out, buf);
  console.log('Document genere : ' + out);
});

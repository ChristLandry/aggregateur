// Genere une collection Postman v2.1 + un environnement pour l'API AggregatorPlatform.
// Sortie :
//   docs/AggregatorPlatform.postman_collection.json
//   docs/AggregatorPlatform.postman_environment.json
//
// Conventions :
//   - Authorization : Bearer {{accessToken}} (au niveau collection)
//   - X-Partner-Id  : {{partnerId}} (au niveau collection, peut etre override)
//   - Tests scripts : extrait automatiquement accessToken / refreshToken / partnerId /
//                      customerId / subscriptionId / transactionId / schemaId dans
//                      l'environnement actif apres chaque requete pertinente.
//   - Variables d'environnement par defaut = valeurs de seed local
//     (Partner BANK_DEMO + Customer Aissatou + Subscription 11111111-aaaa-...)
//
const fs = require('fs');
const path = require('path');

const ENV_NAME = 'AggregatorPlatform - Local';
const COLLECTION_NAME = 'AggregatorPlatform API';
const COLLECTION_DESC =
  'Collection generee automatiquement couvrant tous les endpoints du backend ' +
  'AggregatorPlatform (Auth, Partners, Customers, Subscriptions, Financial, ' +
  'Accounting, Dashboard, Reports, System).\n\n' +
  'Pre-requis :\n' +
  '  - API demarree localement (default http://localhost:5000).\n' +
  '  - BD seedee (voir tools/seed-db.ps1) avec superadmin/ChangeMe123!.\n\n' +
  'Workflow recommande :\n' +
  '  1. Auth > Login        -> capture accessToken + refreshToken\n' +
  '  2. Partners > Create   -> capture partnerId + partnerCode + apiKey\n' +
  '  3. Customers > Create  -> capture customerId\n' +
  '  4. Customers > Subscriptions > Create -> capture subscriptionId\n' +
  '  5. Financial > Bank/Debit (ou autres) -> capture transactionId\n';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
const ITEMS = [];

function url(rawPath, queryParams = []) {
  // rawPath ex: "/api/v1/partners/:id/status"
  const segments = rawPath.replace(/^\//, '').split('/');
  return {
    raw: '{{baseUrl}}' + rawPath + (queryParams.length ? '?' + queryParams.map(q => `${q.key}=${q.value ?? ''}`).join('&') : ''),
    host: ['{{baseUrl}}'],
    path: segments,
    query: queryParams.length ? queryParams.map(q => ({ key: q.key, value: q.value ?? '', description: q.description ?? '' })) : undefined,
    variable: extractPathVars(segments),
  };
}

function extractPathVars(segments) {
  const vars = [];
  for (const s of segments) {
    if (s.startsWith(':')) {
      const name = s.substring(1);
      // par convention on injecte la variable d'env du meme nom
      vars.push({ key: name, value: `{{${name}}}`, description: `Path variable: ${name}` });
    }
  }
  return vars.length ? vars : undefined;
}

function jsonBody(obj) {
  return {
    mode: 'raw',
    raw: JSON.stringify(obj, null, 2),
    options: { raw: { language: 'json' } },
  };
}

function ep({
  name,
  method,
  path: rawPath,
  query = [],
  body = null,
  description = '',
  tests = null,
  skipAuth = false,
  skipPartner = false,
}) {
  const headers = [{ key: 'Accept', value: 'application/json' }];
  if (body) headers.push({ key: 'Content-Type', value: 'application/json' });
  if (skipPartner) {
    headers.push({ key: 'X-Partner-Id', value: '', disabled: true });
  }

  const item = {
    name,
    request: {
      method,
      header: headers,
      url: url(rawPath, query),
    },
  };
  if (body) item.request.body = jsonBody(body);
  if (description) item.request.description = description;
  if (skipAuth) item.request.auth = { type: 'noauth' };

  const events = [];
  if (tests) {
    events.push({
      listen: 'test',
      script: { type: 'text/javascript', exec: tests.split('\n') },
    });
  }
  if (events.length) item.event = events;
  return item;
}

function folder(name, description, items) {
  return { name, description, item: items };
}

// ---------------------------------------------------------------------------
// AUTH
// ---------------------------------------------------------------------------
const authItems = [
  ep({
    name: 'Login',
    method: 'POST',
    path: '/api/v1/auth/login',
    skipAuth: true,
    skipPartner: true,
    body: { username: '{{adminUsername}}', password: '{{adminPassword}}', twoFactorCode: null },
    description: 'Authentifie un utilisateur. Capture accessToken et refreshToken dans l\'environnement.',
    tests: `
const json = pm.response.json();
pm.test("Login OK", () => {
  pm.expect(pm.response.code).to.equal(200);
  pm.expect(json.success).to.be.true;
});
if (json.success && json.data) {
  pm.environment.set("accessToken", json.data.accessToken);
  pm.environment.set("refreshToken", json.data.refreshToken);
  console.log("Tokens enregistres dans l'environnement.");
}
`.trim(),
  }),
  ep({
    name: 'Refresh',
    method: 'POST',
    path: '/api/v1/auth/refresh',
    skipAuth: true,
    skipPartner: true,
    body: { refreshToken: '{{refreshToken}}' },
    description: 'Echange un refresh token contre un nouveau access token.',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("accessToken", json.data.accessToken);
  pm.environment.set("refreshToken", json.data.refreshToken);
}
`.trim(),
  }),
  ep({
    name: 'Logout',
    method: 'POST',
    path: '/api/v1/auth/logout',
    skipPartner: true,
    body: { refreshToken: '{{refreshToken}}' },
    description: 'Revoque le refresh token courant.',
  }),
];

// ---------------------------------------------------------------------------
// PARTNERS
// ---------------------------------------------------------------------------
const partnerItems = [
  ep({
    name: 'Create partner',
    method: 'POST',
    path: '/api/v1/partners',
    skipPartner: true,
    body: {
      partnerCode: 'PARTNER_NEW',
      name: 'Banque nouvelle',
      baseUrl: 'http://localhost:5080',
      currency: 'XOF',
      partnerBankAccount: '010101010101',
      accountCode: 'P-NEW',
      webhookUrl: 'https://webhook.example.com/aggregator',
      rateLimitPerMin: 100,
      ipWhitelist: null,
      requireHmac: false,
    },
    description: 'Cree un partenaire et son compte miroir. Le partnerBankAccount est OBLIGATOIRE.',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdPartnerId", json.data.partnerId);
  pm.environment.set("createdPartnerCode", json.data.partnerCode);
  pm.environment.set("createdPartnerApiKey", json.data.apiKey);
  console.log("Partner cree : " + json.data.partnerId);
}
`.trim(),
  }),
  ep({
    name: 'List partners',
    method: 'GET',
    path: '/api/v1/partners',
    skipPartner: true,
  }),
  ep({
    name: 'Get partner by id',
    method: 'GET',
    path: '/api/v1/partners/:id',
    skipPartner: true,
    description: 'Variable de chemin: :id (utilise {{partnerId}}).',
  }),
  ep({
    name: 'Update partner',
    method: 'PUT',
    path: '/api/v1/partners/:id',
    skipPartner: true,
    body: {
      name: 'Banque mise a jour',
      baseUrl: 'http://localhost:5080',
      accountCode: 'P-UPD',
      webhookUrl: 'https://webhook.example.com/aggregator',
      rateLimitPerMin: 200,
      ipWhitelist: null,
      requireHmac: false,
    },
  }),
  ep({
    name: 'Change partner status',
    method: 'PATCH',
    path: '/api/v1/partners/:id/status',
    skipPartner: true,
    body: { status: 1 },
    description: 'Statuts : 0=Inactive, 1=Active, 2=Suspended.',
  }),
  ep({
    name: 'Rotate API key',
    method: 'POST',
    path: '/api/v1/partners/:id/rotate-key',
    skipPartner: true,
    description: 'Renouvelle la cle API du partenaire (capture en variable pour reutilisation).',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdPartnerApiKey", json.data.apiKey);
}
`.trim(),
  }),
  ep({
    name: 'Get partner account',
    method: 'GET',
    path: '/api/v1/partners/:id/account',
    skipPartner: true,
    description: 'Retourne le compte miroir incluant PartnerBankAccount dechiffre + solde.',
  }),
];

// ---------------------------------------------------------------------------
// CUSTOMERS (necessite X-Partner-Id)
// ---------------------------------------------------------------------------
const customerItems = [
  ep({
    name: 'Create customer',
    method: 'POST',
    path: '/api/v1/customers',
    body: {
      externalCustomerId: 'EXT-100',
      fullName: 'Mariama Ba',
      dateOfBirth: '1992-03-15',
      nationalId: 'SN-12345',
      email: 'mariama.ba@example.com',
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdCustomerId", json.data);
  console.log("Customer cree : " + json.data);
}
`.trim(),
  }),
  ep({
    name: 'Get customer',
    method: 'GET',
    path: '/api/v1/customers/:id',
    description: 'Variable de chemin: :id (utilise {{customerId}}).',
  }),
  ep({
    name: 'Update customer',
    method: 'PUT',
    path: '/api/v1/customers/:id',
    body: {
      fullName: 'Mariama Ba (MAJ)',
      dateOfBirth: '1992-03-15',
      email: 'mariama.ba+maj@example.com',
      status: 1,
      kycStatus: 2,
    },
    description: 'Status: 0=Inactive,1=Active,2=Blocked ; KycStatus: 0=NotVerified,1=InProgress,2=Verified,3=Rejected.',
  }),
  ep({
    name: 'List customer subscriptions',
    method: 'GET',
    path: '/api/v1/customers/:id/subscriptions',
  }),
  ep({
    name: 'Create subscription for customer',
    method: 'POST',
    path: '/api/v1/customers/:id/subscriptions',
    body: {
      bankAccountNumber: 'SN012-1111-2222-3333',
      bankCode: '{{createdPartnerCode}}',
      phoneNumber: '+221770001122',
      phoneOperator: 'Orange',
      expiresAt: null,
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdSubscriptionId", json.data);
}
`.trim(),
  }),
];

// ---------------------------------------------------------------------------
// SUBSCRIPTIONS (X-Partner-Id requis)
// ---------------------------------------------------------------------------
const subscriptionItems = [
  ep({
    name: 'Create subscription (direct)',
    method: 'POST',
    path: '/api/v1/subscriptions',
    body: {
      customerId: '{{customerId}}',
      partnerId: '{{partnerId}}',
      bankAccountNumber: 'SN012-9999-8888-7777',
      bankCode: 'BANK_DEMO',
      phoneNumber: '+221770003344',
      phoneOperator: 'Wave',
      expiresAt: null,
    },
    description: 'Si partnerId est non null, il doit egaler le partenaire authentifie (sinon 403 PARTNER_MISMATCH).',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdSubscriptionId", json.data);
}
`.trim(),
  }),
  ep({
    name: 'Get subscription by id',
    method: 'GET',
    path: '/api/v1/subscriptions/:id',
  }),
  ep({
    name: 'List subscriptions of current partner',
    method: 'GET',
    path: '/api/v1/subscriptions',
    query: [
      { key: 'customerId', value: '', description: 'Optionnel : filtre par client.' },
    ],
  }),
  ep({
    name: 'Change subscription status',
    method: 'PATCH',
    path: '/api/v1/subscriptions/:id/status',
    body: { status: 1 },
    description: 'Statuts : 0=Inactive,1=Active,2=Suspended.',
  }),
];

// ---------------------------------------------------------------------------
// FINANCIAL (X-Partner-Id requis)
// ---------------------------------------------------------------------------
const financialItems = [
  ep({
    name: 'Bank balance',
    method: 'GET',
    path: '/api/v1/financial/bank/balance',
    query: [{ key: 'subscriptionId', value: '{{subscriptionId}}' }],
  }),
  ep({
    name: 'Wallet balance',
    method: 'GET',
    path: '/api/v1/financial/wallet/balance',
    query: [{ key: 'subscriptionId', value: '{{subscriptionId}}' }],
  }),
  ep({
    name: 'Bank KYC',
    method: 'GET',
    path: '/api/v1/financial/bank/kyc',
    query: [{ key: 'subscriptionId', value: '{{subscriptionId}}' }],
  }),
  ep({
    name: 'Wallet KYC',
    method: 'GET',
    path: '/api/v1/financial/wallet/kyc',
    query: [{ key: 'subscriptionId', value: '{{subscriptionId}}' }],
  }),
  ep({
    name: 'Bank debit',
    method: 'POST',
    path: '/api/v1/financial/bank/debit',
    body: {
      partnerTransactionRef: 'BANK-DEBIT-{{$timestamp}}',
      bankAccount: '{{bankAccount}}',
      phoneNumber: '{{phoneNumber}}',
      subscriptionId: '{{subscriptionId}}',
      amount: 5000,
      fees: null,
      currency: 'XOF',
      description: 'Bank debit demo',
      extraData: { channel: 'MOBILE', deviceId: 'POSTMAN-{{$randomUUID}}' },
    },
    description: 'bankAccount + phoneNumber OBLIGATOIRES. subscriptionId, fees et extraData optionnels.',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdTransactionId", json.data.transactionId);
}
`.trim(),
  }),
  ep({
    name: 'Bank credit',
    method: 'POST',
    path: '/api/v1/financial/bank/credit',
    body: {
      partnerTransactionRef: 'BANK-CREDIT-{{$timestamp}}',
      bankAccount: '{{bankAccount}}',
      phoneNumber: '{{phoneNumber}}',
      subscriptionId: '{{subscriptionId}}',
      amount: 10000,
      fees: null,
      currency: 'XOF',
      description: 'Bank credit demo',
      extraData: null,
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdTransactionId", json.data.transactionId);
}
`.trim(),
  }),
  ep({
    name: 'Wallet debit',
    method: 'POST',
    path: '/api/v1/financial/wallet/debit',
    body: {
      partnerTransactionRef: 'WAL-DEBIT-{{$timestamp}}',
      bankAccount: '{{bankAccount}}',
      phoneNumber: '{{phoneNumber}}',
      subscriptionId: '{{subscriptionId}}',
      amount: 2500,
      fees: 50,
      currency: 'XOF',
      description: 'Wallet debit demo (fees override)',
      extraData: null,
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdTransactionId", json.data.transactionId);
}
`.trim(),
  }),
  ep({
    name: 'Wallet credit',
    method: 'POST',
    path: '/api/v1/financial/wallet/credit',
    body: {
      partnerTransactionRef: 'WAL-CREDIT-{{$timestamp}}',
      bankAccount: '{{bankAccount}}',
      phoneNumber: '{{phoneNumber}}',
      subscriptionId: '{{subscriptionId}}',
      amount: 7500,
      fees: null,
      currency: 'XOF',
      description: 'Wallet credit demo',
      extraData: null,
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdTransactionId", json.data.transactionId);
}
`.trim(),
  }),
  ep({
    name: 'Wallet cancel',
    method: 'POST',
    path: '/api/v1/financial/wallet/cancel',
    body: {
      partnerTransactionRef: 'WAL-CANCEL-{{$timestamp}}',
      originalExternalRef: '{{externalRef}}',
    },
    description: 'Annule une transaction wallet par son externalRef.',
  }),
  ep({
    name: 'Get transaction by id',
    method: 'GET',
    path: '/api/v1/financial/transactions/:id',
    description: 'Variable de chemin: :id (utilise {{transactionId}}).',
  }),
];

// ---------------------------------------------------------------------------
// ACCOUNTING
// ---------------------------------------------------------------------------
const accountingItems = [
  ep({
    name: 'Create schema',
    method: 'POST',
    path: '/api/v1/accounting/schemas',
    body: {
      name: 'WalletCredit standard',
      partnerId: null,
      transactionType: 3,
      transactionSide: 1,
      channel: 1,
      priority: 100,
      description: 'Schema pour les credits wallet',
      lines: [
        {
          lineOrder: 1,
          accountCode: '411',
          accountType: 0,
          accountExpression: null,
          side: 1,
          amountFormula: 'AMOUNT',
          label: 'Compte client',
          isConditional: false,
          condition: null,
        },
        {
          lineOrder: 2,
          accountCode: '707',
          accountType: 0,
          accountExpression: null,
          side: 0,
          amountFormula: 'AMOUNT_NET',
          label: 'Vente nette',
          isConditional: false,
          condition: null,
        },
      ],
    },
    description: 'TransactionType: 0=BankDebit,1=BankCredit,2=WalletDebit,3=WalletCredit,4=WalletCancel. TransactionSide: 0=Debit,1=Credit. Channel: 0=Bank,1=Wallet. AccountType: 0=Fixed,1=Dynamic. LedgerSide: 0=Debit,1=Credit.',
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdSchemaId", json.data);
}
`.trim(),
  }),
  ep({
    name: 'List schemas',
    method: 'GET',
    path: '/api/v1/accounting/schemas',
  }),
  ep({
    name: 'Get schema by id',
    method: 'GET',
    path: '/api/v1/accounting/schemas/:id',
  }),
  ep({
    name: 'Update schema',
    method: 'PUT',
    path: '/api/v1/accounting/schemas/:id',
    body: {
      name: 'WalletCredit standard (MAJ)',
      isActive: true,
      priority: 110,
      description: 'Schema mis a jour',
    },
  }),
  ep({
    name: 'Add line to schema',
    method: 'POST',
    path: '/api/v1/accounting/schemas/:id/lines',
    body: {
      lineOrder: 3,
      accountCode: '70-FEE',
      accountType: 0,
      accountExpression: null,
      side: 0,
      amountFormula: 'FEE',
      label: 'Commission',
      isConditional: true,
      condition: 'FEE > 0',
    },
    tests: `
const json = pm.response.json();
if (json.success && json.data) {
  pm.environment.set("createdLineId", json.data);
}
`.trim(),
  }),
  ep({
    name: 'Remove line from schema',
    method: 'DELETE',
    path: '/api/v1/accounting/schemas/:id/lines/:lineId',
    description: 'Variables de chemin: :id (utilise {{schemaId}}), :lineId (utilise {{lineId}}).',
  }),
  ep({
    name: 'Get journals',
    method: 'GET',
    path: '/api/v1/accounting/journals',
    query: [
      { key: 'fromDate', value: '2026-01-01', description: 'ISO-8601' },
      { key: 'toDate', value: '2026-12-31' },
      { key: 'page', value: '1' },
      { key: 'pageSize', value: '50' },
    ],
  }),
];

// ---------------------------------------------------------------------------
// DASHBOARD
// ---------------------------------------------------------------------------
const dashboardItems = [
  ep({
    name: 'Admin summary',
    method: 'GET',
    path: '/api/v1/dashboard/summary',
    skipPartner: true,
    description: 'Necessite role Admin/SuperAdmin.',
  }),
  ep({
    name: 'Partner summary',
    method: 'GET',
    path: '/api/v1/dashboard/partners/:id/summary',
    skipPartner: true,
  }),
];

// ---------------------------------------------------------------------------
// REPORTS
// ---------------------------------------------------------------------------
const reportItems = [
  ep({
    name: 'Transactions report',
    method: 'GET',
    path: '/api/v1/reports/transactions',
    skipPartner: true,
    query: [
      { key: 'partnerId', value: '{{partnerId}}', description: 'Optionnel.' },
      { key: 'fromDate', value: '2026-01-01' },
      { key: 'toDate', value: '2026-12-31' },
      { key: 'status', value: '', description: '0=Pending,1=Success,2=Failed,3=Cancelled,4=Reversed' },
    ],
  }),
  ep({
    name: 'Subscriptions report',
    method: 'GET',
    path: '/api/v1/reports/subscriptions',
    skipPartner: true,
    query: [
      { key: 'partnerId', value: '{{partnerId}}' },
      { key: 'status', value: '', description: '0=Inactive,1=Active,2=Suspended' },
    ],
  }),
  ep({
    name: 'Failure analysis',
    method: 'GET',
    path: '/api/v1/reports/failure-analysis',
    skipPartner: true,
    query: [
      { key: 'fromDate', value: '2026-01-01' },
      { key: 'toDate', value: '2026-12-31' },
    ],
  }),
  ep({
    name: 'Accounting report',
    method: 'GET',
    path: '/api/v1/reports/accounting',
    skipPartner: true,
    query: [
      { key: 'fromDate', value: '2026-01-01' },
      { key: 'toDate', value: '2026-12-31' },
    ],
    description: 'Necessite role Admin/SuperAdmin/Finance.',
  }),
  ep({
    name: 'Partner account statement',
    method: 'GET',
    path: '/api/v1/reports/partner-account-statement',
    skipPartner: true,
    query: [
      { key: 'partnerId', value: '{{partnerId}}' },
      { key: 'fromDate', value: '2026-01-01' },
      { key: 'toDate', value: '2026-12-31' },
    ],
  }),
  ep({
    name: 'Export report (CSV/XLSX)',
    method: 'POST',
    path: '/api/v1/reports/export',
    skipPartner: true,
    body: {
      reportType: 'transactions',
      format: 'xlsx',
      partnerId: '{{partnerId}}',
      fromDate: '2026-01-01',
      toDate: '2026-12-31',
    },
    description: 'Format : "csv" ou "xlsx". Renvoie le fichier binaire.',
  }),
];

// ---------------------------------------------------------------------------
// SYSTEM (sans auth, sans X-Partner-Id)
// ---------------------------------------------------------------------------
const systemItems = [
  ep({
    name: 'Health',
    method: 'GET',
    path: '/health',
    skipAuth: true,
    skipPartner: true,
    description: 'Health-check global : SQL Server + APIs externes.',
  }),
  ep({
    name: 'Metrics',
    method: 'GET',
    path: '/metrics',
    skipAuth: true,
    skipPartner: true,
    description: 'Metriques Prometheus (text format).',
  }),
  ep({
    name: 'Swagger UI',
    method: 'GET',
    path: '/swagger',
    skipAuth: true,
    skipPartner: true,
    description: 'Interface Swagger (HTML).',
  }),
];

// ---------------------------------------------------------------------------
// Assemblage collection
// ---------------------------------------------------------------------------
const collection = {
  info: {
    _postman_id: 'd6f3a8e0-2a4c-4f0b-9a4e-aggregator2026',
    name: COLLECTION_NAME,
    description: COLLECTION_DESC,
    schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
  },
  auth: {
    type: 'bearer',
    bearer: [{ key: 'token', value: '{{accessToken}}', type: 'string' }],
  },
  event: [
    {
      listen: 'prerequest',
      script: {
        type: 'text/javascript',
        exec: [
          "// Ajoute automatiquement X-Partner-Id sur toutes les requetes sauf indication contraire.",
          "// Pour les endpoints qui ne veulent pas du header, il est marque 'disabled:true' dans la requete.",
          "const url = pm.request.url.getPath();",
          "const partnerId = pm.environment.get('partnerId');",
          "const hasHeader = pm.request.headers.has('X-Partner-Id');",
          "if (partnerId && !hasHeader) {",
          "  pm.request.headers.add({ key: 'X-Partner-Id', value: partnerId });",
          "}",
        ],
      },
    },
  ],
  variable: [
    { key: 'baseUrl', value: '{{baseUrl}}' },
  ],
  item: [
    folder('Auth', 'Login, refresh, logout. Capture accessToken + refreshToken dans l\'environnement.', authItems),
    folder('Partners', 'CRUD partenaires + rotation API key + compte miroir.', partnerItems),
    folder('Customers', 'Clients + leurs souscriptions.', customerItems),
    folder('Subscriptions', 'Creation directe / consultation / changement de statut.', subscriptionItems),
    folder('Financial', 'Initiation de transactions Bank/Wallet (debit/credit/cancel) + consultations.', financialItems),
    folder('Accounting', 'Schemas comptables et journaux.', accountingItems),
    folder('Dashboard', 'KPIs admin et partenaire.', dashboardItems),
    folder('Reports', 'Reporting et exports CSV/XLSX.', reportItems),
    folder('System', 'Health-check, metrics Prometheus, Swagger.', systemItems),
  ],
};

// ---------------------------------------------------------------------------
// Environnement
// ---------------------------------------------------------------------------
const environment = {
  id: 'env-aggregator-local-2026',
  name: ENV_NAME,
  values: [
    { key: 'baseUrl',         value: 'http://localhost:5000',                              enabled: true, type: 'default' },
    { key: 'adminUsername',   value: 'superadmin',                                         enabled: true, type: 'default' },
    { key: 'adminPassword',   value: 'ChangeMe123!',                                       enabled: true, type: 'secret'  },
    { key: 'accessToken',     value: '',                                                   enabled: true, type: 'secret'  },
    { key: 'refreshToken',    value: '',                                                   enabled: true, type: 'secret'  },

    { key: 'partnerId',       value: '11111111-1111-1111-1111-111111111111',               enabled: true, type: 'default' },
    { key: 'partnerCode',     value: 'BANK_DEMO',                                          enabled: true, type: 'default' },
    { key: 'partnerBankAccount', value: '010101010101',                                    enabled: true, type: 'default' },

    { key: 'customerId',      value: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',               enabled: true, type: 'default' },
    { key: 'subscriptionId',  value: '11111111-aaaa-aaaa-aaaa-111111111111',               enabled: true, type: 'default' },

    { key: 'schemaId',        value: '33333333-3333-3333-3333-333333333333',               enabled: true, type: 'default' },
    { key: 'lineId',          value: '',                                                   enabled: true, type: 'default' },

    { key: 'transactionId',   value: '10000001-0000-0000-0000-000000000001',               enabled: true, type: 'default' },
    { key: 'externalRef',     value: 'EXT-A2',                                             enabled: true, type: 'default' },

    { key: 'bankAccount',     value: '0000000000',                                         enabled: true, type: 'default' },
    { key: 'phoneNumber',     value: '0748556806',                                         enabled: true, type: 'default' },

    // Valeurs renseignees par les tests scripts apres creation
    { key: 'createdPartnerId',      value: '', enabled: true, type: 'default' },
    { key: 'createdPartnerCode',    value: '', enabled: true, type: 'default' },
    { key: 'createdPartnerApiKey',  value: '', enabled: true, type: 'secret'  },
    { key: 'createdCustomerId',     value: '', enabled: true, type: 'default' },
    { key: 'createdSubscriptionId', value: '', enabled: true, type: 'default' },
    { key: 'createdTransactionId',  value: '', enabled: true, type: 'default' },
    { key: 'createdSchemaId',       value: '', enabled: true, type: 'default' },
    { key: 'createdLineId',         value: '', enabled: true, type: 'default' },
  ],
  _postman_variable_scope: 'environment',
  _postman_exported_at: new Date().toISOString(),
  _postman_exported_using: 'AggregatorPlatform/generate-postman.js',
};

// ---------------------------------------------------------------------------
// Ecriture des fichiers
// ---------------------------------------------------------------------------
const outDir = __dirname;
const collectionPath = path.join(outDir, 'AggregatorPlatform.postman_collection.json');
const environmentPath = path.join(outDir, 'AggregatorPlatform.postman_environment.json');

fs.writeFileSync(collectionPath, JSON.stringify(collection, null, 2));
fs.writeFileSync(environmentPath, JSON.stringify(environment, null, 2));

// Stats
let endpointCount = 0;
for (const f of collection.item) endpointCount += f.item.length;
console.log(`Collection generee : ${collectionPath}`);
console.log(`Environnement     : ${environmentPath}`);
console.log(`Endpoints totaux  : ${endpointCount}`);
console.log(`Folders           : ${collection.item.length}`);

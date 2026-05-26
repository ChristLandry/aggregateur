# Prompts front-end Next.js

Trois blocs a copier-coller dans Claude / ChatGPT / Cursor :
- **Design System** : a injecter dans CHAQUE prompt comme directives de style.
- **Prompt A** : bootstrappe l'application complete (sans /financial).
- **Prompt B** : ecrans de test/simulation des schemas comptables (apres A).

> Backend : <https://github.com/ChristLandry/aggregateur>
> Swagger : `http://localhost:5000/swagger/v1/swagger.json`
> Postman : `docs/AggregatorPlatform.postman_collection.json`

> **Note design** : le design ci-dessous est inspire des dashboards admin
> RH modernes (sidebar pliable + topbar + KPI cards + tableaux denses).
> Si vous disposez d'une licence Gxon (LayoutDrop) ou autre template
> commercial similaire, remplacez la section "Design System" par les
> assets livres avec votre licence et demandez la conversion HTML -> Next.js.

---

## Design System (a injecter dans tous les prompts)

```text
## Design System

Style general : dashboard admin moderne et dense, type "HR / FinOps",
clean et professionnel. Inspiration : Linear, Vercel dashboard, Tabler
admin, Soft UI Dashboard. PAS de glassmorphic, PAS de gradient ostentatoire.

### Theme
- Modes : light (defaut), dark, auto (suit prefers-color-scheme).
- Switcher de theme dans la topbar.
- Variables CSS exposees via Tailwind (classe `dark:` automatique).

### Palette
Light mode :
  --background        #FAFBFC   (gris tres clair)
  --surface           #FFFFFF
  --surface-muted     #F4F5F7
  --border            #E5E7EB
  --text-primary      #0F172A   (slate-900)
  --text-secondary    #64748B   (slate-500)
  --text-muted        #94A3B8   (slate-400)
  --accent            #4F46E5   (indigo-600)   <- couleur de marque
  --accent-hover      #4338CA
  --success           #10B981   (emerald-500)
  --warning           #F59E0B   (amber-500)
  --danger            #EF4444   (red-500)
  --info              #3B82F6   (blue-500)

Dark mode :
  --background        #0B0F19
  --surface           #111827
  --surface-muted     #1F2937
  --border            #1F2937
  --text-primary      #F8FAFC
  --text-secondary    #94A3B8
  --text-muted        #64748B
  --accent            #6366F1   (indigo-500, plus lumineux qu'en light)

### Typographie
- Famille : `Inter` (Google Fonts) en principal, fallback system-ui.
- Echelle :
    Display    32/40 semibold tracking-tight
    H1         24/32 semibold
    H2         20/28 semibold
    H3         16/24 semibold
    Body       14/20 normal
    Small      12/18 normal
    Mono       JetBrains Mono ou IBM Plex Mono pour les valeurs
              numeriques dans les tables et les KPI.
- Tracking : tracking-tight sur les titres uniquement.

### Layout
- Sidebar gauche fixe, 256px deploye / 64px collapse.
    * Logo en haut (carre 40px + texte produit).
    * Items : icone 18px + label, padding 10/14, rounded-md, hover
      surface-muted, etat actif : fond accent/10 + texte accent + barre
      gauche 2px accent.
    * Sections separees par un petit label uppercase 11/16 muted.
    * Footer sidebar : avatar utilisateur + nom + role.
- Topbar : 64px, sticky top, fond surface, border-bottom.
    * Gauche  : breadcrumb (page courante) + bouton collapse sidebar.
    * Centre  : barre de recherche globale (Cmd+K) facultative.
    * Droite  : PartnerSelector (combobox), theme toggle, notifications,
      avatar avec menu (Profil, Parametres, Logout).
- Container principal : padding 24px, max-width 1440px sur les pages
  list, sans limite sur les dashboards.

### Composants

#### Card (la brique de base)
- Fond surface, border 1px border-color, rounded-xl (12px), shadow-sm.
- Padding interne 20/24.
- Optionnel : icone 24px en haut a gauche dans un carre 40px arrondi
  rounded-lg avec fond accent/10 et icone en accent.

#### KPI Card (dashboard)
- Card classique +
    * Label uppercase 11/16 muted (ex: "Transactions du jour").
    * Valeur 28/36 semibold (mono pour les chiffres).
    * Delta : badge pill accent (vert +12%, rouge -4%) avec fleche.
    * Mini sparkline 40px de haut, accent translucide.

#### Tableau (DataTable)
- Fond surface, en-tete sticky.
- En-tete : fond surface-muted, texte 12/16 medium uppercase muted.
- Lignes : 48px de haut, border-bottom border-color, hover surface-muted.
- Cellule numerique : font-mono, align right.
- Cellule status : Badge pill (success/warning/danger/info).
- Cellule actions : 3 icones (Eye, Pencil, Trash) right-aligned.
- Filtre/recherche au-dessus du tableau (Input ghost a gauche, Select
  status a droite).
- Pagination en bas : "1-10 sur 247" + boutons Precedent/Suivant +
  selecteur PageSize (10/25/50).

#### Boutons
- Primary  : bg-accent text-white, hover accent-hover, rounded-md,
             height 38px, px-16, font-medium 14.
- Secondary: bg-surface border border-color text-primary, hover bg
             surface-muted.
- Ghost    : pas de fond, hover surface-muted, pour les actions discretes.
- Destructive : bg-danger text-white.
- Disabled : opacity-50 cursor-not-allowed.

#### Formulaire
- Label 13/18 medium au-dessus du champ.
- Input 38px de haut, border border-color, rounded-md, focus
  ring-accent ring-2 ring-offset-0.
- Helper text 12/16 muted ; erreur 12/16 danger.
- Boutons groupes en bas a droite (Cancel ghost + Save primary).
- Fieldsets espaces de 24px.

#### Badge / Status pill
- Hauteur 22px, padding 0/8, rounded-full, 11/14 medium uppercase.
- Couleur de fond = couleur status/10, texte = couleur status.

#### Modal / Sheet
- Backdrop bg-black/50 (light) ou bg-black/70 (dark).
- Modal centre 480-640px, rounded-xl, shadow-xl, header avec titre +
  bouton X, footer separe d'un border-top avec les actions.
- Sheet (drawer) : slide depuis la droite, 480px, meme structure.

#### Toast (sonner)
- Position bottom-right, 4 niveaux (success/info/warning/danger),
  rounded-md, icone gauche, fermeture auto 4s.

### Iconographie
- lucide-react EXCLUSIVEMENT, taille 18px par defaut, 24px sur les cards
  d'entete, stroke-width 1.75.

### Animations
- Toutes les transitions : 150ms ease-out par defaut.
- Hover : background change uniquement (pas de transform).
- Loading : Skeleton de shadcn avec shimmer subtil.
- Page transition : fade 200ms via framer-motion (optionnel).

### Densite
- Mode "compact" optionnel via toggle dans les Settings : reduit les
  hauteurs de ligne de tableau de 48 -> 40px et les paddings de card
  de 24 -> 16px.

### Accessibilite
- Tous les composants shadcn (radix-ui) sont a11y par defaut : focus
  visible, ARIA, navigation clavier.
- Contraste AA minimum sur tout le texte.
- Pas d'information seulement via la couleur (ajouter icone ou label).
```

---

## Prompt A — Bootstrap du front Next.js (sans la zone Financial)

> **Avant de coller ce prompt** : prefixe-le avec le bloc "Design System"
> ci-dessus. Le prompt suppose que ce bloc est dans la meme conversation.

```text
Tu es un expert Next.js 15 / React 19 / TypeScript. Construis un
front-end complet pour la plateforme AggregatorPlatform en suivant
strictement (a) le Design System fourni au-dessus de ce prompt et
(b) le contrat ci-dessous.

## Stack (impose)
- Next.js 15 (App Router), TypeScript strict, React 19
- Tailwind CSS + shadcn/ui (composants : Button, Input, Form, Table,
  Dialog, Sheet, Select, Switch, Tabs, Toast, Card, Badge)
- @tanstack/react-query v5 pour les appels API (queries + mutations)
- react-hook-form + zod pour les formulaires
- axios (instance unique avec interceptors) ou openapi-fetch +
  openapi-typescript a partir de /swagger/v1/swagger.json
- lucide-react pour les icones
- date-fns pour les dates
- sonner pour les toasts

## API a consommer (NE PAS implementer les routes Financial)
Base URL configurable via NEXT_PUBLIC_API_BASE_URL (defaut http://localhost:5000).

### Auth
POST  /api/v1/auth/login    { username, password, twoFactorCode? }
                            -> { accessToken, refreshToken, expiresAt, role }
POST  /api/v1/auth/refresh  { refreshToken } -> meme retour
POST  /api/v1/auth/logout   { refreshToken }

### Partners (header Authorization: Bearer; pas de X-Partner-Id)
POST   /api/v1/partners                       creation
GET    /api/v1/partners                       liste
GET    /api/v1/partners/:id                   detail
PUT    /api/v1/partners/:id                   PATCH partiel
PATCH  /api/v1/partners/:id/status            { status: 0|1|2 }
POST   /api/v1/partners/:id/rotate-key        renouvelle l'ApiKey
GET    /api/v1/partners/:id/account           compte miroir complet
GET    /api/v1/partners/:id/balance           solde + devise
PUT    /api/v1/partners/:id/balance           { balance, reason? }

### Customers (header Authorization + X-Partner-Id)
POST   /api/v1/customers
GET    /api/v1/customers/:id
PUT    /api/v1/customers/:id                  PATCH partiel
GET    /api/v1/customers/:id/subscriptions
POST   /api/v1/customers/:id/subscriptions

### Subscriptions (header Authorization + X-Partner-Id)
POST   /api/v1/subscriptions                  PartnerId IMPLICITE (jamais dans le body)
GET    /api/v1/subscriptions/:id
GET    /api/v1/subscriptions?customerId=
PATCH  /api/v1/subscriptions/:id/status

### Accounting (header Authorization, role Admin/SuperAdmin/Finance)
POST   /api/v1/accounting/schemas
GET    /api/v1/accounting/schemas
GET    /api/v1/accounting/schemas/:id
PUT    /api/v1/accounting/schemas/:id         PATCH partiel
POST   /api/v1/accounting/schemas/:id/lines
DELETE /api/v1/accounting/schemas/:id/lines/:lineId
GET    /api/v1/accounting/movements           ?fromDate&toDate&account&transactionId&page&pageSize
GET    /api/v1/accounting/transactions/:id/movements

### Dashboard (header Authorization)
GET    /api/v1/dashboard/summary              admin
GET    /api/v1/dashboard/partners/:id/summary

### Reports (header Authorization)
GET    /api/v1/reports/transactions           ?partnerId&fromDate&toDate&status
GET    /api/v1/reports/subscriptions          ?partnerId&status
GET    /api/v1/reports/failure-analysis       ?fromDate&toDate
GET    /api/v1/reports/accounting             ?fromDate&toDate
GET    /api/v1/reports/partner-account-statement  ?partnerId&fromDate&toDate
POST   /api/v1/reports/export                 { reportType, format, partnerId?, fromDate?, toDate? }
                                              -> renvoie binaire CSV ou XLSX

### System
GET    /health
GET    /metrics      (texte Prometheus)

Toutes les reponses (sauf exports binaires) suivent la forme :
{ success: boolean, data: T, errorCode?: string, errorMessage?: string, timestamp: string }

## Enums
TransactionType    : 0 BankDebit, 1 BankCredit, 2 WalletDebit, 3 WalletCredit, 4 WalletCancel
TransactionSide    : 0 Debit, 1 Credit
Channel            : 0 Bank, 1 Wallet
TransactionStatus  : 0 Pending, 1 Success, 2 Failed, 3 Cancelled, 4 Reversed
AccountingStatus   : 0 Pending, 1 Applied, 2 Error
CustomerStatus     : 0 Inactive, 1 Active, 2 Blocked
KycStatus          : 0 NotVerified, 1 InProgress, 2 Verified, 3 Rejected
PartnerStatus      : 0 Inactive, 1 Active, 2 Suspended
SubscriptionStatus : 0 Inactive, 1 Active, 2 Suspended
LedgerSide         : 0 Debit, 1 Credit
AccountType        : 0 Fixed, 1 Dynamic
UserRole           : 0 SuperAdmin, 1 Admin, 2 Finance, 3 Partner, 4 ReadOnly

## Authentification & multi-partenaire
1. Login -> stocker accessToken + refreshToken (cookies HttpOnly via une
   route /api/auth Next, ou Zustand persiste si simple).
2. Interceptor axios : sur 401, appeler /auth/refresh, retenter une fois,
   sinon rediriger vers /login.
3. Selecteur de partenaire dans la topbar (Combobox alimente par
   GET /partners). La valeur selectionnee est injectee comme header
   X-Partner-Id sur TOUTES les requetes partner-scoped. Persistance dans
   le store (zustand) + cookie.
4. Pages reservees Admin/SuperAdmin/Finance : afficher 403 si role
   insuffisant (decodage du JWT cote client).

## Arborescence demandee
app/
  (public)/login/page.tsx
  (app)/layout.tsx                # sidebar + topbar (partner selector)
  (app)/dashboard/page.tsx
  (app)/partners/page.tsx         # tableau + bouton "Nouveau"
  (app)/partners/[id]/page.tsx    # detail + edit + balance + rotate key
  (app)/partners/[id]/account/page.tsx
  (app)/customers/page.tsx
  (app)/customers/[id]/page.tsx
  (app)/subscriptions/page.tsx
  (app)/subscriptions/[id]/page.tsx
  (app)/accounting/schemas/page.tsx
  (app)/accounting/schemas/[id]/page.tsx
  (app)/accounting/movements/page.tsx
  (app)/reports/page.tsx
components/
  ui/...                          # shadcn
  forms/PartnerForm.tsx
  forms/CustomerForm.tsx
  forms/SchemaForm.tsx
  tables/DataTable.tsx            # generic, sortable, pagine
  PartnerSelector.tsx
  AuthGuard.tsx
lib/
  api/client.ts                   # axios instance + interceptors
  api/partners.ts                 # hooks usePartners(), useCreatePartner()...
  api/customers.ts
  api/subscriptions.ts
  api/accounting.ts
  api/dashboard.ts
  api/reports.ts
  api/auth.ts
  auth/store.ts                   # zustand : { user, accessToken, partnerId }
  auth/jwt.ts                     # decode + role helpers
  schemas/*.ts                    # zod schemas
hooks/
  useRole.ts
  usePartner.ts

## Conventions UI
- Sidebar avec sections : Dashboard / Partners / Customers /
  Subscriptions / Accounting / Reports / System (ne PAS mettre Financial).
- Chaque table : recherche, tri, pagination 10/25/50, action "Voir".
- Formulaires : zod schema -> react-hook-form ; bouton "Reset" + "Save".
- PATCH partiel : si un champ n'est pas modifie par l'utilisateur,
  l'OMETTRE du PUT (envoyer seulement les champs touches via
  form.formState.dirtyFields).
- Loaders : Skeleton de shadcn pendant les useQuery.
- Erreurs : toast sonner.error(errorMessage || errorCode).
- Format devise : Intl.NumberFormat(currency).
- Format date : date-fns format(date, 'dd/MM/yyyy HH:mm').

## Variables d'environnement
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
NEXT_PUBLIC_DEFAULT_PARTNER_ID=11111111-1111-1111-1111-111111111111

## Acceptance criteria
1. Login fonctionnel avec rotation refresh transparente.
2. CRUD complet sur Partners (creation/lecture/edit PATCH partiel/
   change status/rotate-key/balance get+set).
3. CRUD Customers + souscriptions liees (POST /customers/:id/subscriptions
   ET POST /subscriptions standalone).
4. CRUD AccountingSchemas avec gestion des Lines (ajout / suppression).
5. Affichage paginate des Movements + filtres date/account/transactionId.
6. Page detail transaction : liste Movements + total debit/credit (somme
   doit etre zero).
7. Dashboard : KPI cards (transactions du jour, taux succes, top
   partenaires).
8. Reports : selecteur de filtre + bouton "Export CSV" / "Export XLSX"
   qui telecharge le binaire.
9. Selecteur de partenaire actif persistant entre sessions.
10. NE PAS implementer la zone Financial (debit/credit/cancel).

Genere le projet COMPLET, fichier par fichier, sans demander confirmation.
Utilise les versions LATEST stables des dependances. Termine par les
commandes pour demarrer (npm install + npm run dev).
```

---

## Prompt B — Ecrans de test des schemas comptables

A executer **apres** que le Prompt A a genere l'application.

> **Avant de coller** : si tu es dans une nouvelle conversation, prefixe
> par le Design System ; sinon il est deja en contexte.

```text
Etend l'application Next.js precedente avec un module "Sandbox schemas
comptables" sous /accounting/schemas/[id]/sandbox. Tu DOIS respecter le
Design System defini au debut de cette conversation. Objectif :
permettre aux utilisateurs de TESTER un schema sans creer de vraie
transaction.

## Contexte metier (rappel)
- Une transaction genere N mouvements selon le schema applique.
- Chaque ligne du schema (= 1 mouvement) a :
    * lineOrder (numero), accountCode ou accountExpression (Dynamic),
      side (0 Debit / 1 Credit), amountFormula (NCalc),
      label, code, exploitant, isFee (bool), isConditional + condition.
- Variables disponibles dans amountFormula et condition :
    AMOUNT, AMOUNT_NET, FEE, PARTNER.Balance, PARTNER.AccountCode,
    CUSTOMER.PhoneNumber, CUSTOMER.BankAccount, TX.Currency, TX.Type,
    L1, L2, ... LN (= valeurs absolues des montants calcules des lignes
    deja traitees dans l'ordre LineOrder croissant).
- Convention de signe des montants :
    amount < 0 => Debit ; amount > 0 => Credit.
- Apres calcul de toutes les lignes :
    FeeAmount = somme des |Amount| des lignes IsFee = true
    NetAmount = Amount - FeeAmount
    Solde comptable = somme des amounts signes ; DOIT etre 0.

## Ecrans a creer

### 1. /accounting/schemas/[id]/sandbox  -- "Simulateur"
Layout 2 colonnes (desktop) / stack (mobile).

Colonne gauche -- INPUTS :
- Card "Transaction simulee" :
    - amount (input number > 0, default 10000)
    - currency (input text, default "XOF")
    - txType (select TransactionType, lecture seule = celui du schema)
- Card "Contexte" :
    - partnerId (combobox alimente par GET /partners)
    - subscriptionId optionnel (combobox alimente par
      GET /subscriptions?customerId= apres choix d'un customer)
    - OU saisie libre : customer.phoneNumber, customer.bankAccount
    - partner.balance et partner.accountCode pre-remplis depuis
      GET /partners/:id/account, modifiables a la main
- Bouton "Simuler" -> declenche le calcul CLIENT-SIDE (pas d'appel API).

Colonne droite -- OUTPUTS :
- Table "Mouvements generes" avec colonnes :
    Ordre | Compte | Cote | Formule | Amount (signe) | Code | Exploitant | IsFee
- Encart "Resume" :
    Total Debit  | Total Credit | Ecart (doit etre 0)
    FeeAmount calcule | NetAmount (Amount - FeeAmount)
- Badge global :
    [OK] Schema equilibre  (vert)
    [ERREUR] Schema non equilibre (rouge)
    [ERREUR] Formule invalide ligne N (rouge avec message)

### 2. /accounting/schemas/[id]/playground  -- "Bac a sable interactif"
Permet d'editer les lignes du schema EN LOCAL (sans persister) et de voir
en temps reel l'impact sur la simulation. Boutons :
- "Recharger depuis le serveur" (reset)
- "Appliquer au serveur" (PUT pour les modifs de schema + POST/DELETE
  pour les lignes), avec confirmation.

### 3. /accounting/schemas/compare?ids=A,B
Compare deux schemas (table de leurs lignes) cote a cote, avec
simulateur partage : meme transaction simulee, deux colonnes de
resultats. Bouton "Diff" qui surligne les differences ligne par ligne.

## Implementation technique

Evaluateur de formules client-side : utilise **expr-eval** ou **mathjs**.
Comme NCalc accepte des operateurs +-*/%, des appels IF/ROUND/MIN/MAX,
des comparaisons et des identifiants avec point (PARTNER.Balance), mappe
les variables a point vers _PARTNER_Balance avant evaluation.

Algorithme du simulateur :
```
function simulate(schema, context):
    movements = []
    feeTotal = 0
    ctx = { ...context, FEE: 0, AMOUNT_NET: context.AMOUNT }
    sorted = [...schema.lines].sort((a,b) => a.lineOrder - b.lineOrder)
    for line in sorted:
        if line.isConditional && !evaluate(line.condition, ctx):
            continue
        account = line.accountType === 'Dynamic'
            ? resolveExpression(line.accountExpression, ctx)
            : line.accountCode
        raw = evaluate(line.amountFormula, ctx)
        signed = line.side === 'Debit' ? -Math.abs(raw) : Math.abs(raw)
        movements.push({
            lineOrder: line.lineOrder, account, side: line.side,
            label: line.label, code: line.code, exploitant: line.exploitant,
            amount: signed, isFee: line.isFee, formula: line.amountFormula,
        })
        ctx[`L${line.lineOrder}`] = Math.abs(raw)
        if line.isFee: feeTotal += Math.abs(raw)
    feeAmount = feeTotal
    netAmount = context.AMOUNT - feeTotal
    balance = movements.reduce((s, m) => s + m.amount, 0)
    return { movements, feeAmount, netAmount, balance, isBalanced: balance === 0 }
```

## Tests jeux d'essai

Pre-remplis le simulateur avec 3 scenarios cliquables sous forme de
boutons "Charger" :

1. **BankDebit basique** :
   - Schema : 3 lignes (L1: AMOUNT debit / L2: AMOUNT * 0.05 credit IsFee /
     L3: L1 - L2 credit)
   - Input : amount=1000 currency=XOF
   - Attendu : mouvements [-1000, +50, +950], feeAmount=50, netAmount=950,
     equilibre=0

2. **WalletCredit avec condition** :
   - Schema avec une ligne IsConditional : "AMOUNT > 5000"
   - Input 1 : amount=3000  -> ligne conditionnelle ignoree
   - Input 2 : amount=8000  -> ligne conditionnelle prise en compte

3. **Frais cumules** :
   - Schema avec 2 lignes IsFee qui contribuent toutes les deux a
     feeAmount
   - Verifier que feeAmount additionne bien les deux.

## UX additionnelle
- Coloration des montants : rouge si <0, vert si >0.
- Affichage cote a cote "Formule => Resultat" pour chaque ligne.
- Bouton "Exporter le rapport de simulation" (JSON download).
- Diff visuel quand on modifie une ligne (avant/apres).
- Validation zod du schema avant simulation (lignes ordonnees, formules
  non vides, etc.).

## Acceptance criteria
1. Charger un schema existant via GET /accounting/schemas/:id.
2. Simulateur 100% client-side, instantane apres clic "Simuler".
3. Les 3 scenarios pre-charges donnent les bons resultats.
4. Reference d'une ligne par son numero (L1, L2, ...) fonctionne.
5. Detection d'erreur formule -> message ligne + ligne en rouge.
6. Verification d'equilibre comptable affichee clairement.
7. Capacite a editer un schema localement et re-simuler sans persister.
8. Possibilite de pousser les modifs locales vers le serveur via les
   endpoints PUT /schemas/:id et POST/DELETE /schemas/:id/lines.

Genere les composants, hooks et tests unitaires de l'evaluateur (au
moins 5 cas : formule simple, reference L1, condition vraie/fausse,
formule invalide, equilibre 0).
```

---

## Notes pour l'utilisateur

- Les deux prompts fonctionnent independamment. Lance d'abord le Prompt A
  pour bootstrapper l'application, puis le Prompt B sur le meme projet.
- Pour pousser le typage : ajoute un script `npm run gen:types` qui
  appelle `openapi-typescript http://localhost:5000/swagger/v1/swagger.json
  -o lib/api/schema.d.ts`. Les hooks API peuvent alors etre typees
  automatiquement.
- Pour les tests : Vitest + Testing Library + msw pour mocker l'API
  (le simulateur de schemas se preste tres bien aux tests de logique
  pure cote client).

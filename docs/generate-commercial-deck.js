// Genere un PowerPoint commercial de Fintech Hub.
// Cible : decideurs metier (CEO, COO, CTO non-tech, BD, partenaires).
// Ton : valeur business, pas d'argot technique.

const pptxgen = require("pptxgenjs");
const path = require("path");
const React = require("react");
const ReactDOMServer = require("react-dom/server");
const sharp = require("sharp");
const {
  FaPlug, FaBolt, FaShieldAlt, FaChartLine, FaUniversity, FaMobileAlt,
  FaShoppingCart, FaCheckCircle, FaArrowRight, FaCogs, FaSearch, FaSyncAlt,
  FaUsers, FaHandshake, FaRocket, FaGlobeAfrica, FaLayerGroup, FaLock,
  FaClock, FaCloud, FaWallet, FaBuilding,
} = require("react-icons/fa");

// ============================================================================
// PALETTE & TYPO
// ============================================================================
const C = {
  navy:    "0B1F3A",  // primary (dark)
  steel:   "1B3A6B",
  gold:    "F4B942",  // accent
  white:   "FFFFFF",
  cream:   "F5F7FA",
  slate:   "475569",
  muted:   "7B8794",
  emerald: "10B981",
  rose:    "F43F5E",
  iceblue: "DBEAFE",
  border:  "E2E8F0",
};
const FONT = "Calibri";
const FONT_HEAD = "Calibri";

// ============================================================================
// SETUP
// ============================================================================
const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.3 x 7.5
const W = 13.333;
const H = 7.5;
pres.author = "Fintech Hub";
pres.title  = "Fintech Hub - Presentation commerciale";

// ============================================================================
// ICON HELPERS
// ============================================================================
async function icon(IconComponent, color) {
  const svg = ReactDOMServer.renderToStaticMarkup(
    React.createElement(IconComponent, { color: "#" + color, size: "256" })
  );
  const png = await sharp(Buffer.from(svg)).png().toBuffer();
  return "image/png;base64," + png.toString("base64");
}

// Cercle plein + icone centree
function iconBubble(slide, IconData, x, y, diameter, bgColor, iconColor) {
  slide.addShape(pres.shapes.OVAL, {
    x, y, w: diameter, h: diameter,
    fill: { color: bgColor }, line: { type: "none" },
  });
  const pad = diameter * 0.22;
  slide.addImage({ data: IconData, x: x + pad, y: y + pad, w: diameter - 2 * pad, h: diameter - 2 * pad });
}

function goldAccent(slide, x, y, w = 0.6, h = 0.08) {
  slide.addShape(pres.shapes.RECTANGLE, {
    x, y, w, h, fill: { color: C.gold }, line: { type: "none" },
  });
}

function footer(slide, page, total) {
  slide.addText("Fintech Hub", {
    x: 0.4, y: H - 0.45, w: 5, h: 0.3,
    fontSize: 10, fontFace: FONT, color: C.muted, margin: 0,
  });
  slide.addText(`${page} / ${total}`, {
    x: W - 1.0, y: H - 0.45, w: 0.6, h: 0.3,
    fontSize: 10, fontFace: FONT, color: C.muted, align: "right", margin: 0,
  });
}

// ============================================================================
// BUILD
// ============================================================================
async function build() {
  // -------- Pre-render des icones --------
  const icons = {
    plug:        await icon(FaPlug,        C.gold),
    bolt:        await icon(FaBolt,        C.gold),
    shield:      await icon(FaShieldAlt,   C.gold),
    chart:       await icon(FaChartLine,   C.gold),
    universityG: await icon(FaUniversity,  C.gold),
    mobile:      await icon(FaMobileAlt,   C.gold),
    cart:        await icon(FaShoppingCart,C.gold),
    arrow:       await icon(FaArrowRight,  C.gold),
    cogs:        await icon(FaCogs,        C.gold),
    search:      await icon(FaSearch,      C.gold),
    sync:        await icon(FaSyncAlt,     C.gold),
    handshake:   await icon(FaHandshake,   C.gold),
    rocket:      await icon(FaRocket,      C.gold),
    globe:       await icon(FaGlobeAfrica, C.gold),
    layers:      await icon(FaLayerGroup,  C.gold),
    lock:        await icon(FaLock,        C.gold),
    clock:       await icon(FaClock,       C.gold),
    cloud:       await icon(FaCloud,       C.gold),
    wallet:      await icon(FaWallet,      C.gold),
    bank:        await icon(FaBuilding,    C.gold),
    check:       await icon(FaCheckCircle, C.emerald),

    // versions navy pour fonds clairs
    plugN:       await icon(FaPlug,        C.navy),
    boltN:       await icon(FaBolt,        C.navy),
    shieldN:     await icon(FaShieldAlt,   C.navy),
    chartN:      await icon(FaChartLine,   C.navy),
    cogsN:       await icon(FaCogs,        C.navy),
    syncN:       await icon(FaSyncAlt,     C.navy),
    handshakeN:  await icon(FaHandshake,   C.navy),
    bankN:       await icon(FaBuilding,    C.navy),
    walletN:     await icon(FaWallet,      C.navy),
    mobileN:     await icon(FaMobileAlt,   C.navy),
    cartN:       await icon(FaShoppingCart,C.navy),
    layersN:     await icon(FaLayerGroup,  C.navy),
    lockN:       await icon(FaLock,        C.navy),
    clockN:      await icon(FaClock,       C.navy),
    globeN:      await icon(FaGlobeAfrica, C.navy),
    rocketN:     await icon(FaRocket,      C.navy),
    searchN:     await icon(FaSearch,      C.navy),
    chartW:      await icon(FaChartLine,   C.white),
  };

  const totalSlides = 12;
  let n = 0;

  // ==========================================================================
  // 1 — COVER
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.navy };

    // Bandeau gold a gauche
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: 0.25, h: H, fill: { color: C.gold }, line: { type: "none" },
    });

    // Logo placeholder (texte stylise)
    s.addText("FINTECH", {
      x: 1.0, y: 0.6, w: 6, h: 0.5,
      fontSize: 14, fontFace: FONT_HEAD, color: C.gold,
      bold: true, charSpacing: 8, margin: 0,
    });
    s.addText("HUB", {
      x: 1.0, y: 0.95, w: 6, h: 0.5,
      fontSize: 14, fontFace: FONT_HEAD, color: C.white,
      bold: true, charSpacing: 8, margin: 0,
    });

    // Titre principal
    s.addText("Une porte d'entree unique", {
      x: 1.0, y: 2.3, w: 11.3, h: 0.9,
      fontSize: 44, fontFace: FONT_HEAD, color: C.white, bold: true, margin: 0,
    });
    s.addText("vers tous les flux financiers.", {
      x: 1.0, y: 3.1, w: 11.3, h: 0.9,
      fontSize: 44, fontFace: FONT_HEAD, color: C.gold, bold: true, margin: 0,
    });

    // Tagline
    s.addText(
      "Connectez vos clients en quelques heures aux banques, wallets et reseaux de paiement, " +
      "avec une securite de niveau bancaire et une visibilite operationnelle en temps reel.",
      {
        x: 1.0, y: 4.3, w: 10.5, h: 1.2,
        fontSize: 16, fontFace: FONT, color: "CADCFC", italic: true, margin: 0,
      }
    );

    // Pied
    goldAccent(s, 1.0, 6.4, 1.2, 0.05);
    s.addText("Presentation commerciale - 2026", {
      x: 1.0, y: 6.55, w: 8, h: 0.4,
      fontSize: 12, fontFace: FONT, color: "CADCFC", margin: 0,
    });
  }

  // ==========================================================================
  // 2 — LE DEFI DU MARCHE
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    // Header
    s.addText("Le defi", {
      x: 0.6, y: 0.5, w: 4, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Un ecosysteme financier fragmente", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    // Stat hero a gauche
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.6, y: 2.3, w: 4.8, h: 4.4,
      fill: { color: C.navy }, line: { type: "none" },
    });
    s.addText("12+", {
      x: 0.8, y: 2.6, w: 4.4, h: 1.8,
      fontSize: 110, fontFace: FONT_HEAD, color: C.gold, bold: true, margin: 0,
    });
    s.addText("integrations distinctes a maintenir pour couvrir un marche regional.", {
      x: 0.8, y: 4.5, w: 4.4, h: 1.4,
      fontSize: 16, fontFace: FONT, color: C.white, margin: 0,
    });
    s.addText("(banques, EME, wallets, reseaux de paiement)", {
      x: 0.8, y: 6.0, w: 4.4, h: 0.5,
      fontSize: 11, fontFace: FONT, color: "CADCFC", italic: true, margin: 0,
    });

    // 3 cards probleme a droite
    const cards = [
      {
        icon: icons.layersN,
        title: "Multiplicite des canaux",
        body: "Chaque partenaire impose son protocole, son format, ses regles. La complexite explose.",
      },
      {
        icon: icons.clockN,
        title: "Time-to-market degrade",
        body: "Connecter un nouveau partenaire prend des mois. Les couts d'integration s'accumulent.",
      },
      {
        icon: icons.searchN,
        title: "Visibilite limitee",
        body: "Les transactions, les reconciliations et les frais sont eclates dans des silos.",
      },
    ];
    const cx = 5.8, cw = 7.1, ch = 1.35, gap = 0.2;
    cards.forEach((card, i) => {
      const cy = 2.3 + i * (ch + gap);
      s.addShape(pres.shapes.RECTANGLE, {
        x: cx, y: cy, w: cw, h: ch,
        fill: { color: C.white }, line: { color: C.border, width: 1 },
        shadow: { type: "outer", color: "000000", blur: 8, offset: 2, angle: 90, opacity: 0.06 },
      });
      iconBubble(s, card.icon, cx + 0.3, cy + 0.3, 0.75, C.iceblue, C.navy);
      s.addText(card.title, {
        x: cx + 1.25, y: cy + 0.22, w: cw - 1.4, h: 0.4,
        fontSize: 16, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
      });
      s.addText(card.body, {
        x: cx + 1.25, y: cy + 0.62, w: cw - 1.4, h: 0.65,
        fontSize: 12, fontFace: FONT, color: C.slate, margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 3 — NOTRE VISION
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Notre vision", {
      x: 0.6, y: 0.5, w: 4, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Une seule plateforme. Tous vos partenaires.", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    // Schema CLIENT -> FINTECH HUB -> PARTENAIRES
    const boxY = 2.7, boxH = 2.6;

    // Vos clients
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.6, y: boxY, w: 3.0, h: boxH,
      fill: { color: C.steel }, line: { type: "none" },
    });
    iconBubble(s, icons.mobile, 1.7, boxY + 0.35, 0.8, C.gold, C.navy);
    s.addText("VOS CLIENTS", {
      x: 0.7, y: boxY + 1.3, w: 2.8, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.white, bold: true, align: "center", charSpacing: 4, margin: 0,
    });
    s.addText("Web, mobile, agents", {
      x: 0.7, y: boxY + 1.7, w: 2.8, h: 0.4,
      fontSize: 13, fontFace: FONT, color: "CADCFC", align: "center", margin: 0,
    });

    // Fleche 1
    s.addImage({ data: icons.arrow, x: 3.75, y: boxY + 1.0, w: 0.6, h: 0.6 });

    // Fintech Hub - centre
    s.addShape(pres.shapes.RECTANGLE, {
      x: 4.5, y: boxY - 0.3, w: 4.3, h: boxH + 0.6,
      fill: { color: C.navy }, line: { type: "none" },
      shadow: { type: "outer", color: "000000", blur: 12, offset: 4, angle: 90, opacity: 0.20 },
    });
    s.addShape(pres.shapes.RECTANGLE, {
      x: 4.5, y: boxY - 0.3, w: 4.3, h: 0.15, fill: { color: C.gold }, line: { type: "none" },
    });
    iconBubble(s, icons.cogsN, 6.1, boxY + 0.2, 1.1, C.gold, C.navy);
    s.addText("FINTECH HUB", {
      x: 4.6, y: boxY + 1.5, w: 4.1, h: 0.4,
      fontSize: 13, fontFace: FONT, color: C.gold, bold: true, align: "center", charSpacing: 4, margin: 0,
    });
    s.addText("Orchestration, securite, comptabilite, reporting", {
      x: 4.6, y: boxY + 1.95, w: 4.1, h: 0.7,
      fontSize: 13, fontFace: FONT, color: C.white, align: "center", margin: 0,
    });

    // Fleche 2
    s.addImage({ data: icons.arrow, x: 8.95, y: boxY + 1.0, w: 0.6, h: 0.6 });

    // Partenaires
    s.addShape(pres.shapes.RECTANGLE, {
      x: 9.7, y: boxY, w: 3.0, h: boxH,
      fill: { color: C.steel }, line: { type: "none" },
    });
    s.addText("PARTENAIRES", {
      x: 9.8, y: boxY + 0.2, w: 2.8, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.white, bold: true, align: "center", charSpacing: 4, margin: 0,
    });
    // 3 mini-icones
    iconBubble(s, icons.bank,   9.95, boxY + 0.75, 0.55, C.iceblue, C.navy);
    iconBubble(s, icons.wallet, 10.85, boxY + 0.75, 0.55, C.iceblue, C.navy);
    iconBubble(s, icons.cloud,  11.75, boxY + 0.75, 0.55, C.iceblue, C.navy);
    s.addText("Banques", {
      x: 9.75, y: boxY + 1.4, w: 1.0, h: 0.3,
      fontSize: 10, fontFace: FONT, color: "CADCFC", align: "center", margin: 0,
    });
    s.addText("Wallets", {
      x: 10.65, y: boxY + 1.4, w: 1.0, h: 0.3,
      fontSize: 10, fontFace: FONT, color: "CADCFC", align: "center", margin: 0,
    });
    s.addText("EME", {
      x: 11.55, y: boxY + 1.4, w: 1.0, h: 0.3,
      fontSize: 10, fontFace: FONT, color: "CADCFC", align: "center", margin: 0,
    });
    s.addText("Une integration, des dizaines de canaux.", {
      x: 9.8, y: boxY + 1.95, w: 2.8, h: 0.5,
      fontSize: 11, fontFace: FONT, color: "CADCFC", italic: true, align: "center", margin: 0,
    });

    // Sous-bandeau
    s.addText(
      "Fintech Hub agit comme votre hub unique : un seul contrat, un seul format, " +
      "un seul tableau de bord. Tous les partenaires sont a portee d'integration.",
      {
        x: 0.6, y: 5.9, w: 12.1, h: 1.1,
        fontSize: 15, fontFace: FONT, color: C.slate, italic: true, align: "center", margin: 0,
      }
    );

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 4 — POUR QUI ?
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Pour qui ?", {
      x: 0.6, y: 0.5, w: 4, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Concue pour les acteurs qui vont vite", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const personas = [
      {
        icon: icons.boltN,
        title: "Fintechs en croissance",
        body: "Vous voulez lancer un nouveau service en semaines, pas en trimestres. Vous avez besoin d'evoluer sans refondre votre stack a chaque partenaire.",
      },
      {
        icon: icons.cartN,
        title: "Acteurs e-commerce & paiement",
        body: "Vous encaissez via plusieurs reseaux et reconcilier reste un casse-tete. Vous voulez un seul outil pour tout suivre.",
      },
      {
        icon: icons.bankN,
        title: "Institutions en transformation",
        body: "Vous modernisez votre offre digitale et ouvrez de nouveaux canaux. Vous cherchez un partenaire qui vous garantit conformite et tracabilite.",
      },
    ];
    const cw = 3.95, ch = 4.7, gap = 0.25;
    personas.forEach((p, i) => {
      const x = 0.6 + i * (cw + gap);
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.3, w: cw, h: ch,
        fill: { color: C.white }, line: { color: C.border, width: 1 },
        shadow: { type: "outer", color: "000000", blur: 10, offset: 3, angle: 90, opacity: 0.08 },
      });
      // bandeau gold haut
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.3, w: cw, h: 0.12, fill: { color: C.gold }, line: { type: "none" },
      });
      iconBubble(s, p.icon, x + (cw - 1.0) / 2, 2.7, 1.0, C.iceblue, C.navy);
      s.addText(p.title, {
        x: x + 0.3, y: 4.0, w: cw - 0.6, h: 0.7,
        fontSize: 18, fontFace: FONT_HEAD, color: C.navy, bold: true, align: "center", margin: 0,
      });
      s.addText(p.body, {
        x: x + 0.3, y: 4.8, w: cw - 0.6, h: 1.9,
        fontSize: 12, fontFace: FONT, color: C.slate, align: "center", margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 5 — COMMENT CA MARCHE
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Comment ca marche", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Trois etapes, zero friction", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const steps = [
      { n: "01", title: "Vous vous connectez",
        body: "Une seule cle, un seul format. Notre equipe vous accompagne dans le branchement initial." },
      { n: "02", title: "Vous orchestrez",
        body: "Chaque transaction est routee, securisee et comptabilisee automatiquement selon vos regles." },
      { n: "03", title: "Vous pilotez",
        body: "Suivez en temps reel les volumes, les frais et la sante de vos partenaires depuis une console unique." },
    ];

    // ligne horizontale liant les etapes
    s.addShape(pres.shapes.LINE, {
      x: 1.8, y: 4.0, w: 9.6, h: 0,
      line: { color: C.gold, width: 3, dashType: "dash" },
    });

    const cw = 3.95, gap = 0.25;
    steps.forEach((st, i) => {
      const x = 0.6 + i * (cw + gap);

      // Bulle numero centree au-dessus de la ligne
      const bubbleX = x + cw/2 - 0.55;
      s.addShape(pres.shapes.OVAL, {
        x: bubbleX, y: 3.45, w: 1.1, h: 1.1,
        fill: { color: C.navy }, line: { color: C.gold, width: 3 },
      });
      s.addText(st.n, {
        x: bubbleX, y: 3.55, w: 1.1, h: 0.9,
        fontSize: 26, fontFace: FONT_HEAD, color: C.gold, bold: true,
        align: "center", valign: "middle", margin: 0,
      });

      // Titre
      s.addText(st.title, {
        x: x + 0.2, y: 4.95, w: cw - 0.4, h: 0.6,
        fontSize: 19, fontFace: FONT_HEAD, color: C.navy, bold: true, align: "center", margin: 0,
      });
      // Description
      s.addText(st.body, {
        x: x + 0.3, y: 5.65, w: cw - 0.6, h: 1.4,
        fontSize: 13, fontFace: FONT, color: C.slate, align: "center", margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 6 — BENEFICES
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Vos benefices", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Ce que vous gagnez, des le premier jour", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 30, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const benefits = [
      { icon: icons.boltN,      title: "Time-to-market accelere",
        body: "Lancer un nouveau partenaire devient une question d'heures, pas de mois." },
      { icon: icons.shieldN,    title: "Securite de niveau bancaire",
        body: "Chiffrement des donnees sensibles, audit immuable, conformite reglementaire integree." },
      { icon: icons.chartN,     title: "Visibilite temps reel",
        body: "Une console unique : volumes, frais, taux de reussite, soldes partenaires." },
      { icon: icons.handshakeN, title: "Resilience operationnelle",
        body: "Reconciliation automatique, reprise sur incident, supervision 24/7 par defaut." },
    ];

    // 2 x 2 grid
    const cw = 6.0, ch = 2.35, gx = 0.6, gy = 2.3, gap = 0.25;
    benefits.forEach((b, i) => {
      const col = i % 2, row = Math.floor(i / 2);
      const x = gx + col * (cw + gap);
      const y = gy + row * (ch + gap);

      s.addShape(pres.shapes.RECTANGLE, {
        x, y, w: cw, h: ch,
        fill: { color: C.white }, line: { color: C.border, width: 1 },
        shadow: { type: "outer", color: "000000", blur: 8, offset: 2, angle: 90, opacity: 0.07 },
      });
      // Petite barre laterale gold
      s.addShape(pres.shapes.RECTANGLE, {
        x, y, w: 0.12, h: ch, fill: { color: C.gold }, line: { type: "none" },
      });
      iconBubble(s, b.icon, x + 0.4, y + 0.4, 0.95, C.iceblue, C.navy);
      s.addText(b.title, {
        x: x + 1.55, y: y + 0.35, w: cw - 1.8, h: 0.6,
        fontSize: 19, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
      });
      s.addText(b.body, {
        x: x + 1.55, y: y + 0.95, w: cw - 1.8, h: 1.25,
        fontSize: 13, fontFace: FONT, color: C.slate, margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 7 — CAS D'USAGE
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Cas d'usage", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Trois scenarios qui parlent a votre business", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 30, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const uses = [
      { icon: icons.cartN,
        title: "Encaissement omnicanal",
        body: "Acceptez les paiements depuis n'importe quel reseau, n'importe quel canal. " +
              "Une experience client unifiee, une comptabilite consolidee." },
      { icon: icons.walletN,
        title: "Decaissement multi-reseaux",
        body: "Versez salaires, indemnites ou cashbacks via wallets, banques ou agents — " +
              "automatiquement, en un seul flux." },
      { icon: icons.syncN,
        title: "Reconciliation automatisee",
        body: "Chaque transaction est ecriture comptable : detail des frais, traces d'audit, " +
              "reporting genere a la volee." },
    ];

    const cw = 3.95, ch = 4.7, gap = 0.25;
    uses.forEach((u, i) => {
      const x = 0.6 + i * (cw + gap);
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.3, w: cw, h: ch,
        fill: { color: C.navy }, line: { type: "none" },
      });
      // Header card
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.3, w: cw, h: 1.3, fill: { color: C.steel }, line: { type: "none" },
      });
      iconBubble(s, u.icon, x + (cw - 0.95) / 2, 2.45, 0.95, C.gold, C.navy);

      s.addText(u.title, {
        x: x + 0.3, y: 3.75, w: cw - 0.6, h: 0.7,
        fontSize: 18, fontFace: FONT_HEAD, color: C.white, bold: true, align: "center", margin: 0,
      });
      s.addText(u.body, {
        x: x + 0.4, y: 4.55, w: cw - 0.8, h: 2.3,
        fontSize: 13, fontFace: FONT, color: "CADCFC", align: "center", margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 8 — DIFFERENCIATEURS
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Pourquoi nous", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Ce qui nous distingue", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const diffs = [
      { title: "Comptabilite parametrable",
        body: "Definissez vos propres regles comptables sans une seule ligne de code." },
      { title: "Multi-partenaires natif",
        body: "Bascule fluide d'un fournisseur a l'autre, sans tout reconstruire." },
      { title: "Reporting unifie",
        body: "Un tableau de bord, une seule source de verite pour tous vos canaux." },
    ];
    const cw = 3.95, ch = 4.5, gap = 0.25;
    diffs.forEach((d, i) => {
      const x = 0.6 + i * (cw + gap);
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.4, w: cw, h: ch,
        fill: { color: C.white }, line: { color: C.border, width: 1 },
        shadow: { type: "outer", color: "000000", blur: 10, offset: 3, angle: 90, opacity: 0.08 },
      });
      // Big number gold a gauche
      s.addText(String(i + 1).padStart(2, "0"), {
        x: x + 0.3, y: 2.6, w: 2.5, h: 1.6,
        fontSize: 90, fontFace: FONT_HEAD, color: C.gold, bold: true, margin: 0,
      });
      // titre
      s.addText(d.title, {
        x: x + 0.3, y: 4.2, w: cw - 0.6, h: 0.7,
        fontSize: 18, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
      });
      // body
      s.addText(d.body, {
        x: x + 0.3, y: 4.95, w: cw - 0.6, h: 1.8,
        fontSize: 13, fontFace: FONT, color: C.slate, margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 9 — CHIFFRES CLES (PROMESSES)
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.navy };

    // bandeau gold haut
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: W, h: 0.15, fill: { color: C.gold }, line: { type: "none" },
    });

    s.addText("Ce que nous promettons", {
      x: 0.6, y: 0.55, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Quatre engagements concrets", {
      x: 0.6, y: 0.95, w: 12, h: 0.9,
      fontSize: 34, fontFace: FONT_HEAD, color: C.white, bold: true, margin: 0,
    });

    const stats = [
      { big: "80%",  label: "de reduction du time-to-market\nlors de l'ajout d'un partenaire" },
      { big: "100%", label: "des transactions tracees,\nauditees et reconciliees" },
      { big: "24/7", label: "de supervision continue\net resilience aux incidents" },
      { big: ">99,9%", label: "de disponibilite cible,\ngarantie par SLA" },
    ];
    const cw = 3.0, ch = 3.3, gx = 0.6, gy = 2.6, gap = 0.18;
    stats.forEach((st, i) => {
      const x = gx + i * (cw + gap);
      // Carte foncee avec bord gold
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: gy, w: cw, h: ch,
        fill: { color: C.steel }, line: { color: C.gold, width: 1 },
      });
      // big number
      s.addText(st.big, {
        x, y: gy + 0.3, w: cw, h: 1.6,
        fontSize: 60, fontFace: FONT_HEAD, color: C.gold, bold: true,
        align: "center", valign: "middle", margin: 0,
      });
      // accent
      s.addShape(pres.shapes.RECTANGLE, {
        x: x + cw / 2 - 0.3, y: gy + 1.9, w: 0.6, h: 0.04,
        fill: { color: C.gold }, line: { type: "none" },
      });
      // label
      s.addText(st.label, {
        x: x + 0.2, y: gy + 2.05, w: cw - 0.4, h: 1.1,
        fontSize: 12, fontFace: FONT, color: C.white, align: "center", margin: 0,
      });
    });

    s.addText("Engagements bases sur les standards du marche fintech regional.", {
      x: 0.6, y: 6.3, w: 12.1, h: 0.4,
      fontSize: 11, fontFace: FONT, color: "CADCFC", italic: true, align: "center", margin: 0,
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 10 — MODELE ECONOMIQUE
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Modele economique", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Simple, transparent, predictible", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    const cards = [
      {
        title: "SETUP",
        subtitle: "Frais d'integration uniques",
        body: "Onboarding cle en main, accompagnement par notre equipe, premier partenaire connecte en moins de 30 jours.",
      },
      {
        title: "SUBSCRIPTION",
        subtitle: "Abonnement mensuel",
        body: "Acces complet a la plateforme, support reactif, mises a jour fonctionnelles continues.",
      },
      {
        title: "PER TRANSACTION",
        subtitle: "Frais variables alignes",
        body: "Une commission claire par transaction reussie. Pas de coup cache, pas de surfacturation.",
      },
    ];
    const cw = 3.95, ch = 4.6, gap = 0.25;
    cards.forEach((c, i) => {
      const x = 0.6 + i * (cw + gap);
      const featured = (i === 1);
      // Carte
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 2.3, w: cw, h: ch,
        fill: { color: featured ? C.navy : C.white },
        line: { color: featured ? C.gold : C.border, width: featured ? 2 : 1 },
        shadow: { type: "outer", color: "000000", blur: 10, offset: 3, angle: 90,
                  opacity: featured ? 0.18 : 0.08 },
      });
      // Header zone
      s.addText(c.title, {
        x: x + 0.3, y: 2.6, w: cw - 0.6, h: 0.5,
        fontSize: 14, fontFace: FONT, color: featured ? C.gold : C.gold,
        bold: true, charSpacing: 6, align: "center", margin: 0,
      });
      s.addText(c.subtitle, {
        x: x + 0.3, y: 3.15, w: cw - 0.6, h: 0.7,
        fontSize: 20, fontFace: FONT_HEAD, color: featured ? C.white : C.navy,
        bold: true, align: "center", margin: 0,
      });
      // ligne separatrice
      s.addShape(pres.shapes.RECTANGLE, {
        x: x + 1.2, y: 4.0, w: cw - 2.4, h: 0.03,
        fill: { color: featured ? C.gold : C.border }, line: { type: "none" },
      });
      // body
      s.addText(c.body, {
        x: x + 0.4, y: 4.2, w: cw - 0.8, h: 2.0,
        fontSize: 13, fontFace: FONT, color: featured ? "CADCFC" : C.slate,
        align: "center", margin: 0,
      });
    });

    s.addText("* Tarification ajustee a votre volume et a votre couverture geographique.", {
      x: 0.6, y: 7.0, w: 12.1, h: 0.3,
      fontSize: 10, fontFace: FONT, color: C.muted, italic: true, align: "center", margin: 0,
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 11 — ROADMAP
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.cream };

    s.addText("Roadmap", {
      x: 0.6, y: 0.5, w: 5, h: 0.4,
      fontSize: 12, fontFace: FONT, color: C.gold, bold: true, charSpacing: 6, margin: 0,
    });
    s.addText("Une plateforme qui evolue avec vous", {
      x: 0.6, y: 0.9, w: 12, h: 0.8,
      fontSize: 32, fontFace: FONT_HEAD, color: C.navy, bold: true, margin: 0,
    });
    goldAccent(s, 0.6, 1.75, 1.0, 0.06);

    // Ligne horizontale timeline
    const lineY = 4.0;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 1.0, y: lineY, w: 11.3, h: 0.08,
      fill: { color: C.navy }, line: { type: "none" },
    });

    const steps = [
      { label: "AUJOURD'HUI", title: "Plateforme socle",
        body: "API unifiee, securite niveau bancaire, premiers partenaires connectes.",
        active: true },
      { label: "PROCHAINEMENT", title: "Marketplace de regles",
        body: "Bibliotheque de schemas comptables et de routages prets a l'emploi.",
        active: false },
      { label: "+ 12 MOIS", title: "Intelligence ajoutee",
        body: "Anti-fraude predictif, scoring transactionnel, alertes intelligentes.",
        active: false },
      { label: "+ 24 MOIS", title: "Couverture regionale",
        body: "Expansion vers de nouveaux marches et nouvelles devises.",
        active: false },
    ];
    const cw = 2.75, gap = 0.2;
    steps.forEach((st, i) => {
      const x = 1.0 + i * (cw + gap);

      // pastille sur la ligne
      const bubbleD = 0.5;
      const bx = x + cw / 2 - bubbleD / 2;
      s.addShape(pres.shapes.OVAL, {
        x: bx, y: lineY - 0.21, w: bubbleD, h: bubbleD,
        fill: { color: st.active ? C.gold : C.white },
        line: { color: C.navy, width: 3 },
      });

      // bloc texte au-dessus
      s.addText(st.label, {
        x: x, y: 2.4, w: cw, h: 0.3,
        fontSize: 10, fontFace: FONT, color: C.gold,
        bold: true, charSpacing: 4, align: "center", margin: 0,
      });
      s.addText(st.title, {
        x: x, y: 2.75, w: cw, h: 0.5,
        fontSize: 16, fontFace: FONT_HEAD, color: C.navy, bold: true,
        align: "center", margin: 0,
      });
      s.addText(st.body, {
        x: x + 0.15, y: 4.6, w: cw - 0.3, h: 1.8,
        fontSize: 12, fontFace: FONT, color: C.slate, align: "center", margin: 0,
      });
    });

    footer(s, n, totalSlides);
  }

  // ==========================================================================
  // 12 — APPEL A L'ACTION (clore en dark, miroir du cover)
  // ==========================================================================
  {
    n++;
    const s = pres.addSlide();
    s.background = { color: C.navy };

    s.addShape(pres.shapes.RECTANGLE, {
      x: 0, y: 0, w: 0.25, h: H, fill: { color: C.gold }, line: { type: "none" },
    });

    s.addText("PROCHAINE ETAPE", {
      x: 1.0, y: 0.6, w: 6, h: 0.5,
      fontSize: 13, fontFace: FONT_HEAD, color: C.gold,
      bold: true, charSpacing: 8, margin: 0,
    });

    s.addText("Discutons de votre projet.", {
      x: 1.0, y: 2.0, w: 11.3, h: 1.0,
      fontSize: 44, fontFace: FONT_HEAD, color: C.white, bold: true, margin: 0,
    });
    s.addText("Plus vite que vous ne le pensez.", {
      x: 1.0, y: 2.95, w: 11.3, h: 1.0,
      fontSize: 44, fontFace: FONT_HEAD, color: C.gold, bold: true, margin: 0,
    });

    s.addText(
      "Decouvrez en 30 minutes comment Fintech Hub peut transformer votre " +
      "experience financiere et accelerer vos lancements.",
      {
        x: 1.0, y: 4.4, w: 11.0, h: 1.0,
        fontSize: 16, fontFace: FONT, color: "CADCFC", italic: true, margin: 0,
      }
    );

    // Trois petites cards contact stylisees
    const blocks = [
      { label: "ECHANGEZ AVEC NOUS", value: "contact@aggregator.local" },
      { label: "RESERVEZ UNE DEMO",  value: "demo.aggregator.local" },
      { label: "ECRIVEZ-NOUS",       value: "hello@aggregator.local" },
    ];
    const cw = 3.7, ch = 1.2, gap = 0.25;
    blocks.forEach((b, i) => {
      const x = 1.0 + i * (cw + gap);
      s.addShape(pres.shapes.RECTANGLE, {
        x, y: 5.7, w: cw, h: ch,
        fill: { color: C.steel }, line: { color: C.gold, width: 1 },
      });
      s.addText(b.label, {
        x: x + 0.2, y: 5.8, w: cw - 0.4, h: 0.35,
        fontSize: 10, fontFace: FONT, color: C.gold,
        bold: true, charSpacing: 4, margin: 0,
      });
      s.addText(b.value, {
        x: x + 0.2, y: 6.2, w: cw - 0.4, h: 0.5,
        fontSize: 16, fontFace: FONT_HEAD, color: C.white, bold: true, margin: 0,
      });
    });
  }

  // Sortie
  const out = path.join(__dirname, "FintechHub-Pitch.pptx");
  await pres.writeFile({ fileName: out });
  console.log("Genere : " + out);
}

build().catch(err => { console.error(err); process.exit(1); });

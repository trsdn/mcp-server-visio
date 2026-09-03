/**
 * Quality checks on a generated Visio drawing.
 *
 * These read the drawing through visiocli rather than by parsing the file format. The PowerPoint
 * original scraped DrawingML out of the .pptx zip; Visio's schema is different, and there is no
 * reason to parse a file the tool under test can describe directly.
 */

/** Archetypes whose output is a business diagram, and so is held to a restrained visual style. */
const BUSINESS_STYLE_ARCHETYPES = new Set([
  "block-diagram",
  "cross-functional-flowchart",
  "data-flow-diagram",
  "decision-tree",
  "entity-relationship",
  "flowchart",
  "network-diagram",
  "org-chart",
  "process-map",
  "state-diagram",
  "swimlane",
  "system-context",
  "value-stream-map",
]);

/**
 * Stencil masters that read as decoration rather than notation. A business diagram built from
 * these is telling the reader something the content does not support.
 */
const NOVELTY_MASTERS = new Set([
  "cloud",
  "explosion",
  "heart",
  "lightning bolt",
  "moon",
  "smiley face",
  "star 4",
  "star 5",
  "star 6",
  "star 7",
  "sun",
]);

const MAX_VIVID_FILL_COLORS = 3;

function normalizeText(text) {
  return String(text || "")
    .replace(/\s+/g, " ")
    .trim()
    .toLowerCase();
}

function collectQuotedTexts(text) {
  const values = [];

  for (const match of text.matchAll(/"([^"]+)"/g)) {
    values.push(match[1]);
  }

  return values;
}

/**
 * Text the plan explicitly demands on a page — its title, plus anything quoted after a labelling
 * phrase. Everything else in a plan is guidance rather than a literal requirement.
 */
export function collectRequiredPageTexts(page) {
  const requiredTexts = new Set();

  if (page?.title) {
    requiredTexts.add(page.title);
  }

  const content = page?.content || "";
  const labelAnchorIndex = content.indexOf("Use these labels:");
  if (labelAnchorIndex >= 0) {
    for (const value of collectQuotedTexts(content.slice(labelAnchorIndex))) {
      requiredTexts.add(value);
    }
  } else {
    for (const match of content.matchAll(/(?:label|sentence|with|caption[^:]*):\s*"([^"]+)"/gi)) {
      requiredTexts.add(match[1]);
    }
  }

  return [...requiredTexts];
}

/** Every non-empty shape text on a page, as reported by shape(list). */
export function extractShapeTexts(pageShapes) {
  return (pageShapes || [])
    .map((shape) => String(shape?.text || "").trim())
    .filter((text) => text.length > 0);
}

/** Master names behind the shapes on a page. Drawn shapes have none and are reported as "". */
export function extractMasterNames(pageShapes) {
  return (pageShapes || [])
    .map((shape) => String(shape?.master || "").trim().toLowerCase())
    .filter((name) => name.length > 0);
}

/**
 * Fill colours as RRGGBB, from the FillForegnd formula. Visio writes RGB(r,g,b); a themed fill
 * reads THEMEVAL() and is deliberately ignored, since the theme is not the agent's choice.
 */
export function extractFillColors(pageShapes) {
  const colors = [];

  for (const shape of pageShapes || []) {
    const formula = String(shape?.fillForeground || "");
    const match = formula.match(/RGB\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)/i);

    if (match) {
      const hex = [match[1], match[2], match[3]]
        .map((value) => Number(value).toString(16).padStart(2, "0"))
        .join("")
        .toUpperCase();
      colors.push(hex);
    }
  }

  return colors;
}

function isBusinessStylePage(page) {
  return BUSINESS_STYLE_ARCHETYPES.has(page?.archetypeId || "");
}

function isVividHexColor(hex) {
  const red = Number.parseInt(hex.slice(0, 2), 16);
  const green = Number.parseInt(hex.slice(2, 4), 16);
  const blue = Number.parseInt(hex.slice(4, 6), 16);
  const max = Math.max(red, green, blue);
  const min = Math.min(red, green, blue);

  if (!Number.isFinite(max) || !Number.isFinite(min) || max === 0) {
    return false;
  }

  const saturation = (max - min) / max;
  return saturation >= 0.35 && (max - min) >= 40 && max >= 80;
}

function getHueFamily(hex) {
  const red = Number.parseInt(hex.slice(0, 2), 16) / 255;
  const green = Number.parseInt(hex.slice(2, 4), 16) / 255;
  const blue = Number.parseInt(hex.slice(4, 6), 16) / 255;
  const max = Math.max(red, green, blue);
  const min = Math.min(red, green, blue);
  const delta = max - min;

  if (delta === 0) {
    return "neutral";
  }

  let hue;
  if (max === red) {
    hue = ((green - blue) / delta) % 6;
  } else if (max === green) {
    hue = ((blue - red) / delta) + 2;
  } else {
    hue = ((red - green) / delta) + 4;
  }

  const degrees = ((hue * 60) + 360) % 360;
  if (degrees < 30 || degrees >= 330) {
    return "red";
  }
  if (degrees < 75) {
    return "orange";
  }
  if (degrees < 150) {
    return "green";
  }
  if (degrees < 210) {
    return "cyan";
  }
  if (degrees < 270) {
    return "blue";
  }

  return "purple";
}

export function findPageQualityIssues(page, pageShapes) {
  if (!isBusinessStylePage(page)) {
    return [];
  }

  const issues = [];
  const noveltyMasters = [...new Set(
    extractMasterNames(pageShapes).filter((name) => NOVELTY_MASTERS.has(name))
  )];

  if (noveltyMasters.length > 0) {
    issues.push(
      `Page ${page.index} uses novelty stencil masters that are not acceptable for a business diagram: ${noveltyMasters.join(", ")}. Replace them with rectangles, rounded rectangles or the appropriate flowchart master.`
    );
  }

  const vividFillColors = [...new Set(extractFillColors(pageShapes).filter(isVividHexColor))];
  const vividColorFamilies = [...new Set(vividFillColors.map(getHueFamily).filter((family) => family !== "neutral"))];

  if (vividColorFamilies.length > MAX_VIVID_FILL_COLORS) {
    issues.push(
      `Page ${page.index} uses too many distinct vivid color families for a business diagram: ${vividFillColors.join(", ")}. Use a restrained palette with neutrals plus one main accent and semantic red/green only where justified.`
    );
  }

  return issues;
}

/**
 * A page with shapes but no connectors is usually a diagram that was drawn but never wired up,
 * which is the most common way a generated Visio drawing is wrong while looking plausible.
 */
export function findMissingConnectors(page, pageShapes, connectorCount) {
  if (!isBusinessStylePage(page)) {
    return [];
  }

  const nonConnectorShapes = (pageShapes || []).filter((shape) => String(shape?.text || "").trim().length > 0);

  if (nonConnectorShapes.length >= 2 && connectorCount === 0) {
    return [
      `Page ${page.index} has ${nonConnectorShapes.length} labelled shapes but no connectors. A ${page.archetypeId} needs its shapes joined with shape(connect-shapes).`,
    ];
  }

  return [];
}

export function findMissingRequiredTexts(page, actualTexts) {
  const combinedActualText = normalizeText(actualTexts.join(" "));

  return collectRequiredPageTexts(page).filter((requiredText) => {
    return !combinedActualText.includes(normalizeText(requiredText));
  });
}

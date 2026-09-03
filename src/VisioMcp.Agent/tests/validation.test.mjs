import assert from "node:assert/strict";
import test from "node:test";

import {
  collectRequiredPageTexts,
  extractFillColors,
  extractMasterNames,
  extractShapeTexts,
  findMissingConnectors,
  findMissingRequiredTexts,
  findPageQualityIssues,
} from "../src/validation.mjs";

test("collectRequiredPageTexts includes title, labels, callouts, and footer from a diagram plan", () => {
  const page = {
    index: 1,
    title: "Approve a West recovery plan to protect otherwise strong regional growth",
    content: [
      "Use these labels: \"North and East contribute most of the Q4 acceleration.\"; \"West remains below Q1 revenue and is about $0.8M behind plan.\".",
      "Beside the process lane, add one red risk callout with one sentence: \"Risk: If West stays flat next quarter, the annual target gap widens by about $1.6M.\".",
      "Beneath that, add one green next-step callout with: \"Next step: Approve a 90-day West recovery plan and weekly executive review cadence.\".",
      "Add a small 8pt gray caption at the bottom: \"Source: FY2026 regional revenue tracker; diagram as of Q4 close.\"",
    ].join(" "),
  };

  assert.deepEqual(collectRequiredPageTexts(page), [
    "Approve a West recovery plan to protect otherwise strong regional growth",
    "North and East contribute most of the Q4 acceleration.",
    "West remains below Q1 revenue and is about $0.8M behind plan.",
    "Risk: If West stays flat next quarter, the annual target gap widens by about $1.6M.",
    "Next step: Approve a 90-day West recovery plan and weekly executive review cadence.",
    "Source: FY2026 regional revenue tracker; diagram as of Q4 close.",
  ]);
});

test("extractShapeTexts returns the non-empty text of each shape", () => {
  const shapes = [
    { name: "Sheet.1", text: "Risk:" },
    { name: "Sheet.2", text: "  " },
    { name: "Sheet.3", text: "If West stays flat & delivery slips <2 pts>" },
    { name: "Sheet.4" },
  ];

  assert.deepEqual(extractShapeTexts(shapes), [
    "Risk:",
    "If West stays flat & delivery slips <2 pts>",
  ]);
});

test("extractMasterNames lowercases and skips shapes drawn without a master", () => {
  const shapes = [
    { name: "Sheet.1", master: "Sun" },
    { name: "Sheet.2", master: "" },
    { name: "Sheet.3" },
    { name: "Sheet.4", master: "Rounded Rectangle" },
  ];

  assert.deepEqual(extractMasterNames(shapes), ["sun", "rounded rectangle"]);
});

test("extractFillColors reads RGB formulas and ignores themed fills", () => {
  const shapes = [
    { name: "Sheet.1", fillForeground: "RGB(68,114,196)" },
    // A themed fill is the document's choice, not the agent's, so it is not part of the palette.
    { name: "Sheet.2", fillForeground: "THEMEVAL()" },
    { name: "Sheet.3", fillForeground: "RGB(192, 0, 0)" },
    { name: "Sheet.4" },
  ];

  assert.deepEqual(extractFillColors(shapes), ["4472C4", "C00000"]);
});

test("findMissingRequiredTexts matches combined page text across separate shapes", () => {
  const page = {
    index: 1,
    title: "Approve a West recovery plan to protect otherwise strong regional growth",
    content: [
      "Use these labels: \"North and East contribute most of the Q4 acceleration.\".",
      "Beside the process lane, add one red risk callout with one sentence: \"Risk: If West stays flat next quarter, the annual target gap widens by about $1.6M.\".",
      "Beneath that, add one green next-step callout with: \"Next step: Approve a 90-day West recovery plan and weekly executive review cadence.\".",
      "Add a small 8pt gray caption at the bottom: \"Source: FY2026 regional revenue tracker; diagram as of Q4 close.\"",
    ].join(" "),
  };

  const actualTexts = [
    "Approve a West recovery plan to protect otherwise strong regional growth",
    "North and East contribute most of the Q4 acceleration.",
    "Risk:",
    "If West stays flat next quarter, the annual target gap widens by about $1.6M.",
    "Next step: Approve a 90-day West recovery plan and weekly executive review cadence.",
  ];

  assert.deepEqual(findMissingRequiredTexts(page, actualTexts), [
    "Source: FY2026 regional revenue tracker; diagram as of Q4 close.",
  ]);
});

test("findPageQualityIssues flags novelty stencil masters on business diagrams", () => {
  const page = { index: 1, archetypeId: "flowchart" };
  const shapes = [
    { name: "Sheet.1", master: "Sun" },
    { name: "Sheet.2", master: "Rounded Rectangle" },
  ];

  assert.deepEqual(findPageQualityIssues(page, shapes), [
    "Page 1 uses novelty stencil masters that are not acceptable for a business diagram: sun. Replace them with rectangles, rounded rectangles or the appropriate flowchart master.",
  ]);
});

test("findPageQualityIssues flags overly colourful business diagrams", () => {
  const page = { index: 1, archetypeId: "org-chart" };
  const shapes = [
    { name: "Sheet.1", fillForeground: "RGB(68,114,196)" },
    { name: "Sheet.2", fillForeground: "RGB(0,176,240)" },
    { name: "Sheet.3", fillForeground: "RGB(112,173,71)" },
    { name: "Sheet.4", fillForeground: "RGB(192,0,0)" },
  ];

  assert.deepEqual(findPageQualityIssues(page, shapes), [
    "Page 1 uses too many distinct vivid color families for a business diagram: 4472C4, 00B0F0, 70AD47, C00000. Use a restrained palette with neutrals plus one main accent and semantic red/green only where justified.",
  ]);
});

test("findPageQualityIssues allows semantic red and green shades without flagging palette sprawl", () => {
  const page = { index: 1, archetypeId: "process-map" };
  const shapes = [
    { name: "Sheet.1", fillForeground: "RGB(134,239,172)" },
    { name: "Sheet.2", fillForeground: "RGB(22,101,52)" },
    { name: "Sheet.3", fillForeground: "RGB(185,28,28)" },
    { name: "Sheet.4", fillForeground: "RGB(153,27,27)" },
  ];

  assert.deepEqual(findPageQualityIssues(page, shapes), []);
});

test("findPageQualityIssues leaves non-business archetypes alone", () => {
  const page = { index: 1, archetypeId: "freeform-sketch" };
  const shapes = [{ name: "Sheet.1", master: "Sun" }];

  assert.deepEqual(findPageQualityIssues(page, shapes), []);
});

test("findMissingConnectors flags a diagram whose shapes were never joined", () => {
  const page = { index: 2, archetypeId: "flowchart" };
  const shapes = [
    { name: "Sheet.1", text: "Start" },
    { name: "Sheet.2", text: "Decide" },
    { name: "Sheet.3", text: "End" },
  ];

  // The most common way a generated Visio drawing is wrong while looking plausible: the boxes
  // are there, so a screenshot looks right, but nothing is connected.
  assert.deepEqual(findMissingConnectors(page, shapes, 0), [
    "Page 2 has 3 labelled shapes but no connectors. A flowchart needs its shapes joined with shape(connect-shapes).",
  ]);
});

test("findMissingConnectors accepts a connected diagram", () => {
  const page = { index: 2, archetypeId: "flowchart" };
  const shapes = [
    { name: "Sheet.1", text: "Start" },
    { name: "Sheet.2", text: "End" },
  ];

  assert.deepEqual(findMissingConnectors(page, shapes, 1), []);
});

test("findMissingConnectors does not demand connectors for a single shape", () => {
  const page = { index: 1, archetypeId: "flowchart" };
  const shapes = [{ name: "Sheet.1", text: "Only box" }];

  assert.deepEqual(findMissingConnectors(page, shapes, 0), []);
});

import {
  existsSync,
  mkdirSync,
  openSync,
  closeSync,
  readFileSync,
  readdirSync,
  rmSync,
  writeFileSync,
} from "fs";
import { execFileSync } from "child_process";
import { basename, dirname, extname, join, resolve } from "path";
import {
  DEFAULT_EXECUTE_TIMEOUT_MS,
  DEFAULT_MODEL,
  DEFAULT_PLAN_TIMEOUT_MS,
  DEFAULT_VERIFY_TIMEOUT_MS,
  REPO_ROOT,
} from "./constants.mjs";
import { parsePlanFromText } from "./planner.mjs";
import { createRuntime, destroyRuntime, runPhase } from "./runtime.mjs";
import { extractShapeTexts, findMissingConnectors, findMissingRequiredTexts, findPageQualityIssues } from "./validation.mjs";
import { closeSessionsFor, readDrawing } from "./visioCli.mjs";

const MAX_REPAIR_ATTEMPTS = 3;

function buildBusinessQualityRules() {
  return [
    "- Treat business-page quality as a hard requirement, not a nice-to-have.",
    "- Do NOT use novelty or decorative shapes like sun, star, heart, cloud, moon, or smiley shapes unless the plan explicitly asks for them.",
    "- For KPI cards, panels, and callouts, prefer rectangles or rounded rectangles with flat fills and simple lines.",
    "- Use a restrained business palette: neutral background, dark text, one main accent, and semantic red/green only where the content explicitly calls for risk or positive next steps.",
    "- If the result looks like default Visio theme art or contains gaudy styling, replace it before finishing.",
  ];
}

function withoutExtension(filePath) {
  return basename(filePath, extname(filePath));
}

function defaultOutputPath() {
  const now = new Date();
  const stamp = [
    now.getFullYear(),
    String(now.getMonth() + 1).padStart(2, "0"),
    String(now.getDate()).padStart(2, "0"),
    "-",
    String(now.getHours()).padStart(2, "0"),
    String(now.getMinutes()).padStart(2, "0"),
    String(now.getSeconds()).padStart(2, "0"),
  ].join("");

  return resolve(process.cwd(), `visio-mcp-agent-${stamp}.vsdx`);
}

function preparePaths(outputPath, overwrite) {
  const resolvedOutputPath = resolve(outputPath || defaultOutputPath());
  const planPath = join(dirname(resolvedOutputPath), `${withoutExtension(resolvedOutputPath)}.plan.json`);
  const artifactsDir = join(dirname(resolvedOutputPath), `${withoutExtension(resolvedOutputPath)}-artifacts`);
  const summaryPath = join(artifactsDir, "run-summary.json");

  const conflictingPath = [resolvedOutputPath, planPath, artifactsDir].find((path) => existsSync(path));
  if (conflictingPath && !overwrite) {
    throw new Error(`Output artifacts already exist: ${conflictingPath}. Use --overwrite to replace them.`);
  }

  if (overwrite) {
    rmSync(resolvedOutputPath, { force: true });
    rmSync(planPath, { force: true });
    rmSync(artifactsDir, { recursive: true, force: true });
  }

  mkdirSync(dirname(resolvedOutputPath), { recursive: true });
  mkdirSync(artifactsDir, { recursive: true });

  return {
    outputPath: resolvedOutputPath,
    planPath,
    artifactsDir,
    summaryPath,
  };
}

function loadArchetypeIds() {
  const archetypesDir = join(REPO_ROOT, "src", "VisioMcp.Core", "Data", "archetypes");

  return readdirSync(archetypesDir)
    .filter((fileName) => fileName.endsWith(".md"))
    .map((fileName) => fileName.replace(/\.md$/i, ""))
    .filter((id) => id !== "registry" && id !== "evidence-design")
    .sort();
}

function readText(relativePath) {
  return readFileSync(join(REPO_ROOT, relativePath), "utf-8").trim();
}

function loadPlanningGuidance() {
  const registry = readText("src\\VisioMcp.Core\\Data\\archetypes\\registry.md");
  const generationPipeline = readText("skills\\shared\\generation-pipeline.md");

  return [
    "Use this repository guidance while planning:",
    "",
    "## Archetype Registry",
    registry,
    "",
    "## Generation Pipeline",
    generationPipeline,
  ].join("\n");
}

function buildPlanningPrompt({ task, archetypeIds }) {
  return [
    "You are the planning phase of a Visio diagram agent.",
    "Do not create or modify any document in this phase.",
    "Do not rely on MCP batch execution or subagents.",
    "Return ONLY valid JSON and nothing else.",
    "",
    "Required schema:",
    "{",
    '  "pages": [',
    "    {",
    '      "index": 1,',
    '      "title": "Action title",',
    '      "archetypeId": "executive-summary",',
    '      "intent": "What the page must help the audience understand or decide",',
    '      "content": "Detailed build instructions specific enough for an execution phase"',
    "    }",
    "  ]",
    "}",
    "",
    `Allowed archetypeIds: ${archetypeIds.join(", ")}`,
    "",
    loadPlanningGuidance(),
    "",
    "User task:",
    task,
  ].join("\n");
}

function loadPlanFromFile(filePath) {
  const resolvedPath = resolve(filePath);
  if (!existsSync(resolvedPath)) {
    throw new Error(`Plan file was not found: ${resolvedPath}`);
  }

  const content = readFileSync(resolvedPath, "utf-8");
  const plan = parsePlanFromText(content);
  if (!plan) {
    throw new Error(`Plan file did not contain a valid diagram plan: ${resolvedPath}`);
  }

  return plan;
}

function buildPageExecutionRules(plan) {
  const blankFriendlyArchetypes = new Set([
    "appendix",
    "big-number",
    "chart-insight-callout",
    "column-bar-chart",
    "comparison",
    "executive-summary",
    "framework",
    "kpi-card-dashboard",
    "operational-kpi",
    "process-diagram",
    "recommendations",
    "simple-table",
    "timeline-roadmap",
    "waterfall-chart",
  ]);

  return plan.pages.flatMap((page) => {
    const rules = [`- Page ${page.index}: archetype '${page.archetypeId}'.`];
    const content = page.content || "";
    const contentLower = content.toLowerCase();
    const minimumShapeMatch = content.match(/(\d+)\+\s*shapes/i);

    rules.push(`- Page ${page.index}: render the exact page title text "${page.title}" as a visible heading and preserve it through later edits.`);

    if (page.archetypeId === "title-page") {
      rules.push(`- Page ${page.index}: use page(action='create', layout_name='Title Page').`);
      rules.push(`- Page ${page.index}: prefer placeholders for title and subtitle.`);
      return rules;
    }

    rules.push(`- Page ${page.index}: do NOT use page(action='create', layout_name='Title Page').`);

    if (contentLower.includes("blank layout") || blankFriendlyArchetypes.has(page.archetypeId)) {
      rules.push(`- Page ${page.index}: create the page with page(action='create', layout_name='Blank').`);
    }

    rules.push(`- Page ${page.index}: implement the detailed content literally; do not collapse it into only a title and subtitle.`);
    rules.push(`- Page ${page.index}: use separate shapes/text boxes/containers for distinct panels, cards, and callouts.`);

    if (minimumShapeMatch) {
      rules.push(`- Page ${page.index}: do not finish below ${minimumShapeMatch[1]} shapes because the plan explicitly requires that density.`);
    }

    if (contentLower.includes("kpi card") || contentLower.includes("kpi cards")) {
      rules.push(`- Page ${page.index}: build distinct KPI cards, each with its own background shape and text elements.`);
    }

    if (contentLower.includes("clustered column chart") || contentLower.includes("chart")) {
      rules.push(`- Page ${page.index}: create a real chart object when chart data is specified, not a text placeholder describing a chart.`);
    }

    if (contentLower.includes("insight panel")) {
      rules.push(`- Page ${page.index}: build the insight panel as its own container with separate bullet text elements.`);
    }

    if (contentLower.includes("callout")) {
      rules.push(`- Page ${page.index}: build each callout as a separate colored box with its own text.`);
    }

    return rules;
  });
}

function buildExecutionPrompt({ task, plan, outputPath, showVisio }) {
  const pageExecutionRules = buildPageExecutionRules(plan);
  const businessQualityRules = buildBusinessQualityRules();

  return [
    "You are the execution phase of a Visio diagram agent.",
    "You are operating through mcp-server-visio only.",
    "Do not rely on MCP batch execution or subagents.",
    "Treat the plan as fixed input. Build page-by-page with normal sequential MCP tool calls.",
    "",
    "Execution rules:",
    `- Create a new document at this exact path: ${outputPath}`,
    `- When creating the file, set show=${showVisio ? "true" : "false"}`,
    "- Keep one Visio session open for the full build.",
    "- Use the skill guidance plus design tools as needed.",
    "- Build pages in plan order.",
    `- The final document MUST contain exactly ${plan.pages.length} page(s).`,
    "- Prefer targeted edits over delete/rebuild.",
    "- Before finishing, verify page count with page list/read operations.",
    "- Finish only after file(action='close', save=true).",
    "",
    "Required MCP tool pattern:",
    "- Start with file(action='create', path=..., show=...)",
    "- For each page, create the page first, then populate content",
    "- Use page(action='list') to confirm the drawing structure before closing",
    "- Prefer placeholder(action='set-text') when a layout already provides title/subtitle placeholders",
    "",
    "Business design quality rules:",
    ...businessQualityRules,
    "",
    "Page-specific execution rules:",
    ...pageExecutionRules,
    "",
    "Title-page recipe:",
    "- If the archetype is title-page, create page(action='create', layout_name='Title Page')",
    "- Then use placeholder(action='list')",
    "- Set the title/subtitle with placeholder(action='set-text', placeholder_index=..., text=...)",
    "- Only fall back to freeform text boxes if the title layout does not expose placeholders",
    "",
    "Original user task:",
    task,
    "",
    "Structured plan:",
    "```json",
    JSON.stringify(plan, null, 2),
    "```",
    "",
    "Return a concise summary of what was built and any unresolved risks.",
  ].join("\n");
}

function buildRepairPrompt({ task, plan, outputPath, validationError, showVisio }) {
  const pageExecutionRules = buildPageExecutionRules(plan);
  const businessQualityRules = buildBusinessQualityRules();

  return [
    "You are the repair phase of a Visio diagram agent.",
    "A previous execution produced an incomplete document.",
    "Repair the document through mcp-server-visio only.",
    "Do not rely on MCP batch execution or subagents.",
    "",
    "Repair goal:",
    `- Output file path: ${outputPath}`,
    `- Required final page count: ${plan.pages.length}`,
    `- Validation failure to fix: ${validationError}`,
    "- If the validation failure names missing required text elements, add those text elements literally and preserve everything already built correctly.",
    "- If the validation failure names quality issues such as novelty shapes or palette problems, restyle the page until those issues are gone.",
    "- If the file exists, open and repair it. If it is missing, create it.",
    `- When creating a new file, set show=${showVisio ? "true" : "false"}`,
    "- Build or repair pages so the final drawing matches the plan.",
    "- Do not stop while a planned callout or footer container exists without its required text.",
    "- Use page list/read operations before closing to confirm the final structure.",
    "- Finish only after file(action='close', save=true).",
    "",
    "Business design quality rules:",
    ...businessQualityRules,
    "",
    "Page-specific repair rules:",
    ...pageExecutionRules,
    "",
    "Repair recipe for title-page outputs:",
    "- If the file is empty or missing pages, open or create it",
    "- Use page(action='create', layout_name='Title Page') for title-page plan items",
    "- Use placeholder(action='list') and placeholder(action='set-text') to write title and subtitle",
    "- Confirm the final page count with page(action='list') before closing",
    "- If a non-title page is missing its planned heading, add the exact page title from the plan as a visible top heading before closing",
    "",
    "Original user task:",
    task,
    "",
    "Structured plan:",
    "```json",
    JSON.stringify(plan, null, 2),
    "```",
    "",
    "Return a concise summary of what was repaired.",
  ].join("\n");
}

function buildVerificationPrompt({ task, outputPath, artifactsDir, plan }) {
  const businessQualityRules = buildBusinessQualityRules();
  const pageExecutionRules = buildPageExecutionRules(plan);

  return [
    "You are the verification phase of a Visio diagram agent.",
    "Re-open the generated document and review it with normal sequential MCP tool calls.",
    "Do not rely on MCP batch execution or subagents.",
    "Apply only targeted fixes for obvious structural issues.",
    "Preserve all content that already matches the plan, especially planned headings, callouts, and footer text.",
    "",
    "Verification rules:",
    `- Open this document: ${outputPath}`,
    "- Inspect pages with page list/read plus shape/text inspection as needed.",
    `- Export page images for human review into this directory: ${artifactsDir}`,
    "- Focus on both structure and visual business quality.",
    "- Specifically look for novelty shapes, default Visio theme art, weak palette choices, poor alignment, unreadable density, and obviously unprofessional styling.",
    "- If you find a visual quality issue, fix it instead of merely reporting it.",
    "- Before finishing, confirm that each page still contains the exact planned title text and any other required literal text from the plan.",
    "",
    "Business design quality rules:",
    ...businessQualityRules,
    "",
    "Page-specific verification rules:",
    ...pageExecutionRules,
    "",
    "Structured plan:",
    "```json",
    JSON.stringify(plan, null, 2),
    "```",
    "- Save and close the document when done.",
    "",
    "Original user task:",
    task,
    "",
    "Return a concise verification report with:",
    "- what was checked",
    "- what was fixed",
    "- any remaining concerns",
  ].join("\n");
}

function writeJson(path, value) {
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`, "utf-8");
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Closes any session still holding the artefact, so it can be inspected.
 *
 * The PowerPoint original attached to a running PowerPoint over COM with
 * GetObject("", "PowerPoint.Application") — which starts PowerPoint when none is running — and
 * closed the document from there. The service already owns every Visio session this agent
 * creates, so asking it to close is both correct and does not touch another application.
 */
function closeDrawingIfOpen(filePath) {
  try {
    closeSessionsFor(filePath);
  } catch {
    // Best-effort cleanup only: the caller waits for the file lock either way.
  }
}

async function waitForExpectedPageCount(filePath, expectedPageCount, timeoutMs = 15000) {
  const startedAt = Date.now();
  let lastCount = 0;

  while ((Date.now() - startedAt) < timeoutMs) {
    try {
      lastCount = readDrawing(filePath).length;
      if (lastCount >= expectedPageCount) {
        return lastCount;
      }
    } catch {
      // Keep retrying until Visio has flushed the file.
    }

    await delay(500);
  }

  return lastCount;
}

async function waitForFileUnlock(filePath, timeoutMs = 15000) {
  const startedAt = Date.now();

  while ((Date.now() - startedAt) < timeoutMs) {
    try {
      const handle = openSync(filePath, "r+");
      closeSync(handle);
      return true;
    } catch {
      await delay(500);
    }
  }

  return false;
}
async function validateDrawingArtifact(outputPath, expectedPageCount) {
  if (!existsSync(outputPath)) {
    throw new Error(`Output file was not created: ${outputPath}`);
  }

  closeDrawingIfOpen(outputPath);

  const unlocked = await waitForFileUnlock(outputPath);
  if (!unlocked) {
    throw new Error(`Output file is still locked after execution: ${outputPath}`);
  }

  const pageCount = await waitForExpectedPageCount(outputPath, expectedPageCount);
  if (pageCount < expectedPageCount) {
    throw new Error(
      `Output file contains ${pageCount} page(s), but ${expectedPageCount} were expected.`
    );
  }

  return { pageCount };
}

async function validateDrawingContent(outputPath, plan) {
  const problems = [];

  // One read of the whole drawing, rather than one per page: opening a Visio session is the
  // expensive part, and the planned pages are all in the same file.
  const drawing = readDrawing(outputPath);
  const byIndex = new Map(drawing.map((page) => [page.index, page]));

  for (const page of plan.pages) {
    const actual = byIndex.get(page.index);

    if (!actual) {
      problems.push(`Page ${page.index} ("${page.title}") is missing from the drawing.`);
      continue;
    }

    const missingTexts = findMissingRequiredTexts(page, extractShapeTexts(actual.shapes));
    if (missingTexts.length > 0) {
      problems.push(
        `Page ${page.index} is missing required text elements: ${missingTexts.map((text) => `"${text}"`).join(", ")}`
      );
    }

    problems.push(...findPageQualityIssues(page, actual.shapes));
    problems.push(...findMissingConnectors(page, actual.shapes, actual.connectorCount));
  }

  if (problems.length > 0) {
    throw new Error(problems.join(" "));
  }
}

async function validateDrawingOutput(outputPath, plan) {
  await validateDrawingArtifact(outputPath, plan.pages.length);
  await validateDrawingContent(outputPath, plan);
}
export async function runDiagramAgent(options) {
  const model = options.model || DEFAULT_MODEL;
  const paths = preparePaths(options.outputPath, Boolean(options.overwrite));
  const archetypeIds = loadArchetypeIds();
  const task = options.task || "Execute the supplied diagram plan exactly.";
  let plan;

  if (options.planFilePath) {
    plan = loadPlanFromFile(options.planFilePath);
  } else {
    const planRuntime = await createRuntime({
      model,
      verbose: options.verbose,
      enableMcp: false,
    });

    let planResult;
    try {
      planResult = await runPhase(planRuntime, {
        name: "plan",
        label: "Planning",
        enableMcp: false,
        enableSkills: true,
        timeoutMs: options.planTimeoutMs || DEFAULT_PLAN_TIMEOUT_MS,
        prompt: buildPlanningPrompt({
          task,
          archetypeIds,
        }),
      });
    } finally {
      await destroyRuntime(planRuntime);
    }

    if (!planResult.ok) {
      throw new Error(`Planning failed: ${planResult.error}`);
    }

    plan = parsePlanFromText(planResult.content);
    if (!plan) {
      throw new Error("Planning phase did not return a valid diagram plan.");
    }
  }

  writeJson(paths.planPath, plan);

  const executeRuntime = await createRuntime({
    model,
    verbose: options.verbose,
    mcpServerPath: options.mcpServerPath,
  });

  let executeResult;
  let repairSummary = null;
  try {
    executeResult = await runPhase(executeRuntime, {
      name: "execute",
      label: "Execution",
      enableMcp: true,
      enableSkills: true,
      successArtifactPath: paths.outputPath,
      timeoutMs: options.executeTimeoutMs || DEFAULT_EXECUTE_TIMEOUT_MS,
      prompt: buildExecutionPrompt({
        task,
        plan,
        outputPath: paths.outputPath,
        showVisio: Boolean(options.showVisio),
      }),
    });
  } finally {
    await destroyRuntime(executeRuntime);
  }

  if (!executeResult.ok) {
    if (!(executeResult.isTimeout && executeResult.artifactDetected)) {
      throw new Error(`Execution failed: ${executeResult.error}`);
    }
  }

  let drawingValidationError = null;
  try {
    await validateDrawingOutput(paths.outputPath, plan);
  } catch (validationError) {
    drawingValidationError = validationError;
  }

  if (drawingValidationError) {
    const repairSummaries = [];

    for (let attempt = 1; attempt <= MAX_REPAIR_ATTEMPTS; attempt++) {
      const repairRuntime = await createRuntime({
        model,
        verbose: options.verbose,
        mcpServerPath: options.mcpServerPath,
      });

      try {
        const repairResult = await runPhase(repairRuntime, {
          name: "improve",
          label: `Repair ${attempt}/${MAX_REPAIR_ATTEMPTS}`,
          enableMcp: true,
          enableSkills: true,
          successArtifactPath: paths.outputPath,
          timeoutMs: options.executeTimeoutMs || DEFAULT_EXECUTE_TIMEOUT_MS,
          prompt: buildRepairPrompt({
            task,
            plan,
            outputPath: paths.outputPath,
            validationError: drawingValidationError instanceof Error ? drawingValidationError.message : String(drawingValidationError),
            showVisio: Boolean(options.showVisio),
          }),
        });

        if (!repairResult.ok) {
          if (!(repairResult.isTimeout && repairResult.artifactDetected)) {
            throw new Error(`Repair failed: ${repairResult.error}`);
          }
        }

        repairSummaries.push(repairResult.content || repairResult.error || `Repair attempt ${attempt} completed.`);
      } finally {
        await destroyRuntime(repairRuntime);
      }

      try {
        await validateDrawingOutput(paths.outputPath, plan);
        repairSummary = repairSummaries.join("\n\n");
        drawingValidationError = null;
        break;
      } catch (validationError) {
        drawingValidationError = validationError;
      }
    }

    if (drawingValidationError) {
      throw drawingValidationError;
    }
  }

  let verifyResult = null;
  if (!options.skipVerify) {
    const verifyRuntime = await createRuntime({
      model,
      verbose: options.verbose,
      mcpServerPath: options.mcpServerPath,
    });

    try {
      verifyResult = await runPhase(verifyRuntime, {
        name: "verify",
        label: "Verification",
        enableMcp: true,
        enableSkills: true,
        successArtifactPath: paths.outputPath,
        timeoutMs: options.verifyTimeoutMs || DEFAULT_VERIFY_TIMEOUT_MS,
        prompt: buildVerificationPrompt({
          task,
          outputPath: paths.outputPath,
          artifactsDir: paths.artifactsDir,
          plan,
        }),
      });
    } finally {
      await destroyRuntime(verifyRuntime);
    }

    if (!verifyResult.ok) {
      if (!(verifyResult.isTimeout && verifyResult.artifactDetected)) {
        throw new Error(`Verification failed: ${verifyResult.error}`);
      }
    }

    if (!existsSync(paths.outputPath)) {
      throw new Error(`Verification completed but output file is missing: ${paths.outputPath}`);
    }

    await validateDrawingOutput(paths.outputPath, plan);
  }

  const summary = {
    task: options.task,
    model,
    outputPath: paths.outputPath,
    planPath: paths.planPath,
    artifactsDir: paths.artifactsDir,
    plan,
    executionSummary: executeResult.content || executeResult.error,
    repairSummary,
    verificationSummary: verifyResult?.content || null,
    constraints: {
      noMcpBatchDependency: true,
      noSubagentDependency: true,
    },
  };

  writeJson(paths.summaryPath, summary);

  return summary;
}

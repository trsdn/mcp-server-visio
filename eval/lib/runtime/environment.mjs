import { dirname, isAbsolute, join, resolve } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));

export const RUNTIME_ROOT = __dirname;
export const EVAL_ROOT = join(RUNTIME_ROOT, "..", "..");
export const REPO_ROOT = join(EVAL_ROOT, "..");
export const SKILLS_DIR = join(REPO_ROOT, "skills", "shared");
export const ARCHETYPES_DIR = join(REPO_ROOT, "src", "VisioMcp.Core", "Data", "archetypes");
export const CLI_PATH = join(REPO_ROOT, "src", "VisioMcp.CLI", "bin", "Release", "net9.0-windows", "visiocli.exe");
export const EVAL_ASSET_REPO_ROOT_ENVIRONMENT_VARIABLE = "VISIOMCP_EVAL_ASSET_REPO_ROOT";

export function getEvalAssetRepoRoot() {
  const configuredRoot = process.env[EVAL_ASSET_REPO_ROOT_ENVIRONMENT_VARIABLE];
  return configuredRoot ? resolve(configuredRoot) : REPO_ROOT;
}

export function getEvalAssetEvalRoot() {
  return join(getEvalAssetRepoRoot(), "eval");
}

export function resolveEvalAssetPath(...segments) {
  return join(getEvalAssetEvalRoot(), ...segments);
}

export function resolveEvalAssetPathFromRelative(relativeOrAbsolutePath) {
  const candidate = String(relativeOrAbsolutePath ?? "").trim();
  if (!candidate) {
    throw new Error("Eval asset path must be provided.");
  }

  return isAbsolute(candidate) ? resolve(candidate) : resolveEvalAssetPath(candidate);
}

export const EVAL_INPUT_ROOT = resolveEvalAssetPath("input");
export const EVAL_OUTPUT_ROOT = resolveEvalAssetPath("output");
export const EVAL_RESULTS_ROOT = resolveEvalAssetPath("results");
export const EVAL_DATA_ROOT = resolveEvalAssetPath("data");
export const EVAL_REFERENCE_CATALOG_ROOT = join(EVAL_DATA_ROOT, "archetype-references");

/**
 * Maps eval config archetype IDs (which may be variant names) to their
 * canonical family file basenames in ARCHETYPES_DIR.
 */
const ARCHETYPE_FAMILY_MAP = Object.freeze({
  // Direct matches (archetype ID === family file basename in src/VisioMcp.Core/Data/archetypes)
  "flowchart": "flowchart",
  "cross-functional-flowchart": "cross-functional-flowchart",
  "bpmn-process": "bpmn-process",
  "org-chart": "org-chart",
  "network-diagram": "network-diagram",
  "system-context": "system-context",
  "block-diagram": "block-diagram",
  "fault-tree": "fault-tree",
  "annotated-diagram": "annotated-diagram",
  // Variant aliases -> parent family
  "process-diagram": "flowchart",
  "process-map": "flowchart",
  "decision-tree": "flowchart",
  "swimlane": "cross-functional-flowchart",
  "swimlane-diagram": "cross-functional-flowchart",
  "bpmn": "bpmn-process",
  "org-structure": "org-chart",
  "network-topology": "network-diagram",
  "c4-context": "system-context",
  "context-diagram": "system-context",
  "architecture-diagram": "block-diagram",
  "layered-architecture": "block-diagram",
  "root-cause-analysis": "fault-tree",
  "callout-markup": "annotated-diagram",
});

/**
 * Resolves an archetype ID from an eval config to its canonical family ID.
 * Returns the input unchanged if no mapping exists.
 */
export function resolveArchetypeFamily(archetypeId) {
  return ARCHETYPE_FAMILY_MAP[archetypeId] || archetypeId;
}
export const MCP_SERVER_PATH = join(REPO_ROOT, "src", "VisioMcp.McpServer", "bin", "Release", "net9.0-windows", "VisioMcp.McpServer.exe");
export const ISOLATED_WORKER_PATH = join(EVAL_ROOT, "copilot-isolated-worker.mjs");

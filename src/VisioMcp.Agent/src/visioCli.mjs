import { execFileSync } from "child_process";
import { existsSync } from "fs";
import { join } from "path";

import { REPO_ROOT } from "./constants.mjs";

const CLI_ENV_KEYS = Object.freeze(["VISIO_MCP_AGENT_CLI", "VISIOCLI_PATH"]);

const DEFAULT_CLI_PATHS = Object.freeze([
  join(REPO_ROOT, "src", "VisioMcp.CLI", "bin", "Release", "net9.0-windows", "visiocli.exe"),
  join(REPO_ROOT, "src", "VisioMcp.CLI", "bin", "Debug", "net9.0-windows", "visiocli.exe"),
]);

/**
 * Locates visiocli.
 *
 * The agent verifies its own output by querying the drawing rather than by reading the file
 * format. The CLI is the supported way to do that, and it is already built alongside the MCP
 * server the agent drives.
 */
export function resolveCliPath(explicitPath = null) {
  if (explicitPath) {
    return explicitPath;
  }

  for (const key of CLI_ENV_KEYS) {
    if (process.env[key]) {
      return process.env[key];
    }
  }

  const found = DEFAULT_CLI_PATHS.find((candidate) => existsSync(candidate));
  if (found) {
    return found;
  }

  throw new Error(
    "visiocli was not found. Build it with 'dotnet build src\\VisioMcp.CLI -c Release', "
    + `or set ${CLI_ENV_KEYS[0]} to its path.`
  );
}

function runCli(args, { cliPath = null, timeoutMs = 120000 } = {}) {
  const executable = resolveCliPath(cliPath);

  let stdout;
  try {
    stdout = execFileSync(executable, args, {
      encoding: "utf-8",
      timeout: timeoutMs,
      windowsHide: true,
    });
  } catch (error) {
    // The CLI prints its JSON error body to stdout and exits non-zero, so prefer that over the
    // process-level message, which says only that the command failed.
    const body = error?.stdout?.toString().trim();
    throw new Error(body || error?.message || `visiocli ${args.join(" ")} failed.`);
  }

  const text = stdout.trim();
  if (!text) {
    throw new Error(`visiocli ${args.join(" ")} produced no output.`);
  }

  let parsed;
  try {
    parsed = JSON.parse(text);
  } catch {
    throw new Error(`visiocli ${args.join(" ")} did not return JSON: ${text.slice(0, 200)}`);
  }

  if (parsed.success === false) {
    throw new Error(parsed.errorMessage || `visiocli ${args.join(" ")} reported failure.`);
  }

  return parsed;
}

/**
 * Opens a session, runs a callback against it, and always closes it.
 */
export function withSession(filePath, callback, { cliPath = null } = {}) {
  const opened = runCli(["session", "open", filePath], { cliPath });
  const sessionId = opened.sessionId;

  if (!sessionId) {
    throw new Error(`Opening '${filePath}' returned no session id.`);
  }

  try {
    return callback((args) => runCli([...args, "-s", sessionId], { cliPath }));
  } finally {
    // Closing without saving: verification must never alter the artefact it is judging.
    try {
      runCli(["session", "close", "-s", sessionId, "--save", "false"], { cliPath });
    } catch {
      // A session that failed to open cleanly may already be gone.
    }
  }
}

/**
 * Reads every page with its shapes, enriched with what the quality checks need.
 *
 * shape(list) reports geometry and text but not the master a shape came from or its fill, so
 * those are fetched alongside: master(list-instances) once per master, and cell(read-formula)
 * per shape. Verification runs once at the end of a build, so the extra calls are cheap next to
 * the build itself.
 */
export function readDrawing(filePath, { cliPath = null } = {}) {
  return withSession(filePath, (call) => {
    const pages = call(["page", "list"]).pages || [];

    // Which shapes came from which master, keyed by "pageIndex\u0000shapeName".
    const masterByShape = new Map();
    for (const master of call(["master", "list"]).masters || []) {
      const instances = call(["master", "list-instances", "--master-name", master.name]).instances || [];
      for (const instance of instances) {
        masterByShape.set(`${instance.pageIndex}\u0000${instance.shapeName}`, master.name);
      }
    }

    return pages.map((page) => {
      const shapes = call(["shape", "list", "--page-index", String(page.pageIndex)]).shapes || [];
      const connectors = call(["shape", "list-connectors", "--page-index", String(page.pageIndex)]).connectors || [];
      const connectorNames = new Set(connectors.map((connector) => connector.name));

      return {
        index: page.pageIndex,
        name: page.name,
        isBackground: page.isBackground === true,
        connectorCount: connectors.length,
        shapes: shapes
          .filter((shape) => !connectorNames.has(shape.name))
          .map((shape) => ({
            name: shape.name,
            text: shape.text || "",
            shapeType: shape.shapeType || "",
            isGroup: shape.isGroup === true,
            width: shape.width,
            height: shape.height,
            master: masterByShape.get(`${page.pageIndex}\u0000${shape.name}`) || "",
            fillForeground: readFillForeground(call, page.pageIndex, shape.name),
          })),
      };
    });
  }, { cliPath });
}

/**
 * Fill formula for one shape, or "" when the cell cannot be read. A shape without a readable fill
 * is not a verification failure — it simply contributes no colour to the palette check.
 */
function readFillForeground(call, pageIndex, shapeName) {
  try {
    const result = call([
      "cell", "read-formula",
      "--page-index", String(pageIndex),
      "--shape-name", shapeName,
      "--cell-name", "FillForegnd",
    ]);

    return result?.cell?.formula || "";
  } catch {
    return "";
  }
}

/**
 * Closes any session this machine still holds on a file, so the artefact can be inspected.
 */
export function closeSessionsFor(filePath, { cliPath = null } = {}) {
  let listed;
  try {
    listed = runCli(["session", "list"], { cliPath });
  } catch {
    // No daemon running means nothing holds the file.
    return;
  }

  const normalized = filePath.toLowerCase();
  for (const session of listed.sessions || []) {
    const held = String(session.filePath || session.documentPath || "").toLowerCase();
    if (held === normalized) {
      try {
        runCli(["session", "close", "-s", session.sessionId, "--save"], { cliPath });
      } catch {
        // Best effort: the caller waits for the file lock to clear either way.
      }
    }
  }
}

import { CopilotClient, approveAll } from "@github/copilot-sdk";
import { execSync } from "child_process";
import { existsSync } from "fs";
import { CLI_PATH, MCP_SERVER_PATH, REPO_ROOT } from "./environment.mjs";
import { verifyBuildArtifacts, waitForArtifactFile } from "./reliability.mjs";

const BUILD_POLL_INTERVAL_MS = 5000;

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function getErrorMessage(error) {
  return error instanceof Error ? error.message : String(error);
}

export function buildCopilotSessionConfig(agent) {
  const sessionConfig = {
    model: agent.model,
    onPermissionRequest: approveAll,
    workingDirectory: REPO_ROOT,
  };

  if (agent.reasoningEffort) {
    sessionConfig.reasoningEffort = agent.reasoningEffort;
  }

  if (agent.transport === "mcp") {
    sessionConfig.mcpServers = {
      ppt: {
        command: MCP_SERVER_PATH,
        args: [],
        tools: ["*"],
        cwd: REPO_ROOT,
      },
    };
    sessionConfig.skillDirectories = [joinSkillsDirectory()];
  }

  return sessionConfig;
}

function joinSkillsDirectory() {
  return `${REPO_ROOT}\\skills`;
}

export async function createCopilotSdkRuntime(agent) {
  const client = new CopilotClient();
  await client.start();
  const session = await client.createSession(buildCopilotSessionConfig(agent));
  return { kind: "copilot-sdk", agent, client, session };
}

export async function destroyCopilotSdkRuntime(runtime, options = {}) {
  if (!runtime) return;

  const { force = false } = options;

  try { await runtime.session?.destroy?.(); } catch {}
  try {
    if (force) {
      await runtime.client?.forceStop?.();
    } else {
      await runtime.client?.stop?.();
    }
  } catch {}
}

export async function executeCopilotSdkFreshRequest(agent, request) {
  if (request.type === "build") {
    return executeFreshBuildRequest(agent, request);
  }

  const runtime = await createCopilotSdkRuntime(agent);
  try {
    return await executeCopilotSdkSessionRequest(runtime, request);
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  } finally {
    await destroyCopilotSdkRuntime(runtime);
  }
}

export async function executeCopilotSdkSessionRequest(runtime, request) {
  switch (request.type) {
    case "prompt":
      return executePromptRequest(runtime, request);
    case "build":
      return executeSessionBuildRequest(runtime, request);
    default:
      throw new Error(`Unsupported Copilot SDK request type: ${request.type}`);
  }
}

async function executePromptRequest(runtime, request) {
  try {
    const response = await runtime.session.sendAndWait({ prompt: request.prompt }, request.timeoutMs);
    return { ok: true, content: response?.data?.content || "" };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

async function executeFreshBuildRequest(agent, request) {
  const initialVisioPids = getVisioProcessIds();
  let runtime = null;
  let result = null;

  try {
    runtime = await createCopilotSdkRuntime(agent);
    result = await executeBuildLoop(runtime, request, {
      beforeFallback: async () => {
        await destroyCopilotSdkRuntime(runtime);
        runtime = null;
      },
      afterSuccess: async () => {
        await destroyCopilotSdkRuntime(runtime);
        runtime = null;
      },
      onError: async () => {
        await destroyCopilotSdkRuntime(runtime, { force: true });
        runtime = null;
      },
    });
    return result;
  } finally {
    if (agent.transport === "mcp") {
      cleanupExtraVisioProcesses(initialVisioPids);
    }
    if (runtime?.client || runtime?.session) {
      await destroyCopilotSdkRuntime(runtime, { force: true });
    }
  }
}

async function executeSessionBuildRequest(runtime, request) {
  const initialVisioPids = getVisioProcessIds();

  try {
    return await executeBuildLoop(runtime, request);
  } finally {
    // When reusing session context, the MCP server manages its own Visio
    // lifecycle — don't kill its process between loops.
    if (runtime.agent.transport === "mcp" && !runtime.agent.reuseSessionContext) {
      cleanupExtraVisioProcesses(initialVisioPids);
    }
  }
}

async function executeBuildLoop(runtime, request, hooks = {}) {
  const session = runtime.session;

  try {
    session.send({ prompt: request.prompt });
    const startedAt = Date.now();

    while ((Date.now() - startedAt) < request.timeoutMs) {
      const artifactStatus = await waitForArtifactFile(request.pngPath, {
        kind: "png",
        timeoutMs: 1500,
      });

      if (artifactStatus.ok) {
        const summaryContent = await requestBuildSummary(session, request);
        if (hooks.afterSuccess) {
          await hooks.afterSuccess();
        }
        return buildSuccess("png-detected", summaryContent, request);
      }

      await sleep(BUILD_POLL_INTERVAL_MS);
    }

    if (hooks.beforeFallback) {
      await hooks.beforeFallback();
    }

    const delayedArtifact = await verifyBuildArtifacts({
      pngPath: request.pngPath,
      drawingPath: request.drawingPath,
      requireDrawing: existsSync(request.drawingPath),
      timeoutMs: 2500,
    });

    if (delayedArtifact.ok) {
      return buildSuccess("png-detected-after-destroy", "", request);
    }

    if (existsSync(request.drawingPath) && tryExportFirstPage(request.drawingPath, request.pngPath)) {
      return buildSuccess("manual-export-after-timeout", "", request);
    }

    return { ok: false, error: `Timeout after ${request.timeoutMs}ms waiting for build artifact` };
  } catch (error) {
    if (hooks.onError) {
      await hooks.onError(error);
    }

    const recoveredArtifact = await verifyBuildArtifacts({
      pngPath: request.pngPath,
      drawingPath: request.drawingPath,
      requireDrawing: existsSync(request.drawingPath),
      timeoutMs: 2500,
    });

    if (recoveredArtifact.ok) {
      return buildSuccess("png-detected-after-error", "", request);
    }

    if (existsSync(request.drawingPath) && tryExportFirstPage(request.drawingPath, request.pngPath)) {
      return buildSuccess("manual-export-after-error", "", request);
    }

    return { ok: false, error: getErrorMessage(error) };
  }
}

/**
 * Every success path returns through here so the structural read is never skipped, and never runs
 * before the runtime has been torn down — the agent's own Visio session holds the drawing open
 * until then, and a second session on a locked file returns nothing useful.
 */
function buildSuccess(completion, summaryContent, request) {
  return {
    ok: true,
    completion,
    summaryContent,
    structure: readDrawingStructure(request.drawingPath),
  };
}

async function requestBuildSummary(session, request) {
  if (!request.summaryPrompt) return "";

  const response = await session.sendAndWait(
    { prompt: request.summaryPrompt },
    request.summaryTimeoutMs || 30000
  );

  return response?.data?.content || "";
}

function tryExportFirstPage(drawingPath, pngPath) {
  try {
    const openOut = execSync(`"${CLI_PATH}" session open "${drawingPath}"`, { encoding: "utf-8", timeout: 15000 });
    const match = openOut.match(/\{[\s\S]*\}/);
    if (!match) return false;
    const data = JSON.parse(match[0]);
    const sessionId = data.sessionId;
    if (!sessionId) return false;

    try {
      execSync(
        `"${CLI_PATH}" export page-export -s ${sessionId} --page-index 1 --destination-path "${pngPath}"`,
        { encoding: "utf-8", timeout: 30000 }
      );
    } finally {
      try {
        // --save false, not --no-save: the flag takes an explicit value. This is a
        // recovery path for a build that already failed, so nothing here should
        // write to the drawing under evaluation.
        execSync(`"${CLI_PATH}" session close -s ${sessionId} --save false`, { encoding: "utf-8", timeout: 15000 });
      } catch {}
    }

    return existsSync(pngPath);
  } catch {
    return false;
  }
}

/**
 * Reads the drawing's structure through the CLI.
 *
 * The judge cannot score a diagram from a picture. Connectivity and completeness — whether the
 * shapes are actually joined, whether a path terminates — are the two things that most often go
 * wrong, and a drawing whose boxes are placed but unconnected renders as a perfectly plausible
 * PNG. Scoring the image alone reliably praises the wrong output.
 *
 * Returns null rather than throwing: this augments the artifact, and a build that produced a
 * valid PNG should not be failed because the structural read did not come back.
 */
export function readDrawingStructure(drawingPath) {
  if (!existsSync(drawingPath)) return null;

  let sessionId = null;

  try {
    const openOut = execSync(`"${CLI_PATH}" session open "${drawingPath}"`, { encoding: "utf-8", timeout: 15000 });
    const openMatch = openOut.match(/\{[\s\S]*\}/);
    if (!openMatch) return null;
    sessionId = JSON.parse(openMatch[0]).sessionId;
    if (!sessionId) return null;

    const pages = runCliJson(`page list -s ${sessionId}`);
    if (!pages) return null;

    const structure = { pages: [] };

    for (const page of pages.pages || []) {
      const pageIndex = page.pageIndex;
      if (!Number.isFinite(pageIndex)) continue;

      const shapes = runCliJson(`shape list -s ${sessionId} --page-index ${pageIndex}`);
      const connectors = runCliJson(`shape list-connectors -s ${sessionId} --page-index ${pageIndex}`);

      // shape list returns connectors alongside nodes, distinguished by shapeType. Keeping the
      // discriminator means the judge can count nodes without counting the lines between them.
      structure.pages.push({
        pageIndex,
        name: page.name ?? null,
        isBackground: page.isBackground ?? null,
        shapes: (shapes?.shapes || []).map((shape) => ({
          shapeId: shape.shapeId ?? null,
          name: shape.name ?? null,
          shapeType: shape.shapeType ?? null,
          text: shape.text ?? "",
        })),
        connectors: (connectors?.connectors || []).map((connector) => ({
          shapeId: connector.shapeId ?? null,
          name: connector.name ?? null,
          startShapeName: connector.startShapeName ?? null,
          endShapeName: connector.endShapeName ?? null,
        })),
      });
    }

    return structure;
  } catch {
    return null;
  } finally {
    if (sessionId) {
      try {
        execSync(`"${CLI_PATH}" session close -s ${sessionId} --save false`, { encoding: "utf-8", timeout: 15000 });
      } catch {}
    }
  }
}

function runCliJson(argumentText) {
  try {
    const out = execSync(`"${CLI_PATH}" ${argumentText}`, { encoding: "utf-8", timeout: 20000 });
    const match = out.match(/\{[\s\S]*\}/);
    return match ? JSON.parse(match[0]) : null;
  } catch {
    return null;
  }
}

function getVisioProcessIds() {
  try {
    const out = execSync(
      "powershell -NoProfile -Command \"Get-Process -Name VISIO -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id\"",
      { encoding: "utf-8", timeout: 10000 }
    );

    return new Set(
      out
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter(Boolean)
        .map((id) => Number.parseInt(id, 10))
        .filter(Number.isFinite)
    );
  } catch {
    return new Set();
  }
}

function cleanupExtraVisioProcesses(initialPids) {
  const currentPids = getVisioProcessIds();
  for (const pid of currentPids) {
    if (!initialPids.has(pid)) {
      try {
        execSync(`powershell -NoProfile -Command "Stop-Process -Id ${pid}"`, { encoding: "utf-8", timeout: 10000 });
      } catch {}
    }
  }
}

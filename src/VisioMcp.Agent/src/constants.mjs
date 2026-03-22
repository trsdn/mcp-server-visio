import { dirname, join, resolve } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));

export const PACKAGE_ROOT = resolve(__dirname, "..");
export const REPO_ROOT = resolve(PACKAGE_ROOT, "..", "..");
export const SKILLS_ROOT = join(REPO_ROOT, "skills");

export const DEFAULT_MCP_SERVER_PATH = join(
  REPO_ROOT,
  "src",
  "VisioMcp.McpServer",
  "bin",
  "Release",
  "net9.0-windows",
  "VisioMcp.McpServer.exe"
);

export const MCP_SERVER_ENV_KEYS = Object.freeze([
  "VISIO_MCP_AGENT_MCP_SERVER",
  "VISIO_MCP_SERVER_COMMAND",
  "visio_mcp_SERVER_COMMAND",
]);

export const DEFAULT_MODEL = process.env.VISIO_MCP_AGENT_MODEL || "gpt-5.4";

export const DEFAULT_PLAN_TIMEOUT_MS = 120000;
export const DEFAULT_EXECUTE_TIMEOUT_MS = 900000;
export const DEFAULT_VERIFY_TIMEOUT_MS = 300000;

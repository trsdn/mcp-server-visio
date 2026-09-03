import { execFileSync } from "child_process";
import { join } from "path";
import { tmpdir } from "os";
import { rmSync } from "fs";

import { readDrawing, resolveCliPath } from "../src/visioCli.mjs";

const cli = resolveCliPath();
const file = join(tmpdir(), `agent-readdrawing-${Date.now()}.vsdx`);

function run(args) {
  return JSON.parse(execFileSync(cli, args, { encoding: "utf-8", windowsHide: true }).trim());
}

try {
  const sid = run(["session", "create", file]).sessionId;
  run(["shape", "add-shape", "-s", sid, "--page-index", "1", "--auto-shape-type", "1",
       "--left", "1", "--top", "1", "--width", "2", "--height", "1"]);
  run(["shape", "add-shape", "-s", sid, "--page-index", "1", "--auto-shape-type", "1",
       "--left", "5", "--top", "1", "--width", "2", "--height", "1"]);
  run(["text", "set", "-s", sid, "--page-index", "1", "--shape-name", "Sheet.1", "--text", "Start"]);
  run(["text", "set", "-s", sid, "--page-index", "1", "--shape-name", "Sheet.2", "--text", "End"]);
  run(["shape", "connect-shapes", "-s", sid, "--page-index", "1", "--shape-names", "Sheet.1,Sheet.2"]);
  run(["cell", "set-formula", "-s", sid, "--page-index", "1", "--shape-name", "Sheet.1",
       "--cell-name", "FillForegnd", "--formula", "RGB(68,114,196)"]);
  run(["session", "close", "-s", sid, "--save"]);

  const drawing = readDrawing(file);
  console.log(JSON.stringify(drawing, null, 2));

  const page = drawing[0];
  const ok =
    drawing.length === 1 &&
    page.shapes.length === 2 &&
    page.connectorCount === 1 &&
    page.shapes.map((s) => s.text).sort().join(",") === "End,Start" &&
    page.shapes.some((s) => s.fillForeground === "RGB(68,114,196)");

  console.log(ok ? "SMOKE OK" : "SMOKE FAILED");
  process.exitCode = ok ? 0 : 1;
} catch (error) {
  console.error("SMOKE ERROR:", error?.message || error);
  process.exitCode = 1;
} finally {
  try {
    rmSync(file, { force: true });
  } catch {
    console.error(`(could not delete ${file} — still locked)`);
  }
}

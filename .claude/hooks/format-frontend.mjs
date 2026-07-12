// PostToolUse (Write|Edit) hook: format edited frontend files with Prettier.
// Reads the hook payload on stdin, runs `pnpm --dir frontend exec prettier --write`
// on the edited file when it's a formattable file under frontend/. No-op otherwise.
import { spawnSync } from "node:child_process";
import path from "node:path";

const FORMATTABLE = new Set([
  ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs", ".json", ".css", ".md", ".html",
]);

let raw = "";
process.stdin.on("data", (c) => (raw += c));
process.stdin.on("end", () => {
  let file = "";
  try {
    const j = JSON.parse(raw || "{}");
    file = j.tool_input?.file_path || j.tool_response?.filePath || "";
  } catch {
    return;
  }
  if (!file) return;

  const norm = file.replace(/\\/g, "/");
  const inFrontend = /(^|\/)frontend\//.test(norm);
  if (!inFrontend || !FORMATTABLE.has(path.extname(norm).toLowerCase())) return;

  const frontendDir = path.join(process.cwd(), "frontend");
  spawnSync(
    "pnpm",
    ["--dir", frontendDir, "exec", "prettier", "--write", "--ignore-unknown", file],
    { stdio: "ignore", shell: process.platform === "win32" }
  );
});

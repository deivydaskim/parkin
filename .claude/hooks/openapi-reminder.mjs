// PostToolUse (Write|Edit) hook: when a backend Parkin.Api endpoint file changes,
// remind (user + model) to regenerate the frontend OpenAPI TypeScript client so the
// generated types don't silently drift from the API contract. No-op for other files.
let raw = "";
process.stdin.on("data", (c) => (raw += c));
process.stdin.on("end", () => {
  let file = "";
  try {
    file = JSON.parse(raw || "{}").tool_input?.file_path || "";
  } catch {
    return;
  }
  const norm = file.replace(/\\/g, "/");
  const isEndpoint =
    /\/Parkin\.Api\/.*Features\//i.test(norm) || /Endpoint\.cs$/i.test(norm);
  if (!isEndpoint) return;

  process.stdout.write(
    JSON.stringify({
      systemMessage:
        "⚠️  Backend endpoint changed — regenerate the frontend OpenAPI types (openapi-typescript) so the client stays in sync.",
      hookSpecificOutput: {
        hookEventName: "PostToolUse",
        additionalContext:
          "A backend Parkin.Api endpoint file was just edited. If the request/response contract changed, regenerate the frontend OpenAPI TypeScript types before wiring UI against them.",
      },
    })
  );
});

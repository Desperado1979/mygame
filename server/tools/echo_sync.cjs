/**
 * Minimal local echo for D5 rehearsal: POST JSON to /sync, respond with { ok, ... } (no disk write).
 * For saving payloads to disk use persist_sync.cjs instead.
 * Run: node EpochOfDawn/server/tools/echo_sync.cjs
 */
const http = require("http");

const port = 8787;
const path = "/sync";

http
  .createServer((req, res) => {
    if (req.method !== "POST" || req.url !== path) {
      res.writeHead(404, { "Content-Type": "application/json; charset=utf-8" });
      res.end(JSON.stringify({ ok: false, error: "use POST " + path }));
      return;
    }
    const chunks = [];
    req.on("data", (c) => chunks.push(c));
    req.on("end", () => {
      const text = Buffer.concat(chunks).toString("utf8");
      let body;
      try {
        body = JSON.parse(text);
      } catch (e) {
        res.writeHead(400, { "Content-Type": "application/json; charset=utf-8" });
        res.end(JSON.stringify({ ok: false, error: "invalid_json" }));
        return;
      }
      const auditLen = Array.isArray(body.audit) ? body.audit.length : 0;
      res.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
      res.end(
        JSON.stringify({
          ok: true,
          schemaVersion: body.schemaVersion,
          playerId: body.playerId,
          auditCount: auditLen,
          bytes: text.length,
        })
      );
      console.log(new Date().toISOString(), "sync", body.playerId, "audit", auditLen);
    });
  })
  .listen(port, "127.0.0.1", () => {
    console.log("Echo sync listening http://127.0.0.1:" + port + path);
  });

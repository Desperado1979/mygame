/**
 * Replay gold + potion tails from a client_sync_request-shaped JSON (audit[] + state).
 * Usage: node tools/audit_replay_cli.cjs path/to.json
 */
const fs = require("fs");
const path = require("path");
const { replayGoldTailForCli, replayPotionInventoryTail } = require("./audit_validate.cjs");

const fp = process.argv[2];
if (!fp || fp.startsWith("--")) {
  console.error("Usage: node audit_replay_cli.cjs <client_sync_request.json>");
  process.exit(2);
}
const text = fs.readFileSync(path.resolve(fp), "utf8");
let body;
try {
  body = JSON.parse(text);
} catch (e) {
  console.error("invalid_json", e.message);
  process.exit(1);
}
if (!Array.isArray(body.audit)) {
  console.error("missing audit[]");
  process.exit(1);
}
const audits = [...body.audit].sort((a, b) => (a.seq || 0) - (b.seq || 0));
const g = replayGoldTailForCli(audits);
const p = replayPotionInventoryTail(audits);
const st = body.state && body.state.inventory ? body.state.inventory : {};
const out = {
  goldTailFromReplay: g.goldTail,
  goldChainErrors: g.chainErrors,
  potionTailFromReplay: p,
  stateGold: body.state && typeof body.state.gold === "number" ? body.state.gold : null,
  stateHpPotion: typeof st.hpPotion === "number" ? st.hpPotion : null,
  stateMpPotion: typeof st.mpPotion === "number" ? st.mpPotion : null,
};
console.log(JSON.stringify(out, null, 2));
process.exit(g.chainErrors.length ? 1 : 0);

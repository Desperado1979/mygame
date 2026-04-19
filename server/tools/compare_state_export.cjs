/**
 * Offline "read vs export" rehearsal: compare numeric state between
 * - server saved sync (full POST body or { state: {...} })
 * - F12 player_state_export.json (flat state object)
 *
 * Usage (from EpochOfDawn/server):
 *   node tools/compare_state_export.cjs data/local_player_001.json C:/Users/.../player_state_export.json
 *
 * Exit 0 = no differences on checked fields; 1 = mismatch or read error.
 */
const fs = require("fs");
const path = require("path");

const EPS = 0.02;

function die(msg, code = 1) {
  console.error(msg);
  process.exit(code);
}

function readJson(fp) {
  const abs = path.resolve(fp);
  return JSON.parse(fs.readFileSync(abs, "utf8"));
}

function extractState(obj) {
  if (!obj || typeof obj !== "object") die("not an object");
  if (obj.state && typeof obj.state === "object") return obj.state;
  if (obj.gold !== undefined && obj.inventory && obj.skills) return obj;
  die("could not find state (need full sync with .state or flat player_state_export)");
}

function numEq(a, b, eps = 0) {
  if (typeof a !== "number" || typeof b !== "number") return a === b;
  return Math.abs(a - b) <= eps;
}

function compareStates(a, b, labelA, labelB) {
  const diffs = [];

  const pairs = [
    ["level", (x) => x.level],
    ["gold", (x) => x.gold],
    ["xp.xpIntoLevel", (x) => x.xp && x.xp.xpIntoLevel],
    ["xp.xpBank", (x) => x.xp && x.xp.xpBank],
    ["xp.skillPoints", (x) => x.xp && x.xp.skillPoints],
    ["inventory.hpPotion", (x) => x.inventory && x.inventory.hpPotion],
    ["inventory.mpPotion", (x) => x.inventory && x.inventory.mpPotion],
    ["inventory.weight", (x) => x.inventory && x.inventory.weight],
    ["skills.qTier", (x) => x.skills && x.skills.qTier],
    ["skills.rTier", (x) => x.skills && x.skills.rTier],
    ["skills.qLevel", (x) => x.skills && x.skills.qLevel],
    ["skills.rLevel", (x) => x.skills && x.skills.rLevel],
    ["bank.gold", (x) => x.bank && x.bank.gold],
    ["bank.hpPotion", (x) => x.bank && x.bank.hpPotion],
    ["bank.mpPotion", (x) => x.bank && x.bank.mpPotion],
    ["hpNow", (x) => x.hpNow],
    ["hpMax", (x) => x.hpMax],
  ];

  for (const [name, get] of pairs) {
    const va = get(a);
    const vb = get(b);
    if (va === undefined && vb === undefined) continue;
    if (va === undefined || vb === undefined) {
      diffs.push(`${name}: missing in one side (${va}) vs (${vb})`);
      continue;
    }
    const w = name === "inventory.weight";
    if (!numEq(va, vb, w ? EPS : 0))
      diffs.push(`${name}: ${labelA}=${va} ${labelB}=${vb}`);
  }

  return diffs;
}

const leftPath = process.argv[2];
const rightPath = process.argv[3];
if (!leftPath || !rightPath) {
  die(
    "Usage: node tools/compare_state_export.cjs <server_or_sync.json> <player_state_export.json>\n" +
      "Example: node tools/compare_state_export.cjs data/local_player_001.json \"%USERPROFILE%/AppData/LocalLow/DefaultCompany/EpochOfDawn/player_state_export.json\""
  );
}

let leftRaw;
let rightRaw;
try {
  leftRaw = readJson(leftPath);
  rightRaw = readJson(rightPath);
} catch (e) {
  die(String(e.message || e));
}

const leftState = extractState(leftRaw);
const rightState = extractState(rightRaw);

const diffs = compareStates(leftState, rightState, "server", "export");
if (diffs.length === 0) {
  console.log("OK: no differences on compared fields.");
  console.log("  left:", path.resolve(leftPath));
  console.log("  right:", path.resolve(rightPath));
  process.exit(0);
}

console.log("MISMATCH:");
for (const d of diffs) console.log(" -", d);
process.exit(1);

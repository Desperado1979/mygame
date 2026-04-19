/**
 * D10: Placeholder for future Localization/*.json placeholder validation. Exits 0 when no locale dir.
 */
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..", "..", "Assets", "Localization");
if (!fs.existsSync(root)) {
  console.log("check_locale_placeholder: skip (no Assets/Localization)");
  process.exit(0);
}

console.log("check_locale_placeholder: Localization present — extend this script when strings ship.");
process.exit(0);

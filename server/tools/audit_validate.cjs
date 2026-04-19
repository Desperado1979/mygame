/**
 * Minimal D5 validation for POST /sync bodies (no external deps).
 * - Structural checks on state + audit entries
 * - Gold audit payload shape (gold_add / gold_spend / gold_death_loss / gold_set)
 * - Inventory audit payload shape (inv_add / inv_use_* / inv_sell_* / inv_remove / inv_set)
 * - Sequential gold replay; internal break => error; tail vs state.gold mismatch => warning (ring buffer)
 * - Inventory replay (hp/mp potions) vs state.inventory => warning (ring buffer/truncation tolerant)
 *
 * D14: SYNC_AUDIT_STATE_STRICT=1 promotes selected cross-checks from warnings to hard errors (rehearsal gate).
 */

function parseBoolEnv(raw, fallback = false) {
  if (raw == null) return fallback;
  const s = String(raw).trim().toLowerCase();
  if (["1", "true", "yes", "y", "on"].includes(s)) return true;
  if (["0", "false", "no", "n", "off"].includes(s)) return false;
  return fallback;
}

const syncAuditStateStrict = parseBoolEnv(process.env.SYNC_AUDIT_STATE_STRICT, false);

/**
 * @param {any} body
 * @returns {{ ok: boolean, errors: string[], warnings: WarningItem[] }}
 */
function validateClientSyncPayload(body) {
  const errors = [];
  const warnings = [];

  if (!body || typeof body !== "object") {
    errors.push("body_not_object");
    return { ok: false, errors, warnings };
  }
  if (body.schemaVersion !== 1) {
    if (syncAuditStateStrict) {
      errors.push(`unexpected_schema_version:${body.schemaVersion}`);
    } else {
      warnings.push(
        makeWarning(
          "high",
          "unexpected_schema_version",
          `schemaVersion=${body.schemaVersion}, expected=1`
        )
      );
    }
  }
  if (!body.playerId || typeof body.playerId !== "string")
    errors.push("missing_or_bad_playerId");
  if (!body.state || typeof body.state !== "object")
    errors.push("missing_state");
  if (!Array.isArray(body.audit))
    errors.push("audit_not_array");

  if (errors.length > 0)
    return { ok: false, errors, warnings };

  const st = body.state;
  if (typeof st.playerId === "string" && st.playerId.trim() && st.playerId !== body.playerId) {
    if (syncAuditStateStrict) {
      errors.push("player_id_mismatch_body_vs_state");
    } else {
      warnings.push(
        makeWarning(
          "high",
          "player_id_mismatch",
          `body.playerId=${body.playerId}, state.playerId=${st.playerId}`
        )
      );
    }
  }
  if (typeof st.gold !== "number" || !Number.isFinite(st.gold) || st.gold < 0)
    errors.push("state_gold_invalid");
  if (typeof st.level !== "number" || st.level < 1)
    errors.push("state_level_invalid");
  if (!st.inventory || typeof st.inventory !== "object")
    errors.push("state_inventory_missing");
  else {
    const inv = st.inventory;
    for (const k of ["hpPotion", "mpPotion"]) {
      if (typeof inv[k] !== "number" || inv[k] < 0 || !Number.isFinite(inv[k]))
        errors.push(`state_inventory_${k}_invalid`);
    }
  }

  if (errors.length > 0)
    return { ok: false, errors, warnings };

  if (
    typeof st.version === "number" &&
    Number.isFinite(st.version) &&
    typeof body.schemaVersion === "number" &&
    Number.isFinite(body.schemaVersion) &&
    st.version !== body.schemaVersion
  ) {
    if (syncAuditStateStrict) {
      errors.push("state_version_vs_schema_mismatch");
    } else {
      warnings.push(
        makeWarning(
          "low",
          "state_version_vs_schema_mismatch",
          `state.version=${st.version}, schemaVersion=${body.schemaVersion}`
        )
      );
    }
  }

  const audits = [...body.audit].sort((a, b) => (a.seq || 0) - (b.seq || 0));
  for (const e of audits) {
    if (e == null || typeof e !== "object") {
      errors.push("audit_entry_not_object");
      break;
    }
    if (typeof e.seq !== "number" || e.seq < 1 || !Number.isFinite(e.seq)) {
      errors.push(`audit_bad_seq:${JSON.stringify(e.seq)}`);
      break;
    }
    if (!e.category || typeof e.category !== "string") {
      errors.push(`audit_bad_category_at_seq:${e.seq}`);
      break;
    }
    if (typeof e.payload !== "string") {
      errors.push(`audit_bad_payload_at_seq:${e.seq}`);
      break;
    }
  }
  if (errors.length > 0)
    return { ok: false, errors, warnings };

  for (let i = 1; i < audits.length; i++) {
    if (audits[i].seq === audits[i - 1].seq) {
      errors.push(`audit_duplicate_seq:${audits[i].seq}`);
      break;
    }
  }
  if (errors.length > 0)
    return { ok: false, errors, warnings };

  validateGoldPayloadShapes(audits, errors);
  if (errors.length > 0)
    return { ok: false, errors, warnings };

  validateInventoryPayloadShapes(audits, errors);
  if (errors.length > 0)
    return { ok: false, errors, warnings };

  const hadGold = audits.some((e) =>
    ["gold_add", "gold_spend", "gold_death_loss", "gold_set"].includes(e.category)
  );
  if (hadGold) {
    const tail = replayGoldChain(audits, errors);
    if (errors.length > 0)
      return { ok: false, errors, warnings };
    if (tail != null && tail !== st.gold) {
      if (syncAuditStateStrict) {
        errors.push("gold_tail_mismatch_vs_state");
      } else {
        warnings.push(
          makeWarning(
            "low",
            "gold_tail_mismatch",
            `gold tail replay=${tail}, state.gold=${st.gold} (likely audit ring-buffer truncation or offline events)`
          )
        );
      }
    }
  }

  const invTail = replayPotionInventoryTail(audits);
  if (invTail.hp != null && invTail.hp !== st.inventory.hpPotion) {
    if (syncAuditStateStrict) {
      errors.push("inventory_hp_tail_mismatch_vs_state");
    } else {
      warnings.push(
        makeWarning(
          "low",
          "inventory_hp_tail_mismatch",
          `inventory hp tail replay=${invTail.hp}, state.inventory.hpPotion=${st.inventory.hpPotion} (likely audit ring-buffer truncation or offline events)`
        )
      );
    }
  }
  if (invTail.mp != null && invTail.mp !== st.inventory.mpPotion) {
    if (syncAuditStateStrict) {
      errors.push("inventory_mp_tail_mismatch_vs_state");
    } else {
      warnings.push(
        makeWarning(
          "low",
          "inventory_mp_tail_mismatch",
          `inventory mp tail replay=${invTail.mp}, state.inventory.mpPotion=${st.inventory.mpPotion} (likely audit ring-buffer truncation or offline events)`
        )
      );
    }
  }

  const srvValN = audits.filter(
    (e) => e && typeof e.category === "string" && e.category.startsWith("SrvVal_")
  ).length;
  if (srvValN > 0) {
    warnings.push(
      makeWarning("low", "audit_contains_srvval", `count=${srvValN} (client-reported rejects / rehearsal)`)
    );
  }

  if (errors.length > 0) return { ok: false, errors, warnings };
  return { ok: true, errors, warnings };
}

/** Matches PlayerInventorySimple / PlayerWalletSimple audit strings. */
function validateInventoryPayloadShapes(audits, errors) {
  for (const e of audits) {
    const c = e.category;
    const p = e.payload;
    if (c === "inv_add") {
      if (!/^[^,]+,count=\d+,w=-?\d+(\.\d+)?$/.test(p))
        errors.push(`malformed_inv_add_seq_${e.seq}`);
    } else if (c === "inv_use_hp" || c === "inv_use_mp") {
      if (!/^count=1,remain=-?\d+$/.test(p))
        errors.push(`malformed_${c}_seq_${e.seq}`);
    } else if (c === "inv_sell_hp" || c === "inv_sell_mp") {
      if (p !== "count=1")
        errors.push(`malformed_${c}_seq_${e.seq}`);
    } else if (c === "inv_remove") {
      if (!/^[^,]+,count=\d+$/.test(p))
        errors.push(`malformed_inv_remove_seq_${e.seq}`);
    } else if (c === "inv_set") {
      if (!/^[^,]+,-?\d+$/.test(p))
        errors.push(`malformed_inv_set_seq_${e.seq}`);
    }
  }
}

function validateGoldPayloadShapes(audits, errors) {
  for (const e of audits) {
    const c = e.category;
    const p = e.payload;
    if (c === "gold_add") {
      if (!/^\+\d+,now=\d+$/.test(p))
        errors.push(`malformed_gold_add_seq_${e.seq}`);
    } else if (c === "gold_spend") {
      if (!/^-\d+,now=\d+$/.test(p))
        errors.push(`malformed_gold_spend_seq_${e.seq}`);
    } else if (c === "gold_death_loss") {
      if (!/^-\d+,now=\d+,percent=/.test(p))
        errors.push(`malformed_gold_death_loss_seq_${e.seq}`);
    } else if (c === "gold_set") {
      if (!/^\d+->\d+$/.test(p))
        errors.push(`malformed_gold_set_seq_${e.seq}`);
    }
  }
}

/**
 * @returns {number|null} gold after last gold-related audit, or null if no gold audits
 */
function replayGoldChain(audits, errors) {
  let running = null;

  for (const e of audits) {
    const c = e.category;
    const p = e.payload;
    if (c === "gold_add") {
      const m = p.match(/^\+(\d+),now=(\d+)$/);
      if (!m) continue;
      const amt = +m[1];
      const now = +m[2];
      if (running === null) running = now - amt;
      if (running + amt !== now) {
        errors.push(`gold_add_chain_seq_${e.seq}_${running}+${amt}!=${now}`);
        return null;
      }
      running = now;
    } else if (c === "gold_spend" || c === "gold_death_loss") {
      const m = p.match(/^-(\d+),now=(\d+)/);
      if (!m) continue;
      const amt = +m[1];
      const now = +m[2];
      if (running === null) {
        errors.push(`gold_subtract_before_basis_seq_${e.seq}`);
        return null;
      }
      if (running - amt !== now) {
        errors.push(`gold_subtract_chain_seq_${e.seq}_${running}-${amt}!=${now}`);
        return null;
      }
      running = now;
    } else if (c === "gold_set") {
      const m = p.match(/^(\d+)->(\d+)$/);
      if (!m) continue;
      running = +m[2];
    }
  }
  return running;
}

/**
 * Replay only potion counts from inventory-related audit tail.
 * Returns null for a channel when there is no reliable basis in the available tail.
 */
function replayPotionInventoryTail(audits) {
  let hp = null;
  let mp = null;

  for (const e of audits) {
    const c = e.category;
    const p = e.payload;

    if (c === "inv_set") {
      const m = p.match(/^([^,]+),(-?\d+)$/);
      if (!m) continue;
      const item = m[1];
      const val = +m[2];
      if (isHpPotionId(item)) hp = val;
      if (isMpPotionId(item)) mp = val;
    } else if (c === "inv_add") {
      const m = p.match(/^([^,]+),count=(\d+),w=-?\d+(?:\.\d+)?$/);
      if (!m) continue;
      const item = m[1];
      const cnt = +m[2];
      if (isHpPotionId(item)) hp = (hp == null ? 0 : hp) + cnt;
      if (isMpPotionId(item)) mp = (mp == null ? 0 : mp) + cnt;
    } else if (c === "inv_use_hp" || c === "inv_sell_hp") {
      if (c === "inv_use_hp") {
        const m = p.match(/^count=1,remain=(-?\d+)$/);
        if (m) hp = +m[1];
      } else if (hp != null) {
        hp -= 1;
      }
    } else if (c === "inv_use_mp" || c === "inv_sell_mp") {
      if (c === "inv_use_mp") {
        const m = p.match(/^count=1,remain=(-?\d+)$/);
        if (m) mp = +m[1];
      } else if (mp != null) {
        mp -= 1;
      }
    } else if (c === "inv_remove") {
      const m = p.match(/^([^,]+),count=(\d+)$/);
      if (!m) continue;
      const item = m[1];
      const cnt = +m[2];
      if (isHpPotionId(item) && hp != null) hp -= cnt;
      if (isMpPotionId(item) && mp != null) mp -= cnt;
    }
  }

  return { hp, mp };
}

function isHpPotionId(itemId) {
  return itemId === "potion" || itemId === "hppotion" || itemId === "hp_potion";
}

function isMpPotionId(itemId) {
  return itemId === "mana" || itemId === "mppotion" || itemId === "mp_potion";
}

/**
 * @typedef {"low"|"high"} WarningSeverity
 * @typedef {{ severity: WarningSeverity, code: string, message: string }} WarningItem
 */
function makeWarning(severity, code, message) {
  return { severity, code, message };
}

/** CLI / 排练：重放金币链（不写 validate 全量）。 */
function replayGoldTailForCli(audits) {
  const errors = [];
  const tail = replayGoldChain(audits, errors);
  return { goldTail: tail, chainErrors: errors };
}

module.exports = {
  validateClientSyncPayload,
  replayGoldTailForCli,
  replayPotionInventoryTail,
  syncAuditStateStrict,
};

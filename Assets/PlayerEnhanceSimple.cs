using UnityEngine;

/// <summary>D3: 强化占位 — 全部位共用一条费用曲线（README §3.5）；按 T 尝试 +1，现已接入受伤减免。</summary>
public class PlayerEnhanceSimple : MonoBehaviour
{
    public PlayerWalletSimple wallet;
    PlayerHotkeysSimple hotkeys;

    [Tooltip("已成功强化的次数（占位：七部位共曲线时可复用此计数）")]
    public int enhanceStep;

    public int goldBase = 10;
    public int goldPerStep = 12;

    [Tooltip("每级强化提供的受伤减免比例")]
    [Range(0f, 0.2f)] public float damageReductionPerStep = 0.02f;
    [Tooltip("减伤上限，避免无敌")]
    [Range(0f, 0.9f)] public float maxDamageReduction = 0.6f;

    void Reset()
    {
        wallet = GetComponent<PlayerWalletSimple>();
    }

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        KeyCode enhanceKey = hotkeys != null ? hotkeys.enhance : KeyCode.T;
        if (Input.GetKeyDown(enhanceKey))
            TryEnhance();
    }

    public int GoldCostNext()
    {
        return Mathf.Max(1, goldBase + enhanceStep * goldPerStep);
    }

    public bool TryEnhance()
    {
        if (wallet == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValWalletReject,
                "op=enhance&reason=no_wallet");
            Debug.LogWarning("PlayerEnhanceSimple: assign PlayerWalletSimple.");
            return false;
        }

        int cost = GoldCostNext();
        if (!wallet.TrySpend(cost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValWalletReject,
                $"op=enhance&needGold={cost}&haveGold={wallet.Gold}&step={enhanceStep}");
            Debug.Log($"强化需要金币 {cost}，当前 {wallet.Gold}");
            return false;
        }

        enhanceStep++;
        Debug.Log($"强化成功 → 累计 {enhanceStep} 次，本次花费 {cost} 金，减伤 {DamageReductionFraction * 100f:F0}%");
        return true;
    }

    public float DamageReductionFraction => Mathf.Min(maxDamageReduction, enhanceStep * damageReductionPerStep);
    public float DamageTakenMultiplier => 1f - DamageReductionFraction;
}

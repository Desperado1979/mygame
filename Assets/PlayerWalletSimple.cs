using UnityEngine;

/// <summary>D3: 金币占位 — 击杀等入账，强化/商店后续接。</summary>
public class PlayerWalletSimple : MonoBehaviour
{
    public static PlayerWalletSimple Instance { get; private set; }

    int gold;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerWalletSimple — keeping first.");
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int Gold => gold;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        ServerAuditLogSimple.Push("gold_add", $"+{amount},now={gold}");
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;
        gold -= amount;
        ServerAuditLogSimple.Push("gold_spend", $"-{amount},now={gold}");
        return true;
    }

    public int LosePercent(int percent)
    {
        if (percent <= 0 || gold <= 0)
            return 0;

        int lose = Mathf.Clamp(Mathf.FloorToInt(gold * (percent / 100f)), 1, gold);
        gold -= lose;
        ServerAuditLogSimple.Push("gold_death_loss", $"-{lose},now={gold},percent={percent}");
        return lose;
    }

    public void SetGold(int value)
    {
        int old = gold;
        gold = Mathf.Max(0, value);
        ServerAuditLogSimple.Push("gold_set", $"{old}->{gold}");
    }
}

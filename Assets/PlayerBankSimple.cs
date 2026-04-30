using UnityEngine;

/// <summary>D3: 仓库占位（本地单人）— 存取金币与药品。</summary>
public class PlayerBankSimple : MonoBehaviour
{
    public PlayerWalletSimple wallet;
    public PlayerInventorySimple inventory;
    PlayerHotkeysSimple hotkeys;

    public int bankGold;
    public int bankHpPotion;
    public int bankMpPotion;

    [Header("快捷键每次存取数量（默认由 DefaultD3Growth 覆盖）")]
    public int goldStep = 50;
    public int potionStep = 1;

    void Awake()
    {
        ApplyD3BankStepsFromBalance();
    }

    void ApplyD3BankStepsFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        goldStep = Mathf.Max(1, d.bankGoldStep);
        potionStep = Mathf.Max(1, d.bankPotionStep);
    }

    void Reset()
    {
        wallet = GetComponent<PlayerWalletSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
    }

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        KeyCode dGold = hotkeys != null ? hotkeys.bankDepositGold : KeyCode.F5;
        KeyCode wGold = hotkeys != null ? hotkeys.bankWithdrawGold : KeyCode.F6;
        KeyCode dPot = hotkeys != null ? hotkeys.bankDepositPotion : KeyCode.F7;
        KeyCode wPot = hotkeys != null ? hotkeys.bankWithdrawPotion : KeyCode.F8;

        if (Input.GetKeyDown(dGold)) DepositGold();
        if (Input.GetKeyDown(wGold)) WithdrawGold();
        if (Input.GetKeyDown(dPot)) DepositPotion();
        if (Input.GetKeyDown(wPot)) WithdrawPotion();
    }

    public bool DepositGold()
    {
        if (wallet == null)
        {
            ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValBankReject, "op=deposit_gold&reason=no_wallet");
            return false;
        }
        int amount = Mathf.Max(1, goldStep);
        if (!wallet.TrySpend(amount))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValBankReject,
                $"op=deposit_gold&reason=wallet_spend_fail&amount={amount}&gold={wallet.Gold}");
            return false;
        }
        bankGold += amount;
        return true;
    }

    public bool WithdrawGold()
    {
        int amount = Mathf.Max(1, goldStep);
        if (bankGold < amount)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValBankReject,
                $"op=withdraw_gold&reason=bank_low&need={amount}&have={bankGold}");
            return false;
        }
        if (wallet == null)
        {
            ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValBankReject, "op=withdraw_gold&reason=no_wallet");
            return false;
        }
        bankGold -= amount;
        wallet.AddGold(amount);
        return true;
    }

    public bool DepositPotion()
    {
        if (inventory == null)
        {
            ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValBankReject, "op=deposit_potion&reason=no_inventory");
            return false;
        }
        int step = Mathf.Max(1, potionStep);

        if (inventory.HpPotionCount >= step && inventory.RemoveItemById(inventory.hpPotionId, step, inventory.buyPotionWeight))
        {
            bankHpPotion += step;
            return true;
        }

        if (inventory.MpPotionCount >= step && inventory.RemoveItemById(inventory.mpPotionId, step, inventory.buyPotionWeight))
        {
            bankMpPotion += step;
            return true;
        }

        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValBankReject,
            $"op=deposit_potion&reason=no_potions&step={step}&hp={inventory.HpPotionCount}&mp={inventory.MpPotionCount}");
        return false;
    }

    public bool WithdrawPotion()
    {
        if (inventory == null)
        {
            ServerAuditLogSimple.Push(ServerAuditLogSimple.CategorySrvValBankReject, "op=withdraw_potion&reason=no_inventory");
            return false;
        }
        int step = Mathf.Max(1, potionStep);

        if (bankHpPotion >= step)
        {
            if (!inventory.TryAddPickup(inventory.buyPotionWeight, inventory.hpPotionId, step))
            {
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValInventoryFull,
                    $"op=withdraw_hp_potion&itemId={inventory.hpPotionId}&step={step}");
                return false;
            }
            bankHpPotion -= step;
            return true;
        }

        if (bankMpPotion >= step)
        {
            if (!inventory.TryAddPickup(inventory.buyPotionWeight, inventory.mpPotionId, step))
            {
                ServerAuditLogSimple.Push(
                    ServerAuditLogSimple.CategorySrvValInventoryFull,
                    $"op=withdraw_mp_potion&itemId={inventory.mpPotionId}&step={step}");
                return false;
            }
            bankMpPotion -= step;
            return true;
        }

        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValBankReject,
            $"op=withdraw_potion&reason=bank_empty&step={step}&bh={bankHpPotion}&bm={bankMpPotion}");
        return false;
    }
}

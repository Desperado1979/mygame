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

    [Header("快捷键每次存取数量")]
    public int goldStep = 50;
    public int potionStep = 1;

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
        if (wallet == null) return false;
        int amount = Mathf.Max(1, goldStep);
        if (!wallet.TrySpend(amount)) return false;
        bankGold += amount;
        return true;
    }

    public bool WithdrawGold()
    {
        int amount = Mathf.Max(1, goldStep);
        if (bankGold < amount) return false;
        if (wallet == null) return false;
        bankGold -= amount;
        wallet.AddGold(amount);
        return true;
    }

    public bool DepositPotion()
    {
        if (inventory == null) return false;
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

        return false;
    }

    public bool WithdrawPotion()
    {
        if (inventory == null) return false;
        int step = Mathf.Max(1, potionStep);

        if (bankHpPotion >= step)
        {
            if (!inventory.TryAddPickup(inventory.buyPotionWeight, inventory.hpPotionId, step)) return false;
            bankHpPotion -= step;
            return true;
        }

        if (bankMpPotion >= step)
        {
            if (!inventory.TryAddPickup(inventory.buyPotionWeight, inventory.mpPotionId, step)) return false;
            bankMpPotion -= step;
            return true;
        }

        return false;
    }
}

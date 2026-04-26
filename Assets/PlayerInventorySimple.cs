using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>D3: 背包负重占位 — 拾取失败则不掉落物不销毁。</summary>
public class PlayerInventorySimple : MonoBehaviour
{
    public float maxCarryWeight = 45f;
    public string hpPotionId = GameItemIdsSimple.HpPotion;
    public int hpPotionHealAmount = 45;
    public string mpPotionId = GameItemIdsSimple.MpPotion;
    public int mpPotionRestoreAmount = 35;
    public int buyPotionGoldCost = 20;
    public int buyManaGoldCost = 24;
    public int buyPotionCount = 1;
    public float buyPotionWeight = 1f;
    public int sellPotionGold = 8;
    public int sellManaGold = 10;
    float currentWeight;
    int stackCount;
    PlayerHotkeysSimple hotkeys;
    readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();

    public float CurrentWeight => currentWeight;
    public int StackCount => stackCount;
    public int GetCount(string itemId) => itemCounts.TryGetValue(itemId, out int c) ? c : 0;
    public int HpPotionCount => GetCount(hpPotionId);
    public int MpPotionCount => GetCount(mpPotionId);

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();
        MultiplayerPlayerSimple mpNet = GetComponent<MultiplayerPlayerSimple>();

        KeyCode hpKey = hotkeys != null ? hotkeys.useHpPotion : KeyCode.Alpha1;
        KeyCode mpKey = hotkeys != null ? hotkeys.useMpPotion : KeyCode.Alpha2;
        KeyCode buyHpKey = hotkeys != null ? hotkeys.buyHpPotion : KeyCode.B;
        KeyCode buyMpKey = hotkeys != null ? hotkeys.buyMpPotion : KeyCode.N;
        KeyCode sellKey = hotkeys != null ? hotkeys.sellPotion : KeyCode.V;
        KeyCode discardKey = hotkeys != null ? hotkeys.discardJunk : KeyCode.C;

        if (mpNet != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            if (Input.GetKeyDown(hpKey))
                mpNet.RequestUsePotion_ServerRpc(true);
            if (Input.GetKeyDown(mpKey))
                mpNet.RequestUsePotion_ServerRpc(false);
            if (Input.GetKeyDown(buyHpKey))
                mpNet.RequestBuyPotion_ServerRpc(true);
            if (Input.GetKeyDown(buyMpKey))
                mpNet.RequestBuyPotion_ServerRpc(false);
            if (Input.GetKeyDown(sellKey))
                mpNet.RequestSellOnePotion_ServerRpc();
            if (Input.GetKeyDown(discardKey))
                mpNet.RequestDiscardJunk_ServerRpc();
            return;
        }

        if (Input.GetKeyDown(hpKey))
            TryUseHpPotion();
        if (Input.GetKeyDown(mpKey))
            TryUseMpPotion();
        if (Input.GetKeyDown(buyHpKey))
            TryBuyHpPotion();
        if (Input.GetKeyDown(buyMpKey))
            TryBuyMpPotion();
        if (Input.GetKeyDown(sellKey))
            TrySellOnePotion();
        if (Input.GetKeyDown(discardKey))
            TryDiscardOneJunk();
    }

    public bool TryAddPickup(float weight, string itemId = "loot", int count = 1)
    {
        if (count <= 0) count = 1;
        if (weight <= 0f)
            weight = 1f;

        float totalWeight = weight * count;
        if (currentWeight + totalWeight > maxCarryWeight + 0.0001f)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValInventoryFull,
                $"op=add&itemId={GameItemIdsSimple.Normalize(itemId)}&addW={totalWeight:F1}&curW={currentWeight:F1}&max={maxCarryWeight:F1}");
            return false;
        }

        currentWeight += totalWeight;
        stackCount += count;

        itemId = GameItemIdsSimple.Normalize(itemId);
        if (!itemCounts.ContainsKey(itemId))
            itemCounts[itemId] = 0;
        itemCounts[itemId] += count;

        Debug.Log($"Pickup +{count} {itemId} (W+{totalWeight:F1})  W:{currentWeight:F0}/{maxCarryWeight}");
        ServerAuditLogSimple.Push("inv_add", $"{itemId},count={count},w={totalWeight:F1}");
        return true;
    }

    public bool TryUseHpPotion()
    {
        int count = HpPotionCount;
        if (count <= 0)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_hp&reason=no_potion");
            return false;
        }

        PlayerHealthSimple hp = GetComponent<PlayerHealthSimple>();
        if (hp == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_hp&reason=no_player_health");
            return false;
        }
        if (!hp.Heal(hpPotionHealAmount, hpPotionId))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_hp&reason=heal_rejected");
            return false;
        }

        itemCounts[hpPotionId] = count - 1;
        stackCount = Mathf.Max(0, stackCount - 1);
        currentWeight = Mathf.Max(0f, currentWeight - 1f);
        ServerAuditLogSimple.Push("inv_use_hp", $"count=1,remain={itemCounts[hpPotionId]}");
        return true;
    }

    public bool TryUseMpPotion()
    {
        int count = MpPotionCount;
        if (count <= 0)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_mp&reason=no_potion");
            return false;
        }

        PlayerMpSimple mp = GetComponent<PlayerMpSimple>();
        if (mp == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_mp&reason=no_player_mp");
            return false;
        }
        if (!mp.Restore(mpPotionRestoreAmount))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValItemUseReject,
                "op=use_mp&reason=restore_rejected");
            return false;
        }

        itemCounts[mpPotionId] = count - 1;
        stackCount = Mathf.Max(0, stackCount - 1);
        currentWeight = Mathf.Max(0f, currentWeight - 1f);
        ServerAuditLogSimple.Push("inv_use_mp", $"count=1,remain={itemCounts[mpPotionId]}");
        return true;
    }

    /// <summary>Client mirror only: apply server-confirmed consume without extra heal/restore side effects.</summary>
    public void ApplyRemotePotionConsume(string itemId, int count, float weightPerItem = 1f)
    {
        itemId = GameItemIdsSimple.Normalize(itemId);
        count = Mathf.Max(1, count);
        weightPerItem = Mathf.Max(0.01f, weightPerItem);
        int cur = GetCount(itemId);
        int next = Mathf.Max(0, cur - count);
        itemCounts[itemId] = next;
        stackCount = Mathf.Max(0, stackCount - count);
        currentWeight = Mathf.Max(0f, currentWeight - weightPerItem * count);
    }

    public bool TryBuyHpPotion()
    {
        PlayerWalletSimple wallet = GetComponent<PlayerWalletSimple>();
        if (wallet == null)
            return false;

        int count = Mathf.Max(1, buyPotionCount);
        float totalWeight = Mathf.Max(0.1f, buyPotionWeight) * count;
        int cost = Mathf.Max(1, buyPotionGoldCost) * count;

        if (currentWeight + totalWeight > maxCarryWeight + 0.0001f)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValInventoryFull,
                $"op=buy_hp&reason=overweight&w={currentWeight:F1}&max={maxCarryWeight:F1}");
            return false;
        }
        if (!wallet.TrySpend(cost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValWalletReject,
                $"op=buy_hp&needGold={cost}&haveGold={wallet.Gold}");
            return false;
        }

        return TryAddPickup(Mathf.Max(0.1f, buyPotionWeight), hpPotionId, count);
    }

    public bool TryBuyMpPotion()
    {
        PlayerWalletSimple wallet = GetComponent<PlayerWalletSimple>();
        if (wallet == null)
            return false;

        int count = Mathf.Max(1, buyPotionCount);
        float totalWeight = Mathf.Max(0.1f, buyPotionWeight) * count;
        int cost = Mathf.Max(1, buyManaGoldCost) * count;

        if (currentWeight + totalWeight > maxCarryWeight + 0.0001f)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValInventoryFull,
                $"op=buy_mp&reason=overweight&w={currentWeight:F1}&max={maxCarryWeight:F1}");
            return false;
        }
        if (!wallet.TrySpend(cost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValWalletReject,
                $"op=buy_mp&needGold={cost}&haveGold={wallet.Gold}");
            return false;
        }

        return TryAddPickup(Mathf.Max(0.1f, buyPotionWeight), mpPotionId, count);
    }

    public bool TrySellOnePotion()
    {
        PlayerWalletSimple wallet = GetComponent<PlayerWalletSimple>();
        if (wallet == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValTradeReject,
                "op=sell&reason=no_wallet");
            return false;
        }

        if (HpPotionCount > 0)
        {
            itemCounts[hpPotionId] = HpPotionCount - 1;
            stackCount = Mathf.Max(0, stackCount - 1);
            currentWeight = Mathf.Max(0f, currentWeight - 1f);
            wallet.AddGold(Mathf.Max(1, sellPotionGold));
            ServerAuditLogSimple.Push("inv_sell_hp", "count=1");
            return true;
        }

        if (MpPotionCount > 0)
        {
            itemCounts[mpPotionId] = MpPotionCount - 1;
            stackCount = Mathf.Max(0, stackCount - 1);
            currentWeight = Mathf.Max(0f, currentWeight - 1f);
            wallet.AddGold(Mathf.Max(1, sellManaGold));
            ServerAuditLogSimple.Push("inv_sell_mp", "count=1");
            return true;
        }

        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValTradeReject,
            "op=sell&reason=no_potions");
        return false;
    }

    public bool TryDiscardOneJunk()
    {
        if (TryDiscardById(GameItemIdsSimple.Shard, 1f))
            return true;
        if (TryDiscardById(mpPotionId, 1f))
            return true;
        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValTradeReject,
            "op=discard_junk&reason=no_junk");
        return false;
    }

    bool TryDiscardById(string itemId, float unitWeight)
    {
        itemId = GameItemIdsSimple.Normalize(itemId);
        int c = GetCount(itemId);
        if (c <= 0)
            return false;
        itemCounts[itemId] = c - 1;
        stackCount = Mathf.Max(0, stackCount - 1);
        currentWeight = Mathf.Max(0f, currentWeight - Mathf.Max(0.1f, unitWeight));
        ServerAuditLogSimple.Push("inv_discard", $"{itemId},count=1");
        return true;
    }

    public bool RemoveItemById(string itemId, int count, float unitWeight = 1f)
    {
        itemId = GameItemIdsSimple.Normalize(itemId);
        if (string.IsNullOrEmpty(itemId) || count <= 0)
            return false;
        int has = GetCount(itemId);
        if (has < count)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValStorageReject,
                $"op=remove&itemId={itemId}&need={count}&have={has}");
            return false;
        }

        itemCounts[itemId] = has - count;
        stackCount = Mathf.Max(0, stackCount - count);
        currentWeight = Mathf.Max(0f, currentWeight - Mathf.Max(0.1f, unitWeight) * count);
        ServerAuditLogSimple.Push("inv_remove", $"{itemId},count={count}");
        return true;
    }

    public void SetItemCount(string itemId, int count, float unitWeight = 1f)
    {
        itemId = GameItemIdsSimple.Normalize(itemId);

        int clamped = Mathf.Max(0, count);
        itemCounts[itemId] = clamped;
        RebuildWeightAndStacks(unitWeight);
        ServerAuditLogSimple.Push("inv_set", $"{itemId},{clamped}");
    }

    void OnValidate()
    {
        hpPotionId = GameItemIdsSimple.Normalize(hpPotionId);
        mpPotionId = GameItemIdsSimple.Normalize(mpPotionId);
        if (maxCarryWeight < 1f) maxCarryWeight = 1f;
        if (buyPotionWeight <= 0f) buyPotionWeight = 1f;
        if (buyPotionCount < 1) buyPotionCount = 1;
    }

    void RebuildWeightAndStacks(float fallbackUnitWeight)
    {
        int totalCount = 0;
        foreach (KeyValuePair<string, int> kv in itemCounts)
            totalCount += Mathf.Max(0, kv.Value);

        stackCount = totalCount;
        currentWeight = Mathf.Max(0f, totalCount * Mathf.Max(0.1f, fallbackUnitWeight));
        currentWeight = Mathf.Min(currentWeight, maxCarryWeight);
    }
}

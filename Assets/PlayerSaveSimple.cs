using UnityEngine;

/// <summary>D3: 本地存档占位（PlayerPrefs）。F9 保存，F10 读取。</summary>
public class PlayerSaveSimple : MonoBehaviour
{
    const string Prefix = "EOD_SAVE_";
    const int SaveVersion = 1;
    public bool autoLoadOnStart = true;
    PlayerHotkeysSimple hotkeys;

    public PlayerProgressSimple progress;
    public PlayerWalletSimple wallet;
    public PlayerEnhanceSimple enhance;
    public PlayerInventorySimple inventory;
    public PlayerBankSimple bank;
    public PlayerSkillUnlockSimple unlocks;
    public PlayerSkillMasterySimple mastery;
    public PlayerHealthSimple health;

    void Reset()
    {
        progress = GetComponent<PlayerProgressSimple>();
        wallet = GetComponent<PlayerWalletSimple>();
        enhance = GetComponent<PlayerEnhanceSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
        bank = GetComponent<PlayerBankSimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        health = GetComponent<PlayerHealthSimple>();
    }

    void Start()
    {
        if (autoLoadOnStart)
            Load();
    }

    void Update()
    {
        if (hotkeys == null)
            hotkeys = GetComponent<PlayerHotkeysSimple>();

        KeyCode saveKey = hotkeys != null ? hotkeys.save : KeyCode.F9;
        KeyCode loadKey = hotkeys != null ? hotkeys.load : KeyCode.F10;
        KeyCode clearKey = hotkeys != null ? hotkeys.clearSave : KeyCode.F11;

        if (Input.GetKeyDown(saveKey))
            Save();
        if (Input.GetKeyDown(loadKey))
            Load();
        if (Input.GetKeyDown(clearKey))
            ClearSave();
    }

    public void Save()
    {
        PlayerPrefs.SetInt(Prefix + "version", SaveVersion);
        if (progress != null)
        {
            PlayerPrefs.SetInt(Prefix + "level", progress.level);
            PlayerPrefs.SetInt(Prefix + "xpLevel", progress.xpIntoCurrentLevel);
            PlayerPrefs.SetInt(Prefix + "xpBank", progress.xpBank);
            PlayerPrefs.SetInt(Prefix + "sp", progress.skillUnlockPoints);
        }

        if (wallet != null)
            PlayerPrefs.SetInt(Prefix + "gold", wallet.Gold);
        if (enhance != null)
            PlayerPrefs.SetInt(Prefix + "enhance", enhance.enhanceStep);
        if (inventory != null)
        {
            PlayerPrefs.SetInt(Prefix + "hpPot", inventory.HpPotionCount);
            PlayerPrefs.SetInt(Prefix + "mpPot", inventory.MpPotionCount);
        }
        if (bank != null)
        {
            PlayerPrefs.SetInt(Prefix + "bankGold", bank.bankGold);
            PlayerPrefs.SetInt(Prefix + "bankHp", bank.bankHpPotion);
            PlayerPrefs.SetInt(Prefix + "bankMp", bank.bankMpPotion);
        }
        if (unlocks != null)
        {
            PlayerPrefs.SetInt(Prefix + "qTier", unlocks.burstTier);
            PlayerPrefs.SetInt(Prefix + "rTier", unlocks.frostTier);
        }
        if (mastery != null)
        {
            PlayerPrefs.SetInt(Prefix + "qLv", mastery.burstSkillLevel);
            PlayerPrefs.SetInt(Prefix + "rLv", mastery.frostSkillLevel);
        }
        if (health != null)
            PlayerPrefs.SetInt(Prefix + "hpNow", health.CurrentHp);

        PlayerPrefs.Save();
        Debug.Log("Saved (F9)");
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(Prefix + "level"))
            return;
        int storedVersion = PlayerPrefs.GetInt(Prefix + "version", 0);
        if (storedVersion != SaveVersion)
            Debug.Log($"Save version mismatch: {storedVersion} -> {SaveVersion} (attempting compatible load)");

        if (progress != null)
        {
            progress.SetState(
                PlayerPrefs.GetInt(Prefix + "level", progress.level),
                PlayerPrefs.GetInt(Prefix + "xpLevel", progress.xpIntoCurrentLevel),
                PlayerPrefs.GetInt(Prefix + "xpBank", progress.xpBank),
                PlayerPrefs.GetInt(Prefix + "sp", progress.skillUnlockPoints)
            );
        }

        if (wallet != null)
            wallet.SetGold(PlayerPrefs.GetInt(Prefix + "gold", wallet.Gold));
        if (enhance != null)
            enhance.enhanceStep = PlayerPrefs.GetInt(Prefix + "enhance", enhance.enhanceStep);
        if (inventory != null)
        {
            inventory.SetItemCount(GameItemIdsSimple.HpPotion, PlayerPrefs.GetInt(Prefix + "hpPot", inventory.HpPotionCount), inventory.buyPotionWeight);
            inventory.SetItemCount(GameItemIdsSimple.MpPotion, PlayerPrefs.GetInt(Prefix + "mpPot", inventory.MpPotionCount), inventory.buyPotionWeight);
        }
        if (bank != null)
        {
            bank.bankGold = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "bankGold", bank.bankGold));
            bank.bankHpPotion = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "bankHp", bank.bankHpPotion));
            bank.bankMpPotion = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "bankMp", bank.bankMpPotion));
        }
        if (unlocks != null)
        {
            unlocks.burstTier = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "qTier", unlocks.burstTier), 1, 2);
            unlocks.frostTier = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "rTier", unlocks.frostTier), 1, 2);
        }
        if (mastery != null)
        {
            mastery.burstSkillLevel = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "qLv", mastery.burstSkillLevel), 1, 10);
            mastery.frostSkillLevel = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "rLv", mastery.frostSkillLevel), 1, 10);
        }
        if (health != null)
            health.SetCurrentHp(PlayerPrefs.GetInt(Prefix + "hpNow", health.CurrentHp));

        Debug.Log("Loaded (F10)");
    }

    public void ClearSave()
    {
        string[] keys =
        {
            "version",
            "level","xpLevel","xpBank","sp","gold","enhance",
            "hpPot","mpPot","bankGold","bankHp","bankMp",
            "qTier","rTier","qLv","rLv","hpNow"
        };
        for (int i = 0; i < keys.Length; i++)
            PlayerPrefs.DeleteKey(Prefix + keys[i]);
        PlayerPrefs.Save();
        Debug.Log("Save Cleared (F11)");
    }
}

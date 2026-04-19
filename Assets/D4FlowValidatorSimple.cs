using UnityEngine;

/// <summary>D4 gate helper: validates City->Field->PvP->BackCity flow.</summary>
public class D4FlowValidatorSimple : MonoBehaviour
{
    public PlayerAreaStateSimple area;
    public PlayerPvpSimple pvp;
    public PlayerProgressSimple progress;
    public PlayerInventorySimple inventory;

    int baseXpBank;
    int baseXpInto;
    int baseHpPotion;
    int step = 1;

    public int CurrentStep => step;
    public bool Completed => step > 4;

    public string StepText
    {
        get
        {
            if (step == 1) return "Step1: stay in City";
            if (step == 2) return "Step2: go Field and gain combat XP";
            if (step == 3) return "Step3: toggle PvP ON";
            if (step == 4) return "Step4: back City and get supply (H+)";
            return "Flow Complete";
        }
    }

    void Start()
    {
        if (area == null) area = GetComponent<PlayerAreaStateSimple>();
        if (pvp == null) pvp = GetComponent<PlayerPvpSimple>();
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (inventory == null) inventory = GetComponent<PlayerInventorySimple>();

        if (progress != null)
        {
            baseXpBank = progress.xpBank;
            baseXpInto = progress.xpIntoCurrentLevel;
        }
        if (inventory != null)
            baseHpPotion = inventory.HpPotionCount;
    }

    void Update()
    {
        if (Completed)
            return;

        if (step == 1)
        {
            if (area != null && area.IsInCity)
                step = 2;
            return;
        }

        if (step == 2)
        {
            bool inField = area != null && !area.IsInCity;
            bool gainedXp = progress != null &&
                            (progress.xpBank > baseXpBank || progress.xpIntoCurrentLevel > baseXpInto);
            if (inField && gainedXp)
                step = 3;
            return;
        }

        if (step == 3)
        {
            if (pvp != null && pvp.pvpEnabled)
                step = 4;
            return;
        }

        if (step == 4)
        {
            bool backCity = area != null && area.IsInCity;
            bool supplied = inventory != null && inventory.HpPotionCount > baseHpPotion;
            if (backCity && supplied)
                step = 5;
        }
    }
}

using UnityEngine;

/// <summary>Week1: unified hotkey registry for progression/economy/save actions.</summary>
public class PlayerHotkeysSimple : MonoBehaviour
{
    [Header("Progression")]
    public KeyCode spendXpToLevel = KeyCode.U;
    public KeyCode spendXpToSkillPoint = KeyCode.I;
    public KeyCode unlockQ2 = KeyCode.O;
    public KeyCode unlockR2 = KeyCode.P;

    [Header("Economy / Inventory")]
    public KeyCode useHpPotion = KeyCode.Alpha1;
    public KeyCode useMpPotion = KeyCode.Alpha2;
    public KeyCode buyHpPotion = KeyCode.B;
    public KeyCode buyMpPotion = KeyCode.N;
    public KeyCode sellPotion = KeyCode.V;
    public KeyCode enhance = KeyCode.T;

    [Header("Bank")]
    public KeyCode bankDepositGold = KeyCode.F5;
    public KeyCode bankWithdrawGold = KeyCode.F6;
    public KeyCode bankDepositPotion = KeyCode.F7;
    public KeyCode bankWithdrawPotion = KeyCode.F8;

    [Header("Social / PvP")]
    public KeyCode togglePvp = KeyCode.K;

    [Header("Save")]
    public KeyCode save = KeyCode.F9;
    public KeyCode load = KeyCode.F10;
    public KeyCode clearSave = KeyCode.F11;
    public KeyCode exportStateJson = KeyCode.F12;
    public KeyCode exportAuditNdjson = KeyCode.F4;
}

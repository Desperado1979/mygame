using UnityEngine;

/// <summary>P1-A-1：每只怪记录是否曾被 Q 伤害、是否曾被 R 冰冻。</summary>
public class MonsterP1A1Mark : MonoBehaviour
{
    bool sawBurstHit;
    bool sawFreeze;

    public void RegisterBurstHit() => sawBurstHit = true;
    public void RegisterFreeze() => sawFreeze = true;

    public bool IsCompliantForKill => sawBurstHit && sawFreeze;
}

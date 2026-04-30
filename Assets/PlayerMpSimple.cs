using UnityEngine;
using Unity.Netcode;

/// <summary>D2/D3: MP 池；上限由 <see cref="PlayerDerivedStatsSimple"/> 按 README §5.2 写入。</summary>
[DefaultExecutionOrder(40)]
public class PlayerMpSimple : MonoBehaviour
{
    public int maxMp = 100;
    [Tooltip("MP per second while below max.（DefaultD3Growth mpRegenPerSecond 覆盖）")]
    public float mpRegenPerSecond = 1.2f;

    float mp;

    void Awake()
    {
        ApplyD3MpRegenFromBalance();
    }

    void ApplyD3MpRegenFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        mpRegenPerSecond = Mathf.Max(0f, d.mpRegenPerSecond);
    }

    void Start()
    {
        if (maxMp > 0 && mp <= 0f)
            mp = maxMp;
    }

    /// <summary>D3：由公式刷新 MaxMP；默认保留蓝量比例以免升级瞬间突变手感。</summary>
    public void ApplyMaxMpFromDerived(int newMaxMp, bool preserveFillRatio)
    {
        newMaxMp = Mathf.Max(1, newMaxMp);
        if (newMaxMp == maxMp && preserveFillRatio)
            return;

        float r = maxMp <= 0 ? 1f : Mp01;
        maxMp = newMaxMp;
        if (preserveFillRatio)
        {
            if (mp <= 0f)
                mp = 0f;
            else
                mp = Mathf.Clamp(r * maxMp, 0f, maxMp);
        }
        else
            mp = Mathf.Clamp(mp, 0f, maxMp);
    }

    void Update()
    {
        if (IsNetcodeClientUsingReplicatedMp())
            return;
        if (mpRegenPerSecond > 0f && mp < maxMp)
            mp = Mathf.Min(maxMp, mp + mpRegenPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// 纯 Client 上蓝量由 <see cref="MultiplayerPlayerSimple"/> 的 NetworkVariable 与服务器对齐；
    /// 若仍本地回蓝会与 <c>ServerRpc</c> 里扣蓝打架，出现「不耗蓝」假满蓝。
    /// </summary>
    public bool IsNetcodeClientUsingReplicatedMp()
    {
        NetworkObject no = GetComponent<NetworkObject>();
        if (no == null || !no.IsSpawned)
            return false;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return false;
        return !NetworkManager.Singleton.IsServer;
    }

    /// <summary>供 Multiplayer 客户端从权威同步写入当前蓝量（下取整）。</summary>
    public void SetReplicatedCurrentMpFromNetwork(int currentFloor)
    {
        int cap = maxMp;
        int v = Mathf.Clamp(currentFloor, 0, cap);
        mp = v;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (mp < amount) return false;
        mp -= amount;
        return true;
    }

    public bool Restore(int amount)
    {
        if (amount <= 0 || mp >= maxMp)
            return false;
        mp = Mathf.Min(maxMp, mp + amount);
        return true;
    }

    public int CurrentMpRounded => Mathf.FloorToInt(mp);
    public int MaxMp => maxMp;
    public float Mp01 => maxMp <= 0 ? 0f : Mathf.Clamp01(mp / maxMp);
}

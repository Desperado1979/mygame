using UnityEngine;
using Unity.Netcode;

/// <summary>D2-1: MP pool with optional regen (placeholder for INT-driven MP later).</summary>
public class PlayerMpSimple : MonoBehaviour
{
    public int maxMp = 100;
    [Tooltip("MP per second while below max.")]
    public float mpRegenPerSecond = 1.2f;

    float mp;

    void Start()
    {
        mp = maxMp;
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

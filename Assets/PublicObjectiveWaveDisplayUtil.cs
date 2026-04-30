using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 公共目标波次在 HUD / 世界提示 / W1 中的**唯一**数值来源，避免 <see cref="DebugHudSimple"/> 与 <see cref="PublicObjectiveWorldHintSimple"/> 分叉。
/// 纯 Client 时以 <see cref="PublicObjectiveEventStateSimple"/> 的 NetworkVariable 为准；否则以场景 <see cref="WaveSpawnerSimple"/> 为准。
/// </summary>
public static class PublicObjectiveWaveDisplayUtil
{
    public static float JoinRadiusScale => Mathf.Max(0.1f, D3GrowthBalance.Load().publicObjectiveJoinRadiusScale);

    /// <summary>
    /// HUD / W1 / 世界提示共用的「参与」半径：<c>max(表下限, spawnRingRadius×缩放)</c>。
    /// </summary>
    public static float ParticipationJoinRadius(WaveSpawnerSimple waveSpawner)
    {
        if (waveSpawner == null)
            return 0f;
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float floor = Mathf.Max(0.1f, d.publicObjectiveJoinRadiusMin);
        float scale = Mathf.Max(0.1f, d.publicObjectiveJoinRadiusScale);
        return Mathf.Max(floor, waveSpawner.spawnRingRadius * scale);
    }

    public static void GetWaveDisplay(WaveSpawnerSimple waveSpawner, out int waveOneBased, out int totalWaves, out bool allWavesCompleteOut)
    {
        waveOneBased = 0;
        totalWaves = 0;
        allWavesCompleteOut = false;
        if (waveSpawner == null)
            return;

        int totalConfig = Mathf.Max(1, waveSpawner.TotalWaveCount);
        NetworkManager nm = NetworkManager.Singleton;
        PublicObjectiveEventStateSimple ev = PublicObjectiveEventStateSimple.Instance;
        if (nm != null && nm.IsListening && !nm.IsServer && ev != null && ev.IsSpawned)
        {
            allWavesCompleteOut = ev.PublicAllWavesComplete;
            totalWaves = ev.PublicTotalWaves > 0 ? Mathf.Max(1, ev.PublicTotalWaves) : totalConfig;
            if (!allWavesCompleteOut)
                waveOneBased = ev.PublicWaveIndex0 + 1;
            return;
        }

        allWavesCompleteOut = waveSpawner.allWavesComplete;
        totalWaves = totalConfig;
        if (!allWavesCompleteOut)
            waveOneBased = waveSpawner.currentWaveIndex + 1;
    }
}

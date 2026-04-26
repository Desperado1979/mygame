using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>D2-2: Burn (DOT) + Freeze (root placeholder, no DOT) — aligned with README §11.1.</summary>
[RequireComponent(typeof(EnemyHealthSimple))]
public class EnemyStatusEffectsSimple : MonoBehaviour
{
    [Header("Burn (DOT, independent of res in full design)")]
    public int burnDamagePerTick = 1;
    public float burnTickInterval = 0.55f;

    [Header("Runtime")]
    float burnEndTime;
    float freezeEndTime;
    float nextBurnTickTime;

    MeshRenderer meshRenderer;
    Color originalColor;
    bool cachedColor;

    Rigidbody body;
    EnemyHealthSimple _health;

    float SimTime
    {
        get
        {
            NetworkObject n = GetComponent<NetworkObject>();
            if (n != null && n.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                return (float)NetworkManager.Singleton.ServerTime.Time;
            return Time.time;
        }
    }

    /// <summary>HUD：存活且启用的敌人实例（无全场景 FindObjectsOfType）。</summary>
    static readonly List<EnemyStatusEffectsSimple> s_HudInstances = new List<EnemyStatusEffectsSimple>();

    void Awake()
    {
        _health = GetComponent<EnemyHealthSimple>();
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            originalColor = meshRenderer.material.color;
            cachedColor = true;
        }
    }

    void OnEnable()
    {
        s_HudInstances.Add(this);
    }

    void OnDisable()
    {
        s_HudInstances.Remove(this);
    }

    public static int HudLivingApproxCount => s_HudInstances.Count;

    /// <summary>玩测 HUD：最近敌人状态条；遍历登记列表而非 FindObjectsOfType。</summary>
    public static EnemyStatusEffectsSimple FindNearestForHud(Vector3 from, float maxDist)
    {
        float maxSq = maxDist * maxDist;
        EnemyStatusEffectsSimple best = null;
        float bestSq = maxSq;
        List<EnemyStatusEffectsSimple> list = s_HudInstances;
        for (int i = 0; i < list.Count; i++)
        {
            EnemyStatusEffectsSimple e = list[i];
            if (e == null || !e.gameObject.activeInHierarchy)
                continue;
            float sq = (e.transform.position - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = e;
            }
        }

        return best;
    }

    void Update()
    {
        float t = SimTime;
        TickBurn(t);
        ClampVelocityIfFrozen(t);
        UpdateTint(t);
    }

    void TickBurn(float t)
    {
        if (t >= burnEndTime)
            return;

        if (t < nextBurnTickTime)
            return;

        if (_health != null)
            _health.TakeHit(burnDamagePerTick);

        nextBurnTickTime = t + burnTickInterval;
    }

    void ClampVelocityIfFrozen(float t)
    {
        if (body == null || body.isKinematic)
            return;
        if (t >= freezeEndTime)
            return;

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void UpdateTint(float t)
    {
        if (!cachedColor || meshRenderer == null)
            return;

        bool burning = t < burnEndTime;
        bool frozen = t < freezeEndTime;
        Color c = originalColor;

        if (burning && frozen)
            c = Color.Lerp(Color.red, Color.cyan, 0.45f);
        else if (frozen)
            c = Color.Lerp(originalColor, Color.cyan, 0.38f);
        else if (burning)
            c = Color.Lerp(originalColor, new Color(1f, 0.35f, 0.08f), 0.42f);

        meshRenderer.material.color = c;
    }

    /// <summary>Refresh or extend burn duration; DOT ticks use burnTickInterval.</summary>
    public void ApplyBurn(float durationSeconds)
    {
        float t = SimTime;
        burnEndTime = Mathf.Max(burnEndTime, t + Mathf.Max(0.01f, durationSeconds));
        nextBurnTickTime = Mathf.Min(nextBurnTickTime, t + 0.02f);
        var aut = GetComponent<MultiplayerEnemyAuthoritySimple>();
        if (aut != null && aut.IsServer)
            aut.SetServerBurnEnd(burnEndTime);
    }

    public void ApplyFreeze(float durationSeconds)
    {
        float t = SimTime;
        freezeEndTime = Mathf.Max(freezeEndTime, t + Mathf.Max(0.01f, durationSeconds));
        var aut = GetComponent<MultiplayerEnemyAuthoritySimple>();
        if (aut != null && aut.IsServer)
            aut.SetServerFreezeEnd(freezeEndTime);
    }

    public bool IsFrozen => SimTime < freezeEndTime;
    public bool IsBurning => SimTime < burnEndTime;

    /// <summary>For future movers: 0 while frozen.</summary>
    public float MoveSpeedMultiplier => IsFrozen ? 0f : 1f;

    public string GetHudSummary()
    {
        float t = SimTime;
        if (t >= burnEndTime && t >= freezeEndTime)
            return "";

        P1AContentConfig cfg = P1AContentConfig.TryLoadDefault();
        string burnL = (cfg != null && !string.IsNullOrWhiteSpace(cfg.hudVfxBurnLabel)) ? cfg.hudVfxBurnLabel.Trim() : "燃";
        string iceL = (cfg != null && !string.IsNullOrWhiteSpace(cfg.hudVfxFrostLabel)) ? cfg.hudVfxFrostLabel.Trim() : "冰";
        string unitS = (cfg != null && !string.IsNullOrWhiteSpace(cfg.hudVfxTimeUnitS)) ? cfg.hudVfxTimeUnitS.Trim() : "s";
        string midS = " ";
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.hudVfxDualMidSpace)) midS = cfg.hudVfxDualMidSpace;
        int secDp = cfg != null ? Mathf.Clamp(cfg.hudNearEnemySecondsDecimals, 0, 3) : 1;
        string secFmt = "F" + secDp;

        if (t < burnEndTime && t < freezeEndTime)
            return burnL + (burnEndTime - t).ToString(secFmt) + unitS + midS + iceL + (freezeEndTime - t).ToString(secFmt) + unitS;
        if (t < burnEndTime)
            return burnL + (burnEndTime - t).ToString(secFmt) + unitS;
        return iceL + (freezeEndTime - t).ToString(secFmt) + unitS;
    }
}

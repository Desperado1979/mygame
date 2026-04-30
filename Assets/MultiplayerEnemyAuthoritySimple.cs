using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server authority for chasers/touch; replicates HP + status VFX to clients.
/// (EnemyStatusEffectsSimple stays server-only; clients apply tint from NetworkVariable here.)
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MultiplayerEnemyAuthoritySimple : NetworkBehaviour
{
    public static readonly int UnsetHp = -1;

    readonly NetworkVariable<int> _netHp = new NetworkVariable<int>(
        UnsetHp, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Absolute end times in NetworkTime.ServerTime seconds; negative means inactive
    readonly NetworkVariable<float> _netFreezeEnd = new NetworkVariable<float>(
        -1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<float> _netBurnEnd = new NetworkVariable<float>(
        -1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    EnemyChaseSimple chase;
    EnemyTouchDamageSimple touchDamage;
    EnemySpellTouchDamageSimple spellTouchDamage;
    EnemyStatusEffectsSimple status;
    EnemyHealthSimple health;
    EnemyHitFeedbackSimple hitFeedback;
    MeshRenderer vfxRenderer;
    Color vfxBaseColor;
    bool vfxColorCached;

    public override void OnNetworkSpawn()
    {
        health = GetComponent<EnemyHealthSimple>();
        chase = GetComponent<EnemyChaseSimple>();
        touchDamage = GetComponent<EnemyTouchDamageSimple>();
        spellTouchDamage = GetComponent<EnemySpellTouchDamageSimple>();
        status = GetComponent<EnemyStatusEffectsSimple>();
        hitFeedback = GetComponent<EnemyHitFeedbackSimple>();
        CacheVfxRoot();
        ApplyAuthorityState();
        if (IsServer)
        {
            if (health != null)
                _netHp.Value = health.CurrentHp;
        }
        else
        {
            if (health != null && _netHp.Value != UnsetHp)
                health.SetHpFromNetwork(_netHp.Value);
        }

        _netHp.OnValueChanged += OnHpReplicated;
    }

    public override void OnNetworkDespawn()
    {
        _netHp.OnValueChanged -= OnHpReplicated;
    }

    void OnHpReplicated(int prev, int next)
    {
        if (IsServer)
            return;
        if (health != null && next != UnsetHp)
            health.SetHpFromNetwork(next);
    }

    void CacheVfxRoot()
    {
        vfxRenderer = GetComponentInChildren<MeshRenderer>();
        if (vfxRenderer == null)
            return;
        if (vfxRenderer.sharedMaterial == null)
            vfxRenderer = null;
        if (vfxRenderer != null)
        {
            vfxBaseColor = vfxRenderer.material.color;
            vfxColorCached = true;
        }
    }

    void ApplyAuthorityState()
    {
        bool serverOwnsLogic = IsServer;
        if (chase != null) chase.enabled = serverOwnsLogic;
        if (touchDamage != null) touchDamage.enabled = serverOwnsLogic;
        if (spellTouchDamage != null && !spellTouchDamage.InactiveByBalance)
            spellTouchDamage.enabled = serverOwnsLogic;
        if (status != null) status.enabled = serverOwnsLogic;
    }

    /// <summary>Server: push current HP to observers (e.g. after <see cref="EnemyHealthSimple.TakeHit"/>).</summary>
    public void SetServerHpReplicated(int hp)
    {
        if (!IsServer)
            return;
        _netHp.Value = hp;
    }

    /// <summary>Server: absolute freeze end in <see cref="NetworkManager.ServerTime"/>.<see cref="NetworkTime.Time"/> (seconds).</summary>
    public void SetServerFreezeEnd(float serverTimeEnd)
    {
        if (!IsServer)
            return;
        _netFreezeEnd.Value = serverTimeEnd;
    }

    public void SetServerBurnEnd(float serverTimeEnd)
    {
        if (!IsServer)
            return;
        _netBurnEnd.Value = serverTimeEnd;
    }

    /// <summary>Server: play local hit flash/knock and replicate the same feedback to pure clients.</summary>
    public void BroadcastHitFeedback(Vector3 hitFromWorldPos)
    {
        if (!IsServer)
            return;
        if (hitFeedback == null)
            hitFeedback = GetComponent<EnemyHitFeedbackSimple>();
        if (hitFeedback != null)
            hitFeedback.Play(hitFromWorldPos);
        PlayHitFeedbackClientRpc(hitFromWorldPos);
    }

    [ClientRpc]
    void PlayHitFeedbackClientRpc(Vector3 hitFromWorldPos)
    {
        if (IsServer)
            return; // host already played via server path
        if (hitFeedback == null)
            hitFeedback = GetComponent<EnemyHitFeedbackSimple>();
        if (hitFeedback != null)
            hitFeedback.Play(hitFromWorldPos);
    }

    public static float NowServer()
    {
        if (NetworkManager.Singleton == null)
            return Time.time;
        return (float)NetworkManager.Singleton.ServerTime.Time;
    }

    void LateUpdate()
    {
        if (IsServer || !vfxColorCached || vfxRenderer == null)
            return;
        if (NetworkManager.Singleton == null)
            return;
        float now = (float)NetworkManager.Singleton.ServerTime.Time;
        bool burn = _netBurnEnd.Value >= 0f && _netBurnEnd.Value > now;
        bool frz = _netFreezeEnd.Value >= 0f && _netFreezeEnd.Value > now;
        Color c = vfxBaseColor;
        if (burn && frz)
            c = Color.Lerp(Color.red, Color.cyan, 0.45f);
        else if (frz)
            c = Color.Lerp(vfxBaseColor, Color.cyan, 0.38f);
        else if (burn)
            c = Color.Lerp(vfxBaseColor, new Color(1f, 0.35f, 0.08f), 0.42f);
        vfxRenderer.material.color = c;
    }
}

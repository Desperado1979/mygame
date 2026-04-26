using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Owner-input player controller for NGO test sessions (up to 5 players).
/// Server keeps authority over combat and enemy state mutations.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class MultiplayerPlayerSimple : NetworkBehaviour
{
    public float moveSpeed = 6f;
    public float attackRange = 2.4f;
    public int damagePerHit = 1;
    PlayerHealthSimple health;
    PlayerMpSimple mp;
    PlayerSkillMasterySimple mastery;
    PlayerSkillUnlockSimple unlocks;
    PlayerSkillBurstSimple burstSkill;
    PlayerSkillFrostSimple frostSkill;
    PlayerProgressSimple progress;
    PlayerWalletSimple wallet;
    PlayerInventorySimple inventory;
    PlayerPickupSimple pickup;
    PlayerEnhanceSimple enhance;
    PlayerBankSimple bank;
    PlayerSaveSimple save;
    readonly NetworkVariable<int> hpSync = new NetworkVariable<int>(-1);
    readonly NetworkVariable<int> goldSync = new NetworkVariable<int>(0);
    readonly NetworkVariable<int> mpSync = new NetworkVariable<int>(-1);
    readonly NetworkVariable<int> levelSync = new NetworkVariable<int>(1);
    readonly NetworkVariable<int> xpIntoLevelSync = new NetworkVariable<int>(0);
    readonly NetworkVariable<int> xpBankSync = new NetworkVariable<int>(0);
    readonly NetworkVariable<int> skillPointsSync = new NetworkVariable<int>(0);
    readonly NetworkVariable<int> burstTierSync = new NetworkVariable<int>(1);
    readonly NetworkVariable<int> frostTierSync = new NetworkVariable<int>(1);
    // Q/R 施放升档在 ServerRpc 里推进；必须同步到纯 Client，否则 HUD 的 q/r 一直显示 1。
    readonly NetworkVariable<int> masteryBurstLevelSync = new NetworkVariable<int>(1);
    readonly NetworkVariable<int> masteryFrostLevelSync = new NetworkVariable<int>(1);
    float burstCooldownEndTimeServer;
    float frostCooldownEndTimeServer;
    // Client → server: one RPC with accumulated motion (per-frame ServerRpc hammers bandwidth / causes lag).
    const float ClientMoveSendInterval = 0.04f;
    float nextClientMoveSendUnscaled = float.NegativeInfinity;
    Vector3 clientMovePosDelta;

    public override void OnNetworkSpawn()
    {
        health = GetComponent<PlayerHealthSimple>();
        mp = GetComponent<PlayerMpSimple>();
        mastery = GetComponent<PlayerSkillMasterySimple>();
        unlocks = GetComponent<PlayerSkillUnlockSimple>();
        burstSkill = GetComponent<PlayerSkillBurstSimple>();
        frostSkill = GetComponent<PlayerSkillFrostSimple>();
        progress = GetComponent<PlayerProgressSimple>();
        wallet = GetComponent<PlayerWalletSimple>();
        inventory = GetComponent<PlayerInventorySimple>();
        pickup = GetComponent<PlayerPickupSimple>();
        enhance = GetComponent<PlayerEnhanceSimple>();
        bank = GetComponent<PlayerBankSimple>();
        save = GetComponent<PlayerSaveSimple>();
        hpSync.OnValueChanged += OnHpSyncChanged;
        goldSync.OnValueChanged += OnGoldSyncChanged;
        mpSync.OnValueChanged += OnMpSyncChanged;
        levelSync.OnValueChanged += OnProgressSyncChanged;
        xpIntoLevelSync.OnValueChanged += OnProgressSyncChanged;
        xpBankSync.OnValueChanged += OnProgressSyncChanged;
        skillPointsSync.OnValueChanged += OnProgressSyncChanged;
        burstTierSync.OnValueChanged += OnBurstTierSyncChanged;
        frostTierSync.OnValueChanged += OnFrostTierSyncChanged;
        if (IsServer && health != null)
            hpSync.Value = health.CurrentHp;
        if (IsServer && wallet != null)
            goldSync.Value = wallet.Gold;
        if (IsServer && mp != null)
            mpSync.Value = mp.CurrentMpRounded;
        if (IsServer)
        {
            SyncLocalProgressFromNetwork();
            SyncLocalMasteryFromNetwork();
        }
        if (IsOwner)
        {
            PlayerHealthSimple.SetPreferredInstance(health);
            PlayerProgressSimple.SetPreferredInstance(progress);
            PlayerWalletSimple.SetPreferredInstance(wallet);
        }
        ConfigureOwnerOnlyGameplayComponents();
        AssignRespawnPointIfMissing();
        if (IsServer)
        {
            float x = ((int)OwnerClientId % 5) * 2.2f;
            float z = ((int)OwnerClientId / 5) * 2.2f;
            transform.position = new Vector3(x, 1f, z);
        }
        if (IsOwner)
            BindCameraToSelf();
    }

    void Update()
    {
        if (!IsOwner)
        {
            SyncLocalHpFromNetwork();
            SyncLocalGoldFromNetwork();
            SyncLocalMpFromNetwork();
            SyncLocalProgressFromNetwork();
            SyncLocalMasteryFromNetwork();
            return;
        }
        SyncLocalHpFromNetwork();
        SyncLocalGoldFromNetwork();
        SyncLocalMpFromNetwork();
        SyncLocalProgressFromNetwork();
        SyncLocalMasteryFromNetwork();
        if (health != null && health.IsDead)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = BuildCameraRelativeDir(h, v);

        if (IsServer)
        {
            ApplyMove(dir, Time.deltaTime);
        }
        else
        {
            if (Time.deltaTime > 0f)
            {
                Vector3 n = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.zero;
                if (n != Vector3.zero)
                    clientMovePosDelta += n * moveSpeed * Time.deltaTime;
            }
            if (Time.unscaledTime >= nextClientMoveSendUnscaled)
            {
                if (clientMovePosDelta.sqrMagnitude > 1e-8f)
                {
                    MoveAccumulatedServerRpc(clientMovePosDelta);
                    clientMovePosDelta = Vector3.zero;
                }
                nextClientMoveSendUnscaled = Time.unscaledTime + ClientMoveSendInterval;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
            AttackServerRpc(attackRange, damagePerHit);
        if (Input.GetKeyDown(KeyCode.Q))
            CastBurstServerRpc();
        if (Input.GetKeyDown(KeyCode.R))
            CastFrostServerRpc();
        if (Input.GetKeyDown(KeyCode.U))
            RequestSpendXpToLevel_ServerRpc(10);
        if (Input.GetKeyDown(KeyCode.I))
            RequestSpendXpToSkillPoint_ServerRpc(15);
        if (Input.GetKeyDown(KeyCode.O))
            RequestUnlockBurstTier2_ServerRpc();
        if (Input.GetKeyDown(KeyCode.P))
            RequestUnlockFrostTier2_ServerRpc();
    }

    public override void OnNetworkDespawn()
    {
        hpSync.OnValueChanged -= OnHpSyncChanged;
        goldSync.OnValueChanged -= OnGoldSyncChanged;
        mpSync.OnValueChanged -= OnMpSyncChanged;
        levelSync.OnValueChanged -= OnProgressSyncChanged;
        xpIntoLevelSync.OnValueChanged -= OnProgressSyncChanged;
        xpBankSync.OnValueChanged -= OnProgressSyncChanged;
        skillPointsSync.OnValueChanged -= OnProgressSyncChanged;
        burstTierSync.OnValueChanged -= OnBurstTierSyncChanged;
        frostTierSync.OnValueChanged -= OnFrostTierSyncChanged;
        base.OnNetworkDespawn();
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestSpendXpToLevel_ServerRpc(int amount)
    {
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress != null)
            progress.SpendXpForLevel(amount);
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestSpendXpToSkillPoint_ServerRpc(int costPerPoint)
    {
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress != null)
            progress.SpendXpForSkillUnlock(costPerPoint);
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestUnlockBurstTier2_ServerRpc()
    {
        if (unlocks == null) unlocks = GetComponent<PlayerSkillUnlockSimple>();
        if (unlocks != null)
            unlocks.UnlockBurstTier2();
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestUnlockFrostTier2_ServerRpc()
    {
        if (unlocks == null) unlocks = GetComponent<PlayerSkillUnlockSimple>();
        if (unlocks != null)
            unlocks.UnlockFrostTier2();
    }

    [ClientRpc]
    public void MirrorInventoryAddClientRpc(float weight, string itemId, int count)
    {
        if (IsServer)
            return;
        if (!IsOwner)
            return;
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null) return;
        if (!inv.TryAddPickup(weight, itemId, count))
            Debug.LogWarning("[MP] MirrorInventoryAddClientRpc: apply failed (should match server).");
    }

    [ClientRpc]
    public void MirrorPotionConsumeClientRpc(string itemId, int count, float weightPerItem)
    {
        if (IsServer)
            return;
        if (!IsOwner)
            return;
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;
        inv.ApplyRemotePotionConsume(itemId, count, weightPerItem);
    }

    [ClientRpc]
    public void MirrorInventoryRemoveClientRpc(string itemId, int count, float weightPerItem)
    {
        if (IsServer)
            return;
        if (!IsOwner)
            return;
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;
        inv.ApplyRemotePotionConsume(itemId, count, weightPerItem);
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestUsePotion_ServerRpc(bool hpPotion)
    {
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;

        bool ok = hpPotion ? inv.TryUseHpPotion() : inv.TryUseMpPotion();
        if (!ok)
            return;

        string id = hpPotion ? GameItemIdsSimple.HpPotion : GameItemIdsSimple.MpPotion;
        MirrorPotionConsumeClientRpc(id, 1, 1f);
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestSellOnePotion_ServerRpc()
    {
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;
        int hpBefore = inv.HpPotionCount;
        int mpBefore = inv.MpPotionCount;
        bool ok = inv.TrySellOnePotion();
        if (!ok)
            return;
        if (hpBefore > inv.HpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.HpPotion, 1, 1f);
        else if (mpBefore > inv.MpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.MpPotion, 1, 1f);
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestDiscardJunk_ServerRpc()
    {
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;
        int shardBefore = inv.GetCount(GameItemIdsSimple.Shard);
        int mpBefore = inv.MpPotionCount;
        bool ok = inv.TryDiscardOneJunk();
        if (!ok)
            return;
        if (shardBefore > inv.GetCount(GameItemIdsSimple.Shard))
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.Shard, 1, 1f);
        else if (mpBefore > inv.MpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.MpPotion, 1, 1f);
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestBuyPotion_ServerRpc(bool hpPotion)
    {
        PlayerInventorySimple inv = GetComponent<PlayerInventorySimple>();
        if (inv == null)
            return;
        bool ok = hpPotion ? inv.TryBuyHpPotion() : inv.TryBuyMpPotion();
        if (!ok)
            return;

        int count = Mathf.Max(1, inv.buyPotionCount);
        float w = Mathf.Max(0.1f, inv.buyPotionWeight);
        string id = hpPotion ? GameItemIdsSimple.HpPotion : GameItemIdsSimple.MpPotion;
        MirrorInventoryAddClientRpc(w, id, count);
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestPickupE_ServerRpc()
    {
        if (pickup == null) pickup = GetComponent<PlayerPickupSimple>();
        if (pickup != null) pickup.RunServerSidePickup();
    }

    [ServerRpc(RequireOwnership = true)]
    void MoveAccumulatedServerRpc(Vector3 positionDelta)
    {
        if (positionDelta.sqrMagnitude < 1e-12f)
            return;
        transform.position += positionDelta;
        Vector3 n = new Vector3(positionDelta.x, 0f, positionDelta.z);
        if (n.sqrMagnitude > 1e-6f)
            transform.forward = n.normalized;
    }

    [ServerRpc]
    void AttackServerRpc(float range, int damage)
    {
        // Must use server-authoritative transform. Client-passed positions desync from NetworkTransform
        // and OverlapSphere would miss on the host simulation (Host OK, pure Client always "miss").
        Vector3 attackerPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(attackerPos, range);
        EnemyHealthSimple best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple e = hits[i].GetComponentInParent<EnemyHealthSimple>();
            if (e == null) continue;
            float d = (e.transform.position - attackerPos).sqrMagnitude;
            if (d < bestDist)
            {
                best = e;
                bestDist = d;
            }
        }

        if (best != null)
        {
            best.TakeHit(Mathf.Max(1, damage), attackerPos, gameObject);
            return;
        }

        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValCombatMiss,
            $"net=1&range={range:F1}");
    }

    [ServerRpc]
    void CastBurstServerRpc()
    {
        if (mp == null || burstSkill == null)
            return;
        if (Time.time < burstCooldownEndTimeServer)
            return;
        if (!mp.TrySpend(burstSkill.mpCost))
            return;

        burstCooldownEndTimeServer = Time.time + Mathf.Max(0.1f, burstSkill.cooldownSeconds);
        if (mastery != null)
            mastery.RegisterBurstCast();

        int tier = unlocks != null ? unlocks.burstTier : 1;
        float tierDamageMul = tier >= 2 ? 1.35f : 1f;
        float tierRangeMul = tier >= 2 ? 1.15f : 1f;
        float dmgMult = mastery != null ? mastery.BurstDamageMultiplier : 1f;
        int rolledDamage = Mathf.Max(1, Mathf.RoundToInt(burstSkill.damagePerEnemy * dmgMult * tierDamageMul));

        Vector3 attackerPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(attackerPos, burstSkill.skillRadius * tierRangeMul, burstSkill.enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple enemy = hits[i].GetComponentInParent<EnemyHealthSimple>();
            if (enemy == null) continue;
            enemy.TakeHit(rolledDamage, attackerPos, gameObject);
            EnemyStatusEffectsSimple status = hits[i].GetComponentInParent<EnemyStatusEffectsSimple>();
            if (status != null && burstSkill.burnDurationSeconds > 0f)
                status.ApplyBurn(burstSkill.burnDurationSeconds);
        }
    }

    [ServerRpc]
    void CastFrostServerRpc()
    {
        if (mp == null || frostSkill == null)
            return;
        if (Time.time < frostCooldownEndTimeServer)
            return;
        if (!mp.TrySpend(frostSkill.mpCost))
            return;

        frostCooldownEndTimeServer = Time.time + Mathf.Max(0.1f, frostSkill.cooldownSeconds);
        if (mastery != null)
            mastery.RegisterFrostCast();

        int tier = unlocks != null ? unlocks.frostTier : 1;
        float tierFreezeMul = tier >= 2 ? 1.35f : 1f;
        float tierRadiusMul = tier >= 2 ? 1.12f : 1f;
        float freezeSec = frostSkill.freezeDurationSeconds * tierFreezeMul;
        if (mastery != null)
            freezeSec *= mastery.FrostFreezeDurationMultiplier;

        Vector3 attackerPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(attackerPos, frostSkill.skillRadius * tierRadiusMul, frostSkill.enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyStatusEffectsSimple status = hits[i].GetComponentInParent<EnemyStatusEffectsSimple>();
            if (status != null)
                status.ApplyFreeze(freezeSec);
        }
    }

    void ApplyMove(Vector3 dir, float dt)
    {
        if (dt <= 0f)
            return;
        Vector3 n = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.zero;
        transform.position += n * moveSpeed * dt;
        if (n != Vector3.zero)
            transform.forward = n;
    }

    Vector3 BuildCameraRelativeDir(float h, float v)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return new Vector3(h, 0f, v).normalized;

        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        else fwd.Normalize();

        Vector3 right = cam.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
        else right.Normalize();

        return (right * h + fwd * v).normalized;
    }

    void BindCameraToSelf()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        CameraFollowSimple follow = cam.GetComponent<CameraFollowSimple>();
        if (follow != null)
            follow.target = transform;

        DebugHudSimple hud = FindAnyObjectByType<DebugHudSimple>();
        if (hud != null)
        {
            hud.player = transform;
            hud.health = GetComponent<PlayerHealthSimple>();
            hud.mp = GetComponent<PlayerMpSimple>();
            if (hud.stateExport == null)
                hud.stateExport = GetComponent<PlayerStateExportSimple>();
        }
    }

    void SyncLocalHpFromNetwork()
    {
        if (health == null)
            health = GetComponent<PlayerHealthSimple>();
        if (health == null)
            return;

        if (IsServer)
        {
            if (hpSync.Value != health.CurrentHp)
                hpSync.Value = health.CurrentHp;
            return;
        }

        if (hpSync.Value >= 0 && health.CurrentHp != hpSync.Value)
            health.SetCurrentHp(hpSync.Value);
    }

    void SyncLocalMpFromNetwork()
    {
        if (mp == null)
            mp = GetComponent<PlayerMpSimple>();
        if (mp == null)
            return;

        if (IsServer)
        {
            if (mpSync.Value != mp.CurrentMpRounded)
                mpSync.Value = mp.CurrentMpRounded;
            return;
        }

        if (mpSync.Value < 0)
            return;
        if (mp.CurrentMpRounded != mpSync.Value)
            mp.SetReplicatedCurrentMpFromNetwork(mpSync.Value);
    }

    void SyncLocalGoldFromNetwork()
    {
        if (wallet == null)
            wallet = GetComponent<PlayerWalletSimple>();
        if (wallet == null)
            return;

        if (IsServer)
        {
            if (goldSync.Value != wallet.Gold)
                goldSync.Value = wallet.Gold;
            return;
        }

        if (wallet.Gold != goldSync.Value)
            wallet.SetGold(goldSync.Value);
    }

    void SyncLocalProgressFromNetwork()
    {
        if (progress == null)
            progress = GetComponent<PlayerProgressSimple>();
        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();

        if (IsServer)
        {
            if (progress != null)
            {
                if (levelSync.Value != progress.level)
                    levelSync.Value = progress.level;
                if (xpIntoLevelSync.Value != progress.xpIntoCurrentLevel)
                    xpIntoLevelSync.Value = progress.xpIntoCurrentLevel;
                if (xpBankSync.Value != progress.xpBank)
                    xpBankSync.Value = progress.xpBank;
                if (skillPointsSync.Value != progress.skillUnlockPoints)
                    skillPointsSync.Value = progress.skillUnlockPoints;
            }
            if (unlocks != null)
            {
                if (burstTierSync.Value != unlocks.burstTier)
                    burstTierSync.Value = unlocks.burstTier;
                if (frostTierSync.Value != unlocks.frostTier)
                    frostTierSync.Value = unlocks.frostTier;
            }
            return;
        }

        if (progress != null &&
            (progress.level != levelSync.Value
            || progress.xpIntoCurrentLevel != xpIntoLevelSync.Value
            || progress.xpBank != xpBankSync.Value
            || progress.skillUnlockPoints != skillPointsSync.Value))
        {
            progress.SetState(levelSync.Value, xpIntoLevelSync.Value, xpBankSync.Value, skillPointsSync.Value);
        }
        if (unlocks != null)
        {
            if (unlocks.burstTier != burstTierSync.Value)
                unlocks.burstTier = burstTierSync.Value;
            if (unlocks.frostTier != frostTierSync.Value)
                unlocks.frostTier = frostTierSync.Value;
        }
    }

    void SyncLocalMasteryFromNetwork()
    {
        if (mastery == null)
            mastery = GetComponent<PlayerSkillMasterySimple>();
        if (mastery == null)
            return;

        if (IsServer)
        {
            int b = Mathf.Clamp(mastery.burstSkillLevel, 1, 10);
            if (b != mastery.burstSkillLevel)
                mastery.burstSkillLevel = b;
            int f = Mathf.Clamp(mastery.frostSkillLevel, 1, 10);
            if (f != mastery.frostSkillLevel)
                mastery.frostSkillLevel = f;
            if (masteryBurstLevelSync.Value != mastery.burstSkillLevel)
                masteryBurstLevelSync.Value = mastery.burstSkillLevel;
            if (masteryFrostLevelSync.Value != mastery.frostSkillLevel)
                masteryFrostLevelSync.Value = mastery.frostSkillLevel;
            return;
        }

        int nb = Mathf.Clamp(masteryBurstLevelSync.Value, 1, 10);
        if (mastery.burstSkillLevel != nb)
            mastery.burstSkillLevel = nb;
        int nf = Mathf.Clamp(masteryFrostLevelSync.Value, 1, 10);
        if (mastery.frostSkillLevel != nf)
            mastery.frostSkillLevel = nf;
    }

    void ConfigureOwnerOnlyGameplayComponents()
    {
        bool enable = IsOwner;
        if (progress != null) progress.enabled = enable;
        if (inventory != null) inventory.enabled = enable;
        if (pickup != null) pickup.enabled = enable;
        if (enhance != null) enhance.enabled = enable;
        if (bank != null) bank.enabled = enable;
        if (save != null) save.enabled = enable;
        if (burstSkill != null) burstSkill.enabled = enable;
        if (frostSkill != null) frostSkill.enabled = enable;
    }

    void OnHpSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        if (health == null)
            health = GetComponent<PlayerHealthSimple>();
        if (health != null)
            health.SetCurrentHp(current);
    }

    void OnGoldSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        if (wallet == null)
            wallet = GetComponent<PlayerWalletSimple>();
        if (wallet != null)
            wallet.SetGold(current);
    }

    void OnMpSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        if (mp == null)
            mp = GetComponent<PlayerMpSimple>();
        if (mp == null)
            return;
        if (current < 0)
            return;
        if (mp.CurrentMpRounded != current)
            mp.SetReplicatedCurrentMpFromNetwork(current);
    }

    void OnProgressSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        SyncLocalProgressFromNetwork();
    }

    void OnBurstTierSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();
        if (unlocks != null)
            unlocks.burstTier = current;
    }

    void OnFrostTierSyncChanged(int previous, int current)
    {
        if (IsServer)
            return;
        if (unlocks == null)
            unlocks = GetComponent<PlayerSkillUnlockSimple>();
        if (unlocks != null)
            unlocks.frostTier = current;
    }

    void AssignRespawnPointIfMissing()
    {
        if (health == null || health.respawnPoint != null)
            return;

        Transform t = FindByName("RespawnPoint");
        if (t == null) t = FindByName("CitySpawnPoint");
        if (t == null) t = FindByName("FieldSpawnPoint");
        if (t != null)
            health.respawnPoint = t;
    }

    static Transform FindByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }
}

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
    readonly NetworkVariable<int> d3StrSync = new NetworkVariable<int>(10);
    readonly NetworkVariable<int> d3AgiSync = new NetworkVariable<int>(10);
    readonly NetworkVariable<int> d3IntSync = new NetworkVariable<int>(10);
    readonly NetworkVariable<int> d3VitSync = new NetworkVariable<int>(10);
    readonly NetworkVariable<int> d3UnallocSync = new NetworkVariable<int>(0);
    PlayerStatsSimple d3Stats;
    PlayerHotkeysSimple d3Hotkeys;
    float meleeAttackCooldownEndTimeServer;
    float burstCooldownEndTimeServer;
    float frostCooldownEndTimeServer;
    // Client → server: one RPC with accumulated motion (per-frame ServerRpc hammers bandwidth / causes lag).
    float clientMoveSendInterval = 0.04f;
    float nextClientMoveSendUnscaled = float.NegativeInfinity;
    Vector3 clientMovePosDelta;
    /// <summary>同物体上如仍有 <see cref="PlayerMoveSimple"/>（等 NGO Spawn 的 Capsule 场景体），由后者驱动；未生成前 IsOwner 为 false 时 MPS 不可抢输入。</summary>
    PlayerMoveSimple coexistingPlayerMove;

    void Awake()
    {
        coexistingPlayerMove = GetComponent<PlayerMoveSimple>();
    }

    public override void OnNetworkSpawn()
    {
        EnsureRuntimeSupportComponents();
        d3Stats = GetComponent<PlayerStatsSimple>();
        ApplyD3NetPlayerBaselinesFromBalance();
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
        d3StrSync.OnValueChanged += OnD3SyncTouched;
        d3AgiSync.OnValueChanged += OnD3SyncTouched;
        d3IntSync.OnValueChanged += OnD3SyncTouched;
        d3VitSync.OnValueChanged += OnD3SyncTouched;
        d3UnallocSync.OnValueChanged += OnD3SyncTouched;
        d3Hotkeys = GetComponent<PlayerHotkeysSimple>();
        if (IsServer)
            InitD3FromStatsOnServer();
        ApplyD3MirrorToLocal();
        // EnsureRuntimeSupportComponents 可能在 Spawn 时才挂上 PlayerEquipmentDebug —— 刷新四维推导上限。
        GetComponent<PlayerDerivedStatsSimple>()?.RequestRefresh();
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

    void ApplyD3NetPlayerBaselinesFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        moveSpeed = Mathf.Max(0.1f, d.playerNetMoveSpeed);
        attackRange = Mathf.Max(0.1f, d.playerMeleeAttackRangeNet);
        PlayerStatsSimple st = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
        int str = st != null ? st.strength : d.startingStr;
        damagePerHit = D3GrowthBalance.ComputeMeleePhysicalDamage(d, str);
        clientMoveSendInterval = Mathf.Max(0.01f, d.netClientMoveSendIntervalSec);
    }

    void EnsureRuntimeSupportComponents()
    {
        // Build 直跑时若 NetPlayerHost 预制体漏挂某些脚本，这里兜底补齐，避免 Host/Client 功能不一致。
        if (GetComponent<PlayerStateExportSimple>() == null)
            gameObject.AddComponent<PlayerStateExportSimple>();
        if (GetComponent<PlayerAreaStateSimple>() == null)
            gameObject.AddComponent<PlayerAreaStateSimple>();
        if (GetComponent<PlayerPvpSimple>() == null)
            gameObject.AddComponent<PlayerPvpSimple>();
        if (GetComponent<PartyPlaceholderSimple>() == null)
            gameObject.AddComponent<PartyPlaceholderSimple>();
        if (GetComponent<ChatPlaceholderSimple>() == null)
            gameObject.AddComponent<ChatPlaceholderSimple>();
        if (GetComponent<PlayerStatsSimple>() == null)
            gameObject.AddComponent<PlayerStatsSimple>();
        if (GetComponent<PlayerDerivedStatsSimple>() == null)
            gameObject.AddComponent<PlayerDerivedStatsSimple>();
        if (GetComponent<PlayerEquipmentDebugSimple>() == null)
            gameObject.AddComponent<PlayerEquipmentDebugSimple>();
    }

    void Update()
    {
        if (!IsSpawned)
        {
            if (coexistingPlayerMove != null)
                return;
            if (health == null) health = GetComponent<PlayerHealthSimple>();
            if (health != null && health.IsDead)
                return;
            float uh = Input.GetAxisRaw("Horizontal");
            float uv = Input.GetAxisRaw("Vertical");
            ApplyMove(BuildCameraRelativeDir(uh, uv), Time.deltaTime);
            return;
        }

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
                nextClientMoveSendUnscaled = Time.unscaledTime + clientMoveSendInterval;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
            AttackServerRpc(attackRange, damagePerHit);
        if (Input.GetKeyDown(KeyCode.Q))
            CastBurstServerRpc();
        if (Input.GetKeyDown(KeyCode.R))
            CastFrostServerRpc();
        if (Input.GetKeyDown(KeyCode.U))
            RequestSpendXpToLevel_ServerRpc();
        if (Input.GetKeyDown(KeyCode.I))
            RequestSpendXpToSkillPoint_ServerRpc();
        if (Input.GetKeyDown(KeyCode.O))
            RequestUnlockBurstTier2_ServerRpc();
        if (Input.GetKeyDown(KeyCode.P))
            RequestUnlockFrostTier2_ServerRpc();
        if (d3Hotkeys == null) d3Hotkeys = GetComponent<PlayerHotkeysSimple>();
        if (d3Hotkeys != null)
        {
            if (Input.GetKeyDown(d3Hotkeys.d3AddStr)) RequestD3AllocateServerRpc(0);
            if (Input.GetKeyDown(d3Hotkeys.d3AddAgi)) RequestD3AllocateServerRpc(1);
            if (Input.GetKeyDown(d3Hotkeys.d3AddInt)) RequestD3AllocateServerRpc(2);
            if (Input.GetKeyDown(d3Hotkeys.d3AddVit)) RequestD3AllocateServerRpc(3);
        }
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
        d3StrSync.OnValueChanged -= OnD3SyncTouched;
        d3AgiSync.OnValueChanged -= OnD3SyncTouched;
        d3IntSync.OnValueChanged -= OnD3SyncTouched;
        d3VitSync.OnValueChanged -= OnD3SyncTouched;
        d3UnallocSync.OnValueChanged -= OnD3SyncTouched;
        base.OnNetworkDespawn();
    }

    void InitD3FromStatsOnServer()
    {
        if (d3Stats == null) d3Stats = GetComponent<PlayerStatsSimple>();
        if (d3Stats == null) return;
        d3StrSync.Value = d3Stats.strength;
        d3AgiSync.Value = d3Stats.agility;
        d3IntSync.Value = d3Stats.intellect;
        d3VitSync.Value = d3Stats.vitality;
        d3UnallocSync.Value = d3Stats.unallocatedStatPoints;
    }

    void OnD3SyncTouched(int previous, int current) => ApplyD3MirrorToLocal();

    void ApplyD3MirrorToLocal()
    {
        if (d3Stats == null) d3Stats = GetComponent<PlayerStatsSimple>();
        if (d3Stats == null) return;
        d3Stats.ApplyStatsMirror(
            d3StrSync.Value, d3AgiSync.Value, d3IntSync.Value, d3VitSync.Value, d3UnallocSync.Value);
        PlayerDerivedStatsSimple derived = GetComponent<PlayerDerivedStatsSimple>();
        if (derived != null) derived.RequestRefresh();
        RefreshMeleeDamagePreviewFromStats();
    }

    void RefreshMeleeDamagePreviewFromStats()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        PlayerStatsSimple st = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
        int str = st != null ? st.strength : d.startingStr;
        damagePerHit = D3GrowthBalance.ComputeMeleePhysicalDamage(d, str);
    }

    public void ServerGrantD3StatPoints(int n)
    {
        if (!IsServer || !IsSpawned || n <= 0) return;
        d3UnallocSync.Value = Mathf.Max(0, d3UnallocSync.Value) + n;
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestD3AllocateServerRpc(int which)
    {
        if (d3UnallocSync.Value < 1)
            return;
        d3UnallocSync.Value = d3UnallocSync.Value - 1;
        switch (which)
        {
            case 0: d3StrSync.Value = d3StrSync.Value + 1; break;
            case 1: d3AgiSync.Value = d3AgiSync.Value + 1; break;
            case 2: d3IntSync.Value = d3IntSync.Value + 1; break;
            case 3: d3VitSync.Value = d3VitSync.Value + 1; break;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestSpendXpToLevel_ServerRpc()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress != null)
            progress.SpendXpForLevel(d.spendXpToLevelPerPress);
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestSpendXpToSkillPoint_ServerRpc()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        if (progress == null) progress = GetComponent<PlayerProgressSimple>();
        if (progress != null)
            progress.SpendXpForSkillUnlock(d.spendXpToSkillUnlockPerPress);
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
        float w = inv.PotionUnitWeight;
        MirrorPotionConsumeClientRpc(id, 1, w);
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
        float pw = inv.PotionUnitWeight;
        if (hpBefore > inv.HpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.HpPotion, 1, pw);
        else if (mpBefore > inv.MpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.MpPotion, 1, pw);
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
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.Shard, 1, inv.ShardUnitWeight);
        else if (mpBefore > inv.MpPotionCount)
            MirrorInventoryRemoveClientRpc(GameItemIdsSimple.MpPotion, 1, inv.PotionUnitWeight);
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
        float w = inv.PotionUnitWeight;
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

    /// <summary>
    /// <see cref="AreaPortalSimple"/>：位移必须在服务端与 <see cref="MoveAccumulatedServerRpc"/> 一致，否则纯 Client 会被 NetworkTransform 回滚。
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void TeleportViaAreaPortalServerRpc(Vector3 portalWorldPos, float useRange, Vector3 destination, float portalYTolerance)
    {
        if (!IsServer)
            return;
        Vector3 serverPos = transform.position;
        if (!AreaPortalSimple.IsInUseRangeForPortal(
                serverPos, portalWorldPos, useRange, portalYTolerance, serverSlack: true))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float dx = serverPos.x - portalWorldPos.x;
            float dz = serverPos.z - portalWorldPos.z;
            float horiz = Mathf.Sqrt(dx * dx + dz * dz);
            Debug.LogWarning(
                "[MP][Portal] 服务端未放行传送（纯 Client 上常见：服端坐标比本机晚半步）。player=" + serverPos +
                " portal=" + portalWorldPos + " horiz=" + horiz.ToString("F2") + "m yΔ=" +
                Mathf.Abs(serverPos.y - portalWorldPos.y).ToString("F2") + "m useR=" + useRange);
#endif
            return;
        }
        transform.position = destination;
    }

    [ServerRpc]
    void AttackServerRpc(float range, int damage)
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        float serverRange = Mathf.Max(0.1f, d.playerMeleeAttackRangeNet);
        if (Mathf.Abs(range - serverRange) > 0.01f)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=attack&reason=range_override&client={range:F2}&server={serverRange:F2}");
        }

        if (Time.time < meleeAttackCooldownEndTimeServer)
        {
            float remain = meleeAttackCooldownEndTimeServer - Time.time;
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=attack&reason=cooldown&remainSec={remain:F2}");
            return;
        }
        // 伤害以服端根据 STR×表 重算，不信任 Client 传入的 damage（防篡改）。
        // Must use server-authoritative transform. Client-passed positions desync from NetworkTransform
        // and OverlapSphere would miss on the host simulation (Host OK, pure Client always "miss").
        Vector3 attackerPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(attackerPos, serverRange);
        EnemyHealthSimple best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple e = hits[i].GetComponentInParent<EnemyHealthSimple>();
            if (e == null) continue;
            float distSq = (e.transform.position - attackerPos).sqrMagnitude;
            if (distSq < bestDist)
            {
                best = e;
                bestDist = distSq;
            }
        }

        if (best != null)
        {
            D3GrowthBalanceData db = D3GrowthBalance.Load();
            PlayerStatsSimple st = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
            int str = st != null ? st.strength : db.startingStr;
            int agi = st != null ? st.agility : db.startingAgi;
            float atkInterval = D3GrowthBalance.ComputeMeleeAttackInterval(db, agi);
            meleeAttackCooldownEndTimeServer = Time.time + atkInterval;
            int raw = D3GrowthBalance.ComputeMeleePhysicalDamage(db, str);
            int afterArmor = D3GrowthBalance.ApplyPhysicalDefenseToDamage(raw, best.PhysicalDefense);
            int final = D3GrowthBalance.ApplyMeleeCritAfterPhysicalArmor(db, agi, afterArmor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float critP = D3GrowthBalance.MeleeCritProbability(db, agi);
            bool crit = final > afterArmor;
            Debug.Log($"[MeleeHit][Host] str={str} agi={agi} raw={raw} def={best.PhysicalDefense} base={afterArmor} final={final} crit={(crit ? 1 : 0)} p={critP:P1}");
#endif
            best.TakeHit(final, attackerPos, gameObject);
            return;
        }

        ServerAuditLogSimple.Push(
            ServerAuditLogSimple.CategorySrvValCombatMiss,
            $"net=1&range={serverRange:F1}");
        PlayerStatsSimple stMiss = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
        int agiMiss = stMiss != null ? stMiss.agility : d.startingAgi;
        meleeAttackCooldownEndTimeServer = Time.time + D3GrowthBalance.ComputeMeleeAttackInterval(d, agiMiss);
    }

    [ServerRpc]
    void CastBurstServerRpc()
    {
        if (mp == null || burstSkill == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                "op=cast_burst&reason=missing_component");
            return;
        }
        if (Time.time < burstCooldownEndTimeServer)
        {
            float remain = burstCooldownEndTimeServer - Time.time;
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=cast_burst&reason=cooldown&remainSec={remain:F2}");
            return;
        }
        if (!mp.TrySpend(burstSkill.mpCost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=cast_burst&reason=mp_not_enough&need={burstSkill.mpCost}&have={mp.CurrentMpRounded}");
            return;
        }

        burstCooldownEndTimeServer = Time.time + Mathf.Max(0.1f, burstSkill.cooldownSeconds);
        if (mastery != null)
            mastery.RegisterBurstCast();

        int tier = unlocks != null ? unlocks.burstTier : 1;
        D3GrowthBalanceData d3b = D3GrowthBalance.Load();
        float dmgMult = mastery != null ? mastery.BurstDamageMultiplier : 1f;
        PlayerStatsSimple stBurst = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
        int intelBurst = stBurst != null ? stBurst.intellect : d3b.startingInt;
        int rolledDamage = D3GrowthBalance.ComputeBurstRolledDamage(
            d3b, burstSkill.damagePerEnemy, intelBurst, dmgMult, tier);

        Vector3 attackerPos = transform.position;
        float burstR = D3GrowthBalance.ComputeBurstOverlapRadius(d3b, burstSkill.skillRadius, tier);
        Collider[] hits = Physics.OverlapSphere(attackerPos, burstR, burstSkill.enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealthSimple enemy = hits[i].GetComponentInParent<EnemyHealthSimple>();
            if (enemy == null) continue;
            enemy.TakeSpellHit(rolledDamage, attackerPos, gameObject);
            EnemyStatusEffectsSimple status = hits[i].GetComponentInParent<EnemyStatusEffectsSimple>();
            if (status != null && burstSkill.burnDurationSeconds > 0f)
                status.ApplyBurn(burstSkill.burnDurationSeconds);
        }
    }

    [ServerRpc]
    void CastFrostServerRpc()
    {
        if (mp == null || frostSkill == null)
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                "op=cast_frost&reason=missing_component");
            return;
        }
        if (Time.time < frostCooldownEndTimeServer)
        {
            float remain = frostCooldownEndTimeServer - Time.time;
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=cast_frost&reason=cooldown&remainSec={remain:F2}");
            return;
        }
        if (!mp.TrySpend(frostSkill.mpCost))
        {
            ServerAuditLogSimple.Push(
                ServerAuditLogSimple.CategorySrvValIllegalOperation,
                $"op=cast_frost&reason=mp_not_enough&need={frostSkill.mpCost}&have={mp.CurrentMpRounded}");
            return;
        }

        frostCooldownEndTimeServer = Time.time + Mathf.Max(0.1f, frostSkill.cooldownSeconds);
        if (mastery != null)
            mastery.RegisterFrostCast();

        int tier = unlocks != null ? unlocks.frostTier : 1;
        D3GrowthBalanceData d3f = D3GrowthBalance.Load();
        float freezeSec = D3GrowthBalance.ComputeFrostFreezeDurationSeconds(
            d3f,
            frostSkill.freezeDurationSeconds,
            tier,
            mastery != null ? mastery.FrostFreezeDurationMultiplier : 1f);

        PlayerStatsSimple stFrost = d3Stats != null ? d3Stats : GetComponent<PlayerStatsSimple>();
        int intelFrost = stFrost != null ? stFrost.intellect : d3f.startingInt;
        int rolledDamage = D3GrowthBalance.ComputeFrostRolledDamage(
            d3f, frostSkill.frostDamagePerEnemy, intelFrost, tier);

        Vector3 attackerPos = transform.position;
        float frostR = D3GrowthBalance.ComputeFrostOverlapRadius(d3f, frostSkill.skillRadius, tier);
        Collider[] hits = Physics.OverlapSphere(attackerPos, frostR, frostSkill.enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (rolledDamage > 0)
            {
                EnemyHealthSimple eh = hits[i].GetComponentInParent<EnemyHealthSimple>();
                if (eh != null)
                    eh.TakeSpellHit(rolledDamage, attackerPos, gameObject);
            }

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
            if (hud.playerStats == null)
                hud.playerStats = GetComponent<PlayerStatsSimple>();
            if (hud.equipDebug == null)
                hud.equipDebug = GetComponent<PlayerEquipmentDebugSimple>();
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

    [ServerRpc(RequireOwnership = true)]
    void SubmitRoomChatServerRpc(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        ChatRoomStateSimple room = ChatRoomStateSimple.Instance;
        if (room == null)
            return;
        room.ServerAppendRoomFromPlayer(OwnerClientId, text);
    }

    /// <summary>B2·Day4：房间频道；仅 Owner 调，服端写入 <see cref="ChatRoomStateSimple"/> 并广播全端。</summary>
    public void OwnerSendRoomChat(string message)
    {
        if (!IsOwner)
            return;
        if (string.IsNullOrEmpty(message))
            return;
        string s = message;
        if (s.Length > ChatRoomStateSimple.MaxPayloadChars)
            s = s.Substring(0, ChatRoomStateSimple.MaxPayloadChars);
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        if (s.Length == 0)
            return;
        SubmitRoomChatServerRpc(s);
    }
}

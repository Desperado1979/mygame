using UnityEngine;
using Unity.Netcode;

public class EnemyHealthSimple : MonoBehaviour
{
    public int maxHp = 3;
    [Tooltip("D3: 击杀奖励经验")]
    public int xpOnKill = 8;
    [Tooltip("D3: 击杀掉落金币")]
    public int goldOnKill = 18;
    public GameObject dropPrefab;
    private int hp;
    private bool isDead = false;
    public int CurrentHp => hp;
    public int MaxHp => maxHp;

    /// <summary>Observers: HP from <see cref="MultiplayerEnemyAuthoritySimple"/> replication only.</summary>
    public void SetHpFromNetwork(int value)
    {
        hp = Mathf.Clamp(value, 0, maxHp);
        isDead = hp <= 0;
    }

    void Awake()
    {
        P1MiniBossSimple miniBoss = GetComponent<P1MiniBossSimple>();
        if (miniBoss != null)
            miniBoss.ApplyToEnemyHealth();
        hp = maxHp;
    }

    void Start()
    {
        if (GetComponent<EnemyStatusEffectsSimple>() == null)
            gameObject.AddComponent<EnemyStatusEffectsSimple>();
        if (GetComponent<EnemyTouchDamageSimple>() == null)
            gameObject.AddComponent<EnemyTouchDamageSimple>();
        if (GetComponent<EnemyChaseSimple>() == null)
            gameObject.AddComponent<EnemyChaseSimple>();
        if (GetComponent<MonsterCombatHost>() == null)
            gameObject.AddComponent<MonsterCombatHost>();
        if (GetComponent<MonsterChaseHost>() == null)
            gameObject.AddComponent<MonsterChaseHost>();
        if (GetComponent<MonsterP1A1Mark>() == null)
            gameObject.AddComponent<MonsterP1A1Mark>();
        if (GetComponent<EnemyHitFeedbackSimple>() == null)
            gameObject.AddComponent<EnemyHitFeedbackSimple>();
        if (GetComponent<EnemyFloatingHpSimple>() == null)
            gameObject.AddComponent<EnemyFloatingHpSimple>();
    }

    static T FindOnSourceHierarchy<T>(GameObject damageSource) where T : Component
    {
        if (damageSource == null) return null;
        return damageSource.GetComponent<T>()
            ?? damageSource.GetComponentInParent<T>()
            ?? damageSource.GetComponentInChildren<T>(true);
    }

    public void TakeHit(int damage, Vector3? hitFrom = null, GameObject damageSource = null)
    {
        if (isDead) return;
        NetworkObject net = GetComponent<NetworkObject>();
        if (net != null && net.IsSpawned && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        MultiplayerEnemyAuthoritySimple auth = GetComponent<MultiplayerEnemyAuthoritySimple>();
        Vector3 hitPoint = hitFrom ?? transform.position - transform.forward;
        if (auth != null)
            auth.BroadcastHitFeedback(hitPoint);
        else
        {
            EnemyHitFeedbackSimple feedback = GetComponent<EnemyHitFeedbackSimple>();
            if (feedback != null)
                feedback.Play(hitPoint);
        }

        hp -= damage;
        if (auth != null)
            auth.SetServerHpReplicated(hp);
#if UNITY_EDITOR
        Debug.Log($"{name} HP: {hp}");
#endif

        if (hp <= 0)
        {
            isDead = true;
#if UNITY_EDITOR
            Debug.Log($"{name} Dead");
#endif
            PublicObjectiveEliteSimple elite = GetComponent<PublicObjectiveEliteSimple>();
            if (elite != null)
                elite.MarkDefeated();

            MonsterP1A1Mark p1Mark = GetComponent<MonsterP1A1Mark>();
            if (p1Mark != null && P1A1QuestState.Instance != null)
                P1A1QuestState.Instance.RegisterKillFromMark(p1Mark);

            // Player health may be on a child; GetComponentInParent does not search children.
            // In MP, never use static Instance: first wallet/health in scene is often the host.
            PlayerHealthSimple killerPh = FindOnSourceHierarchy<PlayerHealthSimple>(damageSource);
            if (killerPh == null
                && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening))
                killerPh = PlayerHealthSimple.Instance;

            // Progress/wallet may live on root while PlayerHealth lives on child.
            PlayerProgressSimple killerProgress =
                FindOnSourceHierarchy<PlayerProgressSimple>(damageSource)
                ?? (killerPh != null ? killerPh.GetComponentInParent<PlayerProgressSimple>() : null);
            PlayerWalletSimple killerWallet =
                FindOnSourceHierarchy<PlayerWalletSimple>(damageSource)
                ?? (killerPh != null ? killerPh.GetComponentInParent<PlayerWalletSimple>() : null);

            if (xpOnKill > 0 && killerProgress != null)
                killerProgress.AddXp(xpOnKill);

            if (goldOnKill > 0 && killerWallet != null)
                killerWallet.AddGold(goldOnKill);

            if (dropPrefab != null)
            {
                GameObject dropObj = Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                DropItemSimple drop = dropObj.GetComponent<DropItemSimple>();
                if (drop == null)
                {
                    drop = dropObj.AddComponent<DropItemSimple>();
                    drop.pickupId = GameItemIdsSimple.Shard;
                    drop.randomTypeOnSpawn = false;
                }

                Transform ownerT = killerPh != null ? killerPh.transform : null;
                drop.SetOwner(ownerT);

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    NetworkObject dno = dropObj.GetComponent<NetworkObject>();
                    if (dno == null)
                    {
                        Debug.LogError(
                            "[MP] Drop prefab is missing NetworkObject. Add it to the drop asset and register in MultiplayerNetPrefabsRegister (Resources).");
                    }
                    else
                    {
                        dno.Spawn();
                    }
                }
            }

            NetworkObject no = GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                no.Despawn(true);
            else
                Destroy(gameObject);
        }
    }
}

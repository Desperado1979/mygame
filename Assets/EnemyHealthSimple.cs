using UnityEngine;

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

    void Start()
    {
        hp = maxHp;
        if (GetComponent<EnemyStatusEffectsSimple>() == null)
            gameObject.AddComponent<EnemyStatusEffectsSimple>();
        if (GetComponent<EnemyTouchDamageSimple>() == null)
            gameObject.AddComponent<EnemyTouchDamageSimple>();
        if (GetComponent<EnemyChaseSimple>() == null)
            gameObject.AddComponent<EnemyChaseSimple>();
        if (GetComponent<EnemyHitFeedbackSimple>() == null)
            gameObject.AddComponent<EnemyHitFeedbackSimple>();
        if (GetComponent<EnemyFloatingHpSimple>() == null)
            gameObject.AddComponent<EnemyFloatingHpSimple>();
    }

    public void TakeHit(int damage, Vector3? hitFrom = null)
    {
        if (isDead) return;

        EnemyHitFeedbackSimple feedback = GetComponent<EnemyHitFeedbackSimple>();
        if (feedback != null)
            feedback.Play(hitFrom ?? transform.position - transform.forward);

        hp -= damage;
        Debug.Log($"{name} HP: {hp}");

        if (hp <= 0)
        {
            isDead = true;
            Debug.Log($"{name} Dead");

            if (xpOnKill > 0 && PlayerProgressSimple.Instance != null)
                PlayerProgressSimple.Instance.AddXp(xpOnKill);

            if (goldOnKill > 0 && PlayerWalletSimple.Instance != null)
                PlayerWalletSimple.Instance.AddGold(goldOnKill);

            if (dropPrefab != null)
            {
                GameObject dropObj = Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                DropItemSimple drop = dropObj.GetComponent<DropItemSimple>();
                if (drop != null)
                    drop.SetOwner(PlayerHealthSimple.Instance != null ? PlayerHealthSimple.Instance.transform : null);
            }

            Destroy(gameObject);
        }
    }
}

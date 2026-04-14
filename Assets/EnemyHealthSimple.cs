using UnityEngine;

public class EnemyHealthSimple : MonoBehaviour
{
    public int maxHp = 3;
    public GameObject dropPrefab;
    private int hp;
    private bool isDead = false;

    void Start()
    {
        hp = maxHp;
        if (GetComponent<EnemyStatusEffectsSimple>() == null)
            gameObject.AddComponent<EnemyStatusEffectsSimple>();
    }

    public void TakeHit(int damage)
    {
        if (isDead) return;

        hp -= damage;
        Debug.Log($"{name} HP: {hp}");

        if (hp <= 0)
        {
            isDead = true;
            Debug.Log($"{name} Dead");

            if (dropPrefab != null)
            {
                Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}

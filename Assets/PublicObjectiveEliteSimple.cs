using UnityEngine;

/// <summary>
/// Marks an enemy as a public objective elite and boosts rewards.
/// </summary>
public class PublicObjectiveEliteSimple : MonoBehaviour
{
    public int bonusHp = 10;
    public int bonusXp = 18;
    public int bonusGold = 30;
    public string elitePrefix = "[Elite] ";

    bool applied;
    bool defeated;

    void Awake()
    {
        ApplyIfNeeded();
    }

    void Start()
    {
        ApplyIfNeeded();
    }

    public void MarkDefeated()
    {
        if (defeated)
            return;
        defeated = true;
        PublicObjectiveEventStateSimple st = PublicObjectiveEventStateSimple.Instance;
        if (st != null)
            st.ServerMarkEliteDefeated();
    }

    void ApplyIfNeeded()
    {
        if (applied)
            return;
        EnemyHealthSimple enemy = GetComponent<EnemyHealthSimple>();
        if (enemy == null)
            return;

        enemy.maxHp = Mathf.Max(1, enemy.maxHp + Mathf.Max(1, bonusHp));
        enemy.xpOnKill = Mathf.Max(0, enemy.xpOnKill + Mathf.Max(0, bonusXp));
        enemy.goldOnKill = Mathf.Max(0, enemy.goldOnKill + Mathf.Max(0, bonusGold));
        if (!string.IsNullOrEmpty(elitePrefix) && !name.StartsWith(elitePrefix))
            name = elitePrefix + name;
        applied = true;
    }
}

using UnityEngine;

/// <summary>
/// Marks an enemy as a public objective elite and boosts rewards.
/// </summary>
[DefaultExecutionOrder(50)]
public class PublicObjectiveEliteSimple : MonoBehaviour
{
    public int bonusHp = 10;
    public int bonusXp = 18;
    public int bonusGold = 30;
    public string elitePrefix = "[Elite] ";
    [Tooltip("与非精英区分：模型整体略放大")]
    public float eliteScaleMul = 1.14f;
    [Tooltip("与非精英区分：主体着色（实例材质，便于波次刷怪区分）")]
    public Color eliteTint = new Color(1f, 0.82f, 0.22f, 1f);

    bool applied;
    bool defeated;

    void Awake()
    {
        ApplyD3EliteFromBalance();
        ApplyIfNeeded();
    }

    void ApplyD3EliteFromBalance()
    {
        D3GrowthBalanceData d = D3GrowthBalance.Load();
        bonusHp = Mathf.Max(1, d.eliteBonusHp);
        bonusXp = Mathf.Max(0, d.eliteBonusXp);
        bonusGold = Mathf.Max(0, d.eliteBonusGold);
        eliteScaleMul = Mathf.Max(1.01f, d.eliteScaleMul);
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
        else
            PublicObjectiveLocalStateSimple.Instance?.MarkEliteDefeated();
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

        float m = Mathf.Max(1.01f, eliteScaleMul);
        transform.localScale *= m;

        Renderer[] rend = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rend.Length; i++)
        {
            if (rend[i] == null)
                continue;
            Material mat = rend[i].material;
            if (mat == null)
                continue;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                mat.SetColor(
                    "_BaseColor",
                    new Color(
                        Mathf.Clamp01(c.r * eliteTint.r),
                        Mathf.Clamp01(c.g * eliteTint.g),
                        Mathf.Clamp01(c.b * eliteTint.b),
                        c.a));
            }
            else
            {
                Color c = mat.color;
                mat.color = new Color(
                    Mathf.Clamp01(c.r * eliteTint.r),
                    Mathf.Clamp01(c.g * eliteTint.g),
                    Mathf.Clamp01(c.b * eliteTint.b),
                    c.a);
            }
        }
        applied = true;
    }
}

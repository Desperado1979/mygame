using UnityEngine;

/// <summary>P1-A-3：Play 时在野外点生成一只小 Boss（<c>Resources/P1A/Enemy-MiniBoss</c>），避免与城内安全区重叠。</summary>
public class P1MiniBossSceneBootstrap : MonoBehaviour
{
    [Tooltip("若留空，运行时从 Resources/P1A/Enemy-MiniBoss 加载。")]
    public GameObject miniBossPrefab;

    public Vector3 spawnPosition = new Vector3(24f, 0.5f, 16f);

    void Start()
    {
        if (miniBossPrefab == null)
            miniBossPrefab = Resources.Load<GameObject>("P1A/Enemy-MiniBoss");
        if (miniBossPrefab == null)
            return;

        Instantiate(miniBossPrefab, spawnPosition, Quaternion.identity);
    }
}

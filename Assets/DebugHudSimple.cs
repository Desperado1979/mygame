using UnityEngine;
using UnityEngine.UI;

public class DebugHudSimple : MonoBehaviour
{
    public Transform player;
    public Text hudText;

    void Update()
    {
        if (hudText == null) return;

        int enemyCount = 0;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (!obj.activeInHierarchy) continue;
            if (obj.CompareTag("Enemy") || obj.name.Contains("Enemy"))
            {
                enemyCount++;
            }
        }

        string pos = player == null
            ? "N/A"
            : $"{player.position.x:F1},{player.position.z:F1}";

        hudText.text = $"E:{enemyCount}  P:{pos}";
    }
}

using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Single place to build the Host/Client <see cref="NetworkManager"/> player body.
/// Prefer the baked <see cref="ResourcesPath"/> prefab; the runtime template path exists only for dev fallback.
/// </summary>
public static class NetPlayerHostFactory
{
    public const string ResourcesPath = "Netcode/NetPlayerHost";

    /// <summary>Far from gameplay so a DDOL “template” instance never looks like a second local player at origin.</summary>
    public static readonly Vector3 RuntimeTemplatePosition = new Vector3(10000f, 1f, 10000f);

    /// <summary>Builds the same hierarchy/components as legacy runtime bootstrap, without touching <see cref="NetworkManager"/>.</summary>
    public static GameObject CreateRuntimePlayerTemplate(
        Vector3? worldPosition = null,
        Transform parent = null,
        bool hideInHierarchy = false,
        string objectName = "NetPlayerRuntimeTemplate")
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = string.IsNullOrEmpty(objectName) ? "NetPlayerRuntimeTemplate" : objectName;
        player.layer = 0;
        if (parent != null)
            player.transform.SetParent(parent, false);
        player.transform.SetPositionAndRotation(
            worldPosition ?? RuntimeTemplatePosition,
            Quaternion.identity);

        MeshRenderer capRenderer = player.GetComponent<MeshRenderer>();
        if (capRenderer != null)
            capRenderer.enabled = false;

        BuildSimpleHumanoidVisual(player.transform);

        if (player.GetComponent<NetworkObject>() == null)
            player.AddComponent<NetworkObject>();

        NetworkTransform nt = player.GetComponent<NetworkTransform>();
        if (nt == null)
            nt = player.AddComponent<NetworkTransform>();
        nt.Interpolate = true;

        MultiplayerPlayerSimple mps = player.GetComponent<MultiplayerPlayerSimple>();
        if (mps == null)
            mps = player.AddComponent<MultiplayerPlayerSimple>();
        mps.moveSpeed = 6f;
        mps.attackRange = 2.4f;
        mps.damagePerHit = 1;

        if (player.GetComponent<PlayerHotkeysSimple>() == null)
            player.AddComponent<PlayerHotkeysSimple>();
        if (player.GetComponent<PlayerSkillUnlockSimple>() == null)
            player.AddComponent<PlayerSkillUnlockSimple>();
        if (player.GetComponent<PlayerSkillMasterySimple>() == null)
            player.AddComponent<PlayerSkillMasterySimple>();
        if (player.GetComponent<PlayerSkillBurstSimple>() == null)
            player.AddComponent<PlayerSkillBurstSimple>();
        if (player.GetComponent<PlayerSkillFrostSimple>() == null)
            player.AddComponent<PlayerSkillFrostSimple>();

        if (player.GetComponent<PlayerHealthSimple>() == null)
            player.AddComponent<PlayerHealthSimple>();
        if (player.GetComponent<PlayerMpSimple>() == null)
            player.AddComponent<PlayerMpSimple>();
        if (player.GetComponent<PlayerProgressSimple>() == null)
            player.AddComponent<PlayerProgressSimple>();
        if (player.GetComponent<PlayerWalletSimple>() == null)
            player.AddComponent<PlayerWalletSimple>();
        if (player.GetComponent<PlayerInventorySimple>() == null)
            player.AddComponent<PlayerInventorySimple>();
        if (player.GetComponent<PlayerPickupSimple>() == null)
            player.AddComponent<PlayerPickupSimple>();
        if (player.GetComponent<PlayerEnhanceSimple>() == null)
            player.AddComponent<PlayerEnhanceSimple>();
        if (player.GetComponent<PlayerBankSimple>() == null)
            player.AddComponent<PlayerBankSimple>();
        if (player.GetComponent<PlayerSaveSimple>() == null)
            player.AddComponent<PlayerSaveSimple>();
        if (player.GetComponent<PlayerFloatingBarsSimple>() == null)
            player.AddComponent<PlayerFloatingBarsSimple>();

        ConfigureRuntimeSkillEnemyLayer(player);

        if (hideInHierarchy)
            player.hideFlags = HideFlags.HideInHierarchy;

        return player;
    }

    static void ConfigureRuntimeSkillEnemyLayer(GameObject player)
    {
        if (player == null)
            return;
        int enemyMask = LayerMask.GetMask("Enemy");
        if (enemyMask == 0)
            enemyMask = 1 << 6;

        PlayerSkillBurstSimple burst = player.GetComponent<PlayerSkillBurstSimple>();
        if (burst != null)
            burst.enemyLayer = enemyMask;
        PlayerSkillFrostSimple frost = player.GetComponent<PlayerSkillFrostSimple>();
        if (frost != null)
            frost.enemyLayer = enemyMask;
    }

    static void BuildSimpleHumanoidVisual(Transform root)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "BodyVisual";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.9f, 0.35f);
        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) UnityEngine.Object.Destroy(bodyCol);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "HeadVisual";
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        head.transform.localScale = Vector3.one * 0.34f;
        Collider headCol = head.GetComponent<Collider>();
        if (headCol != null) UnityEngine.Object.Destroy(headCol);

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "ArmMarker";
        arm.transform.SetParent(root, false);
        arm.transform.localPosition = new Vector3(0.28f, 1.02f, 0f);
        arm.transform.localScale = new Vector3(0.14f, 0.45f, 0.14f);
        Collider armCol = arm.GetComponent<Collider>();
        if (armCol != null) UnityEngine.Object.Destroy(armCol);

        SetColor(body, new Color(0.20f, 0.45f, 0.90f, 1f));
        SetColor(head, new Color(0.95f, 0.80f, 0.68f, 1f));
        SetColor(arm, new Color(0.90f, 0.25f, 0.20f, 1f));
    }

    static void SetColor(GameObject go, Color c)
    {
        if (go == null) return;
        Renderer r = go.GetComponent<Renderer>();
        if (r == null || r.sharedMaterial == null) return;
        Material m = new Material(r.sharedMaterial) { color = c };
        r.material = m;
    }
}

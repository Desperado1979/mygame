using UnityEngine;

public class DropItemSimple : MonoBehaviour
{
    public float rotateSpeed = 90f;
    [Tooltip("D3: 拾取时计入背包负重")]
    public float pickupWeight = 2.5f;
    public string pickupId = GameItemIdsSimple.Shard;
    [Tooltip("本次拾取增加数量")]
    public int pickupCount = 1;
    [Header("Auto random type")]
    public bool randomTypeOnSpawn = true;
    public int weightPotion = 40;
    public int weightMana = 35;
    public int weightShard = 25;

    [Header("Drop ownership protection")]
    public Transform owner;
    public int ownerTeamId = -1;
    public float ownerProtectUntil;
    public float ownerProtectSeconds = 3f;
    [Header("Lifetime")]
    public float lifetimeSeconds = 20f;
    [Header("Magnet pickup")]
    public bool enableMagnet = false;
    public float magnetRange = 3f;
    public float magnetSpeed = 7f;
    float destroyAtTime;
    MeshRenderer meshRenderer;
    Color baseColor = Color.white;
    TextMesh labelMesh;

    void Update()
    {
        ApplyTypeColor();
        UpdateLabel();
        UpdateMagnet();
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        if (Time.time >= destroyAtTime)
            Destroy(gameObject);
    }

    void OnEnable()
    {
        if (randomTypeOnSpawn)
            AssignRandomType();
        destroyAtTime = Time.time + Mathf.Max(1f, lifetimeSeconds);
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
            baseColor = meshRenderer.material.color;
        EnsureLabel();
        UpdateLabelText();
    }

    public void SetOwner(Transform ownerTransform)
    {
        owner = ownerTransform;
        TeamIdSimple team = owner != null ? owner.GetComponent<TeamIdSimple>() : null;
        ownerTeamId = team != null ? team.teamId : -1;
        ownerProtectUntil = Time.time + Mathf.Max(0f, ownerProtectSeconds);
    }

    public bool CanBePickedBy(Transform picker)
    {
        if (owner == null || picker == owner)
            return true;
        TeamIdSimple pickerTeam = picker != null ? picker.GetComponent<TeamIdSimple>() : null;
        if (ownerTeamId >= 0 && pickerTeam != null && pickerTeam.teamId == ownerTeamId)
            return true;
        return Time.time >= ownerProtectUntil;
    }

    public float LifetimeRemaining => Mathf.Max(0f, destroyAtTime - Time.time);

    void ApplyTypeColor()
    {
        if (meshRenderer == null || meshRenderer.material == null)
            return;

        Color target = baseColor;
        if (pickupId == GameItemIdsSimple.HpPotion)
            target = new Color(1f, 0.2f, 0.2f); // red
        else if (pickupId == GameItemIdsSimple.MpPotion)
            target = new Color(0.2f, 0.45f, 1f); // blue
        else if (pickupId == GameItemIdsSimple.Shard)
            target = new Color(1f, 0.84f, 0.25f); // gold-like
        else
            target = new Color(0.85f, 0.85f, 0.85f); // neutral

        meshRenderer.material.color = Color.Lerp(meshRenderer.material.color, target, Time.deltaTime * 10f);
    }

    void AssignRandomType()
    {
        int wHp = Mathf.Max(0, weightPotion);
        int wMp = Mathf.Max(0, weightMana);
        int wShard = Mathf.Max(0, weightShard);
        int sum = wHp + wMp + wShard;
        if (sum <= 0)
        {
            pickupId = GameItemIdsSimple.Shard;
            return;
        }

        int roll = Random.Range(0, sum);
        if (roll < wHp) pickupId = GameItemIdsSimple.HpPotion;
        else if (roll < wHp + wMp) pickupId = GameItemIdsSimple.MpPotion;
        else pickupId = GameItemIdsSimple.Shard;
    }

    void EnsureLabel()
    {
        Transform child = transform.Find("DropLabel");
        if (child == null)
        {
            GameObject go = new GameObject("DropLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelMesh = go.AddComponent<TextMesh>();
            labelMesh.fontSize = 36;
            labelMesh.characterSize = 0.05f;
            labelMesh.anchor = TextAnchor.MiddleCenter;
            labelMesh.alignment = TextAlignment.Center;
        }
        else
        {
            labelMesh = child.GetComponent<TextMesh>();
            if (labelMesh == null) labelMesh = child.gameObject.AddComponent<TextMesh>();
        }
    }

    void UpdateLabel()
    {
        if (labelMesh == null)
            return;
        Camera cam = Camera.main;
        if (cam != null)
            labelMesh.transform.forward = cam.transform.forward;
    }

    void UpdateLabelText()
    {
        if (labelMesh == null)
            return;

        string txt;
        Color c;
        if (pickupId == GameItemIdsSimple.HpPotion)
        {
            txt = "HP药";
            c = new Color(1f, 0.3f, 0.3f);
        }
        else if (pickupId == GameItemIdsSimple.MpPotion)
        {
            txt = "MP药";
            c = new Color(0.3f, 0.55f, 1f);
        }
        else
        {
            txt = "材料";
            c = new Color(1f, 0.88f, 0.3f);
        }

        labelMesh.text = txt;
        labelMesh.color = c;
    }

    void UpdateMagnet()
    {
        if (!enableMagnet)
            return;

        PlayerHealthSimple p = PlayerHealthSimple.Instance;
        if (p == null) return;
        Transform player = p.transform;

        if (!CanBePickedBy(player))
            return;

        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        float dist = delta.magnitude;
        if (dist > magnetRange || dist < 0.01f)
            return;

        Vector3 step = delta.normalized * magnetSpeed * Time.deltaTime;
        if (step.magnitude > dist) step = delta;
        transform.position += step;
    }

    void OnValidate()
    {
        pickupId = GameItemIdsSimple.Normalize(pickupId);
        if (pickupCount < 1) pickupCount = 1;
        if (pickupWeight <= 0f) pickupWeight = 1f;
        if (ownerProtectSeconds < 0f) ownerProtectSeconds = 0f;
        if (lifetimeSeconds < 1f) lifetimeSeconds = 1f;
        if (weightPotion < 0) weightPotion = 0;
        if (weightMana < 0) weightMana = 0;
        if (weightShard < 0) weightShard = 0;
        if (magnetRange < 0f) magnetRange = 0f;
        if (magnetSpeed < 0f) magnetSpeed = 0f;
    }
}

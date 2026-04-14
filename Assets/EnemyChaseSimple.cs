using UnityEngine;

/// <summary>Simple enemy chase: detect player, chase, then leash back to spawn.</summary>
public class EnemyChaseSimple : MonoBehaviour
{
    public float moveSpeed = 2.8f;
    public float detectRange = 9f;
    public float stopRange = 1.1f;
    public float leashRangeFromSpawn = 14f;

    Vector3 spawnPos;
    Transform player;

    void Start()
    {
        spawnPos = transform.position;
    }

    void Update()
    {
        if (player == null && PlayerHealthSimple.Instance != null)
            player = PlayerHealthSimple.Instance.transform;
        if (player == null)
            return;

        // In safe-zone, enemy returns to spawn instead of pushing in.
        if (SafeZoneSimple.IsInside(player.position))
        {
            MoveToward(spawnPos);
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        float distSpawnToPlayer = Vector3.Distance(spawnPos, player.position);

        bool canChase = distToPlayer <= detectRange && distSpawnToPlayer <= leashRangeFromSpawn;
        if (canChase)
        {
            if (distToPlayer > stopRange)
                MoveToward(player.position);
            return;
        }

        MoveToward(spawnPos);
    }

    void MoveToward(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return;

        Vector3 step = flat.normalized * moveSpeed * Time.deltaTime;
        if (step.sqrMagnitude > flat.sqrMagnitude)
            step = flat;

        transform.position += step;
        transform.forward = flat.normalized;
    }
}

using UnityEngine;

public class PlayerMoveSimple : MonoBehaviour
{
    public float moveSpeed = 5f;
    [Header("Dash")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashMultiplier = 2.2f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.85f;
    float dashEndTime;
    float dashReadyTime;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A D
        float v = Input.GetAxisRaw("Vertical");   // W S

        Vector3 dir = BuildCameraRelativeDir(h, v);
        TryStartDash(dir);

        bool isDashing = Time.time < dashEndTime;
        float speed = moveSpeed * (isDashing ? Mathf.Max(1f, dashMultiplier) : 1f);
        transform.position += dir * speed * Time.deltaTime;

        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }
    }

    Vector3 BuildCameraRelativeDir(float h, float v)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return new Vector3(h, 0f, v).normalized;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward = camForward.sqrMagnitude > 1e-6f ? camForward.normalized : Vector3.forward;

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight = camRight.sqrMagnitude > 1e-6f ? camRight.normalized : Vector3.right;

        return (camRight * h + camForward * v).normalized;
    }

    void TryStartDash(Vector3 dir)
    {
        if (dir == Vector3.zero)
            return;
        if (!Input.GetKeyDown(dashKey))
            return;
        if (Time.time < dashReadyTime)
            return;

        dashEndTime = Time.time + Mathf.Max(0.02f, dashDuration);
        dashReadyTime = Time.time + Mathf.Max(dashDuration, dashCooldown);
    }
}
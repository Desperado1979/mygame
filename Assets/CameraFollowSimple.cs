using UnityEngine;

public class CameraFollowSimple : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 8f, -10f);
    public float smooth = 8f;
    [Header("Orbit")]
    public bool enableOrbit = true;
    public int orbitMouseButton = 1; // RMB
    public float orbitSensitivity = 3f;
    [Tooltip("开启后：鼠标上移=镜头下压（飞行/编辑器常见）；关闭后：鼠标上移=镜头上抬（多数第三人称习惯）。")]
    public bool invertY = false;
    public float minPitch = -15f;
    public float maxPitch = 75f;
    [Header("Zoom")]
    public bool enableZoom = true;
    public float zoomSensitivity = 3f;
    public float minDistance = 5f;
    public float maxDistance = 18f;

    Vector3 _orbitOffset;

    void Start()
    {
        _orbitOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (enableZoom)
            HandleZoom();

        if (enableOrbit && Input.GetMouseButton(orbitMouseButton))
        {
            float mx = Input.GetAxis("Mouse X") * orbitSensitivity;
            float my = Input.GetAxis("Mouse Y") * orbitSensitivity;
            float pitchDelta = invertY ? my : -my;

            _orbitOffset = Quaternion.AngleAxis(mx, Vector3.up) * _orbitOffset;
            Vector3 right = Vector3.Cross(Vector3.up, _orbitOffset).normalized;
            if (right.sqrMagnitude > 1e-6f)
                _orbitOffset = Quaternion.AngleAxis(pitchDelta, right) * _orbitOffset;
            ClampPitch();
        }

        Vector3 desired = target.position + _orbitOffset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f)
            return;
        float dist = _orbitOffset.magnitude;
        if (dist < 1e-6f)
            dist = Mathf.Max(minDistance, 0.1f);
        dist -= scroll * zoomSensitivity;
        dist = Mathf.Clamp(dist, Mathf.Max(0.1f, minDistance), Mathf.Max(minDistance, maxDistance));
        _orbitOffset = _orbitOffset.normalized * dist;
    }

    void ClampPitch()
    {
        float dist = _orbitOffset.magnitude;
        if (dist < 1e-6f)
            return;

        float yaw = Mathf.Atan2(_orbitOffset.x, _orbitOffset.z);
        float pitch = Mathf.Asin(Mathf.Clamp(_orbitOffset.y / dist, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float pitchRad = pitch * Mathf.Deg2Rad;
        float horizontal = Mathf.Cos(pitchRad) * dist;
        _orbitOffset = new Vector3(
            Mathf.Sin(yaw) * horizontal,
            Mathf.Sin(pitchRad) * dist,
            Mathf.Cos(yaw) * horizontal
        );
    }
}
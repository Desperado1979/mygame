using UnityEngine;

public class CameraFollowSimple : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 8f, -10f);
    public float smooth = 8f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
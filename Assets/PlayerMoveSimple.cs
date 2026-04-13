using UnityEngine;

public class PlayerMoveSimple : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A D
        float v = Input.GetAxisRaw("Vertical");   // W S

        Vector3 dir = new Vector3(h, 0f, v).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }
    }
}
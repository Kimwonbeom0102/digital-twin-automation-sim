using UnityEngine;

public class ObserverCameraController : MonoBehaviour
{
    public Transform cameraTransform;
    public Rigidbody rb;
    public float moveSpeed = 6f;
    public float lookSpeed = 2f;
    public float boostMultiplier = 2f;

    float rotX;
    float rotY;

    void Update()
    {
        if (!enabled) return; // ⭐ 중요

        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        rotX += Input.GetAxis("Mouse X") * lookSpeed * 100f * Time.deltaTime;
        rotY -= Input.GetAxis("Mouse Y") * lookSpeed * 100f * Time.deltaTime;

        rotY = Mathf.Clamp(rotY, -80f, 80f);

        // 좌우 회전은 Root
        transform.rotation = Quaternion.Euler(0f, rotX, 0f);

        // 상하 회전은 Camera
        cameraTransform.localRotation = Quaternion.Euler(rotY, 0f, 0f);
    }

    void HandleMove()
    {
        float speed = Input.GetKey(KeyCode.LeftShift)
            ? moveSpeed * boostMultiplier
            : moveSpeed;

        Vector3 moveDir =
            transform.forward * Input.GetAxis("Vertical") +
            transform.right * Input.GetAxis("Horizontal");

        moveDir.Normalize();

        rb.MovePosition(rb.position + moveDir * speed * Time.deltaTime);
    }
}

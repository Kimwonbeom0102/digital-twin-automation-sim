using UnityEngine;

public class ObserverCameraController : MonoBehaviour
{
    public Transform cameraTransform;
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

        cameraTransform.rotation = Quaternion.Euler(rotY, rotX, 0f);
    }

    void HandleMove()
    {
        float speed = Input.GetKey(KeyCode.LeftShift)
            ? moveSpeed * boostMultiplier
            : moveSpeed;

        Vector3 dir =
            cameraTransform.forward * Input.GetAxis("Vertical") +
            cameraTransform.right * Input.GetAxis("Horizontal");

        cameraTransform.position += dir * speed * Time.deltaTime;
    }
}

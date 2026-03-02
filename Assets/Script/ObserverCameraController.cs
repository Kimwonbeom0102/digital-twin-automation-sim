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

    void Awake()
    {
        // 물리 안정화
        rb.useGravity = false;
        // rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void OnEnable()
    {
        // 현재 회전을 기준으로 내부 값 동기화
        rotX = transform.eulerAngles.y;
        rotY = cameraTransform.localEulerAngles.x;

        if (rotY > 180f)
            rotY -= 360f;
    }

    void Update()
    {
        if (!enabled) return;

        HandleLook();
    }

    void FixedUpdate()
    {
        if (!enabled) return;

        HandleMove();
    }

    void HandleLook()
    {
        rotX += Input.GetAxis("Mouse X") * lookSpeed * 100f * Time.deltaTime;
        rotY -= Input.GetAxis("Mouse Y") * lookSpeed * 100f * Time.deltaTime;

        rotY = Mathf.Clamp(rotY, -80f, 80f);

        // 좌우 회전 (Root)
        transform.rotation = Quaternion.Euler(0f, rotX, 0f);

        // 상하 회전 (Camera)
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

        Vector3 targetPos = rb.position + moveDir * speed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    public void StopMotion()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
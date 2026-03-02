using UnityEngine;

public enum CameraMode
{
    Fixed,
    Observer
}

public enum InputMode
{
    Camera,
    UI
}

public class CameraRigController : MonoBehaviour
{
    private bool isObserverMode = false;
    [SerializeField] private GameObject testPanel;
    public InputMode CurrentInputMode;
    public Transform fixedViewPoint;
    public Transform observerStartPoint;
    public ObserverCameraController observerController;

    CameraMode currentMode = CameraMode.Fixed;

    void Start()
    {
        isObserverMode = false;
        SetMode(CameraMode.Fixed);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(CurrentInputMode == InputMode.UI)
                return;
            ToggleMode();
        }
    }

    void ToggleMode()
    {
        SetMode(
            currentMode == CameraMode.Fixed
                ? CameraMode.Observer
                : CameraMode.Fixed
        );
    }

    void SetMode(CameraMode mode)
    {
        currentMode = mode;

        // 이동 주체는 ObserverRoot
        Transform root = observerController.transform;

        if (mode == CameraMode.Fixed)
        {
            observerController.enabled = false;

            // Rigidbody 완전 정지
            observerController.rb.velocity = Vector3.zero;
            observerController.rb.angularVelocity = Vector3.zero;

            // Root 위치 이동
            root.position = fixedViewPoint.position;
            root.rotation = fixedViewPoint.rotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isObserverMode = false;

            testPanel.SetActive(true);
        }
        else
        {
            // Root 위치 이동
            root.position = observerStartPoint.position;
            root.rotation = observerStartPoint.rotation;

            observerController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isObserverMode = true;

            testPanel.SetActive(false);
        }
    
    }
}

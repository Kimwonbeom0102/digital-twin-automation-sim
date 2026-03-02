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
    [SerializeField] private GameObject testPanel;

    public InputMode CurrentInputMode;

    public Transform fixedViewPoint;
    public Transform observerStartPoint;
    public ObserverCameraController observerController;

    CameraMode currentMode = CameraMode.Fixed;

    void Start()
    {
        SetMode(CameraMode.Fixed);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (CurrentInputMode == InputMode.UI)
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

        Transform root = observerController.transform;

        if (mode == CameraMode.Fixed)
        {
            observerController.enabled = false;

            observerController.StopMotion();

            root.position = fixedViewPoint.position;
            root.rotation = fixedViewPoint.rotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            testPanel.SetActive(true);
        }
        else
        {
            observerController.StopMotion();

            root.position = observerStartPoint.position;
            root.rotation = observerStartPoint.rotation;

            observerController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            testPanel.SetActive(false);
        }
    }
}
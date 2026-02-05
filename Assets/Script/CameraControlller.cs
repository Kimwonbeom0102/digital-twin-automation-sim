using UnityEngine;

public enum CameraMode
{
    Fixed,
    Observer
}

public class CameraRigController : MonoBehaviour
{
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

        if (mode == CameraMode.Fixed)
        {
            observerController.enabled = false;

            Transform cam = observerController.cameraTransform;
            cam.position = fixedViewPoint.position;
            cam.rotation = fixedViewPoint.rotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Transform cam = observerController.cameraTransform;
            cam.position = observerStartPoint.position;
            cam.rotation = observerStartPoint.rotation;

            observerController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

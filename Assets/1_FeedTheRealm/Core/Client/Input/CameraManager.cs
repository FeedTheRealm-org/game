using Unity.Cinemachine;
using UnityEngine;

namespace FTR.Core.Client.Input;

public class CameraManager
{
    private CinemachineCamera camera;
    private CinemachineInputAxisController inputController;

    private bool isCameraBlocked;

    public bool IsCameraBlocked => isCameraBlocked;

    public CameraManager()
    {
        camera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
        if (camera != null)
            inputController = camera.GetComponent<CinemachineInputAxisController>();
    }

    /// <summary>
    /// ToggleCameraBlock changes the camera to be movable if blockStatus = false,
    /// and changes to fixed camera if blockStatus = true
    /// </summary>
    public void ToggleCameraBlock(bool blockStatus)
    {
        isCameraBlocked = blockStatus;

        if (inputController != null)
            inputController.enabled = !blockStatus;
    }

    public void TrackTarget(Transform target)
    {
        if (camera == null)
            return;
        camera.Target.TrackingTarget = target;
    }
}

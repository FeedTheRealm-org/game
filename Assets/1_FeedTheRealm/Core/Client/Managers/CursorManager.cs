using UnityEngine;

namespace FTR.Core.Client.Managers;

public class CursorManager
{
    private CameraManager cameraManager;

    private bool isCursorBlocked = false;

    public bool IsCursorBlocked => isCursorBlocked;

    public CursorManager(CameraManager cameraManager)
    {
        this.cameraManager = cameraManager;
    }

    /// <summary>
    /// ToggleCursorBlock changes the cursor to visible and movable if blockStatus = false,
    /// and changes to invisible if blockStatus = true
    /// </summary>
    public void ToggleCursorBlock(bool blockStatus)
    {
        isCursorBlocked = blockStatus;
        UnityEngine.Cursor.lockState = blockStatus ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !blockStatus;

        cameraManager.ToggleCameraBlock(!blockStatus);
    }
}

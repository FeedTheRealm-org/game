using FTR.Core.Client.Managers;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using VContainer;

public class UseController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    private MenuManager menuManager;

    private NetworkAdapter networkAdapter;

    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;
    }

    public void Use()
    {
        if (!isInitialized || menuManager.AreAnyMenusOpen())
            return;

        ActionCommandDTO command = new() { Type = ActionType.Use, Direction = GetUseDirection() };

        networkAdapter.DispatchAction(command);
    }

    private Vector3 GetUseDirection()
    {
        var cam = Camera.main;

        if (cam == null)
            return Vector3.forward;

        Vector3 dir = animator.CurrentFacing switch
        {
            FacingDirection.Left => -cam.transform.right,
            FacingDirection.Right => cam.transform.right,
            FacingDirection.Back => cam.transform.forward,
            FacingDirection.Front => -cam.transform.forward,
            _ => cam.transform.forward,
        };

        dir.y = 0f;

        return dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
    }
}

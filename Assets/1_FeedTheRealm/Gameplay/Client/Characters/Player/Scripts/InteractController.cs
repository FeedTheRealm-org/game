using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using VContainer;

/// <summary>
/// Client-side controller that dispatches interact-related action commands to the server.
/// Analogous to UseController: wraps NetworkAdapter.DispatchAction calls.
///
/// </summary>
public class InteractController : MonoBehaviour
{
    [Inject]
    PlayerInputReader inputReader;
    private NetworkAdapter networkAdapter;
    private bool isInitialized;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        this.networkAdapter = networkAdapter;
        isInitialized = true;
        inputReader.InteractEvent += OnInteract;
        Debug.Log(
            $"[InteractController] Initialized. NetworkAdapter set: {networkAdapter != null}"
        );
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.InteractEvent -= OnInteract;
    }

    /// <summary>
    /// Signals the server that the player wants to interact.
    /// The server resolves the target NPC by proximity.
    /// </summary>
    public void OnInteract()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[InteractController] OnInteract called before Initialize.");
            return;
        }

        var dto = new ActionCommandDTO { Type = ActionType.Interact };
        Debug.Log(
            $"[InteractController] OnInteract -> Dispatching ActionCommandDTO Type={dto.Type}"
        );
        networkAdapter.DispatchAction(dto);
    }

    /// <summary>
    /// Dispatches a DialogNext command. Called from CharacterStateMachine.OnDialogNext().
    /// The server advances the dialog index or closes the sequence.
    /// </summary>
    public void OnDialogNext()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[InteractController] OnDialogNext called before Initialize.");
            return;
        }

        var dto = new ActionCommandDTO { Type = ActionType.DialogNext };
        Debug.Log(
            $"[InteractController] OnDialogNext -> Dispatching ActionCommandDTO Type={dto.Type}"
        );
        networkAdapter.DispatchAction(dto);
    }
}

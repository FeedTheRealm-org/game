using Game.Core.Exceptions;
using Mirror;
using UnityEngine;

/// <summary>
/// Connects player input to the movement component.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    public PlayerInputReader inputReader;

    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private CharacterStateMachine characterStateMachine;

    [SerializeField]
    private Logging.Logger logger;

    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        if (inputReader == null)
            throw new MissingFieldException(nameof(inputReader), nameof(PlayerController));
        if (playerPrefab == null)
            throw new MissingFieldException(nameof(playerPrefab), nameof(PlayerController));
        if (characterStateMachine == null)
            throw new MissingFieldException(
                nameof(characterStateMachine),
                nameof(PlayerController)
            );

        ToggleRegisterInputs(true);
    }

    public override void OnStopAuthority()
    {
        ToggleRegisterInputs(false);
    }

    public void OnDestroy()
    {
        ToggleRegisterInputs(false);
    }

    private void ToggleRegisterInputs(bool register)
    {
        logger.Log($"PlayerController ToggleRegisterInputs: {register}", this);
        if (register)
        {
            inputReader.DashEvent += OnDashInput;
            inputReader.MoveEvent += OnMoveInput;
            inputReader.AttackEvent += OnAttackInput;
            inputReader.InteractEvent += OnInteractInput;
            return;
        }

        inputReader.DashEvent -= OnDashInput;
        inputReader.MoveEvent -= OnMoveInput;
        inputReader.AttackEvent -= OnAttackInput;
        inputReader.InteractEvent -= OnInteractInput;
    }

    private void OnAttackInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnAttack();
    }

    private void OnMoveInput(Vector2 vec)
    {
        // if (Cursor.visible)
        // {
        //     return;
        // }
        // TODO: remove these if and make the state machine know via events when it can execute inputs or not (e.g. Hud manager events).

        logger.Log($"PlayerController OnMoveInput: {vec}", this);
        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnDash();
    }

    private void OnInteractInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnInteract();
    }
}

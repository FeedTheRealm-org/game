using Game.Core.Exceptions;
using UnityEngine;

namespace Game.Gameplay.UnityActors.Movement;

public class MovementView : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;

    private void Awake()
    {
        if (rb == null)
            throw new MissingFieldException(nameof(rb), nameof(MovementView));
    }

    /// <summary>
    /// Moves the Rigidbody to the specified position.
    /// </summary>
    public void MoveToPosition(Vector3 nextPosition)
    {
        rb.MovePosition(nextPosition);
    }
}

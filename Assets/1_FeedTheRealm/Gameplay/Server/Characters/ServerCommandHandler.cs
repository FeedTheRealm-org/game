using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerCommandHandler : MonoBehaviour, ICommandable
    {
        private MovementSystem movementSystem;
        private UseSystem useSystem;

        public void Initialize(MovementSystem movementSystem, UseSystem useSystem)
        {
            this.movementSystem = movementSystem;
            this.useSystem = useSystem;
        }

        public void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public void OnDash(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnDash(direction);
        }

        public void OnDash(IEventCollectable ec) { }

        public void OnInteract(IEventCollectable ec) { }

        public void OnEquip(IEventCollectable ec) { }

        public void OnDrop(IEventCollectable ec) { }

        public void OnPurchase(IEventCollectable ec) { }

        public void OnQuestAccepted(IEventCollectable ec) { }
    }
}

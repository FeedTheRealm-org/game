using UnityEngine;

namespace FTR.Core.Common.Systems.Status
{
    public interface IGroundable
    {
        bool IsGrounded { get; set; }
        bool IsOnSlope { get; set; }
        Vector3 GroundNormal { get; set; }
        bool IsGroundCheckEnabled { get; set; }
        float GetGroundCheckDistance();
    }
}

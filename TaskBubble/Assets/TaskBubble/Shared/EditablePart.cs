using UnityEngine;

namespace TaskBubble.Shared
{
    public enum LocalRotationAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    [DisallowMultipleComponent]
    public sealed class EditablePart : MonoBehaviour
    {
        [SerializeField, Tooltip("A unique, permanent ID shared by both clients, e.g. gripper.connector.")]
        private string partId = "";

        [SerializeField] private LocalRotationAxis rotationAxis = LocalRotationAxis.Z;
        [SerializeField] private float minimumAngleDegrees = -45f;
        [SerializeField] private float maximumAngleDegrees = 45f;

        public string PartId => partId;
        public LocalRotationAxis RotationAxis => rotationAxis;
        public float MinimumAngleDegrees => minimumAngleDegrees;
        public float MaximumAngleDegrees => maximumAngleDegrees;

        // Zero is the unchanged pose at the start of a delegation.
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(partId) && partId == partId.Trim() &&
            rotationAxis >= LocalRotationAxis.X && rotationAxis <= LocalRotationAxis.Z &&
            IsFinite(minimumAngleDegrees) && IsFinite(maximumAngleDegrees) &&
            minimumAngleDegrees <= 0f && maximumAngleDegrees >= 0f;

        public bool IsAngleAllowed(float angleDegrees)
        {
            return IsConfigured && IsFinite(angleDegrees) &&
                angleDegrees >= minimumAngleDegrees && angleDegrees <= maximumAngleDegrees;
        }

        // Pure calculation: this never changes the part or creates a ghost.
        public bool TryGetProposalLocalRotation(
            Quaternion startingLocalRotation, float angleDegrees, out Quaternion proposalLocalRotation)
        {
            proposalLocalRotation = startingLocalRotation;
            if (!IsAngleAllowed(angleDegrees) || !IsFinite(startingLocalRotation.x) ||
                !IsFinite(startingLocalRotation.y) || !IsFinite(startingLocalRotation.z) ||
                !IsFinite(startingLocalRotation.w))
            {
                return false;
            }

            float magnitudeSquared = Quaternion.Dot(startingLocalRotation, startingLocalRotation);
            if (!IsFinite(magnitudeSquared) || magnitudeSquared < 0.000001f)
            {
                return false;
            }

            Vector3 axis = rotationAxis == LocalRotationAxis.X ? Vector3.right :
                rotationAxis == LocalRotationAxis.Y ? Vector3.up : Vector3.forward;

            proposalLocalRotation = startingLocalRotation.normalized * Quaternion.AngleAxis(angleDegrees, axis);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

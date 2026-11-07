using UnityEngine;

namespace TaskBubble.Shared
{
    // A local, immutable view of authoritative state; the network layer owns serialization.
    public readonly struct DelegationSnapshot
    {
        public static DelegationSnapshot Idle => new DelegationSnapshot(
            0, "", DelegationState.Idle, Quaternion.identity, Quaternion.identity, 0f);

        public uint TaskId { get; }
        public string PartId { get; }
        public DelegationState State { get; }
        public Quaternion StartingLocalRotation { get; }
        public Quaternion ProposalLocalRotation { get; }
        public float ProposalAngleDegrees { get; }

        public DelegationSnapshot(
            uint taskId,
            string partId,
            DelegationState state,
            Quaternion startingLocalRotation,
            Quaternion proposalLocalRotation,
            float proposalAngleDegrees)
        {
            TaskId = taskId;
            PartId = partId;
            State = state;
            StartingLocalRotation = startingLocalRotation;
            ProposalLocalRotation = proposalLocalRotation;
            ProposalAngleDegrees = proposalAngleDegrees;
        }
    }
}

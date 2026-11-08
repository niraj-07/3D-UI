using System;
using System.Collections.Generic;
using TaskBubble.Shared;
using Unity.Netcode;
using UnityEngine;

namespace TaskBubble.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class DelegationSession : NetworkBehaviour, IDelegationSession
    {
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private Transform assemblyRoot;

        private readonly EditablePartRegistry parts = new EditablePartRegistry();
        private readonly Dictionary<string, Quaternion> originalRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        // The MVP has one editable part. Keep its committed pose across task cleanup/reconnects.
        private readonly NetworkVariable<CommittedPose> committedPose = new NetworkVariable<CommittedPose>(
            new CommittedPose { PartId = "", Rotation = Quaternion.identity });
        private readonly NetworkVariable<TaskState> task = new NetworkVariable<TaskState>(
            TaskState.From(DelegationSnapshot.Idle));
        private uint lastTaskId;

        public DelegationSnapshot Current { get; private set; } = DelegationSnapshot.Idle;
        public event Action<DelegationSnapshot> StateChanged;
        public event Action<string> OperationRejected;

        public override void OnNetworkSpawn()
        {
            parts.Rebuild(assemblyRoot);
            // Capture once, before applying a saved committed pose. Respawns must not replace the demo pose.
            if (IsServer && parts.IsReady)
            {
                foreach (var part in assemblyRoot.GetComponentsInChildren<EditablePart>(true))
                    if (!originalRotations.ContainsKey(part.PartId) &&
                        part.TryGetProposalLocalRotation(part.transform.localRotation, 0f, out var rotation))
                        originalRotations.Add(part.PartId, rotation);
            }
            committedPose.OnValueChanged += OnCommittedPoseChanged;
            task.OnValueChanged += OnTaskChanged;
            if (connection != null) connection.Changed += OnConnectionChanged;
            ApplyCommittedPose(committedPose.Value);
            Publish(task.Value);
        }

        public override void OnNetworkDespawn()
        {
            task.OnValueChanged -= OnTaskChanged;
            committedPose.OnValueChanged -= OnCommittedPoseChanged;
            if (connection != null) connection.Changed -= OnConnectionChanged;
            // Scene objects can be spawned again after the host restarts.
            if (IsServer) task.Value = TaskState.From(DelegationSnapshot.Idle);
            Current = DelegationSnapshot.Idle;
            StateChanged?.Invoke(Current);
        }

        public void RequestDelegation(string partId)
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before requesting an edit.");
                return;
            }
            RequestDelegationRpc(partId ?? "");
        }

        public void UpdateProposal(float angleDegrees)
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before proposing an edit.");
                return;
            }
            UpdateProposalRpc(Current.TaskId, angleDegrees);
        }

        public void SubmitProposal()
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before submitting an edit.");
                return;
            }
            SubmitProposalRpc(Current.TaskId);
        }

        public void AcceptProposal()
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before accepting an edit.");
                return;
            }
            AcceptProposalRpc(Current.TaskId);
        }

        public void RejectProposal()
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before rejecting an edit.");
                return;
            }
            RejectProposalRpc(Current.TaskId);
        }

        public void ResetTask()
        {
            if (!IsSpawned || connection == null || connection.State != ConnectionState.Connected)
            {
                OperationRejected?.Invoke("Connect before resetting an edit.");
                return;
            }
            ResetTaskRpc(Current.TaskId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ResetTaskRpc(uint taskId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var current = task.Value;
            string reason;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Vr)
                reason = "Only the connected VR participant can reset an edit.";
            else if (taskId != current.TaskId)
                reason = "This reset belongs to an old task.";
            else if (current.State == DelegationState.Idle)
                return;
            else if (!parts.TryGet(current.PartId, out _) || !originalRotations.TryGetValue(current.PartId, out var rotation))
                reason = "The selected part or its original demo rotation is unavailable.";
            else
            {
                var idle = TaskState.From(DelegationSnapshot.Idle);
                idle.ResetPose = new CommittedPose { PartId = current.PartId, Rotation = rotation };
                committedPose.Value = idle.ResetPose;
                task.Value = idle;
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RejectProposalRpc(uint taskId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var current = task.Value;
            string reason;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Vr)
                reason = "Only the connected VR participant can reject an edit.";
            else if (taskId == 0 || taskId != current.TaskId)
                reason = "This rejection belongs to an old or missing task.";
            else if (current.State != DelegationState.Review)
                reason = "Only a submitted proposal can be rejected.";
            else
            {
                // Keep the reviewed snapshot for feedback; leave the committed pose untouched.
                current.State = DelegationState.Rejected;
                task.Value = current;
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AcceptProposalRpc(uint taskId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var current = task.Value;
            string reason;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Vr)
                reason = "Only the connected VR participant can accept an edit.";
            else if (taskId == 0 || taskId != current.TaskId)
                reason = "This approval belongs to an old or missing task.";
            else if (current.State != DelegationState.Review)
                reason = "Only a submitted proposal can be accepted.";
            else if (!parts.TryGet(current.PartId, out var part) ||
                !part.TryGetProposalLocalRotation(current.StartingRotation, current.Angle, out var rotation) ||
                Quaternion.Angle(rotation, current.ProposalRotation) > 0.01f)
                reason = "The part or its rotation settings changed. The reviewed proposal is no longer valid.";
            else
            {
                committedPose.Value = new CommittedPose { PartId = current.PartId, Rotation = current.ProposalRotation };
                current.State = DelegationState.Accepted;
                task.Value = current;
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitProposalRpc(uint taskId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var current = task.Value;
            string reason;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Desktop)
                reason = "Only the connected desktop participant can submit an edit.";
            else if (taskId == 0 || taskId != current.TaskId)
                reason = "This submission belongs to an old or missing task.";
            else if (current.State != DelegationState.Editing && current.State != DelegationState.Rejected)
                reason = "Start editing before submitting a proposal for review.";
            else if (!parts.TryGet(current.PartId, out var part) || !part.IsAngleAllowed(current.Angle))
                reason = "The selected part or proposal is no longer valid.";
            else if (current.State == DelegationState.Rejected && lastTaskId == uint.MaxValue)
                reason = "Task IDs are exhausted. Restart the session.";
            else
            {
                if (current.State == DelegationState.Rejected) current.TaskId = ++lastTaskId;
                current.State = DelegationState.Review;
                task.Value = current;
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void UpdateProposalRpc(uint taskId, float angleDegrees, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var current = task.Value;
            string reason;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Desktop)
                reason = "Only the connected desktop participant can propose an edit.";
            else if (taskId == 0 || taskId != current.TaskId)
                reason = "This proposal belongs to an old or missing task.";
            else if (current.State != DelegationState.Requested && current.State != DelegationState.Editing && current.State != DelegationState.Rejected)
                reason = "This task is not accepting adjustments.";
            else if (!parts.TryGet(current.PartId, out var part))
                reason = "The selected part is missing or has invalid settings.";
            else if (!part.TryGetProposalLocalRotation(current.StartingRotation, angleDegrees, out var rotation))
                reason = "Enter a finite angle within the selected part's rotation limits.";
            else if (current.State == DelegationState.Rejected && lastTaskId == uint.MaxValue)
                reason = "Task IDs are exhausted. Restart the session.";
            else
            {
                // A retry gets a fresh ID so decisions from the previous review cannot apply to it.
                if (current.State == DelegationState.Rejected) current.TaskId = ++lastTaskId;
                current.State = DelegationState.Editing;
                current.ProposalRotation = rotation;
                current.Angle = angleDegrees;
                task.Value = current;
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestDelegationRpc(string partId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            string reason = null;
            if (connection == null || !connection.TryGetRole(sender, out var role) || role != ParticipantRole.Vr)
                reason = "Only the connected VR participant can request an edit.";
            else if (task.Value.State != DelegationState.Idle && task.Value.State != DelegationState.Accepted)
                reason = "An edit is already active.";
            else if (!parts.IsReady)
                reason = parts.Error;
            else if (!parts.TryGet(partId, out var part))
                reason = "The requested part is missing or has invalid settings.";
            else if (!part.TryGetProposalLocalRotation(part.transform.localRotation, 0f, out var rotation))
                reason = "The requested part has an invalid starting rotation.";
            else if (lastTaskId == uint.MaxValue)
                reason = "Task IDs are exhausted. Restart the session.";
            else
            {
                task.Value = TaskState.From(new DelegationSnapshot(
                    ++lastTaskId, partId, DelegationState.Requested, rotation, rotation, 0f));
                return;
            }

            RejectOperationRpc(reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void RejectOperationRpc(string reason, RpcParams rpcParams = default)
        {
            OperationRejected?.Invoke(reason);
        }

        private void OnTaskChanged(TaskState previous, TaskState next) => Publish(next);

        private void OnCommittedPoseChanged(CommittedPose previous, CommittedPose next) => ApplyCommittedPose(next);

        private void ApplyCommittedPose(CommittedPose pose)
        {
            if (string.IsNullOrEmpty(pose.PartId)) return;
            if (!parts.TryGet(pose.PartId, out var part))
            {
                Debug.LogError($"Cannot apply committed rotation: editable part '{pose.PartId}' is missing or invalid.", this);
                return;
            }
            part.transform.localRotation = pose.Rotation;
        }

        private void OnConnectionChanged()
        {
            if (!IsSpawned || !IsServer || connection.HasVrClient && connection.State == ConnectionState.Connected)
                return;

            var state = task.Value.State;
            if (state == DelegationState.Requested || state == DelegationState.Editing || state == DelegationState.Review)
                task.Value = TaskState.From(DelegationSnapshot.Idle);
        }

        private void Publish(TaskState state)
        {
            // Apply before notifying views, regardless of NetworkVariable callback order.
            if (state.State == DelegationState.Accepted)
                ApplyCommittedPose(new CommittedPose { PartId = state.PartId, Rotation = state.ProposalRotation });
            else if (state.State == DelegationState.Idle)
                ApplyCommittedPose(state.ResetPose);
            Current = state.ToSnapshot();
            StateChanged?.Invoke(Current);
        }

        private struct CommittedPose : INetworkSerializable, IEquatable<CommittedPose>
        {
            public string PartId;
            public Quaternion Rotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PartId);
                serializer.SerializeValue(ref Rotation);
            }

            public bool Equals(CommittedPose other) => PartId == other.PartId && Rotation.Equals(other.Rotation);
        }

        private struct TaskState : INetworkSerializable, IEquatable<TaskState>
        {
            public uint TaskId;
            public string PartId;
            public DelegationState State;
            public Quaternion StartingRotation;
            public Quaternion ProposalRotation;
            public float Angle;
            // Reset carries its pose with Idle so views always observe the restored part first.
            public CommittedPose ResetPose;

            public static TaskState From(DelegationSnapshot snapshot) => new TaskState
            {
                TaskId = snapshot.TaskId, PartId = snapshot.PartId, State = snapshot.State,
                StartingRotation = snapshot.StartingLocalRotation,
                ProposalRotation = snapshot.ProposalLocalRotation, Angle = snapshot.ProposalAngleDegrees,
                ResetPose = new CommittedPose { PartId = "", Rotation = Quaternion.identity }
            };

            public DelegationSnapshot ToSnapshot() => new DelegationSnapshot(
                TaskId, PartId ?? "", State, StartingRotation, ProposalRotation, Angle);

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref TaskId);
                serializer.SerializeValue(ref PartId);
                serializer.SerializeValue(ref State);
                serializer.SerializeValue(ref StartingRotation);
                serializer.SerializeValue(ref ProposalRotation);
                serializer.SerializeValue(ref Angle);
                serializer.SerializeValue(ref ResetPose);
            }

            public bool Equals(TaskState other) => TaskId == other.TaskId && PartId == other.PartId &&
                State == other.State && StartingRotation.Equals(other.StartingRotation) &&
                ProposalRotation.Equals(other.ProposalRotation) && Angle.Equals(other.Angle) && ResetPose.Equals(other.ResetPose);
        }
    }
}

using TaskBubble.Networking;
using TaskBubble.Shared;
using TMPro;
using UnityEngine;

namespace TaskBubble.Review
{
    public sealed class VrReviewControls : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private UnityEngine.UI.Button acceptButton;
        [SerializeField] private UnityEngine.UI.Button rejectButton;
        [SerializeField] private UnityEngine.UI.Button resetButton;
        [SerializeField] private TMP_Text feedback;
        private bool awaitingOperation;
        private bool awaitingReset;
        private uint pendingTaskId;

        private void OnEnable()
        {
            if (acceptButton == null || rejectButton == null || resetButton == null)
            {
                Debug.LogError("VrReviewControls needs its Accept, Reject, and Reset buttons.", this);
                enabled = false;
                return;
            }
            acceptButton.onClick.AddListener(Accept);
            rejectButton.onClick.AddListener(Reject);
            resetButton.onClick.AddListener(Reset);
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.OperationRejected += OnRejected;
            }
            if (connection != null) connection.Changed += OnConnectionChanged;
            awaitingOperation = false;
            Refresh();
        }

        private void OnDisable()
        {
            if (acceptButton != null) acceptButton.onClick.RemoveListener(Accept);
            if (rejectButton != null) rejectButton.onClick.RemoveListener(Reject);
            if (resetButton != null) resetButton.onClick.RemoveListener(Reset);
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.OperationRejected -= OnRejected;
            }
            if (connection != null) connection.Changed -= OnConnectionChanged;
            awaitingOperation = false;
        }

        private bool HasVrSession => session != null && session.IsSpawned &&
            connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Vr;
        private bool CanDecide => !awaitingOperation && HasVrSession && session.Current.TaskId != 0 &&
            session.Current.State == DelegationState.Review;
        private bool CanReset => !awaitingOperation && HasVrSession && session.Current.TaskId != 0 &&
            session.Current.State != DelegationState.Idle;

        private void OnStateChanged(DelegationSnapshot snapshot)
        {
            if (snapshot.TaskId != pendingTaskId || (awaitingReset ? snapshot.State == DelegationState.Idle : snapshot.State != DelegationState.Review))
                awaitingOperation = false;
            Refresh();
        }
        private void OnRejected(string reason)
        {
            awaitingOperation = false;
            Refresh();
            if (feedback != null) feedback.text = reason;
        }
        private void OnConnectionChanged()
        {
            if (connection.State != ConnectionState.Connected) awaitingOperation = false;
            Refresh();
        }

        private void Refresh()
        {
            if (acceptButton != null) acceptButton.interactable = CanDecide;
            if (rejectButton != null) rejectButton.interactable = CanDecide;
            if (resetButton != null) resetButton.interactable = CanReset;
            if (feedback == null) return;
            feedback.text = !HasVrSession ? "Disconnected" : awaitingOperation
                ? (awaitingReset ? "Resetting task..." : "Sending decision...") : session.Current.State switch
                {
                    DelegationState.Requested => "Help requested",
                    DelegationState.Editing => "Desktop is editing",
                    DelegationState.Review => "Ready for review",
                    DelegationState.Accepted => "Change accepted",
                    DelegationState.Rejected => "Rejected. Waiting for desktop revision",
                    _ => "No active task"
                };
        }

        private void Accept() => Decide(true);
        private void Reject() => Decide(false);
        private void Decide(bool accept)
        {
            if (!CanDecide) { Refresh(); return; }
            BeginOperation(false);
            if (accept) session.AcceptProposal();
            else session.RejectProposal();
        }

        private void Reset()
        {
            if (!CanReset) { Refresh(); return; }
            BeginOperation(true);
            session.ResetTask();
        }

        private void BeginOperation(bool reset)
        {
            awaitingOperation = true;
            awaitingReset = reset;
            pendingTaskId = session.Current.TaskId;
            Refresh();
        }
    }
}

using TaskBubble.Networking;
using TaskBubble.Shared;
using UnityEngine;

namespace TaskBubble.Review
{
    public sealed class VrReviewControls : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private UnityEngine.UI.Button acceptButton;
        [SerializeField] private UnityEngine.UI.Button rejectButton;
        private bool awaitingDecision;

        private void OnEnable()
        {
            if (acceptButton == null || rejectButton == null)
            {
                Debug.LogError("VrReviewControls needs its Accept and Reject buttons.", this);
                enabled = false;
                return;
            }
            acceptButton.onClick.AddListener(Accept);
            rejectButton.onClick.AddListener(Reject);
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.OperationRejected += OnRejected;
            }
            if (connection != null) connection.Changed += OnConnectionChanged;
            awaitingDecision = false;
            Refresh();
        }

        private void OnDisable()
        {
            if (acceptButton != null) acceptButton.onClick.RemoveListener(Accept);
            if (rejectButton != null) rejectButton.onClick.RemoveListener(Reject);
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.OperationRejected -= OnRejected;
            }
            if (connection != null) connection.Changed -= OnConnectionChanged;
            awaitingDecision = false;
        }

        private bool CanDecide => !awaitingDecision && session != null && session.IsSpawned &&
            connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Vr && session.Current.TaskId != 0 &&
            session.Current.State == DelegationState.Review;

        private void OnStateChanged(DelegationSnapshot snapshot) { awaitingDecision = false; Refresh(); }
        private void OnRejected(string reason) { awaitingDecision = false; Refresh(); }
        private void OnConnectionChanged()
        {
            if (connection.State != ConnectionState.Connected) awaitingDecision = false;
            Refresh();
        }

        private void Refresh()
        {
            if (acceptButton != null) acceptButton.interactable = CanDecide;
            if (rejectButton != null) rejectButton.interactable = CanDecide;
        }

        private void Accept() => Decide(true);
        private void Reject() => Decide(false);
        private void Decide(bool accept)
        {
            if (!CanDecide) { Refresh(); return; }
            awaitingDecision = true;
            Refresh();
            if (accept) session.AcceptProposal();
            else session.RejectProposal();
        }
    }
}

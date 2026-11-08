using TaskBubble.Networking;
using TaskBubble.Shared;
using TMPro;
using UnityEngine;

namespace TaskBubble.Review
{
    public sealed class DesktopReviewControls : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private UnityEngine.UI.Button submitButton;
        [SerializeField] private TMP_Text feedback;

        private void OnEnable()
        {
            if (submitButton == null)
            {
                Debug.LogError("DesktopReviewControls needs its Submit button.", this);
                enabled = false;
                return;
            }
            submitButton.onClick.AddListener(Submit);
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.OperationRejected += OnRejected;
            }
            if (connection != null) connection.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(Submit);
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.OperationRejected -= OnRejected;
            }
            if (connection != null) connection.Changed -= Refresh;
        }

        private bool CanSubmit => session != null && session.IsSpawned &&
            connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Desktop && session.Current.TaskId != 0 &&
            (session.Current.State == DelegationState.Editing || session.Current.State == DelegationState.Rejected);

        private void OnStateChanged(DelegationSnapshot snapshot) => Refresh();
        private void OnRejected(string reason) { Refresh(); if (feedback != null) feedback.text = reason; }

        private void Refresh()
        {
            if (submitButton != null) submitButton.interactable = CanSubmit;
            if (feedback == null) return;
            feedback.text = session == null || !session.IsSpawned || connection == null || connection.State != ConnectionState.Connected
                ? "Disconnected" : session.Current.State switch
                {
                    DelegationState.Requested => "Help requested",
                    DelegationState.Editing => "Ready to submit",
                    DelegationState.Review => "Waiting for VR review",
                    DelegationState.Accepted => "Proposal accepted",
                    DelegationState.Rejected => "Rejected. Adjust and resubmit",
                    _ => "No active task"
                };
        }

        private void Submit()
        {
            if (!CanSubmit) { Refresh(); return; }
            session.SubmitProposal();
        }
    }
}

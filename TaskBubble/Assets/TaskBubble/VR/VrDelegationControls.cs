using TaskBubble.Networking;
using TaskBubble.Shared;
using TMPro;
using UnityEngine;

namespace TaskBubble.VR
{
    public sealed class VrDelegationControls : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private VrPartSelection selection;
        [SerializeField] private UnityEngine.UI.Button delegateButton;
        [SerializeField] private TMP_Text selectedPartLabel;
        [SerializeField] private TMP_Text feedback;
        private bool awaitingRequest;
        private string requestedPartId;
        private string requestedPartName;

        private bool HasVrSession => session != null && session.IsSpawned && connection != null &&
            connection.State == ConnectionState.Connected && connection.LocalRole == ParticipantRole.Vr;
        public bool CanDelegate => HasVrSession && !awaitingRequest && session.Current.State == DelegationState.Idle &&
            selection != null && selection.SelectedPart != null && selection.SelectedPart.isActiveAndEnabled &&
            selection.SelectedPart.IsConfigured;

        private void OnEnable()
        {
            if (delegateButton == null)
            {
                Debug.LogError("VrDelegationControls needs its Delegate button.", this);
                enabled = false;
                return;
            }
            delegateButton.onClick.AddListener(RequestSelectedPart);
            if (selection != null) selection.SelectionChanged += OnSelectionChanged;
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.OperationRejected += OnRejected;
            }
            if (connection != null) connection.Changed += Refresh;
            awaitingRequest = false;
            Refresh();
        }

        private void OnSelectionChanged(EditablePart part) => Refresh();
        private void OnStateChanged(DelegationSnapshot state)
        {
            if (state.State != DelegationState.Idle) awaitingRequest = false;
            Refresh();
        }
        private void OnRejected(string reason)
        {
            if (!awaitingRequest) return;
            awaitingRequest = false;
            Refresh();
            if (feedback != null) feedback.text = reason;
        }

        private void Refresh()
        {
            bool connected = HasVrSession;
            if (!connected) awaitingRequest = false;
            bool idle = connected && session.Current.State == DelegationState.Idle;
            if (selection != null)
            {
                selection.InteractionEnabled = connected;
                // Keep rays usable for world-space review buttons while locking CAD selection.
                selection.PartSelectionEnabled = idle && !awaitingRequest;
            }
            if (delegateButton != null) delegateButton.interactable = CanDelegate;
            if (selectedPartLabel != null)
            {
                string activeName = connected && session.Current.PartId == requestedPartId ? requestedPartName : "Delegated part";
                selectedPartLabel.text = awaitingRequest ? requestedPartName : !idle && connected ? activeName :
                    selection != null && selection.SelectedPart != null ? selection.SelectedPart.name.Replace('_', ' ') : "No part selected";
            }
            if (feedback != null)
                feedback.text = !connected ? "Connect as VR to request help" : awaitingRequest ? "Requesting help..." :
                    idle ? "Select a fingertip, then Delegate" : session.Current.State switch
                    {
                        DelegationState.Requested => "Help requested",
                        DelegationState.Editing => "Desktop is editing",
                        DelegationState.Review => "Proposal ready for review",
                        DelegationState.Accepted => "Accepted. Reset before a new task",
                        DelegationState.Rejected => "Rejected. Desktop can revise and resubmit",
                        _ => "Task in progress"
                    };
        }

        public void RequestSelectedPart()
        {
            if (!CanDelegate) { Refresh(); return; }
            requestedPartId = selection.SelectedPart.PartId;
            requestedPartName = selection.SelectedPart.name.Replace('_', ' ');
            awaitingRequest = true;
            Refresh();
            session.RequestDelegation(requestedPartId);
        }

        private void OnDisable()
        {
            if (delegateButton != null)
            {
                delegateButton.onClick.RemoveListener(RequestSelectedPart);
                delegateButton.interactable = false;
            }
            if (selection != null)
            {
                selection.SelectionChanged -= OnSelectionChanged;
                selection.InteractionEnabled = false;
            }
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.OperationRejected -= OnRejected;
            }
            if (connection != null) connection.Changed -= Refresh;
            awaitingRequest = false;
        }
    }
}

using TaskBubble.Networking;
using TaskBubble.Shared;
using UnityEngine;

namespace TaskBubble.Review
{
    public sealed class DesktopReviewControls : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private UnityEngine.UI.Button submitButton;

        private void OnEnable()
        {
            if (submitButton == null)
            {
                Debug.LogError("DesktopReviewControls needs its Submit button.", this);
                enabled = false;
                return;
            }
            submitButton.onClick.AddListener(Submit);
            if (session != null) session.StateChanged += OnStateChanged;
            if (connection != null) connection.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(Submit);
            if (session != null) session.StateChanged -= OnStateChanged;
            if (connection != null) connection.Changed -= Refresh;
        }

        private bool CanSubmit => session != null && session.IsSpawned &&
            connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Desktop && session.Current.TaskId != 0 &&
            session.Current.State == DelegationState.Editing;

        private void OnStateChanged(DelegationSnapshot snapshot) => Refresh();

        private void Refresh()
        {
            if (submitButton != null) submitButton.interactable = CanSubmit;
        }

        private void Submit()
        {
            if (!CanSubmit) { Refresh(); return; }
            session.SubmitProposal();
        }
    }
}

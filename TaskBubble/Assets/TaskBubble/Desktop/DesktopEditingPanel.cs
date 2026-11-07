using System.Globalization;
using TaskBubble.Networking;
using TaskBubble.Shared;
using TMPro;
using UnityEngine;

namespace TaskBubble.Desktop
{
    public sealed class DesktopEditingPanel : MonoBehaviour
    {
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private EditablePart editablePart;
        [SerializeField] private UnityEngine.UI.Slider rotationSlider;
        [SerializeField] private TMP_Text taskStatus;
        [SerializeField] private TMP_Text partName;
        [SerializeField] private TMP_Text rotationAxis;
        [SerializeField] private TMP_Text angleValue;
        [SerializeField] private TMP_Text minimumAngle;
        [SerializeField] private TMP_Text maximumAngle;
        [SerializeField] private DesktopPartPreview preview;

        private bool refreshing;
        private uint startedTaskId;

        private void OnEnable()
        {
            if (rotationSlider == null || taskStatus == null || partName == null || rotationAxis == null ||
                angleValue == null || minimumAngle == null || maximumAngle == null)
            {
                Debug.LogError("DesktopEditingPanel has missing UI references.", this);
                enabled = false;
                return;
            }
            rotationSlider.onValueChanged.AddListener(OnSliderChanged);
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.OperationRejected += OnRejected;
            }
            if (connection != null) connection.Changed += OnConnectionChanged;
            OnConnectionChanged();
        }

        private void OnDisable()
        {
            if (rotationSlider != null) rotationSlider.onValueChanged.RemoveListener(OnSliderChanged);
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.OperationRejected -= OnRejected;
            }
            if (connection != null) connection.Changed -= OnConnectionChanged;
        }

        private void OnConnectionChanged() => OnStateChanged(session != null ? session.Current : DelegationSnapshot.Idle);

        private bool HasPart(DelegationSnapshot snapshot) => editablePart != null && editablePart.IsConfigured &&
            snapshot.TaskId != 0 && editablePart.PartId == snapshot.PartId;

        private bool CanEdit(DelegationSnapshot snapshot) => session != null && session.IsSpawned &&
            connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Desktop && HasPart(snapshot) &&
            (snapshot.State == DelegationState.Requested || snapshot.State == DelegationState.Editing);

        private void OnStateChanged(DelegationSnapshot snapshot)
        {
            Refresh(snapshot);
            if (CanEdit(snapshot) && snapshot.State == DelegationState.Requested && startedTaskId != snapshot.TaskId)
            {
                startedTaskId = snapshot.TaskId;
                session.UpdateProposal(0f);
            }
        }

        private void Refresh(DelegationSnapshot snapshot)
        {
            if (rotationSlider == null || taskStatus == null) return;
            bool hasPart = HasPart(snapshot);
            refreshing = true;
            try
            {
                rotationSlider.interactable = CanEdit(snapshot);
                if (hasPart)
                {
                    rotationSlider.minValue = editablePart.MinimumAngleDegrees;
                    rotationSlider.maxValue = editablePart.MaximumAngleDegrees;
                }
                rotationSlider.SetValueWithoutNotify(hasPart ? snapshot.ProposalAngleDegrees : 0f);
                taskStatus.text = session == null || connection == null || connection.State != ConnectionState.Connected
                    ? "Disconnected" : snapshot.State.ToString();
                if (snapshot.TaskId != 0 && !hasPart) taskStatus.text = "Selected part is unavailable";
                partName.text = hasPart ? editablePart.name : "No part selected";
                rotationAxis.text = hasPart ? "LOCAL " + editablePart.RotationAxis + " ROTATION" : "ROTATION";
                angleValue.text = Degrees(hasPart ? snapshot.ProposalAngleDegrees : 0f);
                minimumAngle.text = hasPart ? Degrees(editablePart.MinimumAngleDegrees) : "";
                maximumAngle.text = hasPart ? Degrees(editablePart.MaximumAngleDegrees) : "";
                if (preview != null)
                {
                    if (hasPart && session != null && session.IsSpawned && connection != null &&
                        connection.State == ConnectionState.Connected && connection.LocalRole == ParticipantRole.Desktop)
                        preview.Show(editablePart, snapshot);
                    else
                        preview.Clear();
                }
            }
            finally { refreshing = false; }
        }

        private void OnSliderChanged(float angle)
        {
            if (refreshing) return;
            var snapshot = session != null ? session.Current : DelegationSnapshot.Idle;
            if (!CanEdit(snapshot))
            {
                Refresh(snapshot);
                return;
            }
            session.UpdateProposal(angle);
        }

        private void OnRejected(string reason)
        {
            Refresh(session.Current);
            taskStatus.text = reason;
        }

        private static string Degrees(float angle) => angle.ToString("0.0", CultureInfo.InvariantCulture) + "\u00b0";
    }
}

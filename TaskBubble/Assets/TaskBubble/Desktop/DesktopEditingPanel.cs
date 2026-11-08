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
        [SerializeField] private Transform assemblyRoot;
        private readonly EditablePartRegistry parts = new EditablePartRegistry();
        [SerializeField] private UnityEngine.UI.Slider rotationSlider;
        [SerializeField] private TMP_InputField angleInput;
        [SerializeField] private TMP_Text taskStatus;
        [SerializeField] private TMP_Text partName;
        [SerializeField] private TMP_Text rotationAxis;
        [SerializeField] private TMP_Text angleValue;
        [SerializeField] private TMP_Text minimumAngle;
        [SerializeField] private TMP_Text maximumAngle;
        [SerializeField] private DesktopPartPreview preview;

        private bool refreshing;
        private uint startedTaskId;
        private uint angleEditingTaskId;

        private void OnEnable()
        {
            if (assemblyRoot != null) parts.Rebuild(assemblyRoot);
            if (rotationSlider == null || taskStatus == null || partName == null || rotationAxis == null ||
                angleValue == null || minimumAngle == null || maximumAngle == null)
            {
                Debug.LogError("DesktopEditingPanel has missing UI references.", this);
                enabled = false;
                return;
            }
            rotationSlider.onValueChanged.AddListener(OnSliderChanged);
            if (angleInput != null)
            {
                angleInput.onSelect.AddListener(OnAngleSelected);
                angleInput.onEndEdit.AddListener(OnAngleEntered);
            }
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
            if (angleInput != null)
            {
                angleInput.onSelect.RemoveListener(OnAngleSelected);
                angleInput.onEndEdit.RemoveListener(OnAngleEntered);
            }
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
            (snapshot.State == DelegationState.Requested || snapshot.State == DelegationState.Editing || snapshot.State == DelegationState.Rejected);

        private void OnStateChanged(DelegationSnapshot snapshot)
        {
            if (assemblyRoot != null) parts.TryGet(snapshot.PartId, out editablePart);
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
                if (angleInput != null)
                {
                    angleInput.interactable = CanEdit(snapshot);
                    // Preserve unfinished typing only while it still belongs to this editable task.
                    if (!angleInput.isFocused || !angleInput.interactable || angleEditingTaskId != snapshot.TaskId)
                        angleInput.SetTextWithoutNotify(hasPart ? AngleText(snapshot.ProposalAngleDegrees) : "");
                    if (!angleInput.isFocused) angleEditingTaskId = snapshot.TaskId;
                }
                taskStatus.text = session == null || connection == null || connection.State != ConnectionState.Connected
                    ? "Disconnected" : snapshot.State.ToString();
                if (snapshot.TaskId != 0 && !hasPart) taskStatus.text = "Selected part is unavailable";
                partName.text = hasPart ? editablePart.name.Replace('_', ' ') : "No part selected";
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

        private void OnAngleSelected(string value) => angleEditingTaskId = session != null ? session.Current.TaskId : 0;

        private void OnAngleEntered(string value)
        {
            if (refreshing || !isActiveAndEnabled) return;
            var snapshot = session != null ? session.Current : DelegationSnapshot.Idle;
            if (!CanEdit(snapshot) || angleEditingTaskId != snapshot.TaskId || angleInput == null || angleInput.wasCanceled)
            {
                Refresh(snapshot);
                return;
            }
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float angle) ||
                !editablePart.IsAngleAllowed(angle))
            {
                Refresh(snapshot);
                angleInput.SetTextWithoutNotify(AngleText(snapshot.ProposalAngleDegrees));
                taskStatus.text = "Enter an angle from " + Degrees(editablePart.MinimumAngleDegrees) + " to " + Degrees(editablePart.MaximumAngleDegrees) + ".";
                return;
            }
            // Enter followed by focus loss, or unchanged text, must not send the same proposal twice.
            if (angle.Equals(snapshot.ProposalAngleDegrees))
            {
                Refresh(snapshot);
                angleInput.SetTextWithoutNotify(AngleText(angle));
                return;
            }
            session.UpdateProposal(angle);
        }

        private static string AngleText(float angle) => angle.ToString("G", CultureInfo.InvariantCulture);

        private static string Degrees(float angle) => angle.ToString("0.0", CultureInfo.InvariantCulture) + "\u00b0";
    }
}

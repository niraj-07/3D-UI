using System;
using System.Collections.Generic;
using TaskBubble.Shared;
using UnityEngine;

namespace TaskBubble.Networking
{
    // Owned by the delegation session. Registration does not move objects or start networking.
    public sealed class EditablePartRegistry
    {
        private readonly Dictionary<string, EditablePart> parts =
            new Dictionary<string, EditablePart>(StringComparer.Ordinal);
        private Transform assemblyRoot;

        public bool IsReady => assemblyRoot != null && string.IsNullOrEmpty(Error);
        public string Error { get; private set; } = "Editable parts have not been registered.";

        public bool Rebuild(Transform root)
        {
            parts.Clear();
            assemblyRoot = null;
            Error = "";

            if (root == null)
                return Fail("Assign the CAD assembly root before registering editable parts.");

            // Include inactive parts so hidden duplicates cannot become ambiguous when enabled.
            var candidates = root.GetComponentsInChildren<EditablePart>(true);
            if (candidates.Length == 0)
                return Fail("The assembly has no EditablePart components.");

            foreach (var part in candidates)
            {
                string id = part.PartId;
                if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                    return Fail($"Editable part '{part.name}' needs a nonempty ID without surrounding spaces.");

                if (parts.ContainsKey(id))
                    return Fail($"Duplicate editable part ID '{id}'. Give each part a unique ID.");

                if (!part.IsConfigured)
                    return Fail($"Editable part '{id}' has invalid rotation settings. Choose X, Y, or Z and finite limits that include zero.");

                parts.Add(id, part);
            }

            assemblyRoot = root;
            return true;
        }

        public bool TryGet(string partId, out EditablePart part)
        {
            part = null;
            if (!IsReady || string.IsNullOrEmpty(partId) || !parts.TryGetValue(partId, out var candidate))
                return false;

            // Do not return a stale entry after a part is removed, renamed by ID, or misconfigured.
            if (candidate == null || !candidate.IsConfigured || candidate.PartId != partId ||
                !candidate.transform.IsChildOf(assemblyRoot))
                return false;

            part = candidate;
            return true;
        }

        private bool Fail(string reason)
        {
            parts.Clear();
            assemblyRoot = null;
            Error = reason;
            return false;
        }
    }
}

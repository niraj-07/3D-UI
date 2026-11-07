using System;

namespace TaskBubble.Shared
{
    public interface IDelegationSession
    {
        DelegationSnapshot Current { get; }

        // Raised on each client after authoritative state arrives, including live proposals.
        event Action<DelegationSnapshot> StateChanged;

        // Raised only for the caller whose command was refused; Current stays unchanged.
        event Action<string> OperationRejected;

        // Commands are asynchronous requests. Observe events for the result.
        void RequestDelegation(string partId);
        void UpdateProposal(float angleDegrees);
        void SubmitProposal();
        void AcceptProposal();
        void RejectProposal();
        void ResetTask();
    }
}

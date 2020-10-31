using System;
namespace state_machine.TranscriptWorkflow
{
    public enum State
    {
        WaitingToAccept,
        EditorPool,
        Editor,
        EditorReview,
        End
    }
}

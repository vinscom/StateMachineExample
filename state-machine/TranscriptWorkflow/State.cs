using System;
namespace JustTranscript.TranscriptWorkflow
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

using System;
namespace Foo.TranscriptWorkflow
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

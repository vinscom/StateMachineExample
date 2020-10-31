using System;
namespace JustTranscript.TranscriptWorkflow
{
    public class WorkflowData
    {
        public string EditorId { get; set; }
        public string OwnerId { get; set; }
        public string TranscriptId { get; set; }
        public State CurrentState { get; set; }

        public WorkflowData Clone() => (WorkflowData)MemberwiseClone();
    }
}

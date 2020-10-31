using System.Linq;
using System.Collections.Generic;
using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace JustTranscript.TranscriptWorkflow
{
    [BsonIgnoreExtraElements]
    public class Workflow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public List<WorkflowData> Steps { get; set; }
        public WorkflowData Data { get; set; }
        public bool Open { get; set; } = true;

        public Workflow()
        {
            Steps = new List<WorkflowData>();
        }

        public Workflow(string ownerid, string transcriptid):this()
        {
            OwnerId = ownerid;

            Data = new WorkflowData();
            Data.OwnerId = ownerid;
            Data.TranscriptId = transcriptid;
            Data.CurrentState = State.WaitingToAccept;

            Steps.Add(Data.Clone());
        }

        public State[] NextStates(Principal principal)
        {
            return StateTransition.NextState(Data.CurrentState, Data, principal);
        }

        public State Transition(State nextState, Principal principal)
        {
            State[] nextStates = StateTransition.NextState(Data.CurrentState, Data, principal);

            if (!nextStates.Contains(nextState))
            {
                throw new ArgumentException("State transition not allowed");
            }

            switch (nextState)
            {
                case State.Editor:
                    Data.EditorId = principal.Id;
                    break;
                case State.EditorPool:
                    Data.EditorId = null;
                    break;
            }

            Data = Data.Clone();
            Data.CurrentState = nextState;
            Steps.Add(Data);

            if (nextState == State.End)
            {
                Open = false;
            }

            return nextState;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustTranscript.Repository;
using MongoDB.Driver;

namespace JustTranscript.TranscriptWorkflow
{
    public class WorkflowManager
    {
        private ModelContext _modelContext;

        public WorkflowManager(ModelContext modelContext)
        {
            _modelContext = modelContext;
        }

        public async Task<List<WorkflowData>> FindTasksAsync(State state)
        {
            List<WorkflowData> results = new List<WorkflowData>();

            using (IAsyncCursor<Workflow> asyncCursor = await _modelContext.Workflow.FindAsync(item => item.Open == true && item.Data.CurrentState == state))
            {
                while (await asyncCursor.MoveNextAsync())
                {
                    foreach (Workflow document in asyncCursor.Current)
                    {
                        results.Add(document.Data.Clone());
                    }
                }
            }

            return results;
        }

        public async Task<List<WorkflowData>> FindOwnerTasksAsync(Principal principal, State state)
        {
            List<WorkflowData> results = new List<WorkflowData>();

            using (IAsyncCursor<Workflow> asyncCursor = await _modelContext.Workflow.FindAsync(item => item.OwnerId == principal.Id && item.Open == true && item.Data.CurrentState == state))
            {
                while (await asyncCursor.MoveNextAsync())
                {
                    foreach (Workflow document in asyncCursor.Current)
                    {
                        results.Add(document.Data.Clone());
                    }
                }
            }

            return results;
        }

        public async Task<string> CreateWorkflowAsync(string ownerid, string transcriptid)
        {
            Workflow existingWorkflow = await FindWorkflowForTranscriptAsync(transcriptid);

            if (existingWorkflow != null)
            {
                throw new ArgumentException("Existing workflow is pending");
            }

            Workflow w = new Workflow(ownerid, transcriptid);

            await _modelContext.Workflow.InsertOneAsync(w);
            return w.Id;
        }

        public async Task<State> CurrentWorkflowStepAsync(string workflowid, Principal principal)
        {
            Workflow w = await FindWorkflowAsync(workflowid);
            return w.Data.CurrentState;
        }

        public async Task<State[]> NextWorkflowStepsAsync(string workflowid, Principal principal)
        {
            Workflow w = await FindWorkflowAsync(workflowid);
            if (w == null || !w.Open)
            {
                return new State[] { };
            }
            return w.NextStates(principal);
        }

        protected async Task<Workflow> FindWorkflowAsync(string workflowid)
        {
            using (IAsyncCursor<Workflow> asyncCursor = await _modelContext.Workflow.FindAsync(item => item.Id == workflowid))
            {
                while (await asyncCursor.MoveNextAsync())
                {
                    foreach (Workflow document in asyncCursor.Current)
                    {
                        return document;
                    }
                }
            }

            return null;
        }

        public async Task<Workflow> FindWorkflowForTranscriptAsync(string transcriptid)
        {
            using (IAsyncCursor<Workflow> asyncCursor = await _modelContext.Workflow.FindAsync(item => item.Open == true && item.Data.TranscriptId == transcriptid))
            {
                while (await asyncCursor.MoveNextAsync())
                {
                    foreach (Workflow document in asyncCursor.Current)
                    {
                        return document;
                    }
                }
            }

            return null;
        }

        public async Task<WorkflowData> FindWorkflowDataAsync(string workflowid)
        {
            Workflow w = await FindWorkflowAsync(workflowid);
            return w.Data.Clone();
        }

        public async Task<State> TransitionAsync(string workflowid, State nextState, Principal principal)
        {
            Workflow w = await FindWorkflowAsync(workflowid);
            w.Transition(nextState, principal);
            await _modelContext.Workflow.ReplaceOneAsync(item => item.Id == workflowid, w);
            return nextState;
        }

    }
}

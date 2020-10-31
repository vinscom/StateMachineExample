using Mongo2Go;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using JustTranscript.Repository;
using JustTranscript.TranscriptWorkflow;
using System;
using State = JustTranscript.TranscriptWorkflow.State;

namespace state_machine_test.TranscriptWorkflow
{
    [TestFixture]
    public class WorkflowManagerTest
    {
        private MongoDbRunner _runner;
        private ModelContext _modelContext;
        private WorkflowManager _workflowManager;
        private Principal owner;
        private Principal otherUser;
        private Principal editor;
        private Principal adminEditor;
        private Principal admin;

        [OneTimeSetUp]
        public void Setup()
        {
            owner = new Principal();
            owner.Id = "owner-id";
            owner.Role = new List<Role>();

            otherUser = new Principal();
            otherUser.Id = "other-user-id";
            otherUser.Role = new List<Role>();

            editor = new Principal();
            editor.Id = "editor-id";
            editor.Role = new List<Role> { Role.Editor };

            adminEditor = new Principal();
            adminEditor.Id = "admin-editor-id";
            adminEditor.Role = new List<Role>() { Role.AdminEditor };

            admin = new Principal();
            admin.Id = "admin-id";
            admin.Role = new List<Role>() { Role.Admin };

            _runner = MongoDbRunner.Start();
            _modelContext = ModelContext.Create(_runner.ConnectionString);
            _modelContext.TestConnection();
            _workflowManager = new WorkflowManager(_modelContext);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _runner.Dispose();
        }

        [Test]
        public async Task CreateWorkflow()
        {
            string wid = await _workflowManager.CreateWorkflowAsync("owner-id", "transcript-id");
            WorkflowData data = await _workflowManager.FindWorkflowDataAsync(wid);
            Assert.That(data.OwnerId, Is.EqualTo("owner-id"));
            Assert.That(data.TranscriptId, Is.EqualTo("transcript-id"));
        }

        [Test, Description("No workflow is present for transcript")]
        public async Task NoWorkflowAsync()
        {
            Principal p = new Principal();
            p.Id = "user-id";
            p.Role = new List<Role>();
            var states = await _workflowManager.NextWorkflowStepsAsync("5f9bda38716e7a7f7e4c3891", p);
            Assert.That(states, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Initate 'Mark for Review' process")]
        public async Task MarkForReviewStartAsync()
        {

            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "MarkForReviewStartAsync");
            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.WaitingToAccept));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { State.End }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.WaitingToAccept));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.WaitingToAccept));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.WaitingToAccept));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { State.EditorPool, State.End }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.WaitingToAccept));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Cancel 'Mark for Review' process")]
        public async Task MarkForReviewCancelAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "MarkForReviewCancelAsync");
            await _workflowManager.TransitionAsync(workflowid, State.End, owner);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.End));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.End));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.End));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.End));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.End));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Accept to Editor Pool")]
        public async Task AcceptMarkForReviewAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "AcceptMarkForReviewAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.EditorPool));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.EditorPool));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.EditorPool));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { State.Editor }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.EditorPool));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.EditorPool));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Accept for editing")]
        public async Task EditorAccpetAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "EditorAccpetAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);
            await _workflowManager.TransitionAsync(workflowid, State.Editor, editor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.Editor));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.Editor));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.Editor));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { State.EditorReview, State.EditorPool }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.Editor));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { State.EditorPool }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.Editor));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Editor releasing transcript")]
        public async Task EditorReleaseTranscriptAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "EditorReleaseTranscriptAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);
            await _workflowManager.TransitionAsync(workflowid, State.Editor, editor);
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, editor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.EditorPool));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.EditorPool));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.EditorPool));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { State.Editor }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.EditorPool));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.EditorPool));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Mark for Editor Review")]
        public async Task MarkForEditorReviewAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "MarkForEditorReviewAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);
            await _workflowManager.TransitionAsync(workflowid, State.Editor, editor);
            await _workflowManager.TransitionAsync(workflowid, State.EditorReview, editor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.EditorReview));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.EditorReview));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.EditorReview));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.EditorReview));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { State.EditorPool, State.End }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.EditorReview));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Editor Review Rejected")]
        public async Task EditorReviewRejectedAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "EditorReviewRejectedAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);
            await _workflowManager.TransitionAsync(workflowid, State.Editor, editor);
            await _workflowManager.TransitionAsync(workflowid, State.EditorReview, editor);
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.EditorPool));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.EditorPool));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.EditorPool));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { State.Editor }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.EditorPool));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.EditorPool));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Editor Review Accepted")]
        public async Task EditorReviewAcceptedAsync()
        {
            string workflowid = await _workflowManager.CreateWorkflowAsync(owner.Id, "EditorReviewAcceptedAsync");
            await _workflowManager.TransitionAsync(workflowid, State.EditorPool, adminEditor);
            await _workflowManager.TransitionAsync(workflowid, State.Editor, editor);
            await _workflowManager.TransitionAsync(workflowid, State.EditorReview, editor);
            await _workflowManager.TransitionAsync(workflowid, State.End, adminEditor);

            State ownerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, owner);
            Assert.That(ownerState, Is.EqualTo(State.End));
            State[] nextOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, owner);
            Assert.That(nextOwnerState, Is.EquivalentTo(new State[] { }));

            State otherOwnerState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, otherUser);
            Assert.That(otherOwnerState, Is.EqualTo(State.End));
            State[] nextOtherOwnerState = await _workflowManager.NextWorkflowStepsAsync(workflowid, otherUser);
            Assert.That(nextOtherOwnerState, Is.EquivalentTo(new State[] { }));

            State editorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, editor);
            Assert.That(editorState, Is.EqualTo(State.End));
            State[] nextEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, editor);
            Assert.That(nextEditorState, Is.EquivalentTo(new State[] { }));

            State adminEditorState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, adminEditor);
            Assert.That(adminEditorState, Is.EqualTo(State.End));
            State[] nextAdminEditorState = await _workflowManager.NextWorkflowStepsAsync(workflowid, adminEditor);
            Assert.That(nextAdminEditorState, Is.EquivalentTo(new State[] { }));

            State adminState = await _workflowManager.CurrentWorkflowStepAsync(workflowid, admin);
            Assert.That(adminState, Is.EqualTo(State.End));
            State[] nextAdminState = await _workflowManager.NextWorkflowStepsAsync(workflowid, admin);
            Assert.That(nextAdminState, Is.EquivalentTo(new State[] { }));
        }

        [Test, Description("Only one flow can remain active for a tanscript")]
        public async Task OnlyAllowSingleWorkflowPerTranscriptAsync()
        {
            await _workflowManager.CreateWorkflowAsync(owner.Id, "OnlyAllowSingleWorkflowPerTranscriptAsync");
            Assert.ThrowsAsync<ArgumentException>(async () => await _workflowManager.CreateWorkflowAsync(owner.Id, "OnlyAllowSingleWorkflowPerTranscriptAsync"));
        }

        [Test, Description("Find waiting task with given status")]
        public async Task FindTasksAsync()
        {
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindTasksAsync1");
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindTasksAsync2");
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindTasksAsync3");

            List<WorkflowData> tasks = await _workflowManager.FindTasksAsync(State.WaitingToAccept);

            Assert.That(tasks.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test, Description("Find owner task")]
        public async Task FindOwnerTasksAsync()
        {
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindOwnerTasksAsync1");
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindOwnerTasksAsync2");
            await _workflowManager.CreateWorkflowAsync(owner.Id, "FindOwnerTasksAsync3");

            List<WorkflowData> tasks = await _workflowManager.FindOwnerTasksAsync(owner, State.WaitingToAccept);

            Assert.That(tasks.Count, Is.GreaterThanOrEqualTo(3));
        }
    }
}

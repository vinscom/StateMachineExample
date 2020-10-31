namespace Foo.TranscriptWorkflow
{
    public class StateTransition
    {
        public static State[] NextState(State currentState, WorkflowData data, Principal principal)
        {
            switch (currentState)
            {
                case State.WaitingToAccept:
                    return WaitingToAccept(data, principal);
                case State.EditorPool:
                    return EditorPool(data, principal);
                case State.Editor:
                    return Editor(data, principal);
                case State.EditorReview:
                    return EditorReview(data, principal);
                case State.End:
                    return End(data, principal);
            }

            return new State[0];
        }

        private static State[] WaitingToAccept(WorkflowData data, Principal principal)
        {
            if (data.OwnerId != null && data.OwnerId.Equals(principal.Id))
            {
                return new State[] { State.End };
            }

            if (principal.Role.Contains(Role.AdminEditor))
            {
                return new State[] { State.EditorPool, State.End };
            }

            return new State[0];
        }

        private static State[] EditorPool(WorkflowData data, Principal principal)
        {
            if (principal.Role.Contains(Role.Editor))
            {
                return new State[] { State.Editor };
            }

            return new State[0];
        }

        private static State[] Editor(WorkflowData data, Principal principal)
        {
            if (data.EditorId != null && data.EditorId.Equals(principal.Id))
            {
                return new State[] { State.EditorReview, State.EditorPool };
            }

            if (principal.Role.Contains(Role.AdminEditor))
            {
                return new State[] { State.EditorPool };
            }

            return new State[0];
        }

        private static State[] EditorReview(WorkflowData data, Principal principal)
        {
            if (principal.Role.Contains(Role.AdminEditor))
            {
                return new State[] { State.EditorPool, State.End };
            }

            return new State[0];
        }

        private static State[] End(WorkflowData data, Principal principal)
        {
            return new State[] { State.End };
        }
    }
}

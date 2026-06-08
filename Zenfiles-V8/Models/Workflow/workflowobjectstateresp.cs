namespace Zenfiles.Models.Workflow
{
    public class workflowobjectstateresp
    {
        public string? WorkflowTitle { get; set; }
        public int WorkflowId { get; set; }
        public int CurrentStateid { get; set; }
        public string? CurrentStateTitle { get; set; }
        public string? Assignmentdesc { get; set; }
        public List<currentState>? NextStates { get; set; }
    }
    public class currentState
    {
        public int id { get; set; }
        public string? Title { get; set; }
    }
}

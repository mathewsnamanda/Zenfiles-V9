namespace Zenfiles.Models.Workflow
{
    public class stateWorkflows
    {
        public int WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public int ClassId { get; set; }
        public List<stateStates>? States { get; set; }
    }
    public class stateStates
    {
        public int StateId{ get; set; }
        public string? StateName { get; set; }
        public bool IsSelectable { get; set; }
    }
}

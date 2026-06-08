namespace Testing_Logs.models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string RenderedMessage { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
    }
}

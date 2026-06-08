namespace testing_scheduler_with_signalir.schedule
{
    public class ScheduledJob
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Track when job was
    }
}

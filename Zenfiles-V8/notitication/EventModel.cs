namespace testing_scheduler_with_signalir.notitication
{
    public class EventModel
    {
        public int UserId { get; set; }
        public string? Role { get; set; }   // e.g., "Admin", "Manager", "User"
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

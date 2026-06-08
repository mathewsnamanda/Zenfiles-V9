using System.Text.Json;

namespace testing_scheduler_with_signalir.schedule
{
    public static class FileStorage
    {
        private static readonly string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(),"Files","taskitems.json");
        
        public static List<ScheduledJob> Load()
        {
            if (!File.Exists(filePath))
                return new List<ScheduledJob>();

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<ScheduledJob>>(json) ?? new List<ScheduledJob>();
        }

        public static void Save(List<ScheduledJob> items)
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public static void CleanupExpiredEntries(TimeSpan maxAge)
        {
            var items = Load();
            var cutoff = DateTime.Now - maxAge;

            // keep only items newer than cutoff
            items = items.Where(i => i.CreatedAt >= cutoff).ToList();

            Save(items);
        }
    }

}

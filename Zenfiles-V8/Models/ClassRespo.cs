namespace ZenFiles.Models
{
    public class ClassRespo
    {
        public int ObjectId { get; set; }
        public List<ClassGroupp>? Grouped { get; set; }
        public List<ObjectClassp>? UnGrouped { get; set; }
    }
}
